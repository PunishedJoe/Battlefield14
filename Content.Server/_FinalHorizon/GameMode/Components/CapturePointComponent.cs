using Content.Shared._FinalHorizon.GameMode;

namespace Content.Server._FinalHorizon.GameMode.Components;

[RegisterComponent]
public sealed partial class CapturePointComponent : Component
{
    [DataField]
    public GameFactions PointOwner = GameFactions.Invalid;

    [DataField]
    public string PointName = string.Empty;

    /// <summary>
    /// The ammount of tickets awarded per second for the owner faction.
    /// </summary>
    [DataField]
    public float TicketValue = 1f;

    [DataField]
    public TimeSpan DoAfterTime = TimeSpan.FromSeconds(10);
}
