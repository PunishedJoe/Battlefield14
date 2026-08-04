using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Chamber + mags in one package. If you need just magazine then use <see cref="MagazineAmmoProviderComponent"/>
/// </summary>
[RegisterComponent, AutoGenerateComponentState]
[Access(typeof(SharedGunSystem))]
public sealed partial class ChamberMagazineAmmoProviderComponent : MagazineAmmoProviderComponent
{
    /// <summary>
    /// If the gun has a bolt and whether that bolt is closed. Firing is impossible
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("boltClosed"), AutoNetworkedField]
    public bool? BoltClosed = false;

    /// <summary>
    /// Does the gun automatically open and close the bolt upon shooting.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("autoCycle"), AutoNetworkedField]
    public bool AutoCycle = true;

    /// <summary>
    /// Can the gun be racked, which opens and then instantly closes the bolt to cycle a round.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("canRack"), AutoNetworkedField]
    public bool CanRack = true;

    /// <summary>
    /// If the gun runs out of ammo, does the bolt automatically open/lock back.
    /// If false the bolt stays closed when empty but can still be opened/closed manually.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("autoOpenBoltOnEmpty"), AutoNetworkedField]
    public bool AutoOpenBoltOnEmpty = true;

    [ViewVariables(VVAccess.ReadWrite), DataField("soundBoltClosed"), AutoNetworkedField]
    public SoundSpecifier? BoltClosedSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Bolt/rifle_bolt_closed.ogg");

    [ViewVariables(VVAccess.ReadWrite), DataField("soundBoltOpened"), AutoNetworkedField]
    public SoundSpecifier? BoltOpenedSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Bolt/rifle_bolt_open.ogg");

    [ViewVariables(VVAccess.ReadWrite), DataField("soundRack"), AutoNetworkedField]
    public SoundSpecifier? RackSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Cock/ltrifle_cock.ogg");
}
