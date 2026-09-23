/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using System.Numerics;
using Content.Shared._CE.ZLevels.Core.Components;
using JetBrains.Annotations;

namespace Content.Shared._CE.ZLevels.Core.EntitySystems;

public abstract partial class CESharedZLevelsSystem
{
    /// <summary>
    /// Height of a single z-level, used for spatial audio attenuation between levels.
    /// </summary>
    public const float ZLevelAudioHeight = 4f;

    /// <summary>
    /// Maximum amount of levels, sound can be transmitted through.
    /// </summary>
    public const int MaxZLevelsAudioSync = 3;

    /// <summary>
    /// Volume multiplier applied to a sound that has to pass through an opaque floor/ceiling.
    /// </summary>
    public const float ZLevelAudioBlockedAttenuation = 0.15f;

    /// <summary>
    /// Controls how fast sound fades with vertical distance when the path is open.
    /// </summary>
    public const float ZLevelAudioOpenFalloff = 0.2f;

    /// <summary>
    /// Checks whether an opaque floor/ceiling blocks the path between the map <paramref name="sourceMapUid"/> is
    /// located on and the map <paramref name="depthDelta"/> levels away.
    /// Floors of the upper level are treated as ceilings of the lower level.
    /// </summary>
    [PublicAPI]
    public bool IsZLevelPathBlocked(EntityUid sourceMapUid, int depthDelta, Vector2 worldPos)
    {
        if (depthDelta == 0)
            return false;

        if (!_zMapQuery.TryComp(sourceMapUid, out var sourceMap))
            return false;

        var sourceDepth = sourceMap.Depth;
        var targetDepth = sourceDepth + depthDelta;
        var lower = Math.Min(sourceDepth, targetDepth);
        var upper = Math.Max(sourceDepth, targetDepth);

        // The boundary between depth k and k+1 is represented by the tile on the upper map (k + 1),
        // because the floor of an upper level is the ceiling of the level below.
        for (var depth = lower; depth < upper; depth++)
        {
            var boundaryOffset = depth + 1 - sourceDepth;
            if (!TryMapOffset(sourceMapUid, boundaryOffset, out var boundaryMap))
                continue;

            if (_mapManager.TryFindGridAt(boundaryMap.Owner, worldPos, out var gridUid, out var grid)
                && _map.TryGetTileRef(gridUid, grid, worldPos, out var tileRef))
            {
                // Empty tiles (holes, atriums, stairwells) are openings and let light/sound through.
                if (!CEZLevelOpeningCache.IsOpeningTile(tileRef.Tile, TilDefMan))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Collects the entity uids of every map that belongs to the same z-network as <paramref name="mapUid"/>.
    /// Returns false when the map is not part of a z-network.
    /// </summary>
    [PublicAPI]
    public bool GetNetworkMaps(EntityUid mapUid, List<EntityUid> result)
    {
        result.Clear();

        if (!TryGetMapNetwork(mapUid, out var network))
            return false;

        foreach (var levelUid in network.Comp.SortedZLevels)
        {
            if (levelUid.IsValid() && _zMapQuery.HasComp(levelUid))
                result.Add(levelUid);
        }

        return true;
    }
}
