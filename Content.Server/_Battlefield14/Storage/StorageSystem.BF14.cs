using Content.Shared._Battlefield14.Storage;
using Robust.Shared.Player;

namespace Content.Server.Storage.EntitySystems;

public sealed partial class StorageSystem
{
    /// <inheritdoc />
    protected override void PlayStorageAnimation(EntityUid uid, EntityUid? user)
    {
        var filter = Filter.Pvs(uid).RemoveWhereAttachedEntity(e => e == user);
        RaiseNetworkEvent(new StorageAnimationEvent(GetNetEntity(uid)), filter);
    }
}
