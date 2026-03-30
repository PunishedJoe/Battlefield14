using System.Linq;
using Content.Server.Explosion.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Shared._Battlefield14.ThroughWallProjectileFire;
using Content.Shared.Explosion.Components.OnTrigger;
using Content.Shared.Sound;
using Content.Shared.Sticky;
using Content.Shared.Throwing;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Battlefield14.ThroughWallProjectileFire;

/// <summary>
/// This handles...
/// </summary>
public sealed class ThroughWallProjectileFireSystem : VirtualController
{
    [Dependency]
    private readonly TransformSystem _transform = default!;

    [Dependency]
    private readonly ThrowingSystem _throw = default!;

    [Dependency]
    private readonly SharedAudioSystem _audio = default!;

    [Dependency]
    private readonly IGameTiming _timing = default!;

    [Dependency]
    private readonly EntityLookupSystem _lookup = default!;

    [Dependency]
    private readonly FixtureSystem _fixtures = default!;

    [Dependency]
    private readonly PhysicsSystem _physics = default!;

    [Dependency]
    private readonly TriggerSystem _trigger = default!;

    [Dependency]
    private readonly IRobustRandom _random = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ThroughWallProjectileFireComponent, TriggerEvent>(onTrigger);
        SubscribeLocalEvent<ThroughWallProjectileFireComponent, EntityStuckEvent>(onStuck);
        SubscribeLocalEvent<ThroughWallProjectileFireComponent, EntityUnstuckEvent>(onUnstuck);
    }

    public void onTrigger(Entity<ThroughWallProjectileFireComponent> ent, ref TriggerEvent ev)
    {
        if(ent.Comp.projectileCount > 0)
            EnsureComp<ActiveThroughWallProjectileFireComponent>(ent);
    }

    public void onStuck(Entity<ThroughWallProjectileFireComponent> ent, ref EntityStuckEvent ev)
    {
        ent.Comp.passingThrough = ev.Target;
        _transform.SetWorldRotation(ent, (_transform.GetWorldPosition(ev.Target) - _transform.GetWorldPosition(ev.User)).ToAngle());
    }

    public void onUnstuck(Entity<ThroughWallProjectileFireComponent> ent, ref EntityUnstuckEvent ev)
    {
        ent.Comp.passingThrough = EntityUid.Invalid;

    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        base.UpdateBeforeSolve(prediction, frameTime);
        var secq = EntityQueryEnumerator<PhasingProjectileComponent>();
        while(secq.MoveNext(out var uid, out var comp))
        {
            var intsc = _lookup.GetEntitiesIntersecting(uid, LookupFlags.All);
            //Log.Debug($"Colliding with {String.Join(" ", intsc.ToList().AsQueryable().Select(thing => thing.Id))}");
            var origCount = intsc.Count;
            intsc.IntersectWith(comp.ignoring);
            //Log.Debug($" compared to {String.Join(" ",comp.ignoring.ToList().AsQueryable().Select(thing => thing.Id))}");
            if (intsc.Count == comp.ignoring.Count)
                continue;
            var targetName = "";
            if (TryComp<TriggerOnCollideComponent>(uid, out var triggerComp))
                targetName = triggerComp.FixtureID;
            var fixComp = Comp<FixturesComponent>(uid);
            // find target fixture and rename to the one that the trigger is expecting to arm it
            foreach (var fixture in fixComp.Fixtures)
            {
                var fixt = fixture.Value;
                if (fixture.Key.Contains("trigger"))
                {
                    if (targetName == "")
                        targetName = fixture.Key;
                    //Log.Debug($"Replaced fixture {fixture.Key} with {targetName}");
                    // internals are horrible >:( SPCR 2026
                    _fixtures.DestroyFixture(uid, fixture.Key, fixt, false);
                    _fixtures.TryCreateFixture(uid,
                        fixt.Shape,
                        targetName,
                        fixt.Density,
                        targetName == fixture.Key || fixt.Hard,
                        fixt.CollisionLayer,
                        fixt.CollisionMask,
                        fixt.Friction,
                        fixt.Restitution,
                        true);
                }
                break;
            }

            RemCompDeferred<PhasingProjectileComponent>(uid);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var entq = EntityQueryEnumerator<ActiveThroughWallProjectileFireComponent>();
        while (entq.MoveNext(out var uid, out var _))
        {
            var comp = Comp<ThroughWallProjectileFireComponent>(uid);
            if (comp.nextThrow > _timing.CurTime)
                continue;
            comp.nextThrow = _timing.CurTime + comp.throwDelay;
            comp.projectileCount--;
            var projectile = Spawn(comp.projectilePrototype.Id, _transform.GetMapCoordinates(uid));
            var phase = EnsureComp<PhasingProjectileComponent>(projectile);
            phase.ignoring.Add(comp.passingThrough);
            var calcAngle = _transform.GetWorldRotation(uid)+ (_random.NextFloat() * comp.throwVariation / 2) * (_random.NextFloat() > 0.5f ? 1f : -1f);
            _throw.TryThrow(projectile, calcAngle.ToVec(), baseThrowSpeed: comp.throwSpeed, friction: 0.1f);
            _audio.PlayEntity(_audio.ResolveSound(comp.sound), Filter.Pvs(uid, 1f), uid, true);
            if (comp.projectileCount == 0)
                RemCompDeferred<ActiveThroughWallProjectileFireComponent>(uid);

        }

    }
}
