using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;


internal sealed class ChatOptions
{
    public int MaxMessageLength { get; set; } = 120;
    public ushort ChatColor { get; set; } = 0x44;
}

// The in-game chat input is a real focusable UI field (built on the shared
// SpawnTextField primitive), not a resource-backed overlay: it gets the I-beam
// cursor on hover, a blinking caret, and the global EditFocusedTextField path
// for typing — same as the login fields. ChatField.Glyph is the field's glyph
// entity (the one that holds keyboard focus and the typed Text); 0 before spawn.
internal sealed class ChatField
{
    public ulong Bar;
    public ulong Glyph;
}

// Marks the chat bar + its sub-entities so they're torn down on leaving the
// game scene.
internal struct ChatUi;

internal readonly struct ChatPlugin : IPlugin
{
    private const int BarHeight = 22;
    private const int BottomInset = 6;
    private const int LeftMargin = 6;

    public void Build(App app)
    {
        app.AddResource(new ChatOptions());
        app.AddResource(new ChatField());

        var spawnFn = SpawnChatField;
        var despawnFn = DespawnChatField;
        var positionFn = PositionChatBar;
        var keepFocusFn = KeepChatFocused;
        var submitFn = SubmitChat;

        app
            .AddSystem(spawnFn).OnEnter(GameState.GameScreen).Build()
            .AddSystem(despawnFn).OnExit(GameState.GameScreen).Build()

            // Pin the bar to the bottom of the logical viewport each frame (a
            // root Node can't anchor with Bottom/Percent, so its absolute pixel
            // box is recomputed from the live surface — also follows resizes).
            .AddSystem(positionFn)
            .InStage(Stage.PreUpdate)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen)
            .Build()

            // Chat is the default keyboard sink in-game: whenever no live text
            // field holds focus (nothing focused, or the focused entity was
            // despawned — e.g. a server-gump text entry closed), reclaim focus
            // for the chat glyph so typing lands in chat. A focused split/skills/
            // server-gump field still has its TextInput marker, so chat yields
            // to it until it goes away.
            .AddSystem(keepFocusFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen)
            .Build()

            // Enter submits the chat line. Per-char editing is handled globally
            // by GuiPlugin.EditFocusedTextField (the chat glyph opts in via the
            // EditableText marker SpawnTextField adds).
            .AddSystem(submitFn)
            .InStage(Stage.Update)
            .RunIf((Res<KeyboardContext> kb, Res<NetClient> net)
                => net.Value.IsConnected && kb.Value.IsPressedOnce(Keys.Enter))
            .Build();
    }

    private static void SpawnChatField(
        Commands commands,
        ResMut<ChatField> field,
        ResMut<FocusedInput> focused)
    {
        // Translucent bar, the bounds-hittable focus region. It's an absolute
        // root whose pixel box PositionChatBar pins to the viewport bottom each
        // frame (a root can't anchor with Bottom/Percent — those resolve against
        // a missing parent and collapse to 0). GlobalZIndex lifts it above the
        // world window (z 0, opaque) and threads to the glyph/caret; gumps bump
        // higher z so they still stack over the bar. SpawnTextField hangs the
        // editable glyph + caret off it.
        var bar = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                FlexDirection = FlexDirection.Row,
                AlignItems = AlignItems.Center,
                Left = Val.Px(0),
                Top = Val.Px(0),
                Width = Val.Px(0),
                Height = Val.Px(BarHeight),
            })
            .Insert(new BackgroundColor(new ClayColor(0, 0, 0, 180)))
            .Insert(new GlobalZIndex(100))
            .Insert<ChatUi>();

        var font = new TextFont { FontId = UoFontRuntime.DefaultFont, Size = 18 };
        var glyphId = GuiPlugin.SpawnTextField(
            commands, bar, new Vector2(LeftMargin, 2), font, 0, string.Empty, masked: false,
            decorate: e => e.Insert<ChatUi>());

        field.Value.Bar = bar.Id;
        field.Value.Glyph = glyphId;
        focused.Value.Entity = glyphId;
    }

    // Recompute the bar's absolute box from the live logical surface so it spans
    // the width and sits at the bottom (UiSurface.LogicalSize is the Clay layout
    // space). In-place Node mutation — no Commands needed.
    private static void PositionChatBar(
        Res<ChatField> field,
        Res<TinyEcs.Bevy.UI.UiSurface> surface,
        Query<Data<Node>> nodes)
    {
        var bar = field.Value.Bar;
        if (bar == 0 || !nodes.Contains(bar)) return;

        var size = surface.Value.LogicalSize;
        var (_, n) = nodes.Get(bar);
        n.Ref.Left = Val.Px(0);
        n.Ref.Top = Val.Px(size.Y - BarHeight - BottomInset);
        n.Ref.Width = Val.Px(size.X);
    }

    private static void DespawnChatField(
        Commands commands,
        ResMut<ChatField> field,
        Query<Data<ChatUi>> chatUiQ)
    {
        foreach (var (ent, _) in chatUiQ)
            commands.Entity(ent.Ref).Despawn();
        field.Value.Glyph = 0;
    }

    private static void KeepChatFocused(
        Res<ChatField> field,
        ResMut<FocusedInput> focused,
        Query<Data<TextInput>> textInputQ)
    {
        var glyph = field.Value.Glyph;
        if (glyph == 0) return;
        // A live text field (chat itself, split number box, skills rename,
        // server-gump entry) carries TextInput — leave focus alone. Otherwise
        // reclaim it for chat.
        if (focused.Value.Entity == 0 || !textInputQ.Contains(focused.Value.Entity))
            focused.Value.Entity = glyph;
    }

    private static void SubmitChat(
        Res<ChatField> field,
        Res<UOFileManager> fileManager,
        Res<NetClient> network,
        Res<GameContext> gameCtx,
        Res<Settings> settings,
        Res<ChatOptions> chatOptions,
        Query<Data<Text>> textQ)
    {
        var glyph = field.Value.Glyph;
        if (glyph == 0 || !textQ.Contains(glyph)) return;

        var (_, t) = textQ.Get(glyph);
        var text = t.Ref.Value ?? string.Empty;
        if (text.Length == 0) return;

        var entries = fileManager.Value.Speeches.GetKeywords(text);
        if (gameCtx.Value.ClientVersion >= ClientVersion.CV_200)
        {
            network.Value.Send_UnicodeSpeechRequest(
                text, MessageType.Regular, 3, chatOptions.Value.ChatColor,
                settings.Value.Language, entries);
        }
        else
        {
            network.Value.Send_ASCIISpeechRequest(
                text, MessageType.Regular, 3, chatOptions.Value.ChatColor, entries);
        }

        t.Ref.Value = string.Empty;
    }
}
