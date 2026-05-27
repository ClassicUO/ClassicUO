using ClassicUO.Ecs.Modding.Input;
using ClassicUO.Input;
using Microsoft.Xna.Framework.Input;

namespace ClassicUO.Ecs.Modding.UI;

internal record struct UIEvent(
    InputEventType EventType,
    ulong EntityId,

    ulong? EventId = null,
    float? X = null,
    float? Y = null,
    float? Wheel = null,
    MouseButtonType? MouseButton = null,
    Keys? Key = null
);