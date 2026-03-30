using Content.Server.Explosion.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Spreader;
using Content.Shared.Chemistry.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Maps;
using Content.Shared.Projectiles;
using Content.Shared.Projectiles.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server._Battlefield14.Projectiles;

/// <summary>
/// System for handling projectiles that spawn chemical smoke clouds after penetration stops.
/// </summary>
public sealed class ChemicalSmokeSystem : EntitySystem
{
    [Dependency] private readonly SmokeSystem _smokeSystem = default!;
    [Dependency] private readonly TriggerSystem _triggerSystem = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SpreaderSystem _spreader = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChemicalSmokeComponent, TriggerEvent>(OnChemicalSmokeTriggered);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ChemicalSmokeComponent, ProjectileComponent>();
        while (query.MoveNext(out var uid, out var smoke, out var projectile))
        {
            // Check if projectile penetration has stopped
            if (!projectile.ProjectileSpent || smoke.ReleaseTriggered)
                continue;

            // Mark as triggered to prevent multiple releases
            smoke.ReleaseTriggered = true;
            Dirty(uid, smoke);

            // Schedule release (with delay if specified)
            if (smoke.ReleaseDelay <= 0)
            {
                // Instant release - call directly
                ReleaseSmoke(uid, smoke);
                // Mark as completed to prevent re-triggering
                smoke.ReleaseTriggered = false;
                Dirty(uid, smoke);
            }
            else
            {
                // Delayed release using timer - event handler will be called when timer fires
                _triggerSystem.HandleTimerTrigger(uid, null, smoke.ReleaseDelay, 0, null, null);
            }
        }
    }

    private void OnChemicalSmokeTriggered(EntityUid uid, ChemicalSmokeComponent component, TriggerEvent args)
    {
        // Only release if release was scheduled (ReleaseTriggered is true)
        // This prevents double-release and ensures only our timer triggers the release
        if (!component.ReleaseTriggered)
            return;

        ReleaseSmoke(uid, component);
        // Mark as completed to prevent re-triggering
        component.ReleaseTriggered = false;
        Dirty(uid, component);
    }

    private void ReleaseSmoke(EntityUid uid, ChemicalSmokeComponent component)
    {
        var xform = Transform(uid);
        var mapCoords = _transformSystem.GetMapCoordinates(uid, xform);
        
        // Check if we're on a valid grid
        if (!_mapManager.TryFindGridAt(mapCoords, out _, out var grid) ||
            !grid.TryGetTileRef(xform.Coordinates, out var tileRef) ||
            tileRef.Tile.IsEmpty)
        {
            return;
        }

        if (_spreader.RequiresFloorToSpread(component.SmokePrototype.ToString()) && tileRef.Tile.IsSpace())
            return;

        var coords = grid.MapToGrid(mapCoords);
        var smokeEntity = Spawn(component.SmokePrototype, coords.SnapToGrid());
        
        if (!TryComp<SmokeComponent>(smokeEntity, out var smokeComp))
        {
            Log.Error($"Smoke prototype {component.SmokePrototype} was missing SmokeComponent");
            Del(smokeEntity);
            return;
        }

        // Start the smoke cloud with the configured chemical solution
        _smokeSystem.StartSmoke(smokeEntity, component.Solution, component.Duration, component.SpreadAmount, smokeComp);
    }
}

