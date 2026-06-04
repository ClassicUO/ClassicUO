using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Input;
using ClassicUO.Renderer;
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
    public bool Attached;
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
        var attachFn = AttachChatToWindow;
        var sizeFn = SizeChatBar;
        var keepFocusFn = KeepChatFocused;
        var submitFn = SubmitChat;

        app
            .AddSystem(spawnFn).OnEnter(GameState.GameScreen).Build()
            .AddSystem(despawnFn).OnExit(GameState.GameScreen).Build()

            // Parent the bar under the game-window UI node once both exist (both
            // spawn deferred on entering the scene, so this can't run at spawn).
            // As a child of the viewport it anchors bottom-left with Bottom +
            // Percent width against the window's real box — that's the chat
            // living "inside the game window" like legacy SystemChatControl.
            .AddSystem(attachFn)
            .InStage(Stage.PreUpdate)
            .RunIf((Res<State<GameState>> s, Res<ChatField> f)
                => s.Value.Current == GameState.GameScreen && f.Value.Bar != 0 && !f.Value.Attached)
            .Build()

            // Stretch the bar to the viewport width each frame. Percent(100) on
            // an absolute child collapses to 0 (so the translucent bg rect would
            // be zero-width and invisible), so set an explicit pixel width from
            // the camera viewport (logical px, the Clay layout space).
            .AddSystem(sizeFn)
            .InStage(Stage.PreUpdate)
            .RunIf((Res<State<GameState>> s, Res<ChatField> f)
                => s.Value.Current == GameState.GameScreen && f.Value.Bar != 0)
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
        // Translucent bar pinned to the bottom-left of the game-window viewport.
        // It anchors with Bottom + Percent width, which only resolve once it's a
        // child of the window node (AttachChatToWindow does that) — as a root
        // those collapse to 0. Rendered as a child of the viewport, after the
        // world image, so it sits over the world; gumps are separate higher-z
        // roots and still stack over it.
        var bar = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                FlexDirection = FlexDirection.Row,
                AlignItems = AlignItems.Center,
                Left = Val.Px(0),
                Bottom = Val.Px(BottomInset),
                Width = Val.Px(0), // set by SizeChatBar to the viewport width
                Height = Val.Px(BarHeight),
            })
            .Insert(new BackgroundColor(new ClayColor(0, 0, 0, 180)))
            .Insert<ChatUi>();

        // Unicode font -> white RGB tint (a packed UO hue would read as RGB and
        // black the text out).
        var font = new TextFont { FontId = UoFontRuntime.DefaultFont, Size = 18 };
        var glyphId = GuiPlugin.SpawnTextField(
            commands, bar, new Vector2(LeftMargin, 2), font, new ClayColor(255, 255, 255, 255), string.Empty, masked: false,
            decorate: e => e.Insert<ChatUi>());

        field.Value.Bar = bar.Id;
        field.Value.Glyph = glyphId;
        focused.Value.Entity = glyphId;
    }

    // Make the bar a child of the game-window viewport node so it lives inside
    // the game window. Runs until the window node exists (deferred spawn), then
    // latches via Attached.
    private static void AttachChatToWindow(
        Commands commands,
        ResMut<ChatField> field,
        Query<Data<GameScreenPlugin.GameWindowUI>> windowQ)
    {
        foreach (var (ent, _) in windowQ)
        {
            commands.AddChild(ent.Ref, field.Value.Bar);
            field.Value.Attached = true;
            return;
        }
    }

    private static void DespawnChatField(
        Commands commands,
        ResMut<ChatField> field,
        Query<Data<ChatUi>> chatUiQ)
    {
        foreach (var (ent, _) in chatUiQ)
            commands.Entity(ent.Ref).Despawn();
        field.Value.Bar = 0;
        field.Value.Glyph = 0;
        field.Value.Attached = false;
    }

    // Stretch the bar to the viewport width (camera.Bounds is logical px, the
    // same space Val.Px feeds into). In-place Node mutation — no Commands.
    private static void SizeChatBar(
        Res<ChatField> field,
        Res<Camera> camera,
        Query<Data<Node>> nodes)
    {
        var bar = field.Value.Bar;
        if (bar == 0 || !nodes.Contains(bar)) return;
        var (_, n) = nodes.Get(bar);
        n.Ref.Width = Val.Px(camera.Value.Bounds.Width);
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
