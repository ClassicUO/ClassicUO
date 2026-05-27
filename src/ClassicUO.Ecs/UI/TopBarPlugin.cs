// Minimal port of main's TopBarGump (src/Game/UI/Gumps/TopBarGump.cs):
// the always-on-top toolbar with Map / Paperdoll / Inventory / Journal /
// Chat / Help / WorldMap / Info / Debug / NetStats / UOStore / GlobalChat
// buttons. Spawned once on entering GameScreen, despawned on exit.
//
// Scope of this port:
//   * Background ResizePic (gump 0x13BE).
//   * 12 button slots using 0x098B (small) / 0x098D (large) graphics
//     with hardcoded captions (cliloc lookups deferred until ClilocLoader
//     is wired into the ECS UI text path).
//   * Wired actions: Paperdoll → Send_DoubleClick(player). All other
//     buttons spawn but no-op for now — port the corresponding gumps /
//     scenes as separate gaps.
//   * Page/minimize button (0x15A4 / 0x15A1) NOT yet wired — needs Clay
//     page-switching, deferred.

using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Game;
using ClassicUO.Network;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

internal readonly struct TopBarPlugin : IPlugin
{
    private static readonly object CustomMarker = new();

    public void Build(App app)
    {
        var spawnFn = Spawn;
        var despawnFn = Despawn;

        app
            .AddSystem(spawnFn)
            .OnEnter(GameState.GameScreen)
            .Build()

            .AddSystem(despawnFn)
            .OnExit(GameState.GameScreen)
            .Build();
    }

    // Mirrors TopBarGump.textTable: 0 = small (0x098B), 1 = large (0x098D).
    private static readonly (int sizeKind, string caption, Buttons id)[] s_buttons =
    {
        (0, "Map",        Buttons.Map),
        (1, "Paperdoll",  Buttons.Paperdoll),
        (1, "Inventory",  Buttons.Inventory),
        (1, "Journal",    Buttons.Journal),
        (0, "Chat",       Buttons.Chat),
        (0, "Help",       Buttons.Help),
        (1, "World Map",  Buttons.WorldMap),
        (0, "Info",       Buttons.Info),
        (0, "Debug",      Buttons.Debug),
        (1, "NetStats",   Buttons.NetStats),
        (1, "UO Store",   Buttons.UOStore),
        (1, "Global Chat", Buttons.GlobalChat),
    };

    private enum Buttons : int
    {
        Map,
        Paperdoll,
        Inventory,
        Journal,
        Chat,
        Help,
        WorldMap,
        Info,
        Debug,
        NetStats,
        UOStore,
        GlobalChat,
    }

    private static void Spawn(
        Commands commands,
        Res<AssetsServer> assets,
        Res<GameContext> gameCtx)
    {
        // Resolve button gump dims once. Fallback to main's hard-coded
        // 50 / 100 if the asset is missing — matches TopBarGump.ctor.
        var smallInfo = assets.Value.Gumps.GetGump(0x098B);
        var smallWidth = smallInfo.UV.Width > 0 ? smallInfo.UV.Width : 50;
        var smallHeight = smallInfo.UV.Height > 0 ? smallInfo.UV.Height : 22;

        var largeInfo = assets.Value.Gumps.GetGump(0x098D);
        var largeWidth = largeInfo.UV.Width > 0 ? largeInfo.UV.Width : 100;
        var largeHeight = largeInfo.UV.Height > 0 ? largeInfo.UV.Height : 22;

        // Compute total row width.
        var hasUOStore = gameCtx.Value.ClientVersion >= ClassicUO.Utility.ClientVersion.CV_706400;
        var startX = 30;
        for (var i = 0; i < s_buttons.Length; i++)
        {
            if (!hasUOStore && (int)s_buttons[i].id >= (int)Buttons.UOStore) break;
            startX += (s_buttons[i].sizeKind == 1 ? largeWidth : smallWidth) + 1;
        }
        var totalWidth = startX + 1;
        const int BarHeight = 27;

        // Root container at (0, 0). Z-index high so it sits over world / UI.
        var root = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(0),
                Top = Val.Px(0),
                Width = Val.Px(totalWidth),
                Height = Val.Px(BarHeight),
            })
            .Insert(new ZIndex(50))
            .Insert<IsTopBar>();

        // Background ResizePic (0x13BE).
        ref readonly var bgInfo = ref assets.Value.Gumps.GetGump(0x13BE);
        var bg = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(0),
                Top = Val.Px(0),
                Width = Val.Px(totalWidth),
                Height = Val.Px(BarHeight),
            })
            .Insert(new UiCustom { Data = CustomMarker })
            .Insert(new UOCustomRender
            {
                Kind = UOCustomKind.GumpNinePatch,
                AssetId = 0x13BE,
                Hue = Vector3.UnitZ,
            });
        commands.AddChild(root.Id, bg.Id);

        // Minimize-toggle button (0x15A4) at (5, 3). Page-switching not
        // implemented yet — click currently no-ops.
        var minBtn = commands.Spawn()
            .Insert(new Node
            {
                PositionType = PositionType.Absolute,
                Left = Val.Px(5),
                Top = Val.Px(3),
                Width = Val.Px(15),
                Height = Val.Px(20),
            })
            .Insert(new UiCustom { Data = CustomMarker })
            .Insert(new UOCustomRender
            {
                Kind = UOCustomKind.Gump,
                AssetId = 0x15A4,
                Hue = Vector3.UnitZ,
            })
            .Insert(Interaction.None);
        commands.AddChild(root.Id, minBtn.Id);

        // Row of action buttons, ordered exactly as main's textTable.
        var x = 30;
        for (var i = 0; i < s_buttons.Length; i++)
        {
            var (sizeKind, caption, btnId) = s_buttons[i];
            if (!hasUOStore && (int)btnId >= (int)Buttons.UOStore) break;

            var w = sizeKind == 1 ? largeWidth : smallWidth;
            var h = sizeKind == 1 ? largeHeight : smallHeight;
            var gumpId = (ushort)(sizeKind == 1 ? 0x098D : 0x098B);

            var btn = commands.Spawn()
                .Insert(new Node
                {
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(x),
                    Top = Val.Px(1),
                    Width = Val.Px(w),
                    Height = Val.Px(h),
                })
                .Insert(new UiCustom { Data = CustomMarker })
                .Insert(new UOCustomRender
                {
                    Kind = UOCustomKind.Gump,
                    AssetId = gumpId,
                    Hue = Vector3.UnitZ,
                })
                .Insert(Interaction.None);

            commands.AddChild(root.Id, btn.Id);

            // Caption label centered over the button. Hardcoded strings
            // until ClilocLoader is on the UI text path.
            var label = commands.Spawn()
                .Insert(new Node
                {
                    PositionType = PositionType.Absolute,
                    Left = Val.Px(x),
                    Top = Val.Px(6),
                    Width = Val.Px(w),
                    Height = Val.Px(h),
                })
                .Insert(new Text(caption))
                .Insert(new TextFont { FontId = 0, Size = 12 })
                .Insert(new TextColor(new ClayColor(220, 220, 220, 255)));
            commands.AddChild(root.Id, label.Id);

            // Wire actions one-by-one as the corresponding gumps land.
            // Currently only Paperdoll has an ECS path (Send_DoubleClick
            // on player triggers server 0x88 → PaperdollPlugin).
            switch (btnId)
            {
                case Buttons.Paperdoll:
                    btn.Observe((On<UiPointerDown> _,
                                 Res<NetClient> net,
                                 Res<GameContext> ctx) =>
                    {
                        if (ctx.Value.PlayerSerial != 0)
                            net.Value.Send_DoubleClick(ctx.Value.PlayerSerial);
                    });
                    break;
            }

            x += w + 1;
        }
    }

    private static void Despawn(
        Commands commands,
        Query<Data<Node>, Filter<With<IsTopBar>>> query)
    {
        foreach (var (ent, _) in query)
        {
            commands.Entity(ent.Ref).Despawn();
        }
    }
}

internal struct IsTopBar;
