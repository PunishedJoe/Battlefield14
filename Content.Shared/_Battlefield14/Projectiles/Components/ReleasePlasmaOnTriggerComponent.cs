using Robust.Shared.GameStates;

namespace Content.Shared._Battlefield14.Projectiles.Components;

/// <summary>
/// Component that releases plasma gas when triggered.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ReleasePlasmaOnTriggerComponent : Component
{
    /// <summary>
    /// Amount of plasma gas to release in moles.
    /// </summary>
    [DataField]
    public float PlasmaAmount = 150f;

    /// <summary>
    /// Temperature of the plasma gas in Kelvin.
    /// </summary>
    [DataField]
    public float PlasmaTemperature = 1500f;
}



