using Content.Client.Eui;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client._VXS14.Mortar;

[UsedImplicitly]
public sealed class BasicCalculatorEui : BaseEui
{
    private readonly BasicCalculatorWindow _window;

    public BasicCalculatorEui()
    {
        _window = new BasicCalculatorWindow();
        _window.OnClose += SendClosedMessage;
    }

    public override void Opened()
    {
        base.Opened();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window.OnClose -= SendClosedMessage;
        _window.Close();
    }

    private void SendClosedMessage()
    {
        SendMessage(new CloseEuiMessage());
    }
}
