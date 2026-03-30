using Content.Shared.Shuttles.Systems;
using Robust.Shared.GameStates;

// BF14
using Robust.Shared.Audio;

namespace Content.Shared.Shuttles.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
// [Access(typeof(SharedRadarConsoleSystem))] // BF14
public sealed partial class RadarConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float RangeVV
    {
        get => MaxRange;
        set => IoCManager
            .Resolve<IEntitySystemManager>()
            .GetEntitySystem<SharedRadarConsoleSystem>()
            .SetRange(Owner, value, this);
    }

    [DataField, AutoNetworkedField]
    public float MaxRange = 3072f; // Mono - 256->3072

    /// <summary>
    /// If true, the radar will be centered on the entity. If not - on the grid on which it is located.
    /// </summary>
    [DataField]
    public bool FollowEntity = false;

    // Frontier: ghost radar restrictions
    /// <summary>
    /// If true, the radar will be centered on the entity. If not - on the grid on which it is located.
    /// </summary>
    [DataField]
    public float? MaxIffRange = null;

    /// <summary>
    /// If true, the radar will not show the coordinates of objects on hover
    /// </summary>
    [DataField]
    public bool HideCoords = false;
    // End Frontier

    // <Mono>
    [DataField]
    public bool Pannable = true;

    [DataField]
    public bool RelativePanning = false;
    // </Mono>

    /// <summary>
    /// BF14 - whether sonar is available to this radar at all.
    /// </summary>
    [DataField]
    public bool HasSonar = false;

    // <BF14>
    /// <summary>
    /// When we last pulsed sonar. Only updated on client.
    /// </summary>
    [ViewVariables]
    public TimeSpan? SonarLastPulse = null;

    [DataField]
    public SoundSpecifier? SonarPingSound = new SoundPathSpecifier("/Audio/_Battlefield14/Effects/sonar_ping.ogg");

    [DataField]
    public SoundSpecifier? SonarCooledSound = new SoundPathSpecifier("/Audio/_Battlefield14/Effects/cool_over.ogg");
    // </BF14>
}
