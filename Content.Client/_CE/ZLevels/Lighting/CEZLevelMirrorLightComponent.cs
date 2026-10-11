/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Robust.Shared.GameObjects;

namespace Content.Client._CE.ZLevels.Lighting;

/// <summary>
/// Marker placed on a client-side point light that mirrors a light from an adjacent z-level.
/// Prevents mirroring loops and links the mirror back to its source and target map.
/// </summary>
[RegisterComponent]
public sealed partial class CEZLevelMirrorLightComponent : Component
{
    [ViewVariables]
    public EntityUid Source;

    [ViewVariables]
    public EntityUid TargetMap;
}
