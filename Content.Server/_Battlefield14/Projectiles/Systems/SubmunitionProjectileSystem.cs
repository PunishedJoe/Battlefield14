using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Projectiles;
using Content.Shared.Projectiles.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server._Battlefield14.Projectiles;

/// <summary>
/// System for handling submunition projectiles that spawn submunitions after a delay or distance.
/// </summary>
public sealed class SubmunitionProjectileSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SubmunitionProjectileComponent, ProjectileComponent, PhysicsComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var submunition, out var projectile, out var physics, out var xform))
        {
            if (submunition.SubmunitionsSpawned)
                continue;

            var currentVelocity = physics.LinearVelocity;
            var distanceThisFrame = currentVelocity.Length() * frameTime;

            // Update tracking values
            submunition.TimeElapsed += frameTime;
            submunition.DistanceTraveled += distanceThisFrame;

            // Check if we should spawn submunitions
            bool shouldSpawn = false;

            // Distance-based triggering takes priority
            if (submunition.DistanceDelay > 0)
            {
                shouldSpawn = submunition.DistanceTraveled >= submunition.DistanceDelay;
            }
            // Time-based triggering (if distance not configured)
            else if (submunition.SubmunitionDelay > 0)
            {
                shouldSpawn = submunition.TimeElapsed >= submunition.SubmunitionDelay;
            }
            // Immediate spawn (both delays are 0 or negative)
            else
            {
                shouldSpawn = true;
            }

            if (!shouldSpawn)
                continue;

            // Spawn submunitions
            SpawnSubmunitions(uid, submunition, projectile, physics, xform);
        }
    }

    private void SpawnSubmunitions(EntityUid uid, SubmunitionProjectileComponent component, 
        ProjectileComponent projectile, PhysicsComponent physics, TransformComponent xform)
    {
        component.SubmunitionsSpawned = true;
        Dirty(uid, component);
        
        _adminLogger.Add(LogType.Action, LogImpact.Medium,
            $"Submunitions deployed from {ToPrettyString(uid):projectile} - spawned {component.SubmunitionCount} submunitions of type {component.SubmunitionPrototype}");

        var currentPosition = _transform.GetWorldPosition(xform);
        var currentVelocity = physics.LinearVelocity;
        
        // Get direction from velocity (actual travel direction)
        var baseDirection = GetTravelDirection(currentVelocity, xform);
        var baseAngle = baseDirection.ToWorldAngle();

        // Calculate spread for multiple submunitions
        var spreadPerSub = component.SpreadAngle / Math.Max(1, component.SubmunitionCount - 1);
        var startAngle = baseAngle - Angle.FromDegrees(component.SpreadAngle / 2f);

        for (int i = 0; i < component.SubmunitionCount; i++)
        {
            var (subAngle, subDirection) = GetSubmunitionDirection(
                component.SubmunitionCount, i, baseAngle, baseDirection, startAngle, spreadPerSub);

            var spawnPosition = currentPosition + (subDirection * 0.3f);
            var subUid = Spawn(component.SubmunitionPrototype, new MapCoordinates(spawnPosition, xform.MapID));

            SetupSubmunition(subUid, projectile, currentVelocity, subDirection, subAngle, component.VelocityMultiplier);
        }

        QueueDel(uid);
    }

    /// <summary>
    /// Gets the travel direction from velocity, falling back to transform rotation if velocity is too small.
    /// </summary>
    private Vector2 GetTravelDirection(Vector2 velocity, TransformComponent xform)
    {
        if (velocity.LengthSquared() > 0.01f)
            return velocity.Normalized();
        
        return _transform.GetWorldRotation(xform).ToVec();
    }

    /// <summary>
    /// Calculates the direction and angle for a submunition based on spread settings.
    /// </summary>
    private (Angle angle, Vector2 direction) GetSubmunitionDirection(
        int count, int index, Angle baseAngle, Vector2 baseDirection, Angle startAngle, float spreadPerSub)
    {
        if (count == 1)
            return (baseAngle, baseDirection);
        
        var subAngle = startAngle + Angle.FromDegrees(spreadPerSub * index);
        return (subAngle, subAngle.ToVec());
    }

    /// <summary>
    /// Sets up a spawned submunition with proper velocity, rotation, and projectile metadata.
    /// </summary>
    private void SetupSubmunition(EntityUid subUid, ProjectileComponent parentProjectile, 
        Vector2 parentVelocity, Vector2 direction, Angle angle, float velocityMultiplier)
    {
        // Transfer shooter/weapon info
        if (TryComp<ProjectileComponent>(subUid, out var subProjectile))
        {
            subProjectile.Shooter = parentProjectile.Shooter;
            subProjectile.Weapon = parentProjectile.Weapon;
            Dirty(subUid, subProjectile);
        }

        // Set velocity and rotation
        if (TryComp<PhysicsComponent>(subUid, out var subPhysics))
        {
            var baseSpeed = parentVelocity.Length();
            var subSpeed = baseSpeed * velocityMultiplier;
            _physics.SetLinearVelocity(subUid, direction * subSpeed, body: subPhysics);
        }

        if (TryComp<TransformComponent>(subUid, out var subXform))
        {
            _transform.SetWorldRotation(subXform, angle);
        }
    }
}
