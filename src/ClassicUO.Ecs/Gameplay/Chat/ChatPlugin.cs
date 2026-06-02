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

// Current chat input buffer. Lives as a resource (not system-local) so the
// system-message renderer can draw the in-progress line + its translucent bar.
internal sealed class ChatInputState
{
    public readonly StringBuilder Text = new();
}

internal readonly struct ChatPlugin : IPlugin
{
    public void Build(App app)
    {
        app.AddResource(new ChatOptions());
        app.AddResource(new ChatInputState());

        app.AddSystem((
            EventReader<CharInputEvent> reader,
            Res<ChatInputState> input,
            Res<UOFileManager> fileManager,
            Res<NetClient> network,
            Res<GameContext> gameCtx,
            Res<Settings> settings,
            Res<ChatOptions> chatOptions
        ) =>
            {
                var sb = input.Value.Text;
                foreach (var ev in reader.Read())
                {
                    if (ev.Value == '\n') continue;
                    if (ev.Value == '\t') continue;

                    if (ev.Value == '\b')
                    {
                        if (sb.Length > 0)
                            sb.Remove(sb.Length - 1, 1);

                        continue;
                    }

                    if (ev.Value == '\r')
                    {
                        if (sb.Length > 0)
                        {
                            var text = sb.ToString();
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

                            sb.Clear();
                        }

                        continue;
                    }

                    if (sb.Length < chatOptions.Value.MaxMessageLength)
                        sb.Append(ev.Value);
                }
            }
        )
        .InStage(Stage.Update)
            // Yield while a UI text field (e.g. a skills group-name editor) holds
            // keyboard focus, so typed chars edit the field instead of the chat.
            .RunIf((EventReader<CharInputEvent> reader, Res<NetClient> network,
                    Res<FocusedInput> focused, Query<Data<SkillNameEdit>> editsQ)
            => reader.HasEvents && network.Value.IsConnected
               && !(focused.Value.Entity != 0 && editsQ.Contains(focused.Value.Entity)))
            .Build();
    }
}
