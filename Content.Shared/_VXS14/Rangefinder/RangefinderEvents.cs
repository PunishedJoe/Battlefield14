using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._VXS14.Rangefinder;

[Serializable, NetSerializable]
public sealed partial class RangefinderDoAfterEvent : SimpleDoAfterEvent
{
    public readonly NetCoordinates Coordinates;

    public RangefinderDoAfterEvent(NetCoordinates coordinates)
    {
        Coordinates = coordinates;
    }
}
