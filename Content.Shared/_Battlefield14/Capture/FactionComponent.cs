using Robust.Shared.GameStates;

namespace Content.Shared.Factions;

/// <summary>
/// Component that identifies which faction an entity belongs to.
/// Can be added to clothing, entities, or anything that needs faction identification.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FactionComponent : Component
{
    /// <summary>
    /// The faction ID this entity belongs to (e.g., "Syndicate", "Crew", "Pirates")
    /// </summary>
    [DataField("factionId"), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string FactionId = string.Empty;
}
