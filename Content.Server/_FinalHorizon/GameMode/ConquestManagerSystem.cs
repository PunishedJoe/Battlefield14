using Content.Server._FinalHorizon.GameMode.Components;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Popups;
using Content.Server.RoundEnd;
using Content.Shared._FinalHorizon.GameMode;
using Content.Shared.Chat;
using Content.Server.Chat.Systems;
using Content.Shared.Communications;
using Content.Shared.DoAfter;
using Content.Shared.GameTicking;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server._FinalHorizon.GameMode;

public sealed partial class ConquestManagerSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public ConquestManagerComponent? Manager;
    public TimeSpan NextCheck = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ConquestManagerComponent, ComponentInit>(OnManagerStart);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundEnd);
        SubscribeLocalEvent<CapturePointComponent, ActivateInWorldEvent>(OnActivation);
        SubscribeLocalEvent<CapturePointComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs);
        SubscribeLocalEvent<CapturePointComponent, CapturePointDoAfter>(TryCapture);
    }

    private void OnActivation(EntityUid uid, CapturePointComponent comp, ActivateInWorldEvent args)
    {
        if (!TryStartCapture(args.User, comp))
            return;

        var doAfter = new DoAfterArgs(EntityManager, args.User, comp.DoAfterTime, new CapturePointDoAfter(), target: uid, eventTarget: uid)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            MovementThreshold = 0.5f,
            CancelDuplicate = true
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnGetVerbs(EntityUid uid, CapturePointComponent comp, GetVerbsEvent<ActivationVerb> args)
    {
        var verb = new ActivationVerb()
        {
            Text = "Capture Point",
            Act = () =>
            {
                if (!TryStartCapture(args.User, comp))
                    return;

                var doAfter = new DoAfterArgs(EntityManager, args.User, comp.DoAfterTime, new CapturePointDoAfter(), target: uid, eventTarget: uid)
                {
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    MovementThreshold = 0.5f,
                    CancelDuplicate = true
                };

                _doAfter.TryStartDoAfter(doAfter);
            },
        };

        args.Verbs.Add(verb);
    }

    private bool TryStartCapture(EntityUid user, CapturePointComponent comp)
    {
        if (!TryComp<GameFactionMemberComponent>(user, out var factionComp) ||
            factionComp.Faction == GameFactions.Invalid)
        {
            _popup.PopupEntity($"Invalid or no faction", comp.Owner, user, PopupType.SmallCaution);
            return false;
        }

        if (comp.PointOwner == factionComp.Faction)
        {
            _popup.PopupEntity($"Already own this capture point", comp.Owner, user, PopupType.SmallCaution);
            return false;
        }

        _popup.PopupEntity($"Starting capture", comp.Owner, user, PopupType.Small);
        return true;
    }

    private void TryCapture(EntityUid uid, CapturePointComponent comp, CapturePointDoAfter args)
    {
        if (!TryComp<GameFactionMemberComponent>(args.User, out var factionComp) ||
            factionComp.Faction == GameFactions.Invalid ||
            comp.PointOwner == factionComp.Faction)
            return;

        comp.PointOwner = factionComp.Faction;
        _chat.DispatchGlobalAnnouncement($"Point {comp.PointName} has been captured by the {factionComp.FactionName}.", null, false, null, Color.Yellow);

        if (NextCheck == TimeSpan.Zero && Manager != null)
        {
            NextCheck = _timing.CurTime + Manager.CheckFrequency;
        }
    }

    public void FactionWin(GameFactions faction)
    {
        if (Manager != null)
            Manager.Enabled = false;

        _chat.DispatchGlobalAnnouncement($"{faction} win!", null, false, null, Color.Yellow);
        _ticker.EndRound($"{faction} win.");
    }

    private void OnManagerStart(EntityUid uid, ConquestManagerComponent comp, ComponentInit args)
    {
        Manager = comp;
        Manager.Enabled = true;
        NextCheck = TimeSpan.Zero;
    }

    private void OnRoundEnd(RoundRestartCleanupEvent args)
    {
        Manager = null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (Manager == null || Manager.Enabled == false)
            return;

        var points = EntityQuery<CapturePointComponent>();

        foreach (var point in points)
        {
            var faction = point.PointOwner;
            if (faction == GameFactions.Invalid)
                continue;

            if (!Manager.Tickets.TryGetValue(faction, out _))
                Manager.Tickets.TryAdd(faction, 1f);
            Manager.Tickets[faction] += point.TicketValue * frameTime;
        }

        if (Manager.Tickets.Count > 0)
        {
            var winner = Manager.Tickets.Max();

            if (_timing.CurTime > NextCheck)
            {
                var standings = string.Join(
                    "\n",
                    Manager.Tickets
                        .OrderByDescending(x => x.Value)
                        .Select(x => $"Team {x.Key}: {x.Value:F0}/{Manager.MaxTickets}")
                );

                _chat.DispatchGlobalAnnouncement(
                    $"Conquest standings:\n{standings}",
                    null,
                    false,
                    null,
                    Color.Yellow);

                NextCheck = _timing.CurTime + Manager.CheckFrequency;
            }

            if (winner.Value >= Manager.MaxTickets)
                FactionWin(winner.Key);
        }
    }
}
