// MessageBoxGump — ECS port of Game/UI/Gumps/MessageBoxGump.cs.
//
// A client-side modal dialog: a ResizePic 0x0A28 panel with a wrapped message
// and an OK button (plus an optional Cancel for the OK/Cancel variant). Used by
// host code for confirmations / error notices (skill-group delete, macro key
// conflicts, char-creation validation, etc.). Centered on screen.
//
// Open() tags the dialog with a caller-chosen Kind; pressing a button emits a
// MessageBoxResult { Kind, Ok } trigger and despawns the dialog. Callers react
// by registering an observer (app.AddObserver) keyed on their Kind — no captured
// callback (ECS rule 4: system→system interop goes through observers). Unlike
// most gumps it is NOT UiMovable — legacy disables right-click-close
// (CanCloseWithRightClick = false) and the dialog is fixed/centered, so it
// carries no UiMovable marker (which would re-add the right-click-close path).
// True input-blocking modality is not ported yet.

using System;
using ClassicUO.Assets;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;

namespace ClassicUO.Ecs;

internal enum MessageButtonType : byte { OK, OkCancel }

internal struct MessageBoxWindow;

// Fired when a message box button is pressed. Kind is the caller-chosen id
// passed to Open (lets one observer distinguish its dialog); Ok is true for OK,
// false for Cancel.
internal struct MessageBoxResult { public int Kind; public bool Ok; }

internal readonly struct MessageBoxGumpPlugin : IPlugin
{
    private const ushort Background = 0x0A28;       // ResizePic panel
    private const ushort InnerBackground = 3000;    // optional inset panel (hasBackground)
    private const ushort OkN = 0x0481, OkP = 0x0483, OkO = 0x0482;
    private const ushort CancelN = 0x047E, CancelP = 0x047F, CancelO = 0x0480;
    private const ushort TextHue = 0x0386;
    private const byte Font = 1;

    public void Build(App app)
    {
        var despawnFn = Despawn;
        app.AddSystem(despawnFn).OnExit(GameState.GameScreen).Build();

#if AGENT_BUILD
        app.AddResource(new DebugMessageBoxQueue());
        Action<Commands, ResMut<DebugMessageBoxQueue>, Res<GumpBuilder>, Res<AssetsServer>, Res<UiSurface>, Res<UiZCounter>> drainFn = DrainDebug;
        app.AddSystem(Stage.First, drainFn);
        app.AddObserver((On<MessageBoxResult> t) => Console.WriteLine($"[MessageBox] kind={t.Event.Kind} ok={t.Event.Ok}"));
#endif
    }

    // Spawn a centered message box. On a button press it emits
    // MessageBoxResult { Kind = kind, Ok }; observe that to react. hasBackground
    // adds the inset 3000 panel (legacy).
    public static void Open(
        Commands commands, GumpBuilder builder, AssetsServer assets, UiSurface surface, UiZCounter zCounter,
        int w, int h, string message, MessageButtonType type, int kind = 0, bool hasBackground = false)
    {
        var pos = new Vector2(
            MathF.Max(0, (surface.LogicalSize.X - w) * 0.5f),
            MathF.Max(0, (surface.LogicalSize.Y - h) * 0.5f));

        // Bare container root: UiPick skips it, the ResizePic child is the
        // pixel-perfect surface. Top z so it draws over everything.
        var root = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(pos.X), Top = Val.Px(pos.Y),
                Width = Val.Px(w), Height = Val.Px(h),
            })
            .Insert(Interaction.None)
            .Insert<MessageBoxWindow>()
            .Insert(new GlobalZIndex(zCounter.Bump()));
        var rootId = root.Id;

        var bg = builder.AddGumpNinePatch(commands, Background, Vector3.UnitZ, new Vector2(0, 0), new Vector2(w, h));
        commands.AddChild(rootId, bg.Id);

        if (hasBackground)
        {
            var inner = builder.AddGumpNinePatch(commands, InnerBackground, Vector3.UnitZ,
                new Vector2(30, 40), new Vector2(w - 60, h - 100));
            commands.AddChild(rootId, inner.Id);
        }

        // Wrapped ASCII message (legacy Label hue 0x0386, font 1, maxWidth w-90).
        // Clay's word-wrap doesn't drive the UO atlas, so wrap at spawn via a
        // WrappedText custom sized to the measured height.
        int wrapW = w - 90;
        var (_, msgH) = UoFontRenderer.Measure(message, Font, wrapW, isHtml: false, 0, htmlBg: false, ascii: true);
        var label = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(40), Top = Val.Px(45),
                Width = Val.Px(wrapW), Height = Val.Px(Math.Max(msgH, 18)),
            })
            .Insert(new UiCustom
            {
                Data = new UOCustomRender
                {
                    Kind = UOCustomKind.WrappedText,
                    Hue = Vector3.UnitZ,
                    Text = message,
                    TextFont = Font,
                    TextHue = TextHue,
                    WrapWidth = wrapW,
                    IsHtml = false,
                    TextAscii = true,
                },
            });
        commands.AddChild(rootId, label.Id);

        int okW = GumpW(assets, OkN);
        int buttonY = h - 45;

        if (type == MessageButtonType.OkCancel)
        {
            int cancelW = GumpW(assets, CancelN);
            int okX = w / 2 - cancelW;
            var ok = builder.AddButton(commands, (OkN, OkP, OkO), Vector3.UnitZ, new Vector2(okX, buttonY));
            ok.Observe((On<UiClick> _, Commands cmd, Query<Data<TinyEcs.Children>> childrenQ) =>
                Close(cmd, rootId, kind, true, childrenQ));
            commands.AddChild(rootId, ok.Id);

            var cancel = builder.AddButton(commands, (CancelN, CancelP, CancelO), Vector3.UnitZ, new Vector2(okX + okW + 5, buttonY));
            cancel.Observe((On<UiClick> _, Commands cmd, Query<Data<TinyEcs.Children>> childrenQ) =>
                Close(cmd, rootId, kind, false, childrenQ));
            commands.AddChild(rootId, cancel.Id);
        }
        else
        {
            var ok = builder.AddButton(commands, (OkN, OkP, OkO), Vector3.UnitZ, new Vector2((w - okW) / 2, buttonY));
            ok.Observe((On<UiClick> _, Commands cmd, Query<Data<TinyEcs.Children>> childrenQ) =>
                Close(cmd, rootId, kind, true, childrenQ));
            commands.AddChild(rootId, ok.Id);
        }
    }

    // Emit the result trigger then despawn the dialog subtree.
    private static void Close(Commands commands, ulong rootId, int kind, bool ok, Query<Data<TinyEcs.Children>> childrenQ)
    {
        commands.EmitTrigger(new MessageBoxResult { Kind = kind, Ok = ok });
        DespawnSubtree(commands, rootId, childrenQ);
    }

    private static void Despawn(
        Commands commands,
        Query<Data<MessageBoxWindow>> windowsQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        foreach (var (ent, _) in windowsQ)
            DespawnSubtree(commands, ent.Ref, childrenQ);
    }

    private static void DespawnSubtree(Commands commands, ulong e, Query<Data<TinyEcs.Children>> childrenQ)
    {
        if (childrenQ.Contains(e))
        {
            var (_, kids) = childrenQ.Get(e);
            foreach (var cid in kids.Ref)
                DespawnSubtree(commands, cid, childrenQ);
        }
        commands.Entity(e).Despawn();
    }

    private static int GumpW(AssetsServer a, ushort id) { ref readonly var g = ref a.Gumps.GetGump(id); return g.UV.Width; }

#if AGENT_BUILD
    private static void DrainDebug(
        Commands commands,
        ResMut<DebugMessageBoxQueue> q,
        Res<GumpBuilder> builder,
        Res<AssetsServer> assets,
        Res<UiSurface> surface,
        Res<UiZCounter> zCounter)
    {
        if (!q.Value.Pending) return;
        q.Value.Pending = false;
        Open(commands, builder.Value, assets.Value, surface.Value, zCounter.Value,
            q.Value.Width, q.Value.Height, q.Value.Message,
            q.Value.Cancel ? MessageButtonType.OkCancel : MessageButtonType.OK);
    }
#endif
}

#if AGENT_BUILD
internal sealed class DebugMessageBoxQueue
{
    public bool Pending;
    public int Width = 250, Height = 150;
    public bool Cancel;
    public string Message = string.Empty;
}
#endif
