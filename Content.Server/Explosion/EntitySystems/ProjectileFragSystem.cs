using Content.Server.Explosion.Components;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Projectiles;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Explosion.EntitySystems;

public sealed class ProjectileFragSystem : EntitySystem
{
    [Dependency] private readonly GunSystem _gun = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeFragProjectileComponent, ComponentInit>(OnFragInit);
        SubscribeLocalEvent<HeFragProjectileComponent, ComponentStartup>(OnFragStartup);
        SubscribeLocalEvent<HeFragProjectileComponent, ProjectileHitEvent>(OnFragHit);
    }

    private void OnFragInit(Entity<HeFragProjectileComponent> entity, ref ComponentInit args)
    {
        entity.Comp.Container = _container.EnsureContainer<Container>(entity.Owner, "frag-payload");
    }

    /// <summary>
    /// Setting the unspawned count based on capacity so we know how many new entities to spawn
    /// </summary>
    private void OnFragStartup(Entity<HeFragProjectileComponent> entity, ref ComponentStartup args)
    {
        if (entity.Comp.FillPrototype == null)
            return;

        entity.Comp.UnspawnedCount = Math.Max(0, entity.Comp.Capacity - entity.Comp.Container.ContainedEntities.Count);
    }

    /// <summary>
    /// Triggered when the projectile hits an entity
    /// </summary>
    private void OnFragHit(Entity<HeFragProjectileComponent> entity, ref ProjectileHitEvent args)
    {
        FragmentIntoProjectiles(entity.Owner, entity.Comp);
    }

    /// <summary>
    /// Spawns projectiles at the coordinates of the projectile upon collision
    /// Can customize the angle and velocity the projectiles come out at
    /// </summary>
    private void FragmentIntoProjectiles(EntityUid uid, HeFragProjectileComponent component)
    {
        var projectileCoord = _transformSystem.GetMapCoordinates(uid);
        var shootCount = 0;
        var totalCount = component.Container.ContainedEntities.Count + component.UnspawnedCount;
        var segmentAngle = 360 / totalCount;

        while (TrySpawnContents(projectileCoord, component, out var contentUid))
        {
            Angle angle;
            if (component.RandomAngle)
                angle = _random.NextAngle();
            else
            {
                var angleMin = segmentAngle * shootCount;
                var angleMax = segmentAngle * (shootCount + 1);
                angle = Angle.FromDegrees(_random.Next(angleMin, angleMax));
                shootCount++;
            }

            // velocity is randomized to make the projectiles look
            // slightly uneven, doesn't really change much, but it looks better
            var direction = angle.ToVec().Normalized();
            var velocity = _random.NextVector2(component.MinVelocity, component.MaxVelocity);
            
            // Move fragment to offset position to prevent all fragments from hitting the same entity
            var spawnOffset = direction * 0.5f; // Offset by 0.5 meters in the fragment's direction
            var spawnCoord = new MapCoordinates(projectileCoord.Position + spawnOffset, projectileCoord.MapId);
            _transformSystem.SetMapCoordinates(contentUid, spawnCoord);
            
            _gun.ShootProjectile(contentUid, direction, velocity, uid, null);
        }
    }

    /// <summary>
    /// Spawns one instance of the fill prototype or contained entity at the coordinate indicated
    /// </summary>
    private bool TrySpawnContents(MapCoordinates spawnCoordinates, HeFragProjectileComponent component, out EntityUid contentUid)
    {
        contentUid = default;

        if (component.UnspawnedCount > 0)
        {
            component.UnspawnedCount--;
            contentUid = Spawn(component.FillPrototype, spawnCoordinates);
            return true;
        }

        if (component.Container.ContainedEntities.Count > 0)
        {
            contentUid = component.Container.ContainedEntities[0];

            if (!_container.Remove(contentUid, component.Container))
                return false;

            return true;
        }

        return false;
    }
}


