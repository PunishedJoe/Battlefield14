using Content.Server.Repairable;
using Content.Shared._RMC14.Emplacements;

namespace Content.Server._RMC14.Emplacements;

public sealed class WeaponMountSystem : SharedWeaponMountSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WeaponMountComponent, RepairedEvent>(OnRepaired);
    }

    private void OnRepaired(Entity<WeaponMountComponent> ent, ref RepairedEvent args)
    {
        if (!ent.Comp.Broken)
            return;

        ent.Comp.Broken = false;
        Dirty(ent);
        UpdateAppearance(ent);
    }
}
