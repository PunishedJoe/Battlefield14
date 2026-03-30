using Robust.Shared.Serialization;

namespace Content.Shared._VXS14.Rangefinder;

[RegisterComponent]
public sealed partial class RangefinderComponent : Component
{
    [DataField("measureDelay")]
    public float MeasureDelay = 0.5f;
}
