using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared._Battlefield14.Projectiles.Components;

/// <summary>
/// Marks a projectile that travels between z-levels (maps) mid-flight, so shots can hit targets
/// standing on the level above or below through openings in the floor/ceiling.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CEZProjectileComponent : Component
{
    /// <summary>
    /// Z-level offset the projectile travels. +1 = up, -1 = down.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ZOffset;

    /// <summary>
    /// World position where the projectile was fired from.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 SpawnPoint;

    /// <summary>
    /// World position of the opening the projectile must pass through to reach the adjacent z-level.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 TransitionPoint;

    /// <summary>
    /// Whether the projectile has already transitioned between z-levels.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Transitioned;
}
