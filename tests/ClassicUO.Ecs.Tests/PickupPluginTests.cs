using ClassicUO.Ecs;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using Xunit;

namespace ClassicUO.Ecs.Tests;

// Unit tests for PickupPlugin's pure decision logic — the drop-position clamp,
// the pile-graphic table, the drop-ack matcher, and the root-holder walk. The
// pickup/drop systems themselves are gesture-driven (drag) and have no headless
// harness path, so these cover the side-effect-free helpers the brain methods
// lean on (now internal for direct testing; per repo rule real ECS is used
// where a query is genuinely needed).
public class PickupPluginTests
{
    [Fact]
    public void ClampDropPosition_centres_the_sprite_on_the_cursor_in_bounds()
    {
        var (x, y) = PickupPlugin.ClampDropPosition(
            mx: 100, my: 100, spriteW: 20, spriteH: 20,
            bounds: new Rectangle(0, 0, 200, 200), scale: 1f, chessboard: false);
        Assert.Equal((ushort)90, x); // 100 - 20/2
        Assert.Equal((ushort)90, y);
    }

    [Fact]
    public void ClampDropPosition_pulls_off_the_right_and_bottom_edges()
    {
        var (x, y) = PickupPlugin.ClampDropPosition(
            mx: 300, my: 300, spriteW: 20, spriteH: 20,
            bounds: new Rectangle(0, 0, 200, 200), scale: 1f, chessboard: false);
        Assert.Equal((ushort)180, x); // 200 - 20
        Assert.Equal((ushort)180, y);
    }

    [Fact]
    public void ClampDropPosition_clamps_to_the_left_and_top_edges()
    {
        var (x, y) = PickupPlugin.ClampDropPosition(
            mx: 0, my: 0, spriteW: 20, spriteH: 20,
            bounds: new Rectangle(0, 0, 200, 200), scale: 1f, chessboard: false);
        Assert.Equal((ushort)0, x);
        Assert.Equal((ushort)0, y);
    }

    [Fact]
    public void ClampDropPosition_shifts_chessboard_drops_down_20px()
    {
        var (x, y) = PickupPlugin.ClampDropPosition(
            mx: 100, my: 100, spriteW: 20, spriteH: 20,
            bounds: new Rectangle(0, 0, 200, 200), scale: 1f, chessboard: true);
        Assert.Equal((ushort)90, x);
        Assert.Equal((ushort)110, y); // my += 20, then - sprite/2
    }

    [Fact]
    public void ClampDropPosition_reverses_the_container_scale_into_server_space()
    {
        var (x, y) = PickupPlugin.ClampDropPosition(
            mx: 100, my: 100, spriteW: 20, spriteH: 20,
            bounds: new Rectangle(0, 0, 100, 100), scale: 2f, chessboard: false);
        Assert.Equal((ushort)45, x); // (100 - 10) / 2
        Assert.Equal((ushort)45, y);
    }

    [Fact]
    public void IsPileGraphic_matches_the_pile_table_only()
    {
        Assert.True(PickupPlugin.IsPileGraphic(0x0EFA));
        Assert.True(PickupPlugin.IsPileGraphic(0x2D50));
        Assert.False(PickupPlugin.IsPileGraphic(0x1234));
        Assert.False(PickupPlugin.IsPileGraphic(0));
    }

    [Fact]
    public void IsDropAck_requires_a_pending_drop()
    {
        var grabbed = new GrabbedItem { PendingDrop = false, Serial = 5 };
        Assert.False(PickupPlugin.IsDropAck(grabbed, 5));
    }

    [Fact]
    public void IsDropAck_matches_the_held_serial_or_the_drop_target()
    {
        var grabbed = new GrabbedItem { PendingDrop = true, Serial = 5, DropTargetSerial = 9 };
        Assert.True(PickupPlugin.IsDropAck(grabbed, 5));  // held serial readdressed
        Assert.True(PickupPlugin.IsDropAck(grabbed, 9));  // merge: target stack updated
        Assert.False(PickupPlugin.IsDropAck(grabbed, 7)); // unrelated packet
    }

    [Fact]
    public void IsDropAck_ignores_the_target_when_no_drop_target_is_set()
    {
        var grabbed = new GrabbedItem { PendingDrop = true, Serial = 5, DropTargetSerial = 0 };
        Assert.False(PickupPlugin.IsDropAck(grabbed, 0)); // 0 target must not match a 0 serial packet
    }

    private static Query<Data<TinyEcs.Parent>> Parents(App app)
    {
        var q = new Query<Data<TinyEcs.Parent>>();
        q.Initialize(app);
        q.Fetch(app);
        return q;
    }

    [Fact]
    public void ResolveRootHolder_walks_a_nested_chain_to_the_top()
    {
        var app = new App();
        var w = app.GetWorld();
        // pouch (leaf) -> backpack (mid) -> player (root, no parent)
        var root = w.Entity().ID;
        var mid = w.Entity().ID;
        var leaf = w.Entity().ID;
        w.AddChild(root, mid);
        w.AddChild(mid, leaf);

        Assert.Equal(root, PickupPlugin.ResolveRootHolder(leaf, Parents(app)));
    }

    [Fact]
    public void ResolveRootHolder_returns_a_parentless_entity_as_its_own_root()
    {
        var app = new App();
        var w = app.GetWorld();
        var loose = w.Entity().ID;
        Assert.Equal(loose, PickupPlugin.ResolveRootHolder(loose, Parents(app)));
    }
}
