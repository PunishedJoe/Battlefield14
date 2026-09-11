using Content.Shared._FinalHorizon.GameMode;

namespace Content.Server._FinalHorizon.GameMode.Components;

[RegisterComponent]
public sealed partial class ConquestManagerComponent : Component
{
    [DataField]
    public Dictionary<GameFactions, float> Tickets = [];

    [DataField]
    public float MaxTickets = 600; //BF14 600>500

    [DataField]
    public bool Enabled = false;

    [DataField]
    public TimeSpan CheckFrequency = TimeSpan.FromSeconds(30);
}
