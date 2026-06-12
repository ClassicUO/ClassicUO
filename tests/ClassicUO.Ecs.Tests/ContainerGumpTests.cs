using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Ecs;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using TinyEcs.Bevy.UI;
using Xunit;
using GameLayer = ClassicUO.Game.Data.Layer;
using NumVector2 = System.Numerics.Vector2;

namespace ClassicUO.Ecs.Tests;

// Unit tests for ContainerGumpPlugin's pure decision logic — the slot clamp,
// the wearable/corpse skip filter, the focus-z resolve, and the spawn-position
// rules. These are the bug-prone branches the brain methods used to bury;
// pulled out as internal statics so they can be exercised without UO assets or
// a live network (per repo rule: real ECS, no mocks — App is only spun up where
// a query is genuinely needed).
public class ContainerGumpTests
{
    private static ContainerSlotEvent Ev(int x, int y)
        => new(ContainerSlotAction.Add, 0, 0, 0, 0, (ushort)x, (ushort)y, 0);

    private static ContainerUiMap.Entry Box(ushort graphic = 0x003C, float scale = 1f)
        => new()
        {
            Graphic = graphic,
            Scale = scale,
            // content rect: left/top edge 10,10; right/bottom edge 200,150.
            Bounds = new Rectangle(10, 10, 200, 150),
        };

    [Fact]
    public void ClampSlotPosition_leaves_an_in_bounds_slot_untouched()
    {
        var (x, y) = ContainerGumpPlugin.ClampSlotPosition(Ev(50, 50), Box(), spriteW: 20, spriteH: 20);
        Assert.Equal(50f, x);
        Assert.Equal(50f, y);
    }

    [Fact]
    public void ClampSlotPosition_pulls_a_slot_off_the_right_and_bottom_edges_inward()
    {
        var (x, y) = ContainerGumpPlugin.ClampSlotPosition(Ev(190, 140), Box(), spriteW: 20, spriteH: 20);
        Assert.Equal(180f, x); // 200 (right edge) - 20 (sprite)
        Assert.Equal(130f, y); // 150 (bottom edge) - 20
    }

    [Fact]
    public void ClampSlotPosition_clamps_a_slot_to_the_left_and_top_edges()
    {
        var (x, y) = ContainerGumpPlugin.ClampSlotPosition(Ev(2, 3), Box(), spriteW: 20, spriteH: 20);
        Assert.Equal(10f, x);
        Assert.Equal(10f, y);
    }

    [Fact]
    public void ClampSlotPosition_shifts_chessboard_slots_up_20px_after_the_clamp()
    {
        var (x, y) = ContainerGumpPlugin.ClampSlotPosition(Ev(50, 50), Box(graphic: 0x091A), spriteW: 20, spriteH: 20);
        Assert.Equal(50f, x);
        Assert.Equal(30f, y); // clamped to 50, then -20 for the chessboard offset
    }

    [Fact]
    public void ShouldSkipSlot_keeps_a_plain_item()
    {
        var tileData = new StaticTiles[0x4000];
        tileData[0x100] = new StaticTiles { Flags = 0, Layer = 0 };
        Assert.False(ContainerGumpPlugin.ShouldSkipSlot(0x100, Box(), tileData));
    }

    [Fact]
    public void ShouldSkipSlot_drops_wearable_hair_and_face()
    {
        var tileData = new StaticTiles[0x4000];
        tileData[0x101] = new StaticTiles { Flags = TileFlag.Wearable, Layer = (byte)GameLayer.Hair };
        tileData[0x102] = new StaticTiles { Flags = TileFlag.Wearable, Layer = (byte)GameLayer.Face };
        Assert.True(ContainerGumpPlugin.ShouldSkipSlot(0x101, Box(), tileData));
        Assert.True(ContainerGumpPlugin.ShouldSkipSlot(0x102, Box(), tileData));
    }

    [Fact]
    public void ShouldSkipSlot_ignores_a_graphic_past_the_tiledata_table()
    {
        var tileData = new StaticTiles[0x10];
        Assert.False(ContainerGumpPlugin.ShouldSkipSlot(0x500, Box(), tileData));
    }

    [Fact]
    public void ResolveCurrentZ_returns_the_window_root_live_z()
    {
        var app = new App();
        var w = app.GetWorld();
        var win = w.Entity().Set(new GlobalZIndex(7)).ID;
        var q = new Query<Data<GlobalZIndex>>();
        q.Initialize(app);
        q.Fetch(app);

        var entry = new ContainerUiMap.Entry { UiEntity = win, ZBase = 3 };
        Assert.Equal(7, ContainerGumpPlugin.ResolveCurrentZ(entry, q));
    }

    [Fact]
    public void ResolveCurrentZ_falls_back_to_ZBase_when_the_window_has_no_z()
    {
        var app = new App();
        var q = new Query<Data<GlobalZIndex>>();
        q.Initialize(app);
        q.Fetch(app);

        var entry = new ContainerUiMap.Entry { UiEntity = 999_999, ZBase = 3 };
        Assert.Equal(3, ContainerGumpPlugin.ResolveCurrentZ(entry, q));
    }

    // Empty world-position / parent queries: the world-anchored branch (setting
    // 0) is the only consumer and none of these cases exercise it.
    private static (Query<Data<WorldPosition>>, Query<Data<TinyEcs.Parent>>) EmptyChainQueries(App app)
    {
        var wp = new Query<Data<WorldPosition>>();
        wp.Initialize(app);
        wp.Fetch(app);
        var pq = new Query<Data<TinyEcs.Parent>>();
        pq.Initialize(app);
        pq.Fetch(app);
        return (wp, pq);
    }

    [Fact]
    public void ResolveSpawnPosition_consumes_a_saved_position_once()
    {
        var app = new App();
        var (wp, pq) = EmptyChainQueries(app);
        var memory = new ContainerPositionMemory();
        memory.Saved[0x1234] = new Point(123, 45);
        var profile = new Profile(); // override off — saved position must still win

        var (x, y) = ContainerGumpPlugin.ResolveSpawnPosition(
            serial: 0x1234, width: 100, height: 50,
            registry: new ContainerDataRegistry(), profile: profile,
            gameCtx: default, camera: null,
            surface: new UiSurface { LogicalSize = new NumVector2(800, 600) },
            entitiesMap: new NetworkEntitiesMap(), memory: memory,
            worldPosQ: wp, parentsQ: pq);

        Assert.Equal(123f, x);
        Assert.Equal(45f, y);
        Assert.False(memory.Saved.ContainsKey(0x1234)); // consumed, like UIManager.RemovePosition
    }

    [Fact]
    public void ResolveSpawnPosition_uses_the_registry_default_when_override_is_off()
    {
        var app = new App();
        var (wp, pq) = EmptyChainQueries(app);
        var registry = new ContainerDataRegistry();

        var (x, y) = ContainerGumpPlugin.ResolveSpawnPosition(
            serial: 0x1, width: 100, height: 50,
            registry: registry, profile: new Profile(),
            gameCtx: default, camera: null,
            surface: new UiSurface { LogicalSize = new NumVector2(800, 600) },
            entitiesMap: new NetworkEntitiesMap(), memory: new ContainerPositionMemory(),
            worldPosQ: wp, parentsQ: pq);

        Assert.Equal((float)registry.DefaultX, x);
        Assert.Equal((float)registry.DefaultY, y);
    }

    [Fact]
    public void ResolveSpawnPosition_top_right_setting_anchors_against_the_screen_width()
    {
        var app = new App();
        var (wp, pq) = EmptyChainQueries(app);
        var profile = new Profile
        {
            OverrideContainerLocation = true,
            OverrideContainerLocationSetting = 1, // top right
        };

        var (x, y) = ContainerGumpPlugin.ResolveSpawnPosition(
            serial: 0x1, width: 100, height: 50,
            registry: new ContainerDataRegistry(), profile: profile,
            gameCtx: default, camera: null,
            surface: new UiSurface { LogicalSize = new NumVector2(800, 600) },
            entitiesMap: new NetworkEntitiesMap(), memory: new ContainerPositionMemory(),
            worldPosQ: wp, parentsQ: pq);

        Assert.Equal(700f, x); // screenW (800) - width (100)
        Assert.Equal(0f, y);
    }
}
