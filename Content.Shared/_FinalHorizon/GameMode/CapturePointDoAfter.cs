using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._FinalHorizon.GameMode;

[Serializable, NetSerializable]
public sealed partial class CapturePointDoAfter : SimpleDoAfterEvent;
