using Microsoft.Xna.Framework.Input;
using TinyEcs;

namespace ClassicUO.Ecs;

internal readonly struct TextHandlerPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddEvent<CharInputEvent>();

        scheduler.AddSystem
        (
            (EventWriter<CharInputEvent> writer) =>
            {
                TextInputEXT.TextInput += c =>
                {
                    writer.Enqueue(new() { Value = c });
                };
            },
            Stages.Startup,
            ThreadingMode.Single
        );
    }
}

internal struct CharInputEvent
{
    public char Value;
}