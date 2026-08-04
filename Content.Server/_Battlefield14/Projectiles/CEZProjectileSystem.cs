using System.Numerics;
using Content.Shared._Battlefield14.Projectiles.Components;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._Battlefield14.Projectiles;

public sealed class CEZProjectileSystem : EntitySystem
{
    public const float MinZShotDistance = 4f;

    private const float MaxTransitionLateralOffset = 1.5f;

    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ITileDefinitionManager _tileDef = default!;
    [Dependency] private readonly CESharedZLevelsSystem _zLevels = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesBefore.Add(typeof(SharedPhysicsSystem));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CEZProjectileComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var zProj, out var xform))
        {
            if (zProj.Transitioned || zProj.ZOffset == 0)
                continue;

            if (!ShouldTransition(zProj, _transform.GetWorldPosition(xform)))
                continue;

            // Preserve world position and velocity while hopping to the adjacent z-level map.
            if (_zLevels.TryMove(uid, zProj.ZOffset))
            {
                zProj.Transitioned = true;
                Dirty(uid, zProj);
            }
        }
    }

    public bool TryGetZShot(EntityUid? user, EntityUid? target, MapCoordinates from, Vector2 to, out int zOffset, out Vector2 transitionPoint)
    {
        zOffset = 0;
        transitionPoint = default;

        if (user is not { } shooter)
            return false;

        var shooterMapUid = Transform(shooter).MapUid;
        if (shooterMapUid is null || !_zLevels.TryGetMapNetwork(shooterMapUid.Value, out _))
            return false;

        // Aim at the actual target when one is hovered, otherwise fall back to the raw aim point.
        var aimPoint = target is { Valid: true } targetUid && !TerminatingOrDeleted(targetUid)
            ? _transform.GetWorldPosition(targetUid)
            : to;

        // Target must be far enough away horizontally for the shot to make sense between levels.
        if (Vector2.Distance(from.Position, aimPoint) < MinZShotDistance)
            return false;

        // Looking up means the shot is aimed at the level above, otherwise it is aimed at the
        // current level (levels below are always visible).
        var lookUp = TryComp<CEZLevelViewerComponent>(shooter, out var viewer) && viewer.LookUp;

        if (lookUp)
        {
            if (!_zLevels.TryMapUp(shooterMapUid.Value, out var aboveMap))
                return false;

            // The opening is in the floor of the level above (the ceiling above the shooter).
            // The bullet rises through the first hole along the line of fire.
            if (!TryFindOpeningPoint(aboveMap.Owner, from.Position, aimPoint, out var openingPoint))
                return false;

            zOffset = 1;
            transitionPoint = openingPoint;
            return true;
        }

        // Downward shots only trigger when deliberately aiming into an opening in the current
        // floor, otherwise ordinary shots would randomly drop a level whenever the line of fire
        // happens to cross a floor gap.
        if (!_zLevels.TryMapDown(shooterMapUid.Value, out _))
            return false;

        if (!IsOpeningAt(shooterMapUid.Value, aimPoint))
            return false;

        if (!TryFindOpeningPoint(shooterMapUid.Value, from.Position, aimPoint, out var downOpeningPoint))
            return false;

        zOffset = -1;
        transitionPoint = downOpeningPoint;
        return true;
    }

    private static bool ShouldTransition(CEZProjectileComponent comp, Vector2 currentPos)
    {
        var toTransition = comp.TransitionPoint - comp.SpawnPoint;
        var sqLen = toTransition.LengthSquared();
        if (sqLen <= 0f)
            return false;

        var displacement = currentPos - comp.SpawnPoint;

        // The projectile must have reached (or passed) the opening along the line of fire.
        if (Vector2.Dot(displacement, toTransition) < sqLen)
            return false;

        // ... and must actually be near the line (spread pellets that miss the hole stay behind).
        var cross = displacement.X * toTransition.Y - displacement.Y * toTransition.X;
        var lateral = MathF.Abs(cross) / MathF.Sqrt(sqLen);
        return lateral <= MaxTransitionLateralOffset;
    }

    private bool TryFindOpeningPoint(EntityUid mapUid, Vector2 from, Vector2 to, out Vector2 openingPoint)
    {
        openingPoint = default;

        if (!_mapManager.TryFindGridAt(mapUid, to, out var gridUid, out var grid) &&
            !_mapManager.TryFindGridAt(mapUid, from, out gridUid, out grid))
            return false;

        var startTile = _map.WorldToTile(gridUid, grid, from);
        var endTile = _map.WorldToTile(gridUid, grid, to);

        if (startTile == endTile)
            return false;

        // DDA walk over tiles in grid-local space, skipping the shooter's own tile.
        var dx = Math.Abs(endTile.X - startTile.X);
        var dy = Math.Abs(endTile.Y - startTile.Y);
        var sx = startTile.X < endTile.X ? 1 : -1;
        var sy = startTile.Y < endTile.Y ? 1 : -1;
        var err = dx - dy;

        var current = startTile;
        var first = true;
        while (true)
        {
            if (!first && IsOpeningTile(gridUid, grid, current))
            {
                openingPoint = GetTileCenterWorld(gridUid, grid, current);
                return true;
            }
            first = false;

            if (current == endTile)
                break;

            var e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                current.X += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                current.Y += sy;
            }
        }

        return false;
    }

    private bool IsOpeningAt(EntityUid mapUid, Vector2 pos)
    {
        if (!_mapManager.TryFindGridAt(mapUid, pos, out var gridUid, out var grid))
            return true;

        return IsOpeningTile(gridUid, grid, _map.WorldToTile(gridUid, grid, pos));
    }

    private bool IsOpeningTile(EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        if (!_map.TryGetTileRef(gridUid, grid, tile, out var tileRef) || tileRef.Tile.IsEmpty)
            return true;

        return ((ContentTileDefinition)_tileDef[tileRef.Tile.TypeId]).Transparent;
    }

    private Vector2 GetTileCenterWorld(EntityUid gridUid, MapGridComponent grid, Vector2i tile)
    {
        return _map.ToCenterCoordinates(gridUid, tile, grid).ToMapPos(EntityManager, _transform);
    }
}
