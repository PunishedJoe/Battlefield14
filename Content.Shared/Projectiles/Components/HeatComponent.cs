using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Projectiles.Components;

/// <summary>
/// Component for HEAT (High Explosive Anti-Tank) projectiles and similar penetration-based projectiles.
/// Causes the projectile to explode after it can no longer penetrate armor thickness (when used with ArmorPiercingComponent).
/// Can also fragment into additional projectiles and/or release plasma gas after penetration stops.
/// Note: This system now uses ArmorPiercingComponent and ArmorThicknessComponent instead of penetration threshold.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HeatComponent : Component
{
    /// <summary>
    /// Delay in seconds before explosion after penetration stops (when projectile can no longer penetrate armor thickness).
    /// If 0, explodes instantly.
    /// </summary>
    [DataField]
    public float ExplosionDelay = 0f;

    /// <summary>
    /// Whether the explosion has been triggered.
    /// </summary>
    [DataField]
    public bool ExplosionTriggered = false;

    /// <summary>
    /// If set, the projectile will fragment into this prototype when exploding (requires HeFragProjectileComponent).
    /// </summary>
    [DataField]
    public EntProtoId? FragmentationPrototype = null;

    /// <summary>
    /// Number of fragmentation projectiles to spawn. Only used if FragmentationPrototype is set.
    /// </summary>
    [DataField]
    public int FragmentationCount = 0;

    /// <summary>
    /// Delay in seconds before fragmentation occurs after penetration stops. If 0, fragments instantly.
    /// </summary>
    [DataField]
    public float FragmentationDelay = 0f;

    /// <summary>
    /// Whether fragmentation has been triggered.
    /// </summary>
    [DataField]
    public bool FragmentationTriggered = false;

    /// <summary>
    /// If set, the projectile will release plasma gas after penetration stops.
    /// Amount of plasma gas to spawn in moles.
    /// </summary>
    [DataField]
    public float PlasmaAmount = 0f;

    /// <summary>
    /// Temperature of the plasma gas in Kelvin. High temperatures will rapidly heat the environment.
    /// Only used if PlasmaAmount > 0.
    /// </summary>
    [DataField]
    public float PlasmaTemperature = 1000f;

    /// <summary>
    /// Delay in seconds before releasing plasma after penetration stops. If 0, releases instantly.
    /// Only used if PlasmaAmount > 0.
    /// </summary>
    [DataField]
    public float PlasmaReleaseDelay = 0f;

    /// <summary>
    /// Whether plasma release has been triggered.
    /// </summary>
    [DataField]
    public bool PlasmaReleaseTriggered = false;
}

