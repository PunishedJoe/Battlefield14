using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Battlefield14.ThroughWallProjectileFire;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class ThroughWallProjectileFireComponent : Component
{
    [DataField]
    public EntProtoId projectilePrototype = default!;

    [ViewVariables]
    public EntityUid passingThrough = EntityUid.Invalid;

    [DataField]
    public int projectileCount = 3;

    [DataField]
    public Angle throwVariation;

    [DataField]
    public float throwSpeed = 20;

    [DataField]
    public TimeSpan throwDelay;

    [ViewVariables]
    public TimeSpan nextThrow;

    [DataField]
    public SoundSpecifier sound = default!;


}
