using ClassicUO.Assets;
using ClassicUO.Ecs;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using Microsoft.Xna.Framework;
using TinyEcs;
using TinyEcs.Bevy;
using Xunit;

namespace ClassicUO.Ecs.Tests;

// Behavioural tests for the equipment-layer occlusion table (IsItemCovered2),
// the CCN-50 rule set that decides whether a worn item is hidden by something
// layered over it. Run against a real ECS world: equipment slots reference
// Graphic entities, exactly as the renderer resolves them. These lock the
// legacy ItemGump.IsCovered parity so the per-layer split can't drift.
public class WorldRenderingTests
{
    private static ulong Gfx(World w, ushort g) => w.Entity().Set(new Graphic { Value = g }).Set(new Hue()).ID;

    private static Query<Data<Graphic, Hue>> LayerQuery(App app)
    {
        var q = new Query<Data<Graphic, Hue>>();
        q.Initialize(app);
        q.Fetch(app);
        return q;
    }

    [Fact]
    public void Shoes_are_covered_when_legs_are_worn()
    {
        var app = new App();
        var w = app.GetWorld();
        var slots = new EquipmentSlots();
        slots[Layer.Legs] = w.Entity().ID; // any leg armor hides the shoes
        Assert.True(WorldRenderingPlugin.IsItemCovered2(LayerQuery(app), ref slots, Layer.Shoes));
    }

    [Fact]
    public void Shoes_are_covered_by_the_long_robe_graphic()
    {
        var app = new App();
        var w = app.GetWorld();
        var slots = new EquipmentSlots();
        slots[Layer.Robe] = Gfx(w, 0x0504);
        Assert.True(WorldRenderingPlugin.IsItemCovered2(LayerQuery(app), ref slots, Layer.Shoes));
    }

    [Fact]
    public void Shoes_are_visible_with_nothing_layered_over_them()
    {
        var app = new App();
        var slots = new EquipmentSlots();
        Assert.False(WorldRenderingPlugin.IsItemCovered2(LayerQuery(app), ref slots, Layer.Shoes));
    }

    [Fact]
    public void Tunic_is_covered_by_an_ordinary_robe()
    {
        var app = new App();
        var w = app.GetWorld();
        var slots = new EquipmentSlots();
        slots[Layer.Robe] = Gfx(w, 0x1234); // not one of the see-through robe ids
        Assert.True(WorldRenderingPlugin.IsItemCovered2(LayerQuery(app), ref slots, Layer.Tunic));
    }

    [Fact]
    public void Tunic_shows_through_a_see_through_robe_graphic()
    {
        var app = new App();
        var w = app.GetWorld();
        var slots = new EquipmentSlots();
        slots[Layer.Robe] = Gfx(w, 0x9985); // excluded id -> robe doesn't cover
        Assert.False(WorldRenderingPlugin.IsItemCovered2(LayerQuery(app), ref slots, Layer.Tunic));
    }

    [Fact]
    public void Helmet_is_covered_by_a_full_hood_robe()
    {
        var app = new App();
        var w = app.GetWorld();
        var slots = new EquipmentSlots();
        slots[Layer.Robe] = Gfx(w, 0x4B9D);
        Assert.True(WorldRenderingPlugin.IsItemCovered2(LayerQuery(app), ref slots, Layer.Helmet));
    }

    [Fact]
    public void A_layer_with_no_occlusion_rule_is_never_covered()
    {
        var app = new App();
        var w = app.GetWorld();
        var slots = new EquipmentSlots();
        slots[Layer.Robe] = Gfx(w, 0x1234);
        Assert.False(WorldRenderingPlugin.IsItemCovered2(LayerQuery(app), ref slots, Layer.Ring));
    }

    // --- Pure render-decision helpers ----------------------------------------
    // No ECS world / asset files needed — they take primitives (or a hand-built
    // StaticTiles) and return values. These lock the legacy parity rules the
    // big Render* methods lean on: hue clamping, sprite mirroring per facing,
    // alpha fade stepping, the circle-of-transparency see-through test, and the
    // foliage dedup key's bit layout.

    static StaticTiles St(TileFlag flags, byte height)
        => new StaticTiles((ulong)flags, 0, 0, 0, 0, 0, 0, height, "");

    [Theory]
    [InlineData(0x0000, 0x0000)]            // 0 hue: only the 0x8000 bit survives (none here)
    [InlineData(0x8000, 0x8000)]            // 0 colour but partial-hue flag kept
    [InlineData(0x0042, 0x0042)]            // ordinary hue passes through
    [InlineData(0x0BB8, 0x0001)]            // >= 0x0BB8 clamps to 1
    [InlineData(0xC042, 0xC042)]            // high flag bits re-OR'd onto a valid hue
    public void FixHue_clamps_and_preserves_flag_bits(int hue, int expected)
        => Assert.Equal((ushort)expected, WorldRenderingPlugin.FixHue((ushort)hue));

    [Fact]
    public void FixDirection_maps_facing_to_sprite_dir_and_mirror()
    {
        Assert.Equal((Direction.Right, true), WorldRenderingPlugin.FixDirection(Direction.East));
        Assert.Equal((Direction.Right, false), WorldRenderingPlugin.FixDirection(Direction.South));
        Assert.Equal((Direction.East, true), WorldRenderingPlugin.FixDirection(Direction.Right));
        Assert.Equal((Direction.East, false), WorldRenderingPlugin.FixDirection(Direction.Left));
        Assert.Equal((Direction.Down, true), WorldRenderingPlugin.FixDirection(Direction.North));
        Assert.Equal((Direction.Down, false), WorldRenderingPlugin.FixDirection(Direction.West));
        Assert.Equal((Direction.North, false), WorldRenderingPlugin.FixDirection(Direction.Down));
        Assert.Equal((Direction.South, false), WorldRenderingPlugin.FixDirection(Direction.Up));
    }

    [Fact]
    public void FixDirection_strips_the_running_bit_before_mapping()
        => Assert.Equal(WorldRenderingPlugin.FixDirection(Direction.East),
                        WorldRenderingPlugin.FixDirection(Direction.East | Direction.Running));

    [Fact]
    public void CalculateAlpha_without_fading_snaps_to_max()
    {
        byte a = 200;
        Assert.False(WorldRenderingPlugin.CalculateAlpha(ref a, 0, useObjectsFading: false));
        Assert.Equal(0, a);

        a = 0;
        Assert.True(WorldRenderingPlugin.CalculateAlpha(ref a, 255, useObjectsFading: false));
        Assert.Equal(255, a);
    }

    [Fact]
    public void CalculateAlpha_with_fading_steps_25_toward_max()
    {
        byte a = 255;
        Assert.True(WorldRenderingPlugin.CalculateAlpha(ref a, 0, useObjectsFading: true));
        Assert.Equal(230, a);   // stepped down by 25

        a = 0;
        Assert.True(WorldRenderingPlugin.CalculateAlpha(ref a, 255, useObjectsFading: true));
        Assert.Equal(25, a);    // stepped up by 25
    }

    [Fact]
    public void CalculateAlpha_clamps_overshoot_and_reports_no_change_at_target()
    {
        byte a = 10;
        Assert.True(WorldRenderingPlugin.CalculateAlpha(ref a, 0, useObjectsFading: true));
        Assert.Equal(0, a);     // 10 - 25 clamped to 0

        a = 100;
        Assert.False(WorldRenderingPlugin.CalculateAlpha(ref a, 100, useObjectsFading: true));
        Assert.Equal(100, a);   // already at target
    }

    [Theory]
    [InlineData(0x0192, true)]
    [InlineData(0x0193, true)]
    [InlineData(0x025F, true)]
    [InlineData(0x0260, true)]
    [InlineData(0x02B6, true)]
    [InlineData(0x02B7, true)]
    [InlineData(0x0190, false)]
    [InlineData(0x0261, false)]
    public void IsDeadBody_matches_ghost_body_graphics(int graphic, bool expected)
        => Assert.Equal(expected, WorldRenderingPlugin.IsDeadBody((ushort)graphic));

    [Fact]
    public void CanBeTransparent_rules()
    {
        Assert.True(WorldRenderingPlugin.CanBeTransparent(St(TileFlag.None, 0)));      // height 0
        Assert.True(WorldRenderingPlugin.CanBeTransparent(St(TileFlag.None, 6)));      // height > 5
        Assert.True(WorldRenderingPlugin.CanBeTransparent(St(TileFlag.Roof, 3)));      // roof
        Assert.True(WorldRenderingPlugin.CanBeTransparent(St(TileFlag.Wall, 3)));      // wall
        Assert.True(WorldRenderingPlugin.CanBeTransparent(St(TileFlag.Surface | TileFlag.Background, 3)));
        Assert.True(WorldRenderingPlugin.CanBeTransparent(St(TileFlag.Surface, 5)));   // h==5 surface non-bg
        Assert.False(WorldRenderingPlugin.CanBeTransparent(St(TileFlag.None, 3)));     // plain mid-height
    }

    [Fact]
    public void TransparentTest_below_player_minus_height_is_opaque()
    {
        // objZ <= z5 - height → never see-through
        Assert.False(WorldRenderingPlugin.TransparentTest(10, 0, St(TileFlag.None, 5)));
    }

    [Fact]
    public void TransparentTest_above_player_only_when_can_be_transparent()
    {
        // z5 < objZ and the tile can't be transparent → opaque
        Assert.False(WorldRenderingPlugin.TransparentTest(10, 12, St(TileFlag.None, 3)));
        // same but a roof tile → see-through
        Assert.True(WorldRenderingPlugin.TransparentTest(10, 12, St(TileFlag.Roof, 3)));
    }

    [Fact]
    public void TransparentTest_at_or_below_player_is_transparent()
        => Assert.True(WorldRenderingPlugin.TransparentTest(10, 8, St(TileFlag.None, 3)));

    [Fact]
    public void CalculateObjectHeight_invalid_height_returns_ff_and_leaves_max()
    {
        int max = 50;
        var data = St(TileFlag.None, 0xFF);
        Assert.Equal(0xFF, WorldRenderingPlugin.CalculateObjectHeight(ref max, in data));
        Assert.Equal(50, max);
    }

    [Fact]
    public void CalculateObjectHeight_adds_height_to_max()
    {
        int max = 50;
        var data = St(TileFlag.None, 20);
        Assert.Equal(20, WorldRenderingPlugin.CalculateObjectHeight(ref max, in data));
        Assert.Equal(70, max);
    }

    [Fact]
    public void CalculateObjectHeight_zero_height_non_surface_becomes_ten()
    {
        int max = 0;
        var data = St(TileFlag.None, 0); // not background, not surface
        Assert.Equal(10, WorldRenderingPlugin.CalculateObjectHeight(ref max, in data));
        Assert.Equal(10, max);
    }

    [Fact]
    public void CalculateObjectHeight_zero_height_surface_stays_zero()
    {
        int max = 0;
        var data = St(TileFlag.Surface, 0);
        Assert.Equal(0, WorldRenderingPlugin.CalculateObjectHeight(ref max, in data));
        Assert.Equal(0, max);
    }

    [Fact]
    public void CalculateObjectHeight_bridge_halves_height()
    {
        int max = 0;
        var data = St(TileFlag.Bridge, 20);
        Assert.Equal(10, WorldRenderingPlugin.CalculateObjectHeight(ref max, in data));
        Assert.Equal(10, max);
    }

    [Fact]
    public void FoliageKey_packs_graphic_xy_z_into_distinct_lanes()
    {
        Assert.Equal(1L << 40, WorldRenderingPlugin.FoliageKey(1, 0, 0, 0));
        // each field occupies its own lane → swapping x/y changes the key
        Assert.NotEqual(WorldRenderingPlugin.FoliageKey(5, 10, 20, 0),
                        WorldRenderingPlugin.FoliageKey(5, 20, 10, 0));
        // negative z packs into the low byte as its unsigned form
        Assert.Equal((long)(byte)0xFF, WorldRenderingPlugin.FoliageKey(0, 0, 0, -1) & 0xFF);
    }

    // --- RenderStatics decision blocks (extracted from the CCN-88 loop) -------

    [Fact]
    public void StaticPriorityZ_unflagged_tile_is_unchanged()
        => Assert.Equal(10, WorldRenderingPlugin.StaticPriorityZ(10, St(TileFlag.None, 0), isNormalMulti: false));

    [Fact]
    public void StaticPriorityZ_background_sinks_one()
        => Assert.Equal(9, WorldRenderingPlugin.StaticPriorityZ(10, St(TileFlag.Background, 0), isNormalMulti: false));

    [Fact]
    public void StaticPriorityZ_height_lifts_one()
        => Assert.Equal(11, WorldRenderingPlugin.StaticPriorityZ(10, St(TileFlag.None, 5), isNormalMulti: false));

    [Fact]
    public void StaticPriorityZ_wall_with_height_lifts_three()
        => Assert.Equal(13, WorldRenderingPlugin.StaticPriorityZ(10, St(TileFlag.Wall, 5), isNormalMulti: false));

    [Fact]
    public void StaticPriorityZ_normal_multi_sinks_one()
        => Assert.Equal(9, WorldRenderingPlugin.StaticPriorityZ(10, St(TileFlag.None, 0), isNormalMulti: true));

    [Fact]
    public void StaticPriorityZ_combines_all_adjustments()
        // background -1, height +1, multimovable +1, normal-multi -1 = net 0
        => Assert.Equal(10, WorldRenderingPlugin.StaticPriorityZ(10, St(TileFlag.Background | TileFlag.MultiMovable, 5), isNormalMulti: true));

    [Fact]
    public void TryGradientCotAlpha_at_center_culls()
    {
        // distSq == 0 → ratio 0 → alpha byte 0 → cull
        Assert.False(WorldRenderingPlugin.TryGradientCotAlpha(new Vector2(0, 44), Vector2.Zero, 10000f, out _));
    }

    [Fact]
    public void TryGradientCotAlpha_outside_radius_is_fully_opaque()
    {
        Assert.True(WorldRenderingPlugin.TryGradientCotAlpha(new Vector2(200, 44), Vector2.Zero, 10000f, out var a));
        Assert.Equal(1f, a);
    }

    [Fact]
    public void TryGradientCotAlpha_inside_radius_fades_partially()
    {
        Assert.True(WorldRenderingPlugin.TryGradientCotAlpha(new Vector2(50, 44), Vector2.Zero, 10000f, out var a));
        Assert.InRange(a, 0.01f, 0.99f);
    }

    [Fact]
    public void ResolveStaticHue_highlight_wins_and_clears_partial()
    {
        var hue = WorldRenderingPlugin.ResolveStaticHue(
            highlight: true, outOfRange: true, grayWorld: true, selectedInteractable: true,
            baseHue: 0x1234, basePartialHue: true, out var partial);
        Assert.Equal(Constants.HIGHLIGHT_CURRENT_OBJECT_HUE, hue);
        Assert.False(partial);
    }

    [Fact]
    public void ResolveStaticHue_out_of_range_then_gray_then_selected()
    {
        Assert.Equal((ushort)Constants.OUT_RANGE_COLOR,
            WorldRenderingPlugin.ResolveStaticHue(false, true, true, true, 0x1234, true, out var p1));
        Assert.False(p1);

        Assert.Equal((ushort)Constants.DEAD_RANGE_COLOR,
            WorldRenderingPlugin.ResolveStaticHue(false, false, true, true, 0x1234, true, out var p2));
        Assert.False(p2);

        // selected-interactable keeps the base partial-hue flag
        Assert.Equal((ushort)0x0035,
            WorldRenderingPlugin.ResolveStaticHue(false, false, false, true, 0x1234, true, out var p3));
        Assert.True(p3);
    }

    [Fact]
    public void ResolveStaticHue_no_condition_keeps_base_hue_and_partial()
    {
        var hue = WorldRenderingPlugin.ResolveStaticHue(false, false, false, false, 0x1234, true, out var partial);
        Assert.Equal((ushort)0x1234, hue);
        Assert.True(partial);
    }

    [Theory]
    [InlineData(0x398C, 0x0020)] // fire
    [InlineData(0x3967, 0x0058)] // paralyze
    [InlineData(0x3946, 0x0070)] // energy
    [InlineData(0x3914, 0x0044)] // poison
    [InlineData(0x0082, 0x038A)] // wall of stone
    public void TryFieldReplace_maps_field_graphics_to_replacement_and_hue(int graphic, int expectedHue)
    {
        Assert.True(WorldRenderingPlugin.TryFieldReplace((ushort)graphic, out var draw, out var hue));
        Assert.Equal(Constants.FIELD_REPLACE_GRAPHIC, draw);
        Assert.Equal((ushort)expectedHue, hue);
    }

    [Fact]
    public void TryFieldReplace_non_field_returns_false()
        => Assert.False(WorldRenderingPlugin.TryFieldReplace(0x1234, out _, out _));

    // --- RenderBodies / RenderEquipment / RenderTiles shared blocks ----------

    [Fact]
    public void BodyDepthZ_no_offset_sorts_against_origin_tile()
        => Assert.Equal(Isometric.GetDepthZ(10, 20, 7),
                        WorldRenderingPlugin.BodyDepthZ(10, 20, 7, Vector2.Zero));

    [Fact]
    public void BodyDepthZ_southeast_step_sorts_against_x_plus_one()
        => Assert.Equal(Isometric.GetDepthZ(11, 20, 7),
                        WorldRenderingPlugin.BodyDepthZ(10, 20, 7, new Vector2(1, 1)));

    [Fact]
    public void BodyDepthZ_south_step_sorts_against_next_diagonal()
        => Assert.Equal(Isometric.GetDepthZ(11, 21, 7),
                        WorldRenderingPlugin.BodyDepthZ(10, 20, 7, new Vector2(0, 1)));

    [Fact]
    public void BodyDepthZ_southwest_step_sorts_against_y_plus_one()
        => Assert.Equal(Isometric.GetDepthZ(10, 21, 7),
                        WorldRenderingPlugin.BodyDepthZ(10, 20, 7, new Vector2(-1, 1)));

    [Fact]
    public void BodyDepthZ_upward_offset_keeps_origin()
        // offset.Y <= 0 never nudges, regardless of X
        => Assert.Equal(Isometric.GetDepthZ(10, 20, 7),
                        WorldRenderingPlugin.BodyDepthZ(10, 20, 7, new Vector2(5, -3)));

    [Fact]
    public void ResolveLandHue_precedence_then_zero()
    {
        Assert.Equal(Constants.HIGHLIGHT_CURRENT_OBJECT_HUE, WorldRenderingPlugin.ResolveLandHue(true, true, true));
        Assert.Equal((ushort)Constants.OUT_RANGE_COLOR, WorldRenderingPlugin.ResolveLandHue(false, true, true));
        Assert.Equal((ushort)Constants.DEAD_RANGE_COLOR, WorldRenderingPlugin.ResolveLandHue(false, false, true));
        Assert.Equal((ushort)0, WorldRenderingPlugin.ResolveLandHue(false, false, false));
    }

    [Theory]
    [InlineData(0x0191, true)]
    [InlineData(0x025D, true)]
    [InlineData(0x02B7, true)]
    [InlineData(0x0190, false)]
    [InlineData(0x025E, false)]
    public void IsAltTorsoBody_flags_female_and_gargoyle_bodies(int gfx, bool expected)
        => Assert.Equal(expected, WorldRenderingPlugin.IsAltTorsoBody((ushort)gfx));
}
