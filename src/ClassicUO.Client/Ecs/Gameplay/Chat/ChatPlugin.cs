using System.Text;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Network;
using ClassicUO.Utility;
using TinyEcs;

namespace ClassicUO.Ecs;


internal sealed class ChatOptions
{
    public int MaxMessageLength { get; set; } = 120;
}

internal readonly struct ChatPlugin : IPlugin
{
    public void Build(Scheduler scheduler)
    {
        scheduler.AddResource(new ChatOptions());

        scheduler.OnUpdate((
            EventReader<CharInputEvent> reader,
            Local<StringBuilder> sb,
            Res<UOFileManager> fileManager,
            Res<NetClient> network,
            Res<GameContext> gameCtx,
            Res<Settings> settings,
            Res<ChatOptions> chatOptions
        ) =>
            {
                sb.Value ??= new StringBuilder();

                foreach (var ev in reader)
                {
                    if (ev.Value == '\n') continue;
                    if (ev.Value == '\t') continue;

                    if (ev.Value == '\b')
                    {
                        if (sb.Value.Length > 0)
                            sb.Value.Remove(sb.Value.Length - 1, 1);

                        continue;
                    }

                    if (ev.Value == '\r')
                    {
                        if (sb.Value.Length > 0)
                        {
                            var text = sb.Value.ToString();
                            var entries = fileManager.Value.Speeches.GetKeywords(text);

                            if (gameCtx.Value.ClientVersion >= ClientVersion.CV_200)
                            {
                                network.Value.Send_UnicodeSpeechRequest(
                                    text,
                                    MessageType.Regular,
                                    3,
                                    0x44,
                                    settings.Value.Language,
                                    entries
                                );
                            }
                            else
                            {
                                network.Value.Send_ASCIISpeechRequest(
                                    text,
                                    MessageType.Regular,
                                    3,
                                    0x44,
                                    entries
                                );
                            }

                            sb.Value.Clear();
                        }

                        continue;
                    }

                    if (sb.Value.Length < chatOptions.Value.MaxMessageLength)
                        sb.Value.Append(ev.Value);
                }
            },
            ThreadingMode.Single
        ).RunIf((EventReader<CharInputEvent> reader, Res<NetClient> network)
            => !reader.IsEmpty && network.Value.IsConnected);
    }
}
