using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared.POICapture;
using Content.Shared.Factions;
using Content.Shared.DoAfter;
using Content.Shared.Inventory;
using Robust.Shared.Player;
using Robust.Server.GameObjects;

namespace Content.Server.POICapture;

public sealed class POICaptureSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<POICaptureComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<POICaptureComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<POICaptureComponent, POICaptureDoAfterEvent>(OnPOICaptureComplete);

        _sawmill = Logger.GetSawmill("poi.capture");
    }

    private void OnComponentInit(EntityUid uid, POICaptureComponent component, ComponentInit args)
    {
        component.IsBeingCaptured = false;
        component.CapturingFaction = null;
        component.CurrentCaptureProgress = 0f;

        _sawmill.Debug($"POI '{component.POIName}' initialized with {component.TimeToCapture}s capture time");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<POICaptureComponent>();
        while (query.MoveNext(out var uid, out var poi))
        {
            // Count down grace period
            if (poi.GracePeriod > 0f)
            {
                poi.GracePeriod -= frameTime;
                Dirty(uid, poi);
                continue;
            }

            if (!poi.IsBeingCaptured || poi.CapturingFaction == null)
                continue;

            poi.CurrentCaptureProgress += frameTime;
            Dirty(uid, poi);

            if (poi.CurrentCaptureProgress >= poi.TimeToCapture)
            {
                CompletePOICapture(uid, poi);
            }
        }
    }

    private void OnInteractHand(EntityUid uid, POICaptureComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        // Check grace period
        if (component.GracePeriod > 0)
        {
            var timeLeft = TimeSpan.FromSeconds(component.GracePeriod);
            _popup.PopupEntity($"This area is protected for {timeLeft:mm\\:ss} more.", uid, args.User);
            return;
        }

        // Get user's faction - check worn clothing first, then the entity itself
        var userFaction = GetEntityFaction(args.User);
        
        if (string.IsNullOrEmpty(userFaction))
        {
            _popup.PopupEntity("You need to be part of a faction to capture territory.", uid, args.User);
            return;
        }

        // Already controlled by this faction and not being contested
        if (component.ControllingFaction == userFaction && !component.IsBeingCaptured)
        {
            _popup.PopupEntity($"Your faction already controls {component.POIName}.", uid, args.User);
            return;
        }

        // Determine action text for announcement
        string actionText;
        if (component.ControllingFaction == userFaction && component.IsBeingCaptured)
            actionText = "defending";
        else if (component.ControllingFaction == null)
            actionText = "claiming";
        else
            actionText = "capturing";

        _popup.PopupEntity($"Interfacing with {component.POIName} systems...", uid, args.User);

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, component.DoAfterDelay, new POICaptureDoAfterEvent(userFaction, actionText), uid, uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnPOICaptureComplete(EntityUid uid, POICaptureComponent component, POICaptureDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            _popup.PopupEntity("Capture interface interrupted.", uid, args.User);
            return;
        }

        var userFaction = GetEntityFaction(args.User);
        
        if (string.IsNullOrEmpty(userFaction))
        {
            _popup.PopupEntity("You need to be part of a faction to capture territory.", uid, args.User);
            return;
        }

        if (userFaction != args.Faction)
        {
            _popup.PopupEntity("Your faction changed during the interface process.", uid, args.User);
            return;
        }

        // Faction is already capturing - shouldn't happen but check anyway
        if (component.CapturingFaction == userFaction)
        {
            _popup.PopupEntity($"Your faction is already capturing {component.POIName}.", uid, args.User);
            return;
        }

        var timeMinutes = TimeSpan.FromSeconds(component.TimeToCapture).TotalMinutes;
        var timeString = timeMinutes >= 1 
            ? $"{timeMinutes:F1} minutes" 
            : $"{component.TimeToCapture:F0} seconds";

        // Start fresh capture
        if (component.CapturingFaction == null)
        {
            component.CapturingFaction = userFaction;
            component.IsBeingCaptured = true;
            component.CurrentCaptureProgress = 0f;
            Dirty(uid, component);

            _sawmill.Info($"{userFaction} has begun {args.ActionText} {component.POIName}. Will take {timeString}.");
            
            _chat.DispatchGlobalAnnouncement(
                $"{userFaction} has begun {args.ActionText} {component.POIName}. Capture will complete in {timeString}.",
                "Territory Control",
                playSound: true,
                colorOverride: Color.Yellow);

            // Raise event for game rule system to track
            var ev = new POICaptureStartedEvent(uid, userFaction);
            RaiseLocalEvent(ref ev);
        }
        else
        {
            // Interrupt existing capture
            var oldFaction = component.CapturingFaction;
            component.CapturingFaction = userFaction;
            component.CurrentCaptureProgress = 0f;
            Dirty(uid, component);

            _sawmill.Info($"{userFaction} interrupted {oldFaction}'s capture of {component.POIName}");
            
            _chat.DispatchGlobalAnnouncement(
                $"{userFaction} has interrupted {oldFaction}'s capture of {component.POIName}! {userFaction} is now capturing it. Capture will complete in {timeString}.",
                "Territory Control",
                playSound: true,
                colorOverride: Color.Orange);

            // Raise event for game rule system
            var ev = new POICaptureInterruptedEvent(uid, userFaction, oldFaction);
            RaiseLocalEvent(ref ev);
        }
    }

    private void CompletePOICapture(EntityUid uid, POICaptureComponent component)
    {
        var capturingFaction = component.CapturingFaction;
        var previousFaction = component.ControllingFaction;

        component.ControllingFaction = capturingFaction;
        component.IsBeingCaptured = false;
        component.CapturingFaction = null;
        component.CurrentCaptureProgress = 0f;
        Dirty(uid, component);

        if (previousFaction == null)
        {
            _chat.DispatchGlobalAnnouncement(
                $"{capturingFaction} has successfully claimed {component.POIName}!",
                "Territory Control",
                playSound: true,
                colorOverride: Color.Green);
        }
        else
        {
            _chat.DispatchGlobalAnnouncement(
                $"{capturingFaction} has captured {component.POIName} from {previousFaction}!",
                "Territory Control",
                playSound: true,
                colorOverride: Color.Red);
        }

        _sawmill.Info($"POI '{component.POIName}' captured by {capturingFaction}");

        // Raise event for game rule system to check win conditions
        var ev = new POICaptureCompletedEvent(uid, capturingFaction!, previousFaction);
        RaiseLocalEvent(ref ev);
    }

    /// <summary>
    /// Gets the faction of an entity by checking worn clothing first, then the entity itself
    /// </summary>
    private string GetEntityFaction(EntityUid uid)
    {
        // First check if any worn clothing has a faction
        if (_inventory.TryGetSlots(uid, out var slots))
        {
            foreach (var slot in slots)
            {
                if (_inventory.TryGetSlotEntity(uid, slot.Name, out var item) &&
                    TryComp<FactionComponent>(item.Value, out var factionComp) &&
                    !string.IsNullOrEmpty(factionComp.FactionId))
                {
                    return factionComp.FactionId;
                }
            }
        }

        // Fall back to entity's own faction component
        if (TryComp<FactionComponent>(uid, out var entityFaction))
            return entityFaction.FactionId;

        return string.Empty;
    }
}

/// <summary>
/// Raised when a POI capture attempt is started
/// </summary>
[ByRefEvent]
public record struct POICaptureStartedEvent(EntityUid POI, string Faction);

/// <summary>
/// Raised when a POI capture attempt is interrupted by another faction
/// </summary>
[ByRefEvent]
public record struct POICaptureInterruptedEvent(EntityUid POI, string NewFaction, string OldFaction);

/// <summary>
/// Raised when a POI is successfully captured
/// </summary>
[ByRefEvent]
public record struct POICaptureCompletedEvent(EntityUid POI, string CapturingFaction, string? PreviousFaction);
