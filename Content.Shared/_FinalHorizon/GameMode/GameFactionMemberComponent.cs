namespace Content.Shared._FinalHorizon.GameMode;

/// <summary>
/// Assigns a generic faction team for players/mobs for game mode objectives.
/// </summary>
[RegisterComponent]
public sealed partial class GameFactionMemberComponent : Component
{
    [DataField]
    public GameFactions Faction = GameFactions.Invalid;

    [DataField]
    public string FactionName = string.Empty;
}

public enum GameFactions
{
    Nato,
    Warpact,
    Invalid
}
