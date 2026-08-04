using Content.Shared._CE.ZLevels.Core.Components;
using Content.Shared._CE.ZLevels.Core.EntitySystems;
using Content.Shared.Throwing;

namespace Content.Shared._CE.ZLevels.Throwing;

public sealed partial class CEZLevelThrowingSystem : EntitySystem
{
    private const float NormalArcPeakHeight = 1.0f;
    private const float HighArcPeakHeight = 2.0f;
    private const float MinArcPeakHeight = 0.4f;
    private const float HighThrowTargetHeight = 1.2f;

    [Dependency] private CESharedZLevelsSystem _zLevels = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CEZPhysicsComponent, ThrownEvent>(OnThrown);
    }

    private void OnThrown(Entity<CEZPhysicsComponent> ent, ref ThrownEvent args)
    {
        if (!TryComp<ThrownItemComponent>(ent, out var thrown)
            || thrown.LandTime is not { } landTime
            || thrown.ThrownTime is not { } thrownTime)
            return;

        var flyTime = (float)(landTime - thrownTime).TotalSeconds;
        if (flyTime <= 0f)
            return;

        var highThrow = args.User is { } user
            && TryComp<CEZLevelViewerComponent>(user, out var viewer)
            && viewer.LookUp;

        var targetHeight = highThrow ? HighThrowTargetHeight : 0f;
        var peakCap = highThrow ? HighArcPeakHeight : NormalArcPeakHeight;

        var distToGround = MathF.Max(0f, ent.Comp.LocalPosition - ent.Comp.CachedGroundHeight);

        // Guarantee a minimum arc so short throws still have enough power to clear ledges and obstacles,
        // and long throws can reliably reach the floor above.
        var minV0 = MathF.Sqrt(2f * CESharedZLevelsSystem.ZGravityForce * MinArcPeakHeight);
        var v0 = CESharedZLevelsSystem.ZGravityForce * flyTime * 0.5f + (targetHeight - distToGround) / flyTime;
        var maxV0 = MathF.Sqrt(2f * CESharedZLevelsSystem.ZGravityForce * peakCap);
        _zLevels.SetZVelocity((ent.Owner, ent.Comp), Math.Clamp(v0, minV0, maxV0));
    }
}
