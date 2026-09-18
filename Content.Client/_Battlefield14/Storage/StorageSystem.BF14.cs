using Content.Client._Battlefield14.Storage;

namespace Content.Client.Storage.Systems;

public sealed partial class StorageSystem
{
    [Dependency] private readonly StorageAnimationSystem _storageAnimation = default!;

    /// <inheritdoc />
    protected override void PlayStorageAnimation(EntityUid uid, EntityUid? user)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        _storageAnimation.Play(uid);
    }
}
