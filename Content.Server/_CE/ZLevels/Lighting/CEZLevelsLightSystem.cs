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
/// Synchronizes ambient (map) light across a z-network:
/// every level at or above the first level shares the ambient of the first (ground) level,
/// while levels below the first are considered underground and forced dark.
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

        var networks = EntityQueryEnumerator<CEZMapNetworkComponent>();
        while (networks.MoveNext(out _, out var network))
        {
            SyncNetwork(network);
        }
    }

    private void SyncNetwork(CEZMapNetworkComponent network)
    {
        // The "first" level is the closest to depth 0. It provides the ambient for every level above ground.
        Color? surfaceAmbient = null;
        var surfaceDepth = int.MaxValue;

        foreach (var (depth, mapUid) in network.ZLevels)
        {
            if (mapUid is not { } uid || depth < 0)
                continue;

            if (depth >= surfaceDepth || HasComp<LightCycleComponent>(uid))
                continue;

            if (TryComp<MapLightComponent>(uid, out var light))
            {
                surfaceAmbient = light.AmbientLightColor;
                surfaceDepth = depth;
            }
        }

        foreach (var (depth, mapUid) in network.ZLevels)
        {
            if (mapUid is not { } uid)
                continue;

            // Day/night cycle maps manage their own ambient light.
            if (HasComp<LightCycleComponent>(uid))
                continue;

            if (!TryComp<MapComponent>(uid, out var map))
                continue;

            if (depth < 0)
            {
                _map.SetAmbientLight(map.MapId, UndergroundAmbient);
                continue;
            }

            if (surfaceAmbient is { } ambient)
                _map.SetAmbientLight(map.MapId, ambient);
        }
    }
}
