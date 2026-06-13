using System.Collections.Generic;
using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Network;
using Microsoft.Xna.Framework;
using ClassicUO.Renderer;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using GameLayer = ClassicUO.Game.Data.Layer;

namespace ClassicUO.Ecs;

// Grid-loot window root. Holds the corpse it mirrors + a signature of the
// last-rendered contents so slots only rebuild when the corpse's items change.
internal struct GridLootWindow
{
    public uint CorpseSerial;
    public ulong CorpseEntity;
    public int ContentSig;
}

// One clickable corpse-item cell. Carries the item serial + amount so the
// click-to-grab observer needs no lookup.
internal struct GridLootSlot
{
    public ulong Window;
    public uint Serial;
    public ushort Amount;
}

// Profile.GridLootType: 0 = normal container only, 1 = grid only, 2 = both.
// Ports the legacy GridLootGump trigger (PacketHandlers): a corpse container
// open (gump 0x0009) spawns a grid of item cells; clicking a cell grabs the
// item to the grab-bag / backpack. v1 scope: clickable grid + grab + right-click
// close + auto-close + SkipEmptyCorpse. Deferred vs legacy: paging, set-loot-bag
// button, amount slider, corpse-name label, hover highlight.
internal readonly struct GridLootGumpPlugin : IPlugin
{
    internal const ushort CorpseContainerGump = 0x0009;
    private const int CellSize = 50;
    private const int Cols = 4;
    private const int Pad = 16;
    private const ushort BackgroundGump = 0x0A3C; // resizepic (nine-patch)

    public void Build(App app)
    {
        var openFn = OpenGrids;
        var rebuildFn = RebuildSlots;
        var closeFn = CloseGoneGrids;

        app
            .AddSystem(openFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen)
            .RunIf((Res<Profile> p) => p.Value.GridLootType > 0)
            .Build()

            .AddSystem(rebuildFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen)
            .Build()

            .AddSystem(closeFn)
            .InStage(Stage.Update)
            .RunIf((Res<State<GameState>> s) => s.Value.Current == GameState.GameScreen)
            .Build();
    }

    private static void OpenGrids(
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        ResMut<UiZCounter> zCounter,
        EventReader<ContainerOpenedEvent> reader,
        Query<Data<GridLootWindow>> existingQ)
    {
        foreach (var ev in reader.Read())
        {
            if (ev.Graphic != CorpseContainerGump)
                continue;
            if (!entitiesMap.Value.TryGet(ev.Serial, out var corpseEnt))
                continue;

            bool already = false;
            foreach (var (_, w) in existingQ)
                if (w.Ref.CorpseSerial == ev.Serial) { already = true; break; }
            if (already)
                continue;

            commands.SpawnBundle(new UOGumpBundle
            {
                Position = new Vector2(GridX, GridY),
                Size = new Vector2(Cols * (CellSize + Pad) + Pad, 120),
                BackgroundId = BackgroundGump,
                Hue = Vector3.UnitZ,
                ZOrder = zCounter.Value.Bump(),
                Kind = UOCustomKind.GumpNinePatch,
            })
            .Insert(new GridLootWindow
            {
                CorpseSerial = ev.Serial,
                CorpseEntity = corpseEnt,
                ContentSig = -1,
            });
        }
    }

    // Remember the last window position so a new corpse opens where the last
    // one was (legacy GridLootGump._lastX/_lastY).
    private static int GridX = 100, GridY = 100;

    private static void RebuildSlots(
        Commands commands,
        Res<Profile> profile,
        Res<UOFileManager> fileManager,
        Query<Data<GridLootWindow, Node>> windowsQ,
        Query<Data<GridLootSlot>> slotsQ,
        Query<Data<TinyEcs.Parent, Graphic, Hue, Amount, NetworkSerial>,
            Filter<With<ContainedInto>, Optional<Amount>>> itemsQ,
        Query<Empty, With<AutoOpenedCorpse>> autoQ)
    {
        var tileData = fileManager.Value.TileData.StaticData;

        foreach (var (winEnt, win, node) in windowsQ)
        {
            // Collect this corpse's lootable items.
            var items = new List<(uint serial, ushort graphic, ushort hue, ushort amount)>();
            int sig = 17;
            foreach (var (parent, graphic, hue, amount, serial) in itemsQ)
            {
                if ((ulong)parent.Ref.Id != win.Ref.CorpseEntity)
                    continue;
                var g = graphic.Ref.Value;
                if (SkipLoot(g, tileData))
                    continue;
                var amt = amount.IsValid() ? (ushort)System.Math.Clamp(amount.Ref.Value, 1, ushort.MaxValue) : (ushort)1;
                items.Add((serial.Ref.Value, g, hue.Ref.Value, amt));
                sig = sig * 31 + (int)serial.Ref.Value;
            }

            // SkipEmptyCorpse: an auto-opened corpse that turns out empty stays
            // hidden (legacy GridLootGump._hideIfEmpty). Manual opens always show.
            bool autoOpened = autoQ.Contains(win.Ref.CorpseEntity);
            bool hide = items.Count == 0 && autoOpened && profile.Value.SkipEmptyCorpse;
            var wantDisplay = hide ? Display.None : Display.Flex;
            if (node.Ref.Display != wantDisplay)
                node.Ref.Display = wantDisplay;

            if (sig == win.Ref.ContentSig)
                continue;
            win.Ref.ContentSig = sig;

            // Rebuild cells: drop the old ones, lay out the current items.
            foreach (var (slotEnt, slot) in slotsQ)
                if (slot.Ref.Window == winEnt.Ref)
                    commands.Entity(slotEnt.Ref).Despawn();

            int rows = (items.Count + Cols - 1) / Cols;
            if (rows < 1) rows = 1;
            node.Ref.Width = Val.Px(Cols * (CellSize + Pad) + Pad);
            node.Ref.Height = Val.Px(rows * (CellSize + Pad) + Pad);

            for (int i = 0; i < items.Count; i++)
            {
                var (serial, graphic, hue, amount) = items[i];
                int col = i % Cols, row = i / Cols;
                float x = Pad + col * (CellSize + Pad);
                float y = Pad + row * (CellSize + Pad);
                SpawnCell(commands, winEnt.Ref, serial, graphic, hue, amount, x, y);
            }
        }
    }

    private static void SpawnCell(
        Commands commands, ulong window, uint serial, ushort graphic, ushort hue, ushort amount, float x, float y)
    {
        var drawGraphic = ItemGraphics.Displayed(graphic, amount);
        var hueVec = ShaderHueTranslator.GetHueVector(hue, partial: false, alpha: 1f);

        var cell = commands.Spawn()
            .Insert(new Node
            {
                Display = Display.Flex,
                PositionType = PositionType.Absolute,
                Left = Val.Px(x),
                Top = Val.Px(y),
                Width = Val.Px(CellSize),
                Height = Val.Px(CellSize),
            })
            .Insert(new UiCustom
            {
                Data = new UOCustomRender
                {
                    Kind = UOCustomKind.Art,
                    AssetId = drawGraphic,
                    Hue = hueVec,
                }
            })
            .Insert(Interaction.None)
            // Button makes the cell fire On<UiClick> (Interaction.None alone
            // wouldn't); UiNoWindowDrag keeps the press from latching a window
            // move so the click reaches the grab handler.
            .Insert(new Button())
            .Insert(new GridLootSlot { Window = window, Serial = serial, Amount = amount })
            .Insert<UiNoWindowDrag>();

        // Immutable per-cell capture (serial/amount) — safe inside the observer.
        cell.Observe((On<UiClick> _,
            Res<NetClient> net,
            Res<Profile> prof,
            Query<Data<EquipmentSlots>, Filter<With<Player>>> playerQ,
            Query<Data<NetworkSerial>> serialQ) =>
        {
            uint backpack = 0;
            foreach (var (_, slots) in playerQ)
            {
                var bp = slots.Ref[GameLayer.Backpack];
                if (bp != 0 && serialQ.TryGet(bp, out var bpRow))
                {
                    var (_, ns) = bpRow;
                    backpack = ns.Ref.Value;
                }
                break;
            }
            if (backpack == 0)
                return;

            uint bag = prof.Value.GrabBagSerial != 0 ? prof.Value.GrabBagSerial : backpack;
            net.Value.Send_PickUpRequest(serial, amount);
            net.Value.Send_DropRequest(serial, 0xFFFF, 0xFFFF, 0, 0, bag);
        });

        commands.Entity(window).AddChild(cell.Id);
    }

    private static void CloseGoneGrids(
        Commands commands,
        Res<NetworkEntitiesMap> entitiesMap,
        Query<Data<GridLootWindow, Node>> windowsQ,
        Single<Data<WorldPosition>, With<Player>> playerQ,
        Query<Data<WorldPosition>> worldPosQ)
    {
        const int MaxDist = 3;
        (var _, var playerPos) = playerQ.Get();

        foreach (var (winEnt, win, node) in windowsQ)
        {
            // Remember position for the next corpse.
            if (node.Ref.Left.Value > 0) GridX = (int)node.Ref.Left.Value;
            if (node.Ref.Top.Value > 0) GridY = (int)node.Ref.Top.Value;

            bool gone = !entitiesMap.Value.TryGet(win.Ref.CorpseSerial, out var corpseEnt);
            if (!gone && worldPosQ.TryGet(corpseEnt, out var posRow))
            {
                var (_, pos) = posRow;
                var dist = System.Math.Max(
                    System.Math.Abs(pos.Ref.X - playerPos.Ref.X),
                    System.Math.Abs(pos.Ref.Y - playerPos.Ref.Y));
                if (dist > MaxDist)
                    gone = true;
            }

            if (gone)
                commands.Entity(winEnt.Ref).Despawn();
        }
    }

    // Legacy corpse loot filter: hide un-lootable corpse equipment layers and
    // hair/beard/face. The container here is always a corpse (gump 0x0009).
    private static bool SkipLoot(ushort graphic, StaticTiles[] tileData)
    {
        if (graphic >= tileData.Length)
            return false;
        ref readonly var td = ref tileData[graphic];
        var layer = (GameLayer)td.Layer;
        if (td.Layer > 0 && (int)layer < Constants.BAD_CONTAINER_LAYERS.Length
            && !Constants.BAD_CONTAINER_LAYERS[(int)layer])
            return true;
        if (td.IsWearable && (layer == GameLayer.Face || layer == GameLayer.Beard || layer == GameLayer.Hair))
            return true;
        return false;
    }
}
