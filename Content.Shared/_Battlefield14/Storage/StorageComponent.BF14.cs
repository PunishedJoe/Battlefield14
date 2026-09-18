namespace Content.Shared.Storage;

public sealed partial class StorageComponent
{
    /// <summary>
    /// Whether the storage's sprite plays a short squash-and-stretch "rustle" animation
    /// whenever an item is inserted or removed, or the storage is opened.
    /// Prototype-only: not networked, so it must not be toggled at runtime.
    /// </summary>
    [DataField]
    public bool Animated = true;
}
