using Robust.Shared.Serialization;

namespace Content.Shared._Battlefield14.Storage;

/// <summary>
/// Plays the storage "rustle" animation on clients that did not predict the interaction.
/// </summary>
[Serializable, NetSerializable]
public sealed class StorageAnimationEvent(NetEntity storage) : EntityEventArgs
{
    public readonly NetEntity Storage = storage;
}
