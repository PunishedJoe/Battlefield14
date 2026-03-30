namespace Content.Server._Battlefield14.ThroughWallProjectileFire;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class PhasingProjectileComponent : Component
{
    public HashSet<EntityUid> ignoring = new();
}
