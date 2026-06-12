using ClassicUO.Ecs;
using ClassicUO.Game.Data;
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
}
