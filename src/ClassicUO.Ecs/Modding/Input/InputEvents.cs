namespace ClassicUO.Ecs.Modding.Input;

enum InputEventType
{
    OnMouseMove, // this is the same as MouseOver I guess
    OnMouseWheel,
    OnMouseOver,
    OnMousePressed,
    OnMouseReleased,
    OnMouseDoubleClick,
    OnMouseEnter,
    OnMouseLeave,
    OnDragging,

    OnKeyPressed,
    OnKeyReleased,

    // Fires when the user edits an `editable` text node (UIEvent.Value carries
    // the new text). The guest registers it like any other listener.
    OnTextChanged,
}