using System.Numerics;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Weapons.Ranged;

/// <summary>
/// An event raised before a shot attempt is made.
/// </summary>
[ByRefEvent]
public record struct BeforeAttemptShootEvent(EntityCoordinates Origin, Vector2 Offset, bool Handled = false);

/// <summary>
/// An event raised right before a muzzle flash event is raised.
/// </summary>
[ByRefEvent]
public record struct RMCBeforeMuzzleFlashEvent(EntityUid Weapon, Vector2 Offset = default);
