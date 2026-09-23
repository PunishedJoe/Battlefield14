/*
 * This file is sublicensed under MIT License
 * https://github.com/space-wizards/space-station-14/blob/master/LICENSE.TXT
 */

using Robust.Shared.GameObjects;

namespace Content.Client._CE.ZLevels.Audio;

/// <summary>
/// Marker placed on a client-side echo sound that was spawned to represent a sound coming from another z-level.
/// Prevents the echo from being echoed again and links it back to its source.
/// </summary>
[RegisterComponent]
public sealed partial class CEZLevelAudioEchoComponent : Component
{
    /// <summary>
    /// The original audio entity this echo was created for.
    /// </summary>
    [ViewVariables]
    public EntityUid Source;
}
