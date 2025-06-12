using Microsoft.Xna.Framework.Input;
using TinyEcs;

namespace ClassicUO.Ecs;

internal readonly struct TextHandlerPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddEvent<CharInputEvent>();

        scheduler.OnStartup
        (
            (EventWriter<CharInputEvent> writer) =>
            {
                TextInputEXT.TextInput += c =>
                {
                    writer.Enqueue(new() { Value = c });
                };
                TextInputEXT.StartTextInput();
            }
        );
    }
}

internal struct CharInputEvent
{
    public char Value;
}
