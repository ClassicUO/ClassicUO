// Split-stack menu — ECS port of Game/UI/Gumps/SplitMenuGump.cs.
//
// Dragging a stackable item (Amount > 1) out of a container without holding
// Shift opens this little gump instead of picking up the whole pile (legacy
// GameActions.PickUp with HoldShiftToSplitStack == Keyboard.Shift; the ECS
// profile has no such toggle yet, so the default — split when Shift is up — is
// hardcoded). A blue slider (1..Amount) + a numbers-only text box choose the
// quantity; OK / Enter lifts that many, right-click closes. PickupPlugin hands
// us the full pickup snapshot through SplitPrompt so the commit can write
// GrabbedItem exactly as a normal pickup would.

using System;
using ClassicUO.Assets;
using ClassicUO.Network;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.Input;
using TinyEcs.Bevy.UI;
using TinyEcs.Bevy.UI.Widgets;

namespace ClassicUO.Ecs;

// One-shot pickup snapshot from PickupPlugin. PendingSerial != 0 + Open gate
// the pickup system off so it doesn't re-fire while the menu is up. Built
// flips once the window is spawned; the OnRemove<SplitMenuWindow> observer
// resets everything when the window closes by any path (OK or right-click).
internal sealed class SplitPrompt
{
    public bool Open;
    public bool Built;
    // Explicit anchor (top-left) used instead of the cursor when HasPos is set —
    // the debug.openSplit RPC uses this so placement is deterministic (the
    // synthetic mouse has no meaningful position).
    public bool HasPos;
    public float PosX, PosY;
    public uint PendingSerial;
    public ushort Graphic, Hue;
    public int MaxAmount;
    public ulong SourceUiEntity;
    public uint SourceContainer;
    public ushort OriginalGraphic, OriginalHue, OriginalAmount, OriginalX, OriginalY;
    public sbyte OriginalZ;
    public uint OriginalContainer;
    public byte OriginalGridIndex;
    public bool OriginalFromSlot;

    public void Reset()
    {
        Open = false;
        Built = false;
        HasPos = false;
        PendingSerial = 0;
    }
}

internal struct SplitMenuWindow
{
    public uint Serial;
    public int MaxAmount;
    public ulong Slider, Text, Ok;
    public int LastSlider;
    public string LastText;
    // Pickup snapshot carried to the OK/Enter commit.
    public ushort Graphic, Hue;
    public ulong SourceUiEntity;
    public uint SourceContainer;
    public ushort OriginalGraphic, OriginalHue, OriginalAmount, OriginalX, OriginalY;
    public sbyte OriginalZ;
    public uint OriginalContainer;
    public byte OriginalGridIndex;
    public bool OriginalFromSlot;
}

internal readonly struct SplitMenuPlugin : IPlugin
{
    private const ushort Background = 0x085C;
    private const ushort OkNormal = 0x085D, OkPressed = 0x085E, OkOver = 0x085F;

    public void Build(App app)
    {
        app.AddResource(new SplitPrompt());

        var openFn = OpenWindow;
        app.AddSystem(openFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        var syncFn = SyncSliderText;
        app.AddSystem(syncFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        var enterFn = ConfirmOnEnter;
        app.AddSystem(enterFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        // The login screen's TypeIntoFocused only runs in LoginScreen state, so
        // nothing feeds the in-game split textbox — drain digits/backspace into
        // it ourselves. SyncSliderText then clamps + drives the slider.
        var typeFn = TypeIntoSplit;
        app.AddSystem(typeFn).InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        // Window closed by any path (commit despawn or right-click close via
        // WindowDragPlugin) -> release the pickup gate so the next drag can
        // prompt again. OnRemove<T> doesn't reliably fire on despawn in TinyEcs,
        // so a polling backstop (CleanupClosed) is the source of truth; the
        // observer is just the fast path when it does fire.
        app.AddObserver((On<OnRemove<SplitMenuWindow>> _, ResMut<SplitPrompt> prompt) =>
        {
            prompt.Value.Reset();
        });

        var cleanupFn = CleanupClosed;
        app.AddSystem(cleanupFn).InStage(Stage.PostUpdate)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen).Build();

        var despawnFn = DespawnAll;
        app.AddSystem(despawnFn).OnExit(GameState.GameScreen).Build();
    }

    private static void OpenWindow(
        Commands commands,
        Res<GumpBuilder> builder,
        Res<AssetsServer> assets,
        Res<UiZCounter> z,
        Res<MouseContext> mouse,
        ResMut<SplitPrompt> prompt,
        ResMut<FocusedInput> focused)
    {
        if (!prompt.Value.Open || prompt.Value.Built) return;
        prompt.Value.Built = true;

        int max = Math.Max(1, prompt.Value.MaxAmount);
        ref readonly var bg = ref assets.Value.Gumps.GetGump(Background);
        int w = bg.UV.Width, h = bg.UV.Height;
        if (w <= 0) w = 140;
        if (h <= 0) h = 80;

        var pos = prompt.Value.HasPos
            ? new Vector2(prompt.Value.PosX, prompt.Value.PosY)
            : new Vector2(mouse.Value.Position.X - 80, mouse.Value.Position.Y - 40);
        // Never anchor off the top-left edge (a (0,0) synthetic cursor would put
        // the menu at -80,-40 and clip it away).
        pos.X = Math.Max(0, pos.X);
        pos.Y = Math.Max(0, pos.Y);

        var root = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(pos.X), Top = Val.Px(pos.Y),
                Width = Val.Px(w), Height = Val.Px(h),
            })
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.None, Hue = Vector3.UnitZ } })
            .Insert(Interaction.None)
            .Insert<UIMovable>()
            .Insert(new GlobalZIndex(z.Value.Bump()));
        ulong rootId = root.Id;

        // Background art (drag surface — left untagged so grabbing it moves the
        // window; the interactive children below opt out via UINoWindowDrag).
        var bgPic = builder.Value.AddGump(commands, Background, Vector3.UnitZ, new Vector2(0, 0))
            .Insert(Interaction.None);
        commands.AddChild(rootId, bgPic.Id);

        // Slider built inline so both track and knob can carry UINoWindowDrag —
        // otherwise grabbing the knob latches a window move instead of dragging
        // the slider (WindowDragPlugin yields only to UINoWindowDrag children).
        ref readonly var knobInfo = ref assets.Value.Gumps.GetGump(GumpBuilder.SliderKnob);
        int sliderH = knobInfo.UV.Height;
        if (sliderH <= 0) sliderH = 16;
        var track = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute, Left = Val.Px(29), Top = Val.Px(16),
                Width = Val.Px(105), Height = Val.Px(sliderH),
            })
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.None, Hue = Vector3.UnitZ } })
            .Insert(new Slider { Min = 1, Max = max, Value = max, ThumbLength = knobInfo.UV.Width, Orientation = ScrollbarOrientation.Horizontal })
            .Insert(Interaction.None)
            .Insert<UINoWindowDrag>();
        commands.AddChild(rootId, track.Id);

        var knob = commands.Spawn()
            .Insert(new Node { PositionType = PositionType.Absolute, Left = Val.Px(0), Top = Val.Px(0), Width = Val.Px(knobInfo.UV.Width), Height = Val.Px(sliderH) })
            .Insert(new UiCustom { Data = new UOCustomRender { Kind = UOCustomKind.Gump, AssetId = GumpBuilder.SliderKnob, Hue = Vector3.UnitZ } })
            .Insert(new SliderThumb())
            .Insert(new SliderDragState())
            .Insert(Interaction.None)
            .Insert<UINoWindowDrag>();
        commands.AddChild(track.Id, knob.Id);

        // Numbers-only text box (legacy StbTextBox, font 1, hue 0x0386). The
        // value text + a trailing caret sit in a flex row so the shared
        // GuiPlugin.CaretBlink shows a blinking cursor while the box is focused
        // (same TextCaret mechanism as the login fields). The row is the click
        // target (focuses the value text) and carries the box bounds.
        var textFont = new TextFont { FontId = (ushort)(1 | UoFontRuntime.AsciiFlag), Size = 12 };
        var textColor = new TextColor(UoFontRuntime.AsciiHue(0x0386));
        var text = commands.Spawn()
            .Insert(new Node { Width = Val.Auto, Height = Val.Auto })
            .Insert(new Text(max.ToString()))
            .Insert(textFont)
            .Insert(textColor)
            .Insert<TextInput>()
            .Insert<UINoWindowDrag>();
        ulong textId = text.Id;
        var caret = commands.Spawn()
            .Insert(new Node { Width = Val.Auto, Height = Val.Auto })
            .Insert(new Text(string.Empty))
            .Insert(textFont)
            .Insert(textColor)
            .Insert(new TextCaret { Target = textId })
            .Insert<UINoWindowDrag>();
        var textRow = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute, Left = Val.Px(29), Top = Val.Px(42),
                Width = Val.Px(60), Height = Val.Px(20),
                FlexDirection = FlexDirection.Row, AlignItems = AlignItems.Center,
            })
            .Insert(Interaction.None)
            .Insert<UiContainsByBounds>()
            .Insert<UINoWindowDrag>();
        textRow.Observe((On<UiPointerDown> _, ResMut<FocusedInput> f) => f.Value.Entity = textId);
        commands.AddChild(textRow.Id, text.Id);
        commands.AddChild(textRow.Id, caret.Id);
        commands.AddChild(rootId, textRow.Id);
        focused.Value.Entity = textId;

        // OK button (legacy Button 0x085d/e/f at 102,37).
        var ok = builder.Value.AddButton(commands, (OkNormal, OkPressed, OkOver), Vector3.UnitZ, new Vector2(102, 37))
            .Insert<UINoWindowDrag>();
        commands.AddChild(rootId, ok.Id);
        ulong okId = ok.Id;

        ok.Observe((
            On<UiClick> _,
            ResMut<GrabbedItem> grabbed,
            Res<NetClient> net,
            Commands cmds,
            Query<Data<SplitMenuWindow>> winQ,
            Query<Data<Slider>> sliderQ,
            Query<Data<Node>> nodeQ,
            Query<Data<TinyEcs.Children>> childrenQ) =>
        {
            Commit(rootId, grabbed.Value, net.Value, cmds, winQ, sliderQ, nodeQ, childrenQ);
        });

        commands.Entity(rootId).Insert(new SplitMenuWindow
        {
            Serial = prompt.Value.PendingSerial,
            MaxAmount = max,
            Slider = track.Id,
            Text = text.Id,
            Ok = okId,
            LastSlider = max,
            LastText = max.ToString(),
            Graphic = prompt.Value.Graphic,
            Hue = prompt.Value.Hue,
            SourceUiEntity = prompt.Value.SourceUiEntity,
            SourceContainer = prompt.Value.SourceContainer,
            OriginalGraphic = prompt.Value.OriginalGraphic,
            OriginalHue = prompt.Value.OriginalHue,
            OriginalAmount = prompt.Value.OriginalAmount,
            OriginalX = prompt.Value.OriginalX,
            OriginalY = prompt.Value.OriginalY,
            OriginalZ = prompt.Value.OriginalZ,
            OriginalContainer = prompt.Value.OriginalContainer,
            OriginalGridIndex = prompt.Value.OriginalGridIndex,
            OriginalFromSlot = prompt.Value.OriginalFromSlot,
        });
    }

    // Bidirectional slider<->textbox sync (legacy SplitMenuGump.UpdateText):
    // slider move rewrites the text; typed text clamps and drives the slider.
    private static void SyncSliderText(
        Query<Data<SplitMenuWindow>> winQ,
        Query<Data<Slider>> sliderQ,
        Query<Data<Text>> textQ)
    {
        foreach (var (_, win) in winQ)
        {
            if (!sliderQ.Contains(win.Ref.Slider) || !textQ.Contains(win.Ref.Text)) continue;
            var (_, slider) = sliderQ.Get(win.Ref.Slider);
            var (_, text) = textQ.Get(win.Ref.Text);

            int sv = Math.Clamp((int)MathF.Round(slider.Ref.Value), 1, win.Ref.MaxAmount);
            string tx = text.Ref.Value ?? string.Empty;

            if (sv != win.Ref.LastSlider)
            {
                // Slider drag changed the value -> mirror into the text box.
                text.Ref.Value = sv.ToString();
                slider.Ref.Value = sv;
            }
            else if (tx != win.Ref.LastText)
            {
                // User edited the text box -> parse + clamp, drive the slider.
                int clamped;
                if (tx.Length == 0) clamped = 1;
                else if (!int.TryParse(tx, out var typed)) clamped = sv;
                else clamped = Math.Clamp(typed, 1, win.Ref.MaxAmount);

                slider.Ref.Value = clamped;
                text.Ref.Value = clamped.ToString();
                sv = clamped;
            }

            win.Ref.LastSlider = sv;
            win.Ref.LastText = text.Ref.Value;
        }
    }

    // Feed typed digits / backspace into the open split textbox. Each
    // EventReader has its own cursor, so reading here doesn't starve the chat
    // reader. Non-digits are ignored (legacy NumbersOnly); the clamp lives in
    // SyncSliderText.
    private static void TypeIntoSplit(
        EventReader<CharInput> reader,
        Query<Data<SplitMenuWindow>> winQ,
        Query<Data<Text>> textQ)
    {
        ulong textEnt = 0;
        foreach (var (_, w) in winQ) { textEnt = w.Ref.Text; break; }
        if (textEnt == 0 || !textQ.Contains(textEnt)) return;

        var (_, text) = textQ.Get(textEnt);
        foreach (var ev in reader.Read())
        {
            char ch = ev.Value;
            if (ch == '\b')
            {
                if (!string.IsNullOrEmpty(text.Ref.Value))
                    text.Ref.Value = text.Ref.Value[..^1];
            }
            else if (ch >= '0' && ch <= '9')
            {
                text.Ref.Value = (text.Ref.Value ?? string.Empty) + ch;
            }
        }
    }

    // Backstop for the pickup gate: once the window that existed is gone (right-
    // click close or commit despawn), reset the prompt so the next drag can
    // prompt again. OnRemove<SplitMenuWindow> isn't reliable on despawn, and
    // without this reset the pickup system stays gated and items can't be
    // lifted at all. `seen` defers the reset until the window has actually
    // materialised (Commands spawn is deferred a frame).
    private static void CleanupClosed(
        ResMut<SplitPrompt> prompt,
        Query<Data<SplitMenuWindow>> winQ,
        Local<bool> seen)
    {
        if (!prompt.Value.Open || !prompt.Value.Built) { seen.Value = false; return; }

        bool exists = false;
        foreach (var _ in winQ) { exists = true; break; }

        if (exists) seen.Value = true;
        else if (seen.Value) { prompt.Value.Reset(); seen.Value = false; }
    }

    private static void ConfirmOnEnter(
        Res<KeyboardContext> kb,
        ResMut<GrabbedItem> grabbed,
        Res<NetClient> net,
        Commands commands,
        Query<Data<SplitMenuWindow>> winQ,
        Query<Data<Slider>> sliderQ,
        Query<Data<Node>> nodeQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        if (!kb.Value.IsPressedOnce(Microsoft.Xna.Framework.Input.Keys.Enter)) return;
        foreach (var (ent, _) in winQ)
        {
            Commit(ent.Ref, grabbed.Value, net.Value, commands, winQ, sliderQ, nodeQ, childrenQ);
            return;
        }
    }

    // Lift the chosen amount: write GrabbedItem exactly like PickupPlugin does,
    // send the pickup request, hide the source slot, despawn the menu.
    private static void Commit(
        ulong rootId,
        GrabbedItem grabbed,
        NetClient net,
        Commands commands,
        Query<Data<SplitMenuWindow>> winQ,
        Query<Data<Slider>> sliderQ,
        Query<Data<Node>> nodeQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        if (!winQ.Contains(rootId)) return;
        var (_, w) = winQ.Get(rootId);

        int amount = w.Ref.MaxAmount;
        if (sliderQ.Contains(w.Ref.Slider))
        {
            var (_, s) = sliderQ.Get(w.Ref.Slider);
            amount = (int)MathF.Round(s.Ref.Value);
        }
        amount = Math.Clamp(amount, 1, w.Ref.MaxAmount);

        net.Send_PickUpRequest(w.Ref.Serial, (ushort)amount);

        grabbed.Clear();
        grabbed.IsActive = true;
        grabbed.Serial = w.Ref.Serial;
        grabbed.Graphic = w.Ref.Graphic;
        grabbed.Hue = w.Ref.Hue;
        grabbed.Amount = amount;
        grabbed.SourceUiEntity = w.Ref.SourceUiEntity;
        grabbed.OriginalGraphic = w.Ref.OriginalGraphic;
        grabbed.OriginalHue = w.Ref.OriginalHue;
        grabbed.OriginalAmount = w.Ref.OriginalAmount;
        grabbed.OriginalX = w.Ref.OriginalX;
        grabbed.OriginalY = w.Ref.OriginalY;
        grabbed.OriginalZ = w.Ref.OriginalZ;
        grabbed.OriginalContainer = w.Ref.OriginalContainer;
        grabbed.OriginalGridIndex = w.Ref.OriginalGridIndex;
        grabbed.OriginalFromSlot = w.Ref.OriginalFromSlot;

        if (w.Ref.SourceUiEntity != 0 && nodeQ.Contains(w.Ref.SourceUiEntity))
        {
            var (_, node) = nodeQ.Get(w.Ref.SourceUiEntity);
            node.Ref.Display = Display.None;
        }

        DespawnSubtree(commands, rootId, childrenQ);
    }

    private static void DespawnAll(
        Commands commands,
        Query<Data<SplitMenuWindow>> winQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        foreach (var (ent, _) in winQ) DespawnSubtree(commands, ent.Ref, childrenQ);
    }

    private static void DespawnSubtree(Commands commands, ulong e, Query<Data<TinyEcs.Children>> childrenQ)
    {
        if (childrenQ.Contains(e))
        {
            var (_, kids) = childrenQ.Get(e);
            foreach (var cid in kids.Ref) DespawnSubtree(commands, cid, childrenQ);
        }
        commands.Entity(e).Despawn();
    }
}
