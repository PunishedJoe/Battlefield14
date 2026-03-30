using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Projectiles.Components;

/// <summary>
/// Component for projectiles that spawn other projectiles after a delay or distance.
/// Handles both submunitions (multiple projectiles) and APFSDS (discarding sabot - single penetrator).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SubmunitionProjectileComponent : Component
{
    /// <summary>
    /// The prototype ID of the submunition projectile to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId SubmunitionPrototype = string.Empty;

    /// <summary>
    /// Time in seconds before spawning submunitions. Used if DistanceDelay is 0.
    /// </summary>
    [DataField]
    public float SubmunitionDelay = 1f;

    /// <summary>
    /// Distance to travel before spawning submunitions. If > 0, uses this instead of SubmunitionDelay.
    /// </summary>
    [DataField]
    public float DistanceDelay = 0f;

    /// <summary>
    /// Number of submunitions to spawn.
    /// </summary>
    [DataField]
    public int SubmunitionCount = 1;

    /// <summary>
    /// Spread angle in degrees for submunition spawning (0 = all go straight).
    /// </summary>
    [DataField]
    public float SpreadAngle = 0f;

    /// <summary>
    /// Velocity multiplier for spawned projectiles (e.g., 1.5 = 50% faster). 1.0 = same velocity as parent.
    /// </summary>
    [DataField]
    public float VelocityMultiplier = 1.0f;

    /// <summary>
    /// Time elapsed so far.
    /// </summary>
    [DataField]
    public float TimeElapsed = 0f;

    /// <summary>
    /// Distance traveled so far.
    /// </summary>
    [DataField]
    public float DistanceTraveled = 0f;

    /// <summary>
    /// Whether submunitions have been spawned.
    /// </summary>
    [DataField]
    public bool SubmunitionsSpawned = false;
}

