/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared.Light.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Server._CE.ZLevels.Lighting;

/// <summary>
/// Applies a dark ambient light to every underground z-level (depth below the first level).
/// Levels below the first are considered underground and have to be lit artificially.
/// </summary>
public sealed class CEZLevelsLightSystem : EntitySystem
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private IGameTiming _timing = default!;

    /// <summary>
    /// Ambient color used for underground maps.
    /// </summary>
    private static readonly Color UndergroundAmbient = Color.FromHex("#0a0c14");

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    private TimeSpan _nextUpdate;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        var query = EntityQueryEnumerator<CEZMapComponent>();
        while (query.MoveNext(out var mapUid, out var zMap))
        {
            if (zMap.Depth >= 0)
                continue;

            // Day/night cycle maps manage their own ambient light.
            if (HasComp<LightCycleComponent>(mapUid))
                continue;

            if (!TryComp<MapComponent>(mapUid, out var map))
                continue;

            _map.SetAmbientLight(map.MapId, UndergroundAmbient);
        }
    }
}
