using Robust.Shared.GameStates;

namespace Content.Shared.POICapture;

/// <summary>
/// Component that allows an entity to be captured and controlled by factions
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class POICaptureComponent : Component
{
    /// <summary>
    /// Name of the POI displayed in announcements and popups
    /// </summary>
    [DataField("poiName"), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string POIName = "Unknown Territory";

    /// <summary>
    /// Time in seconds it takes to capture this POI
    /// </summary>
    [DataField("timeToCapture"), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float TimeToCapture = 30f; // 30 seconds default

    /// <summary>
    /// Grace period in seconds before POI can be captured after round start or last capture
    /// </summary>
    [DataField("gracePeriod"), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float GracePeriod = 0f;

    /// <summary>
    /// Delay for the DoAfter when initiating capture interaction
    /// </summary>
    [DataField("doAfterDelay")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float DoAfterDelay = 2f;

    /// <summary>
    /// Current faction controlling this POI (null if neutral/unclaimed)
    /// </summary>
    [DataField("controllingFaction"), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? ControllingFaction;

    /// <summary>
    /// Whether this POI is currently being captured
    /// </summary>
    [DataField("isBeingCaptured"), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsBeingCaptured;

    /// <summary>
    /// Faction currently attempting to capture this POI
    /// </summary>
    [DataField("capturingFaction"), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string? CapturingFaction;

    /// <summary>
    /// Current progress of capture attempt (in seconds)
    /// </summary>
    [DataField("currentCaptureProgress"), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float CurrentCaptureProgress;
}
