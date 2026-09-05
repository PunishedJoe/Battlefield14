using Content.Client.CombatMode;
using Content.Client.Hands.Systems;
using Content.Client._RMC14.Emplacements;
using Content.Shared._RMC14.CombatMode;
using Robust.Client.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.Client._RMC14.CombatMode;

public sealed class RMCCombatModeUISystem : EntitySystem
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly CombatModeSystem _combatMode = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly RMCCombatModeSystem _rmcCombatMode = default!;
    [Dependency] private readonly RMCWeaponControllerSystem _rmcWeaponController = default!;

    private ICursor? _crosshairCursor;

    public override void FrameUpdate(float frameTime)
    {
        if (!_combatMode.IsInCombatMode())
        {
            _clyde.SetCursor(null);
            return;
        }

        var held = _hands.GetActiveHandEntity();
        if (_rmcWeaponController.TryGetControllingWeapon(out var weapon))
            held = weapon;

        if (held == null || _rmcCombatMode.GetCrosshair(held.Value) == null)
        {
            _clyde.SetCursor(null);
            return;
        }

        _crosshairCursor ??= _clyde.CreateCursor(new Image<Rgba32>(1, 1), Vector2i.One);
        _clyde.SetCursor(_crosshairCursor);
    }
}
