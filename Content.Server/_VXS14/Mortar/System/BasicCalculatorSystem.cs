using Content.Server.EUI;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared._VXS14.Mortar;
using Content.Shared.Verbs;
using Robust.Server.Player;

namespace Content.Server._VXS14.Mortar;

public sealed class BasicCalculatorSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BasicCalculatorComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<BasicCalculatorComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerb);
    }

    private void OnUseInHand(EntityUid uid, BasicCalculatorComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        OpenCalculator(uid, args.User);
        args.Handled = true;
    }

    private void OnGetExamineVerb(EntityUid uid, BasicCalculatorComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        args.Verbs.Add(new ExamineVerb
        {
            Text = "Open calculator",
            Act = () => OpenCalculator(uid, args.User),
        });
    }

    private void OpenCalculator(EntityUid calculatorUid, EntityUid user)
    {
        if (!_playerManager.TryGetSessionByEntity(user, out var session))
            return;

        var eui = IoCManager.Resolve<EuiManager>();
        eui.OpenEui(new BasicCalculatorEui(calculatorUid), session);
    }
}
