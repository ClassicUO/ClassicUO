using System.Text;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Network;
using ClassicUO.Utility;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;


internal sealed class ChatOptions
{
    public int MaxMessageLength { get; set; } = 120;
    public ushort ChatColor { get; set; } = 0x44;
}

internal readonly struct ChatPlugin : IPlugin
{
    public void Build(App app)
    {
        app.AddResource(new ChatOptions());

        app.AddSystem((
            EventReader<CharInputEvent> reader,
            Local<StringBuilder> sb,
            Res<UOFileManager> fileManager,
            Res<NetClient> network,
            Res<GameContext> gameCtx,
            Res<Settings> settings,
            Res<ChatOptions> chatOptions
        ) =>
            {
                foreach (var ev in reader.Read())
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
                                    chatOptions.Value.ChatColor,
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
                                    chatOptions.Value.ChatColor,
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
            }
        )
        .InStage(Stage.Update)
            .RunIf((EventReader<CharInputEvent> reader, Res<NetClient> network)
            => reader.HasEvents && network.Value.IsConnected);
    }
}
