using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.POICapture;

[Serializable, NetSerializable]
public sealed partial class POICaptureDoAfterEvent : DoAfterEvent
{
    [DataField("faction")]
    public string Faction = string.Empty;

    [DataField("actionText")]
    public string ActionText = string.Empty;

    public POICaptureDoAfterEvent()
    {
    }

    public POICaptureDoAfterEvent(string faction, string actionText)
    {
        Faction = faction;
        ActionText = actionText;
    }

    public override DoAfterEvent Clone() => this;
}
