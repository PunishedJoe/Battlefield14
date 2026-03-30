using Content.Server.EUI;
using JetBrains.Annotations;

namespace Content.Server._VXS14.Mortar;

[UsedImplicitly]
public sealed class BasicCalculatorEui : BaseEui
{
    private readonly EntityUid _calculator;

    public BasicCalculatorEui(EntityUid calculator)
    {
        _calculator = calculator;
    }
}
