using Content.Server.Atmos.EntitySystems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Atmos;
using Content.Shared.Explosion.Components;
using Content.Shared.Explosion.Components.OnTrigger;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.Projectiles.Components;
using Content.Shared._Mono.ArmorPiercing;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;

namespace Content.Server._Battlefield14.Projectiles;

/// <summary>
/// System for handling HEAT (High Explosive Anti-Tank) projectiles and similar penetration-based projectiles.
/// Handles explosions, fragmentation, and plasma gas release after penetration stops.
/// </summary>
public sealed class HeatSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly TriggerSystem _triggerSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosionSystem = default!;
    [Dependency] private readonly GunSystem _gunSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HeatComponent, TriggerEvent>(OnHeatTriggered);
        SubscribeLocalEvent<HeatComponent, StartCollideEvent>(OnHeatStartCollide);
    }

    private void OnHeatTriggered(EntityUid uid, HeatComponent component, TriggerEvent args)
    {
        if (component.FragmentationTriggered &&
            component.FragmentationPrototype != null &&
            component.FragmentationCount > 0)
        {
            FragmentProjectile(uid, component);
            component.FragmentationTriggered = false;
            Dirty(uid, component);
        }

        if (component.PlasmaReleaseTriggered && component.PlasmaAmount > 0)
        {
            ReleasePlasmaGas(uid, component);
            component.PlasmaReleaseTriggered = false;
            Dirty(uid, component);
        }
    }

    private void OnHeatStartCollide(Entity<HeatComponent> ent, ref StartCollideEvent args)
    {
        // Only handle collisions for projectiles with both HeatComponent and ArmorPiercingComponent
        if (!HasComp<ArmorPiercingComponent>(ent) || !TryComp<ProjectileComponent>(ent, out var projectile))
            return;

        // Only check walls (Impassable collision layer)
        var isWall = (args.OtherFixture.CollisionLayer & (int)CollisionGroup.Impassable) != 0;
        if (!isWall)
            return;

        // Check if target has armor thickness
        if (!TryComp<ArmorThicknessComponent>(args.OtherEntity, out var armorThickness))
            return;

        if (!armorThickness.CanBePierced)
            return;

        // Check if projectile can penetrate
        var armorPiercing = Comp<ArmorPiercingComponent>(ent);
        if (armorPiercing.PiercingThickness >= armorThickness.Thickness)
        {
            // Can penetrate - let ArmorPiercingSystem handle it
            return;
        }

        // Can't penetrate - mark as spent and trigger explosion
        projectile.ProjectileSpent = true;
        Dirty(ent, projectile);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HeatComponent, ProjectileComponent>();
        while (query.MoveNext(out var uid, out var heat, out var projectile))
        {
            if (!projectile.ProjectileSpent || heat.ExplosionTriggered)
                continue;

            heat.ExplosionTriggered = true;
            Dirty(uid, heat);

            projectile.DeleteOnCollide = false;
            Dirty(uid, projectile);

            if (heat.FragmentationPrototype != null && heat.FragmentationCount > 0)
            {
                if (heat.FragmentationDelay <= 0)
                {
                    FragmentProjectile(uid, heat);
                }
                else
                {
                    heat.FragmentationTriggered = true;
                    Dirty(uid, heat);
                    _triggerSystem.HandleTimerTrigger(uid, null, heat.FragmentationDelay, 0, null, null);
                }
            }

            if (heat.PlasmaAmount > 0)
            {
                if (heat.PlasmaReleaseDelay <= 0)
                {
                    ReleasePlasmaGas(uid, heat);
                }
                else
                {
                    heat.PlasmaReleaseTriggered = true;
                    Dirty(uid, heat);
                    _triggerSystem.HandleTimerTrigger(uid, null, heat.PlasmaReleaseDelay, 0, null, null);
                }
            }

            if (heat.ExplosionDelay <= 0)
            {
                if (TryComp<ExplosiveComponent>(uid, out var explosive))
                {
                    _explosionSystem.TriggerExplosive(uid, explosive, delete: true);
                }
            }
            else
            {
                if (!HasComp<ExplodeOnTriggerComponent>(uid))
                {
                    Log.Error($"HEAT projectile {uid} has ExplosionDelay > 0 but is missing ExplodeOnTriggerComponent. Explosion will not trigger.");
                }
                _triggerSystem.HandleTimerTrigger(uid, null, heat.ExplosionDelay, 0, null, null);
            }
        }
    }

    private void FragmentProjectile(EntityUid uid, HeatComponent component)
    {
        var projectileCoord = _transformSystem.GetMapCoordinates(uid);
        var segmentAngle = 360f / component.FragmentationCount;

        for (int i = 0; i < component.FragmentationCount; i++)
        {
            var angleMin = segmentAngle * i;
            var angleMax = segmentAngle * (i + 1);
            var angle = Angle.FromDegrees(_random.NextFloat(angleMin, angleMax));
            var direction = angle.ToVec().Normalized();
            var velocity = _random.NextVector2(5f, 10f);

            var spawnOffset = direction * 0.5f;
            var spawnCoord = new MapCoordinates(projectileCoord.Position + spawnOffset, projectileCoord.MapId);
            var fragUid = Spawn(component.FragmentationPrototype, spawnCoord);
            _gunSystem.ShootProjectile(fragUid, direction, velocity, uid, null);
        }
    }

    private void ReleasePlasmaGas(EntityUid uid, HeatComponent component)
    {
        var environment = _atmosphereSystem.GetContainingMixture(uid, false, true);
        if (environment == null)
            return;

        var plasmaMixture = new GasMixture(1) { Temperature = component.PlasmaTemperature };
        plasmaMixture.SetMoles(Gas.Plasma, component.PlasmaAmount);
        _atmosphereSystem.Merge(environment, plasmaMixture);
    }
}
