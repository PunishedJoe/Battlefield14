/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using System.Numerics;
using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;

namespace Content.Client._CE.ZLevels.Lighting;

/// <summary>
/// Mirrors point lights onto the adjacent z-levels so that lighting is synchronized between levels.
/// A mirror is dimmed when an opaque floor/ceiling blocks the path (floors of a level are the ceilings below).
/// </summary>
public sealed partial class CEClientZLevelsLightSystem : EntitySystem
{
    [Dependency] private SharedPointLightSystem _lights = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private CESharedZLevelsSystem _zLevels = default!;

    private EntityQuery<CEZMapComponent> _zMapQuery = default!;
    private EntityQuery<CEZLevelMirrorLightComponent> _mirrorQuery = default!;

    private readonly Dictionary<(EntityUid Source, EntityUid Target), EntityUid> _mirrors = new();
    private readonly HashSet<EntityUid> _desired = new();
    private readonly List<(EntityUid Source, EntityUid Target)> _stale = new();

    public override void Initialize()
    {
        base.Initialize();

        _zMapQuery = GetEntityQuery<CEZMapComponent>();
        _mirrorQuery = GetEntityQuery<CEZLevelMirrorLightComponent>();
    }

    public override void FrameUpdate(float frameTime)
    {
        _desired.Clear();

        var query = EntityQueryEnumerator<PointLightComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var light, out var xform))
        {
            if (_mirrorQuery.HasComp(uid))
                continue;

            if (xform.MapUid is not { } sourceMap || !_zMapQuery.TryComp(sourceMap, out var sourceZ))
                continue;

            var worldPos = _xform.GetWorldPosition(xform);

            if (sourceZ.MapAbove is { } above && _zMapQuery.TryComp(above, out var aboveZ))
                UpdateMirror(uid, light, sourceMap, sourceZ, above, aboveZ, worldPos);

            if (sourceZ.MapBelow is { } below && _zMapQuery.TryComp(below, out var belowZ))
                UpdateMirror(uid, light, sourceMap, sourceZ, below, belowZ, worldPos);
        }

        // Remove mirrors that no longer have a matching source/map.
        _stale.Clear();
        foreach (var (key, mirror) in _mirrors)
        {
            if (_desired.Contains(mirror))
                continue;

            if (!TerminatingOrDeleted(mirror))
                QueueDel(mirror);

            _stale.Add(key);
        }

        foreach (var key in _stale)
        {
            _mirrors.Remove(key);
        }
    }

    private void UpdateMirror(
        EntityUid source,
        PointLightComponent sourceLight,
        EntityUid sourceMap,
        CEZMapComponent sourceZ,
        EntityUid targetMap,
        CEZMapComponent targetZ,
        Vector2 worldPos)
    {
        var key = (source, targetMap);
        var depthDelta = targetZ.Depth - sourceZ.Depth;

        // Opaque floors/ceilings block light completely: only transparent openings let it through.
        if (_zLevels.IsZLevelPathBlocked(sourceMap, depthDelta, worldPos))
        {
            if (_mirrors.Remove(key, out var blockedMirror) && !TerminatingOrDeleted(blockedMirror))
                QueueDel(blockedMirror);

            return;
        }

        if (!_mirrors.TryGetValue(key, out var mirror) || TerminatingOrDeleted(mirror))
        {
            mirror = Spawn("CEZLevelMirrorLight", new EntityCoordinates(targetMap, worldPos));
            var marker = AddComp<CEZLevelMirrorLightComponent>(mirror);
            marker.Source = source;
            marker.TargetMap = targetMap;
            _lights.EnsureLight(mirror);
            _mirrors[key] = mirror;
        }

        _desired.Add(mirror);

        if (Vector2.DistanceSquared(_xform.GetWorldPosition(mirror), worldPos) > 0.0001f)
            _xform.SetWorldPosition(mirror, worldPos);

        _lights.SetEnabled(mirror, sourceLight.Enabled);
        _lights.SetCastShadows(mirror, sourceLight.CastShadows);
        _lights.SetColor(mirror, sourceLight.Color);
        _lights.SetSoftness(mirror, sourceLight.Softness);
        _lights.SetRadius(mirror, sourceLight.Radius);
        _lights.SetEnergy(mirror, sourceLight.Energy);
    }
}
