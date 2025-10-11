using Microsoft.Xna.Framework.Input;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

internal readonly struct TextHandlerPlugin : IPlugin
{
    public void Build(App app)
    {
        app
            .AddSystem(Stage.Startup, (EventWriter<CharInputEvent> writer) =>
            {
                TextInputEXT.TextInput += c =>
                {
                    writer.Send(new() { Value = c });
                };
                TextInputEXT.StartTextInput();
            });
    }
}

internal struct CharInputEvent
{
    public char Value;
}
