using Content.Server.EUI;
using Content.Server.Explosion.EntitySystems;
//using Content.Server.ArtilleryDetection.Systems;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Content.Server._VXS14.Mortar;
using Content.Shared._VXS14.Mortar;
using Robust.Shared.Map;
using Robust.Shared.GameObjects;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Content.Shared.Audio;
using System.Numerics;
using Robust.Shared.Player;
using Robust.Shared.Audio;

namespace Content.Server._VXS14.Mortar;

/// <summary>
///     Mortar Eui
/// </summary>
///


[UsedImplicitly]
public sealed class MortarEui : BaseEui
{
    private int Count = 0;
    private readonly EntityUid Mortar;

    public MortarEui(EntityUid uid)
    {
        Mortar = uid;
    }

    public override void Opened()
    {
        base.Opened();

        // Send mortar configuration to the client
        var entMan = IoCManager.Resolve<IEntityManager>();
        var mortarComp = entMan.GetComponent<SharedMortarComponent>(Mortar);
        SendMessage(new MortarSpawnExplosionEuiMsg.MortarConfig(
            mortarComp.MinOffsetX,
            mortarComp.MaxOffsetX,
            mortarComp.MinOffsetY,
            mortarComp.MaxOffsetY,
            mortarComp.MinSafeDistance));
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not MortarSpawnExplosionEuiMsg.MortarCords request)
        {
            Close();
            return;
        }

        // Get the mortar's position
        var entMan = IoCManager.Resolve<IEntityManager>();
        var transformSystem = entMan.System<SharedTransformSystem>();
        var mortarPosition = transformSystem.GetMapCoordinates(Mortar);

        // Calculate the target position based on offsets
        var targetPosition = new MapCoordinates(
            new Vector2(
                mortarPosition.X + request.OffsetX,
                mortarPosition.Y + request.OffsetY),
            mortarPosition.MapId);

        // Prevent shooting at too close a range (use mortar's minimum safe distance)
        var distanceFromMortar = (targetPosition.Position - mortarPosition.Position).Length();
        var mortarComp = entMan.GetComponent<SharedMortarComponent>(Mortar);
        var minDistance = mortarComp.MinSafeDistance;
        if (distanceFromMortar < minDistance)
        {
            // Adjust target to minimum distance in the same direction
            var direction = targetPosition.Position - mortarPosition.Position;
            if (direction.Length() > 0)
            {
                direction = Vector2.Normalize(direction);
                var adjustedPosition = mortarPosition.Position + direction * minDistance;
                targetPosition = new MapCoordinates(adjustedPosition, mortarPosition.MapId);
            }
            else
            {
                // If direction is zero, set a default offset to the right
                targetPosition = new MapCoordinates(
                    new Vector2(mortarPosition.X + minDistance, mortarPosition.Y),
                    mortarPosition.MapId);
            }
        }

        // Dumb code
        var sysMan = IoCManager.Resolve<IEntitySystemManager>();
        var itemSlots = sysMan.GetEntitySystem<ItemSlotsSystem>();

        var rocket = itemSlots.GetItemOrNull(Mortar, "mortar_chamber");
        Logger.InfoS("mortar", $"Получен снаряд из каморы: {rocket}");

        if (rocket == null)
        {
            Logger.WarningS("mortar", "Нет снаряда в каморе миномёта!");
            Close();
            return;
        }

        Logger.InfoS("mortar", $"Попытка получения SharedMortarShellComponent для {rocket}");
        entMan.TryGetComponent<SharedMortarShellComponent>(rocket, out var comp);
        Logger.InfoS("mortar", $"Компонент получен: {comp != null}");

        // Play fire sound at mortar position
        if (comp?.FireSound != null)
        {
            var audioSystem = sysMan.GetEntitySystem<SharedAudioSystem>();
            var mortarCoords = entMan.GetComponent<TransformComponent>(Mortar).Coordinates;
            audioSystem.PlayPvs(new SoundPathSpecifier(comp.FireSound), mortarCoords);
        }

        // Calculate distance for delay
        var distance = (targetPosition.Position - mortarPosition.Position).Length();
        var delay = (int)(distance * (comp?.DelayPerTile ?? 0.1f) * 1000); // Convert to milliseconds

        // Schedule the explosion with delay
        var timerManager = IoCManager.Resolve<ITimerManager>();
        timerManager.AddTimer(new Timer(delay, false, () =>
        {
            // Play pre-explosion sound at target position
            if (comp?.PreExplosionSound != null)
            {
                var audioSystem = sysMan.GetEntitySystem<SharedAudioSystem>();
                var mapSystem = sysMan.GetEntitySystem<SharedMapSystem>();
                var mapEntity = mapSystem.GetMapOrInvalid(targetPosition.MapId);
                var targetCoords = transformSystem.ToCoordinates(mapEntity, targetPosition);
                audioSystem.PlayPvs(new SoundPathSpecifier(comp.PreExplosionSound), targetCoords);
            }

            // Add a small delay before the actual explosion
            timerManager.AddTimer(new Timer(500, false, () =>
            {
                Logger.InfoS("mortar", "=== ТАЙМЕР СРАБОТАЛ ===");
                Logger.InfoS("mortar", $"Время срабатывания: {IoCManager.Resolve<IGameTiming>().CurTime}");
                Logger.InfoS("mortar", $"Rocket entity: {rocket}");
                Logger.InfoS("mortar", $"Target position: {targetPosition}");

                // Get shell name BEFORE deleting the entity
                var shellName = "Снаряд";
                if (rocket != null && entMan.TryGetComponent<MetaDataComponent>(rocket.Value, out var shellMetaData))
                {
                    shellName = shellMetaData.EntityName ?? "Снаряд";
                }
                else if (rocket != null)
                {
                    shellName = "Неизвестный снаряд";
                }

                entMan.DeleteEntity(rocket);

                Logger.InfoS("mortar", $"Проверка компонента снаряда для entity {rocket}");
                Logger.InfoS("mortar", $"Rocket.HasValue: {rocket.HasValue}");

                if (!rocket.HasValue)
                {
                    Logger.ErrorS("mortar", "Rocket is null!");
                    return;
                }

                // Apply distance-based accuracy scaling and handle explosion/entity spawning
                if(comp != null)
                {
                    Logger.InfoS("mortar", "Компонент снаряда найден, продолжаем обработку");
                    Logger.InfoS("mortar", $"UseDirectExplosion: {comp.UseDirectExplosion}, ExplosionEntity: {comp.ExplosionEntity}");
                    // Get mortar component for accuracy parameters
                    var mortarComp = entMan.GetComponent<SharedMortarComponent>(Mortar);

                    // Calculate distance for accuracy scaling
                    var distance = (targetPosition.Position - mortarPosition.Position).Length();

                    // Calculate accuracy modifier (decreases with distance)
                    var accuracyModifier = Math.Max(0.1f, mortarComp.BaseAccuracy - (distance * mortarComp.AccuracyDegradation));

                    // Register artillery detection BEFORE spawning/explosion
                    Logger.InfoS("mortar", "=== ПОПЫТКА РЕГИСТРАЦИИ АРТИЛЛЕРИЙСКОГО ВЫСТРЕЛА ===");
                    Logger.InfoS("mortar", $"Цель: {targetPosition}");
                    Logger.InfoS("mortar", $"Время: {IoCManager.Resolve<IGameTiming>().CurTime}");

//                    var artillerySystem = sysMan.GetEntitySystem<ArtilleryDetectionSystem>();
//                    Logger.InfoS("mortar", $"Получена ссылка на ArtilleryDetectionSystem: {artillerySystem != null}");
//
//                    if (artillerySystem == null)
//                    {
//                        Logger.ErrorS("mortar", "ArtilleryDetectionSystem равна null!");
//                        return;
//                    }
//
//                    // Test the system
//                    try
//                    {
//                        artillerySystem.TestMethod();
//                    }
//                    catch (Exception ex)
//                    {
//                        Logger.ErrorS("mortar", $"Ошибка при вызове TestMethod: {ex}");
//                    }

                    var mortarName = "Миномет";
                    if (entMan.TryGetComponent<MetaDataComponent>(Mortar, out var metaData))
                    {
                        mortarName = metaData.EntityName ?? "Миномет";
                    }

//                    var weaponType = $"{mortarName} ({shellName})";
//                    Logger.InfoS("mortar", $"Тип оружия: {weaponType}");
//
//                    artillerySystem.OnArtilleryFired(targetPosition, weaponType, IoCManager.Resolve<IGameTiming>().CurTime, mortarName, shellName);
//                    Logger.InfoS("mortar", "=== ВЫЗОВ OnArtilleryFired ЗАВЕРШЕН ===");

                    if (comp.UseDirectExplosion)
                    {
                        // Apply accuracy modifier to explosion parameters
                        var adjustedTotalIntensity = comp.TotalIntensity * accuracyModifier;
                        var adjustedSlope = comp.Slope * accuracyModifier;
                        var adjustedMaxTileIntensity = comp.MaxTileIntensity * accuracyModifier;

                        sysMan.GetEntitySystem<ExplosionSystem>().QueueExplosion(targetPosition, comp.Type, adjustedTotalIntensity, adjustedSlope, adjustedMaxTileIntensity, null);
                    }
                    else if (!string.IsNullOrEmpty(comp.ExplosionEntity))
                    {
                        Logger.InfoS("mortar", $"Использование ExplosionEntity вместо DirectExplosion: {comp.ExplosionEntity}");
                        // Spawn the specified entity at the target position
                        var spawnedEntity = entMan.SpawnEntity(comp.ExplosionEntity, targetPosition);

                        // Apply accuracy modifier to the spawned entity if it has a damage component
                        // This is a simplified approach - more complex implementations might need specific component handling
                        if (accuracyModifier < 1.0f)
                        {
                            // TODO: Apply accuracy modifier to spawned entity effects if needed
                        }
                    }
                    else
                    {
                        Logger.WarningS("mortar", "Снаряд не имеет ни UseDirectExplosion, ни ExplosionEntity!");
                    }
                }
            }));
        }));

        Close();
    }
}
