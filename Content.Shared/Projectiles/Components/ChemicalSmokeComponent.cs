using Content.Shared.Chemistry.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Projectiles.Components;

/// <summary>
/// Component for projectiles that spawn a chemical smoke cloud after penetration stops.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChemicalSmokeComponent : Component
{
    /// <summary>
    /// Delay in seconds before releasing smoke after penetration stops. If 0, releases instantly.
    /// </summary>
    [DataField]
    public float ReleaseDelay = 0f;

    /// <summary>
    /// Whether the smoke release has been triggered.
    /// </summary>
    [DataField]
    public bool ReleaseTriggered = false;

    /// <summary>
    /// How long the smoke stays for, after it has spread.
    /// </summary>
    [DataField]
    public float Duration = 30f;

    /// <summary>
    /// How much the smoke will spread.
    /// </summary>
    [DataField]
    public int SpreadAmount = 50;

    /// <summary>
    /// Smoke entity prototype to spawn. Defaults to "Smoke".
    /// </summary>
    [DataField]
    public EntProtoId SmokePrototype = "Smoke";

    /// <summary>
    /// Solution containing the chemicals to add to the smoke cloud.
    /// </summary>
    [DataField]
    public Solution Solution = new();
}



