using Content.Shared._RMC14.Emplacements;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.CombatMode;

public sealed class RMCCombatModeSystem : EntitySystem
{
    public SpriteSpecifier.Rsi? GetCrosshair(Entity<WieldedCrosshairComponent?, WieldableComponent?> crosshair)
    {
        if (!Resolve(crosshair, ref crosshair.Comp1, false))
            return null;

        if (!Resolve(crosshair, ref crosshair.Comp2, false))
        {
            if (TryComp(crosshair.Owner, out MountableWeaponComponent? mountable) && mountable.MountedTo != null)
                return crosshair.Comp1?.Rsi;

            return null;
        }

        if (!crosshair.Comp2.Wielded)
            return null;

        return crosshair.Comp1.Rsi;
    }
}
