using Microsoft.Xna.Framework.Input;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Input;

namespace ClassicUO.Ecs;

// FNA/SDL backend for the library's CharInput event: composed text arrives via
// SDL TEXTINPUT (TextInputEXT), same channel the OS uses for IME/layout.
internal readonly struct TextHandlerPlugin : IPlugin
{
    public void Build(App app)
    {
        app
            .AddSystem(Stage.Startup, (EventWriter<CharInput> writer) =>
            {
                TextInputEXT.TextInput += c =>
                {
                    writer.Send(new() { Value = c });
                };
                TextInputEXT.StartTextInput();
            });
    }
}
