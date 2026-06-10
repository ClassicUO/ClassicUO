// Port of legacy NameOverheadGump + NameOverHeadHandlerGump ("object handles").
//
// Nameplates are NOT movable gump windows — they're world-anchored Bevy.UI
// roots (border rect + hued translucent fill + centered name + hp bar) whose
// Node.Left/Top is recomputed each frame from the game entity's WorldPosition,
// exactly like TextOverheadPlugin's speech anchoring. Active while Ctrl+Shift
// is held or the "stay active" toggle is on (legacy GameScene._useObjectHandles).
//
// Interaction model: a Stage.Last system claims SelectedEntity with the GAME
// entity (not the plate UI entity) when the topmost UI hit is a plate. That
// single claim makes the existing world paths work unmodified:
//   * targeting click  -> TargetingPlugin.ResolveTargetClick reads
//     SelectedEntity, finds NetworkSerial/Graphic/WorldPosition -> targets it
//   * double-click     -> UseObjectPlugin (IsPressedDouble) -> Send_DoubleClick
//   * drag from plate  -> PickupPlugin latch (item plates lift the item,
//     mobile plates are rejected by the pickup gate)
//   * drop on plate    -> DropItem world path (give item to mobile)
// Single-click is plate-specific: a clean press+release on a plate arms a
// delayed click (legacy DelayedObjectClickManager); a double-click within the
// window cancels it, otherwise Send_ClickRequest fires after the delay.
// Right-click closes the plate (per-serial, until handles are toggled off).

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ClassicUO.Assets;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using TinyEcs.Bevy.UI.Widgets;
using ClayColor = Clay.Color;
using GameLayer = ClassicUO.Game.Data.Layer;

namespace ClassicUO.Ecs;

[Flags]
internal enum NameplateTypeAllowed
{
    None = 0,
    Items = 1 << 0,
    Corpses = 1 << 1,
    Innocent = 1 << 2,
    Ally = 1 << 3,
    Gray = 1 << 4,
    Criminal = 1 << 5,
    Enemy = 1 << 6,
    Murderer = 1 << 7,
    Invulnerable = 1 << 8,
    All = Items | Corpses | Innocent | Ally | Gray | Criminal | Enemy | Murderer | Invulnerable,
}

// Live state mirroring legacy NameOverHeadManager + GameScene._useObjectHandles.
internal sealed class NameplateState
{
    public NameplateTypeAllowed TypeAllowed = NameplateTypeAllowed.All;
    public bool IsToggled;            // "stay active" checkbox
    public bool ShowHpBar = true;     // legacy Profile.NameOverheadShowHpBar
    public bool Active;               // resolved: IsToggled || Ctrl+Shift

    // Plates closed via right-click stay closed until handles deactivate
    // (legacy ObjectHandlesStatus.CLOSED reset to NONE on handles-off).
    public readonly HashSet<uint> Closed = new();
    // Mobiles we already fired a one-shot Send_StatusRequest for (name fetch).
    public readonly HashSet<uint> StatusRequested = new();
    // serial -> plate root entity
    public readonly Dictionary<uint, ulong> Plates = new();
    // serial -> screen Y of the visible plate's TOP edge (UI logical space),
    // refreshed each frame. Overhead speech reads it to stack ABOVE the plate
    // instead of overlapping it (legacy UpdateTextCoordsV shifts by the gump
    // height when ObjectHandlesStatus == DISPLAYING).
    public readonly Dictionary<uint, float> PlateTops = new();

    public ulong MenuEntity;          // handler menu root (0 = not spawned yet)
    public Vector2? MenuPos;          // persisted across opens (legacy LastPosition)
    // Agent-harness hook (debug.nameplates): synthetic input can't hold
    // Ctrl+Shift, so the menu can be forced visible for screenshots.
    public bool DebugForceMenu;

    // Delayed single-click (legacy DelayedObjectClickManager).
    public uint PendingClickSerial;
    public float PendingClickDeadline;
    // Press latch for the plate click gesture.
    public ulong PressedPlate;
}

// Plate root marker. Caches child entity ids so the per-frame update writes
// straight to them without tree walks.
internal struct NameplateUI
{
    public uint Serial;
    public bool IsMobile;
    public ulong TextEntity;
    public ulong FillEntity;   // hp bar fill (0 on item plates)
    public ushort LastHue;     // last applied notoriety/item hue
    public bool HasName;
}

// Handler-menu checkbox role.
internal enum NameplateCheckKind : byte { StayActive, All, Flag }

internal struct NameplateMenuCheck
{
    public NameplateCheckKind Kind;
    public NameplateTypeAllowed Flag;
}

internal struct NameplateMenuWindow;

internal readonly struct NameplatePlugin : IPlugin
{
    private const int PlateWidth = Constants.OBJECT_HANDLES_GUMP_WIDTH + 4;       // 104
    private const int PlateHeight = Constants.OBJECT_HANDLES_GUMP_HEIGHT + 4;     // 22
    private const int HpBarHeight = Constants.OBJECT_HANDLES_HP_BAR_HEIGHT;       // 3
    private const ushort ItemHue = 0x0481;
    private const ushort CorpseGraphic = 0x2006;
    private const float DoubleClickWindow = 300f;  // TinyEcs.Bevy.Input MouseInput.DoubleClickDelta

    public void Build(App app)
    {
        app.AddResource(new NameplateState());

        var toggleFn = ToggleAndMenu;
        var syncFn = SyncPlates;
        var updateFn = UpdatePlates;
        var claimFn = ClaimSelectedFromPlates;
        var rightCloseFn = CloseOnRightClick;
        var singleClickFn = SingleClick;
        var disposeFn = DisposeOnLogout;

        app
            .AddSystem(toggleFn)
                .InStage(Stage.Update)
                .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen)
                .Build()
            .AddSystem(syncFn)
                .InStage(Stage.Update)
                .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen)
                .Build()
            // PostUpdate: world positions (movement steps / offsets) are settled
            // in Update; layout snapshots Node values after PostUpdate.
            .AddSystem(updateFn)
                .InStage(Stage.PostUpdate)
                .RunIf((Res<NameplateState> st) => st.Value.Plates.Count > 0)
                .Build()
            // Stage.Last next to the container/window claims. Claims only when
            // the topmost rendered element IS a plate, so it never fights them.
            .AddSystem(claimFn)
                .InStage(Stage.Last)
                .RunIf((Res<NameplateState> st) => st.Value.Plates.Count > 0)
                .Build()
            // PreUpdate like WindowDragPlugin.CloseOnRightClick: consume the
            // right press before PlayerMovementPlugin walks the player.
            // Targeting's cancel (Stage.First) already consumed it if targeting.
            .AddSystem(rightCloseFn)
                .InStage(Stage.PreUpdate)
                .RunIf((Res<NameplateState> st) => st.Value.Plates.Count > 0)
                .Build()
            .AddSystem(singleClickFn)
                .InStage(Stage.Update)
                .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen)
                .RunIf((Res<TargetingState> t) => !t.Value.IsTargeting)
                .Build()
            .AddSystem(disposeFn)
                .OnExit(GameState.GameScreen)
                .Build();

        // Handler-menu checkboxes -> state flags. "All" cascades to the
        // individual boxes; individual boxes recompute "All".
        app.AddObserver((
            On<CheckboxChanged> trig,
            Res<NameplateState> state,
            Query<Data<NameplateMenuCheck, Checkbox>> checksQ) =>
        {
            if (!checksQ.TryGet(trig.EntityId, out var row)) return;
            var (_, check, _) = row;
            bool on = trig.Event.Checked;

            switch (check.Ref.Kind)
            {
                case NameplateCheckKind.StayActive:
                    state.Value.IsToggled = on;
                    return;
                case NameplateCheckKind.All:
                    state.Value.TypeAllowed = on ? NameplateTypeAllowed.All : NameplateTypeAllowed.None;
                    break;
                case NameplateCheckKind.Flag:
                    if (on) state.Value.TypeAllowed |= check.Ref.Flag;
                    else state.Value.TypeAllowed &= ~check.Ref.Flag;
                    break;
            }

            // Re-sync every box to the resolved flags (direct Checked writes
            // don't re-trigger CheckboxChanged — that only fires on UiClick).
            foreach (var (_, c, cb) in checksQ)
            {
                cb.Ref.Checked = c.Ref.Kind switch
                {
                    NameplateCheckKind.StayActive => state.Value.IsToggled,
                    NameplateCheckKind.All => state.Value.TypeAllowed == NameplateTypeAllowed.All,
                    _ => state.Value.TypeAllowed.HasFlag(c.Ref.Flag),
                };
            }
        });
    }

    // ------------------------------------------------------------------
    // Activation + handler menu (legacy GameScene.Update object-handles block)
    // ------------------------------------------------------------------

    private static void ToggleAndMenu(
        Commands commands,
        Res<KeyboardContext> keyboard,
        Res<NameplateState> state,
        Res<UOFileManager> fileManager,
        Res<AssetsServer> assets,
        Res<UiZCounter> zCounter,
        Query<Data<Node>, Filter<With<NameplateMenuWindow>>> menuQ)
    {
        bool ctrlShift =
            (keyboard.Value.IsPressed(Keys.LeftControl) || keyboard.Value.IsPressed(Keys.RightControl))
            && (keyboard.Value.IsPressed(Keys.LeftShift) || keyboard.Value.IsPressed(Keys.RightShift))
            || state.Value.DebugForceMenu;

        bool active = state.Value.IsToggled || ctrlShift;

        // Persist the menu's dragged position.
        if (state.Value.MenuEntity != 0 && menuQ.TryGet(state.Value.MenuEntity, out var menuRow))
        {
            var (_, node) = menuRow;
            if (node.Ref.Left.Type == ValType.Px)
                state.Value.MenuPos = new Vector2(node.Ref.Left.Value, node.Ref.Top.Value);
        }

        if (active != state.Value.Active)
        {
            state.Value.Active = active;
            if (active)
            {
                if (state.Value.MenuEntity == 0 || !menuQ.Contains(state.Value.MenuEntity))
                    state.Value.MenuEntity = BuildMenu(commands, state.Value, fileManager.Value, assets.Value, zCounter.Value);
                SetMenuVisible(menuQ, state.Value.MenuEntity, !(state.Value.IsToggled && !ctrlShift));
            }
            else
            {
                SetMenuVisible(menuQ, state.Value.MenuEntity, false);
                state.Value.Closed.Clear();
            }
        }
        else if (active && state.Value.IsToggled)
        {
            SetMenuVisible(menuQ, state.Value.MenuEntity, ctrlShift);
        }
    }

    private static void SetMenuVisible(Query<Data<Node>, Filter<With<NameplateMenuWindow>>> menuQ, ulong menu, bool visible)
    {
        if (menu == 0 || !menuQ.TryGet(menu, out var row)) return;
        var (_, node) = row;
        node.Ref.Display = visible ? Display.Flex : Display.None;
    }

    // ------------------------------------------------------------------
    // Plate lifecycle
    // ------------------------------------------------------------------

    private static bool IsAllowed(NameplateState state, bool isMobile, ushort graphic, NotorietyFlag notoriety)
    {
        if (!isMobile)
        {
            return graphic == CorpseGraphic
                ? state.TypeAllowed.HasFlag(NameplateTypeAllowed.Corpses)
                : state.TypeAllowed.HasFlag(NameplateTypeAllowed.Items);
        }

        return notoriety switch
        {
            NotorietyFlag.Innocent => state.TypeAllowed.HasFlag(NameplateTypeAllowed.Innocent),
            NotorietyFlag.Ally => state.TypeAllowed.HasFlag(NameplateTypeAllowed.Ally),
            NotorietyFlag.Gray => state.TypeAllowed.HasFlag(NameplateTypeAllowed.Gray),
            NotorietyFlag.Criminal => state.TypeAllowed.HasFlag(NameplateTypeAllowed.Criminal),
            NotorietyFlag.Enemy => state.TypeAllowed.HasFlag(NameplateTypeAllowed.Enemy),
            NotorietyFlag.Murderer => state.TypeAllowed.HasFlag(NameplateTypeAllowed.Murderer),
            NotorietyFlag.Invulnerable => state.TypeAllowed.HasFlag(NameplateTypeAllowed.Invulnerable),
            _ => true,
        };
    }

    private static void SyncPlates(
        Commands commands,
        Res<NameplateState> state,
        Res<NetClient> net,
        Query<Data<NetworkSerial, WorldPosition, Graphic, Notoriety>,
            Filter<Without<ContainedInto>, Without<IsMulti>, Optional<Notoriety>>> worldQ,
        Query<Data<NameplateUI>> platesQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        var st = state.Value;

        if (!st.Active)
        {
            if (st.Plates.Count > 0)
            {
                foreach (var plate in st.Plates.Values)
                    UiHierarchy.DespawnSubtree(commands, plate, childrenQ);
                st.Plates.Clear();
            }
            st.StatusRequested.Clear();
            st.PlateTops.Clear();
            return;
        }

        // Eligible world entities this frame.
        var eligible = new HashSet<uint>();
        foreach (var (ent, serial, _, graphic, notoriety) in worldQ)
        {
            var s = serial.Ref.Value;
            bool isMobile = SerialHelper.IsMobile(s);
            if (!isMobile && !SerialHelper.IsItem(s)) continue;
            if (st.Closed.Contains(s)) continue;

            var noto = notoriety.IsValid() ? notoriety.Ref.Value : NotorietyFlag.Unknown;
            if (!IsAllowed(st, isMobile, graphic.Ref.Value, noto)) continue;

            eligible.Add(s);

            if (!st.Plates.ContainsKey(s))
            {
                st.Plates[s] = BuildPlate(commands, s, isMobile, st.ShowHpBar && isMobile);
                // Mobile names arrive via 0x11 status / OPL — fire a one-shot
                // status request so the plate isn't blank (HealthBarPlugin
                // does the same on open).
                if (isMobile && st.StatusRequested.Add(s))
                    net.Value.Send_StatusRequest(s);
            }
        }

        // Despawn plates whose entity vanished or no longer passes the filter.
        if (st.Plates.Count != eligible.Count)
        {
            List<uint> dead = null;
            foreach (var (s, plate) in st.Plates)
            {
                if (eligible.Contains(s)) continue;
                (dead ??= new()).Add(s);
                if (platesQ.Contains(plate))
                    UiHierarchy.DespawnSubtree(commands, plate, childrenQ);
            }
            if (dead != null)
                foreach (var s in dead)
                    st.Plates.Remove(s);
        }
    }

    private static ulong BuildPlate(Commands commands, uint serial, bool isMobile, bool hpBar)
    {
        int height = PlateHeight + (hpBar ? HpBarHeight + 1 : 0);

        // Single bordered node: 1px black Clay border (legacy DrawRectangle
        // outline) + hued translucent fill (legacy AlphaBlendControl(.7)).
        // Content is a flex column: name row, then the hp bar row.
        // PositionType stays Absolute on the ROOT only — it's world-anchored.
        var root = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.None,   // hidden until positioned + named
                PositionType = PositionType.Absolute,
                Width = Val.Px(PlateWidth + 2),
                Height = Val.Px(height + 2),
                Border = UiRect.All(1),
                FlexDirection = FlexDirection.Column,
            })
            .Insert(new BackgroundColor(new ClayColor(0, 0, 0, 178)))
            .Insert(new BorderColor(new ClayColor(0, 0, 0, 255)))
            // z 0: above the world (roots need a GlobalZIndex to outdraw it),
            // below every window (UiZCounter starts at 1).
            .Insert(new GlobalZIndex(0));

        // Name row fills the space above the bar. Legacy draws the label at
        // plate top + 2 (not vertically centered), so top-align with padding.
        var nameRow = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Relative,
                Width = Val.Percent(100),
                Height = Val.Px(height - (hpBar ? HpBarHeight : 0)),
                JustifyContent = JustifyContent.Center,
                AlignItems = AlignItems.Start,
                Padding = new UiRect(Val.Px(0), Val.Px(0), Val.Px(2), Val.Px(0)),
            });

        var text = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Relative,
                Width = Val.Auto,
                Height = Val.Auto,
            })
            .Insert(new Text(string.Empty))
            .Insert(new TextFont { FontId = UoFontRuntime.DefaultFont, Size = 16 })
            .Insert(new TextColor(ClayColor.White));
        nameRow.AddChild(text);
        root.AddChild(nameRow);

        ulong fillId = 0;
        if (hpBar)
        {
            // Black track row with the colored fill flowing from the left.
            var track = commands.Spawn()
                .Insert(new Node
                {
                    Display = Display.Flex,
                    PositionType = PositionType.Relative,
                    Width = Val.Percent(100),
                    Height = Val.Px(HpBarHeight),
                })
                .Insert(new BackgroundColor(new ClayColor(0, 0, 0, 255)));
            var fill = commands.Spawn()
                .Insert(new Node
                {
                    Display = Display.Flex,
                    PositionType = PositionType.Relative,
                    Width = Val.Px(0),
                    Height = Val.Percent(100),
                })
                .Insert(new BackgroundColor(new ClayColor(0, 128, 0, 255)));
            track.AddChild(fill);
            root.AddChild(track);
            fillId = fill.Id;
        }

        root.Insert(new NameplateUI
        {
            Serial = serial,
            IsMobile = isMobile,
            TextEntity = text.Id,
            FillEntity = fillId,
        });
        return root.Id;
    }

    // ------------------------------------------------------------------
    // Per-frame anchor + content refresh
    // ------------------------------------------------------------------

    private static void UpdatePlates(
        Res<NameplateState> state,
        Res<GameContext> gameCtx,
        Res<Camera> camera,
        Res<NetworkEntitiesMap> entitiesMap,
        Res<UOFileManager> fileManager,
        Res<AssetsServer> assets,
        Res<ObjectPropertyLists> opl,
        Query<Data<NameplateUI, Node>> platesQ,
        Query<Data<WorldPosition, ScreenPositionOffset, Notoriety, Hits, EntityName, Graphic, Amount, EquipmentSlots, ServerFlags>,
            Filter<Optional<ScreenPositionOffset>, Optional<Notoriety>, Optional<Hits>, Optional<EntityName>, Optional<Amount>, Optional<EquipmentSlots>, Optional<ServerFlags>>> gameQ,
        Query<Data<Text, TextColor>> textQ,
        Query<Data<Node, BackgroundColor>> rectQ)
    {
        // Same camera math as TextOverheadPlugin.Render — plates live in the
        // logical UI space Clay lays out in.
        var center = Isometric.IsoToScreen(gameCtx.Value.CenterX, gameCtx.Value.CenterY, gameCtx.Value.CenterZ);
        var windowSize = new Vector2(camera.Value.Bounds.Width, camera.Value.Bounds.Height);
        center -= windowSize / 2f;
        center.X += 22f;
        center.Y += 22f;
        center -= gameCtx.Value.CenterOffset;
        var panelOrigin = new Vector2(camera.Value.Bounds.X, camera.Value.Bounds.Y);
        var hues = fileManager.Value.Hues;

        state.Value.PlateTops.Clear();

        foreach (var (plateEnt, plate, node) in platesQ)
        {
            if (!entitiesMap.Value.TryGet(plate.Ref.Serial, out var gameEnt)
                || !gameQ.TryGet(gameEnt, out var gameRow))
            {
                node.Ref.Display = Display.None;
                continue;
            }

            var (_, worldPos, offset, notoriety, hits, name, graphic, amount, equip, flags) = gameRow;

            // Name: mobiles from EntityName (status reply), items from OPL
            // with tiledata fallback (legacy SetName).
            if (!plate.Ref.HasName)
            {
                string resolved = ResolveName(plate.Ref, fileManager.Value, opl.Value,
                    name.IsValid() ? name.Ref.Value : null,
                    graphic.Ref.Value,
                    amount.IsValid() ? amount.Ref.Value : 1);

                if (!string.IsNullOrEmpty(resolved) && textQ.TryGet(plate.Ref.TextEntity, out var textRow))
                {
                    var (_, text, _) = textRow;
                    text.Ref.Value = TruncateToPlate(resolved);
                    plate.Ref.HasName = true;
                }
            }

            // Hue follows notoriety live (legacy Update()).
            ushort hue = plate.Ref.IsMobile
                ? NotorietyHue(notoriety.IsValid() ? notoriety.Ref.Value : NotorietyFlag.Unknown)
                : ItemHue;
            if (hue != plate.Ref.LastHue)
            {
                plate.Ref.LastHue = hue;
                ApplyHue(plate.Ref, plateEnt.Ref, hue, hues, textQ, rectQ);
            }

            // HP bar fill (legacy HitsPercentage thresholds).
            if (plate.Ref.FillEntity != 0 && hits.IsValid() && rectQ.TryGet(plate.Ref.FillEntity, out var fillRow))
            {
                int pct = hits.Ref.MaxValue > 0 ? hits.Ref.Value * 100 / hits.Ref.MaxValue : 0;
                var (_, fillNode, fillBg) = fillRow;
                fillNode.Ref.Width = Val.Percent(pct);
                fillBg.Ref.Value =
                    pct >= 80 ? new ClayColor(0, 128, 0, 255)        // Green
                    : pct >= 50 ? new ClayColor(154, 205, 50, 255)   // YellowGreen
                    : pct >= 30 ? new ClayColor(255, 255, 0, 255)    // Yellow
                    : new ClayColor(255, 0, 0, 255);                 // Red
            }

            // Anchor: legacy NameOverheadGump.AddToRenderLists. Mobiles measure
            // the FIXED standing frame (dir 0, group 0, frame 0) so the anchor
            // doesn't bounce with the walk cycle, then sit above the head;
            // items hang at half the art's real height.
            var position = Isometric.IsoToScreen(worldPos.Ref.X, worldPos.Ref.Y, worldPos.Ref.Z);
            if (offset.IsValid())
                position += offset.Ref.Value;
            position -= center;
            position.X += 22f + 5f;
            if (plate.Ref.IsMobile)
            {
                bool mounted = equip.IsValid() && equip.Ref[GameLayer.Mount] != 0;
                assets.Value.Animations.GetAnimationDimensions(
                    0, graphic.Ref.Value, 0, 0, mounted, 0,
                    out _, out int animCenterY, out _, out int animHeight);
                bool flyingGargoyle = flags.IsValid()
                    && (flags.Ref.Value & Flags.Flying) != 0
                    && IsGargoyle(graphic.Ref.Value);
                position.Y -= animHeight + animCenterY + 8 + 10;
                position.Y += flyingGargoyle ? -22 : (mounted ? 0 : 22);
            }
            else
            {
                var artBounds = assets.Value.Arts.GetRealArtBounds(graphic.Ref.Value);
                position.Y += artBounds.Height >> 1;
            }
            position = camera.Value.WorldToScreen(position) + panelOrigin;

            float w = PlateWidth + 2;
            float h = node.Ref.Height.Value;
            float x = position.X - w / 2f;
            float y = position.Y - h / 2f;

            // Cull when off the world viewport (legacy bails outside camera
            // bounds) or still nameless.
            var b = camera.Value.Bounds;
            bool visible = plate.Ref.HasName
                && x >= b.X && x + w <= b.Right
                && y >= b.Y && y + h <= b.Bottom;

            node.Ref.Display = visible ? Display.Flex : Display.None;
            node.Ref.Left = Val.Px(x);
            node.Ref.Top = Val.Px(y);

            if (visible)
                state.Value.PlateTops[plate.Ref.Serial] = y;
        }
    }

    private static string ResolveName(
        in NameplateUI plate,
        UOFileManager fileManager,
        ObjectPropertyLists opl,
        string entityName,
        ushort graphic,
        int amount)
    {
        if (plate.IsMobile)
            return entityName;

        if (opl.TryGet(plate.Serial, out var entry) && !string.IsNullOrEmpty(entry.Name))
            return entry.Name;
        opl.Request(plate.Serial);

        // Tiledata fallback (legacy: amount prefix + pluralized tiledata name).
        var tileData = fileManager.TileData.StaticData;
        if (graphic >= tileData.Length) return null;
        var tdName = tileData[graphic].Name;
        if (string.IsNullOrEmpty(tdName)) return null;
        return amount > 1 && graphic != CorpseGraphic ? $"{amount} {tdName}" : tdName;
    }

    private static string TruncateToPlate(string name)
    {
        var (w, _) = UoFontRenderer.MeasureFont(name, UoFontRuntime.DefaultFont, 1000, allowHtml: false);
        if (w <= Constants.OBJECT_HANDLES_GUMP_WIDTH) return name;

        while (name.Length > 1)
        {
            name = name[..^1];
            (w, _) = UoFontRenderer.MeasureFont(name + "...", UoFontRuntime.DefaultFont, 1000, allowHtml: false);
            if (w <= Constants.OBJECT_HANDLES_GUMP_WIDTH) break;
        }
        return name + "...";
    }

    // Legacy Mobile.IsGargoyle — flying gargoyles get the -22 anchor shift.
    private static bool IsGargoyle(ushort graphic)
        => graphic == 0x029A || graphic == 0x029B || graphic == 0x02B6 || graphic == 0x02B7;

    // Legacy Notoriety.GetHue with default profile hues (same table
    // HealthBarPlugin uses).
    private static ushort NotorietyHue(NotorietyFlag flag) => flag switch
    {
        NotorietyFlag.Innocent => 0x005A,
        NotorietyFlag.Ally => 0x0044,
        NotorietyFlag.Criminal => 0x03B2,
        NotorietyFlag.Gray => 0x03B2,
        NotorietyFlag.Enemy => 0x0031,
        NotorietyFlag.Murderer => 0x0023,
        NotorietyFlag.Invulnerable => 0x0034,
        _ => ItemHue,
    };

    private static void ApplyHue(
        in NameplateUI plate,
        ulong rootEnt,
        ushort hue,
        HuesLoader hues,
        Query<Data<Text, TextColor>> textQ,
        Query<Data<Node, BackgroundColor>> rectQ)
    {
        // Background: legacy hued AlphaBlendControl over a black texture —
        // the hue shader maps black to the gradient's darkest cell.
        if (rectQ.TryGet(rootEnt, out var bgRow))
        {
            var (_, _, bg) = bgRow;
            var dark = UnpackHue(hues.GetPolygoneColor(4, (ushort)(hue + 1)));
            bg.Ref.Value = new ClayColor(dark.R, dark.G, dark.B, 178);
        }
        // Text: bright hue cell, same convention as ServerGumpPlugin labels.
        if (textQ.TryGet(plate.TextEntity, out var colorRow))
        {
            var (_, _, color) = colorRow;
            var bright = UnpackHue(hues.GetPolygoneColor(30, (ushort)(hue + 1)));
            color.Ref.Value = new ClayColor(bright.R, bright.G, bright.B, 255);
        }
    }

    private static (byte R, byte G, byte B) UnpackHue(uint packed)
        => ((byte)(packed & 0xFF), (byte)((packed >> 8) & 0xFF), (byte)((packed >> 16) & 0xFF));

    // ------------------------------------------------------------------
    // Interaction
    // ------------------------------------------------------------------

    // Resolve a UiPick hit (plate root or one of its children) to the plate root.
    private static ulong PlateRoot(
        ulong entity,
        Query<Data<NameplateUI>> platesQ,
        Query<Data<TinyEcs.Parent>> parents)
    {
        ulong cur = entity;
        for (int i = 0; i < 8 && cur != 0; i++)
        {
            if (platesQ.Contains(cur)) return cur;
            if (!parents.Contains(cur)) return 0;
            var (_, p) = parents.Get(cur);
            cur = (ulong)p.Ref.Id;
        }
        return 0;
    }

    // Claim SelectedEntity with the GAME entity under the plate so targeting /
    // double-click / pickup / drop all route to the serial. Mirrors
    // ContainerGumpPlugin.UpdateSelectedFromContainerUI (claims only when the
    // plate is the true topmost element, so window claims always win on top).
    private static void ClaimSelectedFromPlates(
        Res<MouseContext> mouse,
        Res<SelectedEntity> selected,
        Res<AssetsServer> assets,
        Res<NetworkEntitiesMap> entitiesMap,
        Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> rendered,
        Query<Data<TinyEcs.Parent>> parents,
        Query<Data<NameplateUI>> platesQ)
    {
        var hit = UiPick.Topmost(mouse.Value.Position, assets.Value, rendered, parents);
        if (!hit.Found) return;

        var plateEnt = PlateRoot(hit.Entity, platesQ, parents);
        if (plateEnt == 0) return;

        if (!platesQ.TryGet(plateEnt, out var plateRow)) return;
        var (_, plate) = plateRow;
        if (!entitiesMap.Value.TryGet(plate.Ref.Serial, out var gameEnt)) return;

        selected.Value.Set(gameEnt, float.MaxValue, bypassViewport: true);
    }

    // Right-click closes the plate under the cursor (press+release on the same
    // plate, like UiClick semantics). Consumes the press so the player doesn't
    // walk. Targeting cancel ran earlier in Stage.First and wins when active.
    private static void CloseOnRightClick(
        Commands commands,
        Res<MouseContext> mouse,
        Res<AssetsServer> assets,
        Res<NameplateState> state,
        Local<ulong> pressTarget,
        Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> rendered,
        Query<Data<TinyEcs.Parent>> parents,
        Query<Data<NameplateUI>> platesQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        bool once = mouse.Value.IsPressedOnce(MouseButtonType.Right);
        bool held = mouse.Value.IsPressed(MouseButtonType.Right);
        bool released = mouse.Value.IsReleased(MouseButtonType.Right);

        if (!once && !held && !released && pressTarget.Value == 0)
            return;

        if (once)
        {
            var hit = UiPick.Topmost(mouse.Value.Position, assets.Value, rendered, parents);
            pressTarget.Value = hit.Found ? PlateRoot(hit.Entity, platesQ, parents) : 0;
            if (pressTarget.Value != 0)
                mouse.Value.Consume(MouseButtonType.Right);
            return;
        }

        if (held)
        {
            if (pressTarget.Value != 0)
                mouse.Value.Consume(MouseButtonType.Right);
            return;
        }

        if (released)
        {
            var target = pressTarget.Value;
            pressTarget.Value = 0;
            if (target == 0) return;

            mouse.Value.Consume(MouseButtonType.Right);

            var hit = UiPick.Topmost(mouse.Value.Position, assets.Value, rendered, parents);
            if (!hit.Found || PlateRoot(hit.Entity, platesQ, parents) != target) return;
            if (!platesQ.TryGet(target, out var plateRow)) return;

            var (_, plate) = plateRow;
            state.Value.Closed.Add(plate.Ref.Serial);
            state.Value.Plates.Remove(plate.Ref.Serial);
            UiHierarchy.DespawnSubtree(commands, target, childrenQ);
        }
    }

    // Clean press+release on a plate arms a delayed single click; a
    // double-click within the window cancels it (UseObjectPlugin owns the
    // double-click via the SelectedEntity claim). Legacy
    // DelayedObjectClickManager semantics.
    private static void SingleClick(
        Res<MouseContext> mouse,
        Res<Time> time,
        Res<NetClient> net,
        Res<NameplateState> state,
        Res<AssetsServer> assets,
        Res<GrabbedItem> grabbed,
        Query<Data<ComputedNode, Node, UiCustom, BackgroundColor, Text>, Filter<Optional<UiCustom>, Optional<BackgroundColor>, Optional<Text>>> rendered,
        Query<Data<TinyEcs.Parent>> parents,
        Query<Data<NameplateUI>> platesQ)
    {
        var st = state.Value;

        // A second click landed -> the double-click path owns the gesture.
        if (st.PendingClickSerial != 0 && mouse.Value.IsPressedDouble(MouseButtonType.Left))
            st.PendingClickSerial = 0;

        if (st.PendingClickSerial != 0 && time.Value.Total >= st.PendingClickDeadline)
        {
            net.Value.Send_ClickRequest(st.PendingClickSerial);
            st.PendingClickSerial = 0;
        }

        if (mouse.Value.IsPressedOnce(MouseButtonType.Left))
        {
            var hit = UiPick.Topmost(mouse.Value.Position, assets.Value, rendered, parents);
            st.PressedPlate = hit.Found ? PlateRoot(hit.Entity, platesQ, parents) : 0;
            return;
        }

        if (mouse.Value.IsReleased(MouseButtonType.Left) && st.PressedPlate != 0)
        {
            var target = st.PressedPlate;
            st.PressedPlate = 0;

            // Dragged off / picked something up -> not a click.
            if (grabbed.Value.Serial != 0) return;
            var dragOffset = mouse.Value.DraggingOffset;
            if (Math.Abs(dragOffset.X) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS
                || Math.Abs(dragOffset.Y) >= Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS) return;

            var hit = UiPick.Topmost(mouse.Value.Position, assets.Value, rendered, parents);
            if (!hit.Found || PlateRoot(hit.Entity, platesQ, parents) != target) return;
            if (!platesQ.TryGet(target, out var plateRow)) return;

            var (_, plate) = plateRow;
            st.PendingClickSerial = plate.Ref.Serial;
            st.PendingClickDeadline = time.Value.Total + DoubleClickWindow;
        }
    }

    // ------------------------------------------------------------------
    // Handler menu (legacy NameOverHeadHandlerGump)
    // ------------------------------------------------------------------

    private static ulong BuildMenu(
        Commands commands,
        NameplateState state,
        UOFileManager fileManager,
        AssetsServer assets,
        UiZCounter zCounter)
    {
        var pos = state.MenuPos ?? new Vector2(100, 100);
        var bg = UnpackHue(fileManager.Hues.GetPolygoneColor(4, 34 + 1));

        var root = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(pos.X),
                Top = Val.Px(pos.Y),
                Width = Val.Px(120),
                Height = Val.Auto,
                FlexDirection = FlexDirection.Column,
            })
            .Insert(new BackgroundColor(new ClayColor(bg.R, bg.G, bg.B, 178)))
            .Insert(Interaction.None)
            .Insert(new NameplateMenuWindow())
            .Insert<UiMovable>()
            // Legacy CanCloseWithRightClick = false: WindowDragPlugin skips
            // UiNoRightClickClose windows in its close pass.
            .Insert<UiNoRightClickClose>()
            .Insert(new GlobalZIndex(zCounter.Bump()));

        AddMenuRow(commands, assets, root, "Stay active", state.IsToggled,
            new NameplateMenuCheck { Kind = NameplateCheckKind.StayActive });
        AddMenuRow(commands, assets, root, "All", state.TypeAllowed == NameplateTypeAllowed.All,
            new NameplateMenuCheck { Kind = NameplateCheckKind.All });

        var flags = new (string, NameplateTypeAllowed)[]
        {
            ("Items", NameplateTypeAllowed.Items),
            ("Corpses", NameplateTypeAllowed.Corpses),
            ("Innocent", NameplateTypeAllowed.Innocent),
            ("Ally", NameplateTypeAllowed.Ally),
            ("Gray", NameplateTypeAllowed.Gray),
            ("Criminal", NameplateTypeAllowed.Criminal),
            ("Enemy", NameplateTypeAllowed.Enemy),
            ("Murderer", NameplateTypeAllowed.Murderer),
            ("Invulnerable", NameplateTypeAllowed.Invulnerable),
        };
        foreach (var (label, flag) in flags)
        {
            AddMenuRow(commands, assets, root, label, state.TypeAllowed.HasFlag(flag),
                new NameplateMenuCheck { Kind = NameplateCheckKind.Flag, Flag = flag });
        }

        return root.Id;
    }

    private static void AddMenuRow(
        Commands commands,
        AssetsServer assets,
        EntityCommands parent,
        string label,
        bool isChecked,
        NameplateMenuCheck check)
    {
        ref readonly var off = ref assets.Gumps.GetGump(GumpBuilder.CheckboxOff);
        float boxW = off.UV.Width > 0 ? off.UV.Width : 19;
        float boxH = off.UV.Height > 0 ? off.UV.Height : 19;

        var row = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Relative,
                Width = Val.Percent(100),
                Height = Val.Px(boxH + 2),
                FlexDirection = FlexDirection.Row,
                AlignItems = AlignItems.Center,
                Gap = Val.Px(2),
            });

        var box = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Relative,
                Width = Val.Px(boxW),
                Height = Val.Px(boxH),
            })
            .Insert(new UiCustom
            {
                Data = new UOCustomRender
                {
                    Kind = UOCustomKind.Gump,
                    AssetId = isChecked ? GumpBuilder.CheckboxOn : GumpBuilder.CheckboxOff,
                    Hue = Vector3.UnitZ,
                }
            })
            .Insert(new UOCheckbox { Off = GumpBuilder.CheckboxOff, On = GumpBuilder.CheckboxOn })
            .Insert(new Checkbox { Checked = isChecked })
            .Insert(check)
            .Insert(Interaction.None)
            .Insert<UiNoWindowDrag>();

        var text = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Relative,
                Width = Val.Auto,
                Height = Val.Auto,
            })
            .Insert(new Text(label))
            .Insert(new TextFont { FontId = UoFontRuntime.DefaultFont, Size = 16 })
            .Insert(new TextColor(ClayColor.White))
            .Insert(Interaction.None)
            .Insert<UiContainsByBounds>()
            .Insert<UiNoWindowDrag>()
            .Insert(new CheckboxLabel { Target = box.Id });

        row.AddChild(box);
        row.AddChild(text);
        parent.AddChild(row);
    }

    // ------------------------------------------------------------------

    private static void DisposeOnLogout(
        Commands commands,
        Res<NameplateState> state,
        Query<Data<NameplateUI>> platesQ,
        Query<Data<Node>, Filter<With<NameplateMenuWindow>>> menuQ,
        Query<Data<TinyEcs.Children>> childrenQ)
    {
        foreach (var plate in state.Value.Plates.Values)
            if (platesQ.Contains(plate))
                UiHierarchy.DespawnSubtree(commands, plate, childrenQ);
        if (state.Value.MenuEntity != 0 && menuQ.Contains(state.Value.MenuEntity))
            UiHierarchy.DespawnSubtree(commands, state.Value.MenuEntity, childrenQ);

        state.Value.Plates.Clear();
        state.Value.Closed.Clear();
        state.Value.StatusRequested.Clear();
        state.Value.MenuEntity = 0;
        state.Value.Active = false;
        state.Value.PendingClickSerial = 0;
        state.Value.PressedPlate = 0;
    }
}
