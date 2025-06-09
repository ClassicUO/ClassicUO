using Microsoft.Xna.Framework.Input;
using TinyEcs;

namespace ClassicUO.Ecs;

[TinyPlugin]
internal readonly partial struct TextHandlerPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddEvent<CharInputEvent>();
    }


    [TinySystem(Stages.Startup, ThreadingMode.Single)]
    private static void CharInput(EventWriter<CharInputEvent> writer)
    {
        TextInputEXT.TextInput += c => writer.Enqueue
        (
            new()
            {
                Value = c
            }
        );
        TextInputEXT.StartTextInput();
    }
}

internal struct CharInputEvent
{
    public char Value;
}
