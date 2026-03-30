using Content.Server.Atmos.EntitySystems;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared._Battlefield14.Projectiles.Components;
using Content.Shared.Explosion.Components.OnTrigger;

namespace Content.Server._Battlefield14.Projectiles;

/// <summary>
/// System for releasing plasma gas when triggered.
/// </summary>
public sealed class ReleasePlasmaOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ReleasePlasmaOnTriggerComponent, TriggerEvent>(OnTriggered);
    }

    private void OnTriggered(EntityUid uid, ReleasePlasmaOnTriggerComponent component, TriggerEvent args)
    {
        var environment = _atmosphereSystem.GetContainingMixture(uid, false, true);
        if (environment == null)
            return;

        var plasmaMixture = new GasMixture(1) { Temperature = component.PlasmaTemperature };
        plasmaMixture.SetMoles(Gas.Plasma, component.PlasmaAmount);
        _atmosphereSystem.Merge(environment, plasmaMixture);
    }
}



