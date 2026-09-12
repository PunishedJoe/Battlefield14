using Content.Shared.Whitelist;

namespace Content.Server.Explosion.Components
{
    /// <summary>
    ///     Raises a <see cref="TriggerEvent"/> when the owner impacts another entity as a projectile,
    ///     unless the entity that was hit is in the blacklist.
    /// </summary>
    [RegisterComponent]
    public sealed partial class TriggerOnImpactComponent : Component
    {
        /// <summary>
        ///     Entities that match this blacklist will not trigger the impact.
        /// </summary>
        [DataField]
        public EntityWhitelist? Blacklist;
    }
}
