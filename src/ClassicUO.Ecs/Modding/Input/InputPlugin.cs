using ClassicUO.Ecs.Modding.Host;
using ClassicUO.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs.Modding;

internal readonly struct InputPlugin : IPlugin
{
    public void Build(App app)
    {
        var sendGeneralEventsFn = SendGeneralEvents;
        app.AddSystem(Stage.PostUpdate, sendGeneralEventsFn);
    }

    private static void SendGeneralEvents(
        Res<MouseContext> mouseCtx,
        Res<KeyboardContext> keyboardCtx,
        EventWriter<HostMessage> writer
    )
    {
        if (mouseCtx.Value.PositionOffset != Vector2.Zero)
        {
            writer.Send(new HostMessage.MouseMove(mouseCtx.Value.Position.X, mouseCtx.Value.Position.Y));
        }

        if (mouseCtx.Value.Wheel != 0)
        {
            writer.Send(new HostMessage.MouseWheel(mouseCtx.Value.Wheel));
        }

        for (var button = MouseButtonType.Left; button < MouseButtonType.Size; button += 1)
        {
            if (mouseCtx.Value.IsPressedOnce(button))
            {
                writer.Send(new HostMessage.MousePressed((int)button, mouseCtx.Value.Position.X, mouseCtx.Value.Position.Y));
            }

            if (mouseCtx.Value.IsReleased(button))
            {
                writer.Send(new HostMessage.MouseReleased((int)button, mouseCtx.Value.Position.X, mouseCtx.Value.Position.Y));
            }

            if (mouseCtx.Value.IsPressedDouble(button))
            {
                writer.Send(new HostMessage.MouseDoubleClick((int)button, mouseCtx.Value.Position.X, mouseCtx.Value.Position.Y));
            }
        }

        for (var key = Keys.None + 1; key <= Keys.OemEnlW; key += 1)
        {
            if (keyboardCtx.Value.IsPressedOnce(key))
            {
                writer.Send(new HostMessage.KeyPressed(key));
            }

            if (keyboardCtx.Value.IsReleased(key))
            {
                writer.Send(new HostMessage.KeyReleased(key));
            }
        }
    }
}
