using System;
using ClassicUO.Ecs.Modding.Input;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using TinyEcs;

namespace ClassicUO.Ecs.Modding.UI;

internal readonly struct UIEventsPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        var sendUIEventsFn = SendUIEvents;
        scheduler.OnUpdate(sendUIEventsFn);
    }

    private static void SendUIEvents(
        Query<Data<UINode, UIMouseAction, PluginEntity>, Changed<UIMouseAction>> queryChanged,
        Query<Data<UINode, UIMouseAction, PluginEntity>> query,
        Query<Data<UIEvent>, With<Parent>> queryEvents,
        Query<Data<Children>> children,
        Query<Data<Parent>, With<UINode>> queryUIParents,
        Res<MouseContext> mouseCtx,
        Res<KeyboardContext> keyboardCtx,
        Local<Vector2> lastMousePos
    )
    {
        var isDragging = mouseCtx.Value.PositionOffset.Length() > 1;
        var isMouseWheel = mouseCtx.Value.Wheel != 0;
        var mousePos = mouseCtx.Value.Position;
        var isMousePosChanged = mousePos != lastMousePos.Value;
        lastMousePos.Value = mousePos;


        static bool sendEventForId(
            ulong id,
            Query<Data<UIEvent>, With<Parent>> queryEvents,
            Query<Data<Children>> queryChildren,
            MouseContext mouseCtx,
            InputEventType eventType,
            MouseButtonType button,
            Mod mod
        )
        {
            // check if there is any child event
            if (!queryChildren.Contains(id))
                return true;

            (_, var children) = queryChildren.Get(id);

            foreach (var child in children.Ref)
            {
                // check if child is an event
                if (!queryEvents.Contains(child))
                    continue;

                (var eventId, var uiEv) = queryEvents.Get(child);

                if (uiEv.Ref.EventType != eventType)
                {
                    continue;
                }

                // push the event
                var json = (uiEv.Ref with
                {
                    EntityId = id,
                    EventId = eventId.Ref.ID,
                    X = mouseCtx.Position.X,
                    Y = mouseCtx.Position.Y,
                    Wheel = mouseCtx.Wheel,
                    MouseButton = button,
                }).ToJson();

                var result = mod.Plugin.Call("on_ui_event", json);
                if (result == "0")
                {
                    Console.WriteLine("on_ui_event returned 0, stopping event propagation");
                    return false;
                }
            }

            return true;
        }


        foreach ((var ent, var node, var mouseAction, var pluginEnt) in queryChanged)
        {
            InputEventType? eventType = mouseAction.Ref switch
            {
                { IsPressed: true, WasPressed: false, IsHovered: true } => InputEventType.OnMousePressed,
                { IsPressed: false, WasPressed: true } => InputEventType.OnMouseReleased,
                { IsHovered: true, WasHovered: false } => InputEventType.OnMouseEnter,
                { IsHovered: false, WasHovered: true } => InputEventType.OnMouseLeave,
                _ => null
            };

            if (mouseCtx.Value.IsPressedDouble(mouseAction.Ref.Button))
            {
                eventType = InputEventType.OnMouseDoubleClick;
            }

            if (eventType == null)
                continue;

            var result = sendEventForId(
                ent.Ref.ID,
                queryEvents,
                children,
                mouseCtx,
                eventType.Value,
                mouseAction.Ref.Button,
                pluginEnt.Ref.Mod
            );

            if (!result)
                continue;

            var parentId = ent.Ref.ID;
            while (queryUIParents.Contains(parentId))
            {
                (_, var parent) = queryUIParents.Get(parentId);
                result = sendEventForId(
                    parent.Ref.Id,
                    queryEvents,
                    children,
                    mouseCtx,
                    eventType.Value,
                    mouseAction.Ref.Button,
                    pluginEnt.Ref.Mod
                );

                // block the events propagation
                if (!result)
                    break;

                parentId = parent.Ref.Id;
            }
        }

        if (isMousePosChanged || isMouseWheel)
        {
            foreach ((var ent, var node, var mouseAction, var pluginEnt) in query)
            {
                InputEventType? eventType = mouseAction.Ref switch
                {
                    { IsPressed: true, WasPressed: true, Button: MouseButtonType.Left } when isMousePosChanged => InputEventType.OnDragging,
                    { IsHovered: true } when isMouseWheel => InputEventType.OnMouseWheel,
                    { IsHovered: true } when isMousePosChanged => InputEventType.OnMouseOver,
                    _ => null
                };

                if (eventType == null)
                    continue;

                var result = sendEventForId(
                    ent.Ref.ID,
                    queryEvents,
                    children,
                    mouseCtx,
                    eventType.Value,
                    mouseAction.Ref.Button,
                    pluginEnt.Ref.Mod
                );

                if (!result)
                    continue;

                var parentId = ent.Ref.ID;
                while (queryUIParents.Contains(parentId))
                {
                    (_, var parent) = queryUIParents.Get(parentId);
                    result = sendEventForId(
                        parent.Ref.Id,
                        queryEvents,
                        children,
                        mouseCtx,
                        eventType.Value,
                        mouseAction.Ref.Button,
                        pluginEnt.Ref.Mod
                    );

                    // block the events propagation
                    if (!result)
                        break;

                    parentId = parent.Ref.Id;
                }
            }
        }
    }
}