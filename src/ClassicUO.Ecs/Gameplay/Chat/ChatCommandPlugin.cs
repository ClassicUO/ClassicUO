using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using TinyEcs.Bevy.Modding;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

// A chat line beginning with '-' is a client-side command: ChatPlugin.SubmitChat
// intercepts it (never sent to the server) and emits this trigger. The dispatcher
// below routes by name. Add a new "-command" = add a case in Dispatch.
//
// `-mods` toggles a modern panel (styled like the Options gump) listing the loaded
// WASM mods, each with an enable/disable toggle + a reload button — driven by the
// modding runtime's ModControl resource.
internal struct ChatCommandEvent { public string Name; public string Args; }

// Window-root marker for the mods panel, so the toggle command can find + close it.
internal struct ModsPanelWindow;

// Per-row sync markers (carry the mod's stable index into ModControl.Mods).
internal struct ModToggleControl { public int Index; }
internal struct ModRowLabel { public int Index; }

internal readonly struct ChatCommandPlugin : IPlugin
{
    // Layout (logical px — the Clay layout space).
    private const int PanelW = 348;
    private const int Pad = 14;
    private const int HeaderH = 24;
    private const int CardH = 44;
    private const int RowGap = 8;
    private const int ToggleW = 46;
    private const int ToggleH = 24;
    private const int KnobSz = 18;
    private const int ReloadW = 68;
    private const int CtrlGap = 8;

    // Palette (mirrors OptionsGumpPlugin's modern dark theme).
    private static readonly ClayColor PanelBg = new(16, 17, 22, 245);
    private static readonly ClayColor PanelBorder = new(44, 47, 58, 255);
    private static readonly ClayColor CardBg = new(24, 26, 33, 255);
    private static readonly ClayColor Accent = new(92, 132, 224, 255);
    private static readonly ClayColor TextMain = new(236, 238, 244, 255);
    private static readonly ClayColor TextDim = new(120, 124, 140, 255);
    private static readonly ClayColor ToggleOn = new(74, 178, 112, 255);
    private static readonly ClayColor ToggleOff = new(52, 55, 66, 255);
    private static readonly ClayColor Knob = new(238, 240, 246, 255);
    private static readonly ClayColor ReloadBg = new(48, 52, 64, 255);
    private static readonly ClayColor Shadow = new(0, 0, 0, 130);

    public void Build(App app)
    {
        // chat emits ChatCommandEvent with no entity → the global observer fires.
        app.AddObserver<On<ChatCommandEvent>, Commands, Res<ModControl>, Res<UiZCounter>, Query<Data<ModsPanelWindow>>>(Dispatch);

        // Keep the panel's toggles + labels in sync with the live enabled state
        // (enable/disable/reload land deferred, a frame later). Cheap when closed
        // (the marker queries are empty).
        var syncFn = SyncPanel;
        app.AddSystem(syncFn).InStage(Stage.Update).Build();
    }

    private static void Dispatch(
        On<ChatCommandEvent> trigger,
        Commands commands,
        Res<ModControl> mods,
        Res<UiZCounter> z,
        Query<Data<ModsPanelWindow>> panelQ)
    {
        switch ((trigger.Event.Name ?? string.Empty).ToLowerInvariant())
        {
            case "mods":
                ToggleModsPanel(commands, mods.Value, z.Value, panelQ);
                break;
            // Add more `-command` handlers here.
        }
    }

    private static void ToggleModsPanel(Commands commands, ModControl mods, UiZCounter z, Query<Data<ModsPanelWindow>> panelQ)
    {
        // Open -> close. Despawn cascades to all the panel's children.
        var wasOpen = false;
        foreach (var (e, _) in panelQ)
        {
            commands.Entity(e.Ref).Despawn();
            wasOpen = true;
        }
        if (wasOpen)
            return;

        var count = mods.Mods.Count;
        var bodyRows = count == 0 ? 1 : count;
        var height = Pad + HeaderH + RowGap + bodyRows * (CardH + RowGap) + Pad - RowGap;

        // Modern styled panel (flex column rect, not a UO sprite) — UiMovable gives
        // drag + right-click-close via WindowDragPlugin; GlobalZIndex floats it.
        var root = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                FlexDirection = FlexDirection.Column,
                Left = Val.Px(140), Top = Val.Px(120),
                Width = Val.Px(PanelW), Height = Val.Px(height),
                Padding = UiRect.All(Pad),
                Gap = Val.Px(RowGap),
                Border = UiRect.All(1),
            })
            .Insert(new BackgroundColor(PanelBg))
            .Insert(new BorderColor(PanelBorder))
            .Insert(BorderRadius.All(12))
            .Insert(new BoxShadow { Color = Shadow, OffsetX = 0, OffsetY = 6, BlurRadius = 28, SpreadRadius = 0 })
            .Insert(Interaction.None)
            .Insert<UiMovable>()
            .Insert(new GlobalZIndex(z.Bump()))
            .Insert<ModsPanelWindow>();
        var rootId = root.Id;

        // Header row: title + close (X). The X carries UiNoWindowDrag so a click on
        // it closes rather than latching the window drag.
        var header = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex, FlexDirection = FlexDirection.Row,
                AlignItems = AlignItems.Center,
                Width = Val.Px(PanelW - 2 * Pad), Height = Val.Px(HeaderH),
            });
        commands.AddChild(rootId, header.Id);
        AddText(commands, header.Id, "MODS", 1, 14, Accent, Val.Px(PanelW - 2 * Pad - 22));
        var close = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex, JustifyContent = JustifyContent.Center, AlignItems = AlignItems.Center,
                Width = Val.Px(18), Height = Val.Px(18),
            })
            .Insert(new Text("x"))
            .Insert(new TextFont { FontId = 1, Size = 14 })
            .Insert(new TextColor(TextDim))
            .Insert(Interaction.None)
            .Insert<UiNoWindowDrag>()
            .Observe((On<UiClick> _, Commands c, Query<Data<ModsPanelWindow>> pq) =>
            {
                foreach (var (pe, _) in pq)
                    c.Entity(pe.Ref).Despawn();
            });
        commands.AddChild(header.Id, close.Id);

        if (count == 0)
        {
            var card = SpawnCard(commands, rootId);
            AddText(commands, card, "(no mods loaded)", 1, 13, TextDim, Val.Auto);
            return;
        }

        for (var i = 0; i < count; i++)
        {
            var info = mods.Mods[i];
            var idx = i; // capture an immutable index, not the entity
            var card = SpawnCard(commands, rootId);

            // Name + version (dims when disabled — kept in sync by SyncPanel).
            var nameW = (PanelW - 2 * Pad) - 24 - ToggleW - ReloadW - 2 * CtrlGap;
            var label = commands.Spawn()
                .Insert(new Node { Width = Val.Px(nameW), Height = Val.Auto })
                .Insert(new Text($"{info.Name}  v{info.Version}"))
                .Insert(new TextFont { FontId = 1, Size = 13 })
                .Insert(new TextColor(info.Enabled ? TextMain : TextDim))
                .Insert(new ModRowLabel { Index = idx });
            commands.AddChild(card, label.Id);

            // Enable/disable toggle pill (track + knob; knob position = JustifyContent).
            var track = commands.Spawn()
                .Insert(new Node
                {
                    Display = Display.Flex, FlexDirection = FlexDirection.Row, AlignItems = AlignItems.Center,
                    Width = Val.Px(ToggleW), Height = Val.Px(ToggleH),
                    JustifyContent = info.Enabled ? JustifyContent.End : JustifyContent.Start,
                    Padding = new UiRect { Left = Val.Px(3), Right = Val.Px(3) },
                })
                .Insert(new BackgroundColor(info.Enabled ? ToggleOn : ToggleOff))
                .Insert(BorderRadius.All(12))
                .Insert(new ModToggleControl { Index = idx })
                .Insert(Interaction.None)
                .Insert<UiNoWindowDrag>()
                .Observe((On<UiClick> _, Res<ModControl> ctl) =>
                {
                    if (idx >= ctl.Value.Mods.Count) return;
                    if (ctl.Value.Mods[idx].Enabled) ctl.Value.Disable(idx);
                    else ctl.Value.Enable(idx);
                });
            var trackId = track.Id;
            commands.AddChild(card, trackId);
            commands.AddChild(trackId, commands.Spawn()
                .Insert(new Node { Width = Val.Px(KnobSz), Height = Val.Px(KnobSz) })
                .Insert(new BackgroundColor(Knob))
                .Insert(BorderRadius.All(9))
                .Id);

            // Reload button.
            var reload = commands.Spawn()
                .Insert(new Node
                {
                    Display = Display.Flex, JustifyContent = JustifyContent.Center, AlignItems = AlignItems.Center,
                    Width = Val.Px(ReloadW), Height = Val.Px(26),
                })
                .Insert(new BackgroundColor(ReloadBg))
                .Insert(BorderRadius.All(7))
                .Insert(new Text("Reload"))
                .Insert(new TextFont { FontId = 1, Size = 12 })
                .Insert(new TextColor(TextMain))
                .Insert(Interaction.None)
                .Insert<UiNoWindowDrag>()
                .Observe((On<UiClick> _, Res<ModControl> ctl) =>
                {
                    if (idx < ctl.Value.Mods.Count) ctl.Value.Reload(idx);
                });
            commands.AddChild(card, reload.Id);
        }
    }

    // A card row: flex row, dark card bg, rounded, controls aligned right via fixed
    // widths (Bevy.UI has no space-between — see the layout constants above).
    private static ulong SpawnCard(Commands commands, ulong rootId)
    {
        var card = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex, FlexDirection = FlexDirection.Row, AlignItems = AlignItems.Center,
                Width = Val.Px(PanelW - 2 * Pad), Height = Val.Px(CardH),
                Padding = new UiRect { Left = Val.Px(12), Right = Val.Px(12), Top = Val.Px(8), Bottom = Val.Px(8) },
                Gap = Val.Px(CtrlGap),
            })
            .Insert(new BackgroundColor(CardBg))
            .Insert(BorderRadius.All(9));
        commands.AddChild(rootId, card.Id);
        return card.Id;
    }

    private static void AddText(Commands commands, ulong parent, string text, int font, int size, ClayColor color, Val width)
    {
        var lbl = commands.Spawn()
            .Insert(new Node { Width = width, Height = Val.Auto })
            .Insert(new Text(text))
            .Insert(new TextFont { FontId = (ushort)font, Size = (ushort)size })
            .Insert(new TextColor(color));
        commands.AddChild(parent, lbl.Id);
    }

    // Repaint toggles + dim labels from the live ModControl state (enable/disable/
    // reload apply a frame after the click). Empty + cheap when the panel is closed.
    private static void SyncPanel(
        Res<ModControl> mods,
        Query<Data<Node, BackgroundColor, ModToggleControl>> toggles,
        Query<Data<TextColor, ModRowLabel>> labels)
    {
        // Clay.Color has no equality operator; the rows are few, so just assign.
        foreach (var (_, node, bg, tg) in toggles)
        {
            if (tg.Ref.Index >= mods.Value.Mods.Count) continue;
            var on = mods.Value.Mods[tg.Ref.Index].Enabled;
            node.Ref.JustifyContent = on ? JustifyContent.End : JustifyContent.Start;
            bg.Ref = new BackgroundColor(on ? ToggleOn : ToggleOff);
        }
        foreach (var (_, tc, lbl) in labels)
        {
            if (lbl.Ref.Index >= mods.Value.Mods.Count) continue;
            tc.Ref = new TextColor(mods.Value.Mods[lbl.Ref.Index].Enabled ? TextMain : TextDim);
        }
    }
}
