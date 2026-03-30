using Robust.Shared.GameStates;

namespace Content.Shared._Battlefield14.Sonar;

[RegisterComponent, NetworkedComponent]
// [Access(typeof(SharedRadarConsoleSystem))] // BF14
public sealed partial class SonarModuleComponent : Component
{
    /// <summary>
    /// The coverage arc of the sonar.
    /// </summary>
    [DataField]
    public Angle SonarWidth = Angle.FromDegrees(30);

    /// <summary>
    /// The distance the sonar goes.
    /// </summary>
    [DataField]
    public float SonarDistance = 800f;

    /// <summary>
    /// For how long to reveal grids.
    /// </summary>
    [DataField]
    public TimeSpan SonarDuration = TimeSpan.FromSeconds(7);

    /// <summary>
    /// How long to take to recharge.
    /// </summary>
    [DataField]
    public TimeSpan SonarCooldown = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Whether to allow revealing cloaked ships.
    /// </summary>
    [DataField]
    public bool SeeCloaked = true;
}
