//BF14 using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Barricade.Components;
//BF14 using Content.Shared._RMC14.Construction.Upgrades;
//BF14 using Content.Shared._RMC14.Xenonids.Leap;
//BF14 using Content.Shared._RMC14.Xenonids.Acid;
using Content.Shared.Climbing.Events;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Doors;
using Content.Shared.Doors.Components;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Content.Shared.Examine;

namespace Content.Shared._RMC14.Barricade;

public abstract class SharedBarbedSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly FixtureSystem _fixture = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedStackSystem _stacks = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetManager _netManager = default!;
//BF14    [Dependency] private readonly SharedXenoAcidSystem _xenoAcid = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BarbedComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<BarbedComponent, AttackedEvent>(OnAttacked);
        SubscribeLocalEvent<BarbedComponent, InteractUsingEvent>(OnInteractUsing);

        SubscribeLocalEvent<BarbedComponent, DoAfterAttemptEvent<BarbedDoAfterEvent>>(OnDoAfterAttempt);
//BF14        SubscribeLocalEvent<BarbedComponent, BarbedDoAfterEvent>(OnDoAfter);

//BF14        SubscribeLocalEvent<BarbedComponent, CutBarbedDoAfterEvent>(WireCutterOnDoAfter);
//BF14        SubscribeLocalEvent<BarbedComponent, DoorStateChangedEvent>(OnDoorStateChanged);
        SubscribeLocalEvent<BarbedComponent, AttemptClimbEvent>(OnClimbAttempt);
//BF14        SubscribeLocalEvent<BarbedComponent, CMGetArmorPiercingEvent>(OnGetArmorPiercing);
//BF14        SubscribeLocalEvent<BarbedComponent, RMCConstructionUpgradedEvent>(OnConstructionUpgraded);
//BF14        SubscribeLocalEvent<BarbedComponent, XenoLeapHitAttempt>(OnXenoLeapHitAttempt, after: new[] { typeof(XenoLeapSystem) });
    }

    private void OnExamined(Entity<BarbedComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.IsBarbed)
            return;

        using (args.PushGroup(nameof(SharedBarbedSystem)))
        {
            args.PushMarkup(Loc.GetString("rmc-barricade-examine-barbed"));
        }
    }

    private void OnAttacked(Entity<BarbedComponent> barbed, ref AttackedEvent args)
    {
        if (!barbed.Comp.IsBarbed)
            return;

        _damageableSystem.TryChangeDamage(args.User, barbed.Comp.ThornsDamage, origin: barbed, tool: barbed);
//BF14        _popupSystem.PopupClient(Loc.GetString("barbed-wire-damage"), barbed, args.User, PopupType.SmallCaution);
    }

    private void OnInteractUsing(Entity<BarbedComponent> ent, ref InteractUsingEvent args)
    {
//BF14        if (_xenoAcid.IsMelted(ent))
//BF14        {
//BF14            var failPopup = Loc.GetString("rmc-construction-melted");
//BF14            _popupSystem.PopupClient(failPopup, ent, args.User, PopupType.SmallCaution);
//BF14
//BF14            args.Handled = true;
//BF14            return;
//BF14        }

        if (!ent.Comp.IsBarbed && HasComp<BarbedWireComponent>(args.Used))
        {
            var ev = new BarbedDoAfterEvent();
            var barbDoAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.WireTime, ev, ent, ent, used: args.Used)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                NeedHand = true,
                AttemptFrequency = AttemptFrequency.EveryTick,
                CancelDuplicate = false,
                DuplicateCondition = DuplicateConditions.SameTarget,
            };

            if (_doAfterSystem.TryStartDoAfter(barbDoAfter))
            {
                args.Handled = true;
                _popupSystem.PopupClient(Loc.GetString("barbed-wire-slot-wiring"), ent, args.User);
            }

            return;
        }

        if (ent.Comp.IsBarbed && HasComp<BarbedWireComponent>(args.Used))
        {
            args.Handled = true;
            _popupSystem.PopupClient(Loc.GetString("barbed-wire-slot-insert-full"), ent, args.User);
            return;
        }

        if (!ent.Comp.IsBarbed || !TryComp<ToolComponent>(args.Used, out var tool))
            return;

        if (!_toolSystem.HasQuality(args.Used, ent.Comp.RemoveQuality, tool))
            return;

        args.Handled = true;
        _popupSystem.PopupClient(Loc.GetString("barbed-wire-cutting-action-begin"), ent, args.User);
        var cutDoAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.CutTime, new CutBarbedDoAfterEvent(), ent, used: args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };
        _doAfterSystem.TryStartDoAfter(cutDoAfter);
    }

    private void OnDoAfterAttempt(Entity<BarbedComponent> barbed, ref DoAfterAttemptEvent<BarbedDoAfterEvent> args)
    {
        // If the targeted entity gets barbed during the doafter, end the doafter
        if (barbed.Comp.IsBarbed)
            args.Cancel();
    }

//BF14    private void OnDoAfter(Entity<BarbedComponent> barbed, ref BarbedDoAfterEvent args)
//BF14    {
//BF14        if (args.Used == null || args.Cancelled || args.Handled)
//BF14            return;
//BF14
//BF14        // If the targeted entity gets barbed during the doafter, don't use up a barbed wire
//BF14        if (barbed.Comp.IsBarbed)
//BF14            return;
//BF14
//BF14        args.Handled = true;
//BF14
//BF14        if (TryComp<StackComponent>(args.Used.Value, out var stackComp))
//BF14            _stacks.Use(args.Used.Value, 1, stackComp);
//BF14
//BF14        barbed.Comp.IsBarbed = true;
//BF14        Dirty(barbed);
//BF14        UpdateBarricade(barbed, true);
//BF14
//BF14        _audio.PlayPredicted(barbed.Comp.BarbSound, barbed.Owner, args.User);
//BF14        _popupSystem.PopupClient(Loc.GetString("barbed-wire-slot-insert-success"), barbed.Owner, args.User);
//BF14    }
//BF14
//BF14    private void WireCutterOnDoAfter(Entity<BarbedComponent> barbed, ref CutBarbedDoAfterEvent args)
//BF14    {
//BF14        if (args.Cancelled || args.Handled)
//BF14            return;
//BF14
//BF14        args.Handled = true;
//BF14
//BF14        barbed.Comp.IsBarbed = false;
//BF14        Dirty(barbed);
//BF14        UpdateBarricade(barbed, true);
//BF14
//BF14        _audio.PlayPredicted(barbed.Comp.CutSound, barbed.Owner, args.User);
//BF14        _popupSystem.PopupClient(Loc.GetString("barbed-wire-cutting-action-finish"), barbed.Owner, args.User);
//BF14
//BF14        if (_netManager.IsClient)
//BF14            return;
//BF14
//BF14        var coordinates = _transform.GetMoverCoordinates(barbed);
//BF14        EntityManager.SpawnEntity(barbed.Comp.Spawn, coordinates);
//BF14    }
//BF14
//BF14    private void OnDoorStateChanged(Entity<BarbedComponent> barbed, ref DoorStateChangedEvent args)
//BF14    {
//BF14        UpdateBarricade(barbed);
//BF14    }

    private void OnClimbAttempt(Entity<BarbedComponent> barbed, ref AttemptClimbEvent args)
    {
        if (barbed.Comp.IsBarbed)
        {
            args.Cancelled = true;
            _popupSystem.PopupClient(Loc.GetString("barbed-wire-cant-climb"), barbed.Owner, args.User);
        }
    }

//BF14    private void OnGetArmorPiercing(Entity<BarbedComponent> barbed, ref CMGetArmorPiercingEvent args)
//BF14    {
//BF14        if (barbed.Comp.IsBarbed)
//BF14            args.Piercing = 1000;
//BF14    }

//BF14    private void OnConstructionUpgraded(Entity<BarbedComponent> barbed, ref RMCConstructionUpgradedEvent args)
//BF14    {
//BF14        var newComp = EnsureComp<BarbedComponent>(args.New);
//BF14        newComp.IsBarbed = barbed.Comp.IsBarbed;
//BF14
//BF14        Dirty(args.New, newComp);
//BF14        UpdateBarricade((args.New, newComp), true);
//BF14    }
//BF14
//BF14    private void OnXenoLeapHitAttempt(Entity<BarbedComponent> ent, ref XenoLeapHitAttempt args)
//BF14    {
//BF14        if (!ent.Comp.IsBarbed)
//BF14            return;
//BF14
//BF14        _damageableSystem.TryChangeDamage(args.Leaper, ent.Comp.ThornsDamage, origin: ent, tool: ent);
//BF14    }

//Bf14    protected void UpdateBarricade(Entity<BarbedComponent> barbed, bool updateBarbed = false)
//Bf14    {
//Bf14        var open = TryComp(barbed, out DoorComponent? door) && door.State == DoorState.Open;
//Bf14
//Bf14        var visual = (barbed.Comp.IsBarbed, open) switch
//Bf14        {
//Bf14            (true, true) => BarbedWireVisuals.WiredOpen,
//Bf14            (true, false) => BarbedWireVisuals.WiredClosed,
//Bf14            _ => BarbedWireVisuals.UnWired,
//Bf14        };
//Bf14
//Bf14        if (updateBarbed)
//Bf14        {
//Bf14            var ev = new BarbedStateChangedEvent();
//Bf14            RaiseLocalEvent(barbed, ref ev);
//Bf14        }
//Bf14
//Bf14        // Set fixtures
//Bf14        if (_fixture.GetFixtureOrNull(barbed, barbed.Comp.FixtureId) is { } fixture)
//Bf14        {
//Bf14            if (barbed.Comp.IsBarbed)
//Bf14                _physics.AddCollisionLayer(barbed, barbed.Comp.FixtureId, fixture, (int)CollisionGroup.BarbedBarricade);
//Bf14            else
//Bf14                _physics.RemoveCollisionLayer(barbed, barbed.Comp.FixtureId, fixture, (int)CollisionGroup.BarbedBarricade);
//Bf14        }
//Bf14
//Bf14        _appearance.SetData(barbed, BarbedWireVisualLayers.Wire, visual);
//Bf14    }
}

[ByRefEvent]
public record struct BarbedStateChangedEvent;
