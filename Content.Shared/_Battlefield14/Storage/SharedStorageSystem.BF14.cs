namespace Content.Shared.Storage.EntitySystems;

public abstract partial class SharedStorageSystem
{
    /// <summary>
    /// Plays the storage "rustle" animation (a quick squash-and-stretch of the storage's sprite) if the storage
    /// has <see cref="StorageComponent.Animated"/> enabled. Predicted for <paramref name="user"/> and networked
    /// to everyone else in PVS range.
    /// </summary>
    protected void AnimateStorage(Entity<StorageComponent> storage, EntityUid? user = null)
    {
        if (!storage.Comp.Animated)
            return;

        PlayStorageAnimation(storage, user);
    }

    /// <summary>
    /// Plays the insert sound and rustle animation for <paramref name="storage"/>.
    /// </summary>
    private void PlayInsertFeedback(Entity<StorageComponent> storage, EntityUid? user)
    {
        Audio.PlayPredicted(storage.Comp.StorageInsertSound, storage, user, _audioParams);
        AnimateStorage(storage, user);
    }

    /// <summary>
    /// Plays the remove sound, if any, and the rustle animation for <paramref name="storage"/>.
    /// </summary>
    private void PlayRemoveFeedback(Entity<StorageComponent> storage, EntityUid? user)
    {
        if (storage.Comp.StorageRemoveSound != null)
            Audio.PlayPredicted(storage.Comp.StorageRemoveSound, storage, user, _audioParams);

        AnimateStorage(storage, user);
    }

    /// <summary>
    /// Plays a clientside storage rustle animation for the specified uid.
    /// </summary>
    protected abstract void PlayStorageAnimation(EntityUid uid, EntityUid? user);
}
