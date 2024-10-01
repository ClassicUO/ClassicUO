using System.Net.Sockets;
using System.Text;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Network;
using ClassicUO.Utility;
using Microsoft.Xna.Framework.Input;
using TinyEcs;

namespace ClassicUO.Ecs;

internal readonly struct ChatPlugin : IPlugin
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
                    writer.Enqueue(new () { Value = c });
                };
            },
            Stages.Startup,
            ThreadingMode.Single
        );

        scheduler.AddSystem
        (
            (EventReader<CharInputEvent> reader, Local<StringBuilder> sb, Res<NetClient> network, Res<GameContext> gameCtx, Res<Settings> settings) =>
            {
                sb.Value ??= new StringBuilder();

                foreach (var ev in reader)
                {
                    if (ev.Value == '\n') continue;

                    if (ev.Value == '\r')
                    {
                        if (sb.Value.Length > 0)
                        {
                            if (gameCtx.Value.ClientVersion >= ClientVersion.CV_200)
                            {
                                network.Value.Send_UnicodeSpeechRequest(sb.ToString(),
                                                                        MessageType.Regular,
                                                                        3,
                                                                        0x44,
                                                                        settings.Value.Language);
                            }
                            else
                            {
                                network.Value.Send_ASCIISpeechRequest(sb.ToString(), MessageType.Regular, 3, 0x44);
                            }

                            sb.Value.Clear();
                        }

                        continue;
                    }

                    sb.Value.Append(ev.Value);
                }
            },
            Stages.Update,
            ThreadingMode.Single
        );
    }

    struct CharInputEvent
    {
        public char Value;
    }
}
