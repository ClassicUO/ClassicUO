// Server-pushed single-line text prompt (packet 0xAB → reply 0xAC).
// The server asks for a string (rename a rune, name a pet, …); we render the
// fixed UO dialog (bg 0x0474, two labels, a framed text field, OK/CANCEL) and
// reply with Send_TextEntryDialogResponse on either button.
//
// Unlike server gumps (0xB0) the layout is HARDCODED — the packet carries no
// command DSL, only the prompt/description strings + echo ids. Coordinates
// mirror the OOP TextEntryDialogGump exactly.

using System;
using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.IO;
using ClassicUO.Network;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

internal readonly struct TextEntryDialogPlugin : IPlugin
{
    // OOP TextEntryDialogGump origin (PacketHandlers passes a fixed spot; the
    // packet has no x/y). Children sit at their gump-local coords below.
    private const int OriginX = 143;
    private const int OriginY = 172;

    // Hue of the two descriptive labels (OOP `Label(..., 0x0386, font: 2)`).
    private const ushort LabelHue = 0x0386;

    public void Build(App app)
    {
        app.AddObserver<On<PacketReceived<OnTextEntryDialogPacket_0xAB>>, Commands, TextEntryDialogParams>(Spawn);

        // Tear the dialog down on logout — no per-gump system runs outside the
        // GameScreen, so an open prompt would linger over the login screen.
        Action<Commands, Query<Data<TextEntryDialog>>> teardownFn =
            (cmd, q) =>
            {
                foreach (var (ent, _) in q)
                    cmd.Entity(ent.Ref).Despawn();
            };
        app.AddSystem(teardownFn).OnExit(GameState.GameScreen).Build();

#if AGENT_BUILD
        app.AddResource(new DebugTextEntryDialogQueue());
        Action<Commands, Res<DebugTextEntryDialogQueue>> drainFn = DrainDebugDialogs;
        app.AddSystem(Stage.First, drainFn);
#endif
    }

#if AGENT_BUILD
    // Drains debug.openTextEntryDialog requests by emitting the same
    // PacketReceived trigger the network path fires, so the dialog is built
    // through the real 0xAB observer (deterministic harness repro).
    private static void DrainDebugDialogs(Commands commands, Res<DebugTextEntryDialogQueue> q)
    {
        if (q.Value.Pending.Count == 0) return;
        foreach (var r in q.Value.Pending)
            commands.EmitTrigger(new PacketReceived<OnTextEntryDialogPacket_0xAB>
            {
                Packet = BuildDebugPacket(r.Serial, r.ParentId, r.ButtonId, r.Text, r.ShowCancel, r.Variant, r.MaxLength, r.Description)
            });
        q.Value.Pending.Clear();
    }

    private static OnTextEntryDialogPacket_0xAB BuildDebugPacket(
        uint serial, byte parentId, byte buttonId, string text, bool showCancel, byte variant, uint maxLength, string description)
    {
        // Serialize into the real 0xAB wire body (serial onward; id+len are
        // stripped by the dispatcher) and Fill() — exact network parse path.
        text ??= string.Empty;
        description ??= string.Empty;
        var buf = new List<byte>();
        void U16(int v) { buf.Add((byte)(v >> 8)); buf.Add((byte)v); }
        void U32(uint v) { buf.Add((byte)(v >> 24)); buf.Add((byte)(v >> 16)); buf.Add((byte)(v >> 8)); buf.Add((byte)v); }
        U32(serial);
        buf.Add(parentId);
        buf.Add(buttonId);
        U16(text.Length); foreach (var c in text) buf.Add((byte)c);          // ASCII
        buf.Add((byte)(showCancel ? 1 : 0));
        buf.Add(variant);
        U32(maxLength);
        U16(description.Length); foreach (var c in description) buf.Add((byte)c);
        var pkt = new OnTextEntryDialogPacket_0xAB();
        pkt.Fill(new StackDataReader(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(buf)));
        return pkt;
    }
#endif

    private static void Spawn(
        On<PacketReceived<OnTextEntryDialogPacket_0xAB>> trig,
        Commands commands,
        TextEntryDialogParams p)
    {
        var pkt = trig.Event.Packet;

        var z = p.ZCounter.Value.Bump();
        var rootCmd = commands.Spawn().Insert(new TextEntryDialog());
        var rootId = rootCmd.Id;

        // Background panel — a normal child at gump-local (0,0). It owns the
        // pixel-perfect hit surface (clicks on it walk up to the UIMovable root).
        ref readonly var bg = ref p.Assets.Value.Gumps.GetGump(0x0474);
        int bgW = bg.UV.Width, bgH = bg.UV.Height;
        Attach(commands, rootId, p.Builder.Value.AddGump(commands, 0x0474, Vector3.UnitZ, new Vector2(0, 0)).Id);

        var labelColor = HueToClayColor(p.Files.Value.Hues, LabelHue);
        Attach(commands, rootId, SpawnLabel(commands, new Vector2(60, 50), pkt.Text, labelColor));
        Attach(commands, rootId, SpawnLabel(commands, new Vector2(60, 108), pkt.Description, labelColor));

        // Text field frame (input box backdrop 0x0477 + the editable glyph).
        Attach(commands, rootId, p.Builder.Value.AddGump(commands, 0x0477, Vector3.UnitZ, new Vector2(60, 130)).Id);

        var capRoot = rootId;
        var frame = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(71), Top = Val.Px(137),
                Width = Val.Px(250), Height = Val.Px(20),
            });
        // Unicode font → TextColor is a literal RGB tint (white = readable); the
        // input box hue is irrelevant for the typed glyphs.
        var entryFont = new TextFont { FontId = UoFontRuntime.DefaultFont, Size = 18 };
        var glyphId = GuiPlugin.SpawnTextField(
            commands, frame, new Vector2(2, 2), entryFont, new ClayColor(255, 255, 255, 255), pkt.Text, masked: false,
            decorate: e => e.Insert(new TextEntryDialogField { RootEntity = capRoot }));
        Attach(commands, rootId, frame.Id);

        // Open with the field focused (OOP SetKeyboardFocus on construct) so the
        // user can type immediately without clicking it first.
        p.Focused.Value.Entity = glyphId;

        // OK + CANCEL — both reply (code distinguishes them), then close. Capture
        // only immutable packet values; the field text is collected on click.
        var serial = pkt.Serial;
        var parentId = pkt.ParentId;
        var buttonId = pkt.ButtonId;
        var maxLen = pkt.MaxLength;
        var numbersOnly = pkt.Variant == 2;

        var ok = p.Builder.Value.AddButton(commands, (0x047B, 0x047C, 0x047D), Vector3.UnitZ, new Vector2(117, 190));
        ok.Observe((On<UiClick> _, Res<NetClient> net, Commands cmd, Query<Data<TextEntryDialogField, Text>> fieldsQ) =>
        {
            net.Value.Send_TextEntryDialogResponse(serial, parentId, buttonId, Sanitize(CollectText(capRoot, fieldsQ), maxLen, numbersOnly), true);
            cmd.Entity(capRoot).Despawn();
        });
        Attach(commands, rootId, ok.Id);

        var cancel = p.Builder.Value.AddButton(commands, (0x0478, 0x0478, 0x047A), Vector3.UnitZ, new Vector2(204, 190));
        cancel.Observe((On<UiClick> _, Res<NetClient> net, Commands cmd, Query<Data<TextEntryDialogField, Text>> fieldsQ) =>
        {
            net.Value.Send_TextEntryDialogResponse(serial, parentId, buttonId, Sanitize(CollectText(capRoot, fieldsQ), maxLen, numbersOnly), false);
            cmd.Entity(capRoot).Despawn();
        });
        Attach(commands, rootId, cancel.Id);

        // Root: absolute window anchor at the gump origin sized to the bg panel.
        // UIMovable makes it a window (right-click-close, click-capture-to-world,
        // z-stack); UINoDrag suppresses dragging (OOP CanMove = false) while
        // keeping it a window. Only the root carries GlobalZIndex.
        commands.Entity(rootId)
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(OriginX), Top = Val.Px(OriginY),
                Width = Val.Px(bgW), Height = Val.Px(bgH),
            })
            .Insert<UIMovable>()
            .Insert<UINoDrag>()
            .Insert(new GlobalZIndex(z));
    }

    private static void Attach(Commands commands, ulong rootId, ulong childId)
        => commands.AddChild(rootId, childId);

    // First text-field value belonging to the dialog rooted at rootId.
    private static string CollectText(ulong rootId, Query<Data<TextEntryDialogField, Text>> fieldsQ)
    {
        foreach (var (_, f, t) in fieldsQ)
            if (f.Ref.RootEntity == rootId)
                return t.Ref.Value ?? string.Empty;
        return string.Empty;
    }

    // Enforce the server's limit on reply (the shared text field has no live
    // char-cap / digit filter yet): truncate to maxLen and, for the numbers-only
    // variant, drop non-digits.
    private static string Sanitize(string text, uint maxLen, bool numbersOnly)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        if (numbersOnly)
        {
            var sb = new System.Text.StringBuilder(text.Length);
            foreach (var c in text)
                if (char.IsDigit(c)) sb.Append(c);
            text = sb.ToString();
        }
        if (maxLen > 0 && text.Length > maxLen)
            text = text.Substring(0, (int)maxLen);
        return text;
    }

    // Single-line label (OOP ASCII font 2). Plain Text node — Clay.NET doesn't
    // wrap, so an over-long prompt overflows the panel (acceptable v1; the
    // server's prompt/description strings are short in practice).
    private static ulong SpawnLabel(Commands commands, Vector2 position, string text, ClayColor color)
    {
        return commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(position.X), Top = Val.Px(position.Y),
                Width = Val.Auto, Height = Val.Auto,
            })
            .Insert(new Text(text ?? string.Empty))
            .Insert(new TextFont { FontId = 1, Size = 16 })
            .Insert(new TextColor(color))
            .Id;
    }

    // UO hue → ClayColor for tinting a white unicode glyph. Mirrors
    // ServerGumpPlugin.HueToClayColor (cell 30, +1 wire convention).
    private static ClayColor HueToClayColor(HuesLoader hues, ushort hue)
    {
        if (hue == 0) return ClayColor.Black;
        var packed = hues.GetPolygoneColor(30, (ushort)(hue + 1));
        if (packed == 0 || packed == 0xFF010101) return ClayColor.Black;
        byte r = (byte)(packed & 0xFF);
        byte g = (byte)((packed >> 8) & 0xFF);
        byte b = (byte)((packed >> 16) & 0xFF);
        return new ClayColor(r, g, b, 255);
    }
}

// Marker on the dialog root — used for logout teardown and (via the field tag)
// to scope the reply's text collection.
internal struct TextEntryDialog;

// On the editable glyph entity; CollectText gathers the value of the field
// whose RootEntity matches the clicked dialog's root.
internal struct TextEntryDialogField
{
    public ulong RootEntity;
}

internal sealed class TextEntryDialogParams : CompositeSystemParam
{
    public readonly Res<AssetsServer> Assets;
    public readonly Res<GumpBuilder> Builder;
    public readonly Res<UOFileManager> Files;
    public readonly Res<UiZCounter> ZCounter;
    public readonly ResMut<FocusedInput> Focused;

    public TextEntryDialogParams()
    {
        Assets   = Add(new Res<AssetsServer>());
        Builder  = Add(new Res<GumpBuilder>());
        Files    = Add(new Res<UOFileManager>());
        ZCounter = Add(new Res<UiZCounter>());
        Focused  = Add(new ResMut<FocusedInput>());
    }
}

#if AGENT_BUILD
// Harness-only queue: debug.openTextEntryDialog pushes a prompt here; the drain
// system emits the real 0xAB trigger next frame.
internal sealed class DebugTextEntryDialogQueue
{
    public readonly List<(uint Serial, byte ParentId, byte ButtonId, string Text, bool ShowCancel, byte Variant, uint MaxLength, string Description)> Pending = new();
}
#endif
