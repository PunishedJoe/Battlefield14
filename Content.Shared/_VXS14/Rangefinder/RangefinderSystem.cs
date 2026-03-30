using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Content.Shared.Examine;

namespace Content.Shared._VXS14.Rangefinder;

public sealed class SharedRangeFinderSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RangefinderComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<RangefinderComponent, RangefinderDoAfterEvent>(OnDoAfterCompleted);
    }

    private void OnAfterInteract(EntityUid uid, RangefinderComponent component, AfterInteractEvent args)
    {
        if (!_examine.InRangeUnOccluded(args.User, args.ClickLocation, SharedInteractionSystem.MaxRaycastRange))
            return;

        var netCoords = new NetCoordinates(EntityManager.GetNetEntity(args.ClickLocation.EntityId), args.ClickLocation.Position);
        var doAfterEvent = new RangefinderDoAfterEvent(netCoords);

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, component.MeasureDelay,
                                          doAfterEvent, uid)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnDoAfterCompleted(EntityUid uid, RangefinderComponent component, RangefinderDoAfterEvent args)
    {
        if (args.Cancelled) return;

        var targetEntity = EntityManager.GetEntity(args.Coordinates.NetEntity);
        var targetCoords = new EntityCoordinates(targetEntity, args.Coordinates.Position);
        var userCoords = Transform(args.User).Coordinates;

        var diff = targetCoords.Position - userCoords.Position;
        var distance = (int)diff.Length();

        // Показываем попап с расстоянием
        var message = $"Distance: {distance}m";
        _popup.PopupClient(message, args.User, args.User, PopupType.Medium);
    }
}
