using System.Numerics;
using ClassicUO.Ecs;
using TinyEcs.Bevy.UI;
using Xunit;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;

namespace ClassicUO.Ecs.Tests;

// PixelHit's geometry + fallthrough rules, exercised on the branches that need
// no UO asset masks: the bounding-box reject (shared by every kind), a null
// payload, and the None/nine-patch default (solid fill). assets is null on
// purpose — these branches must answer before touching any loader. Per-pixel
// alpha (Gump/Art/Tiled) is covered behaviourally through UiPick with real art.
public class UiHitTestTests
{
    private static ComputedNode Box(float x, float y, float w, float h)
        => new ComputedNode { Position = new Vector2(x, y), Size = new Vector2(w, h) };

    private static readonly UOCustomRender None = new UOCustomRender { Kind = UOCustomKind.None };

    [Fact]
    public void Inside_a_None_element_is_a_solid_hit()
    {
        var bb = Box(10, 10, 100, 50);
        Assert.True(UiHitTest.PixelHit(null, None, bb, new XnaVector2(50, 30)));
    }

    [Fact]
    public void Null_payload_is_treated_as_a_solid_box()
    {
        var bb = Box(0, 0, 20, 20);
        Assert.True(UiHitTest.PixelHit(null, null, bb, new XnaVector2(5, 5)));
    }

    [Fact]
    public void Point_left_or_above_the_box_misses()
    {
        var bb = Box(10, 10, 100, 50);
        Assert.False(UiHitTest.PixelHit(null, None, bb, new XnaVector2(9, 30)));   // left of
        Assert.False(UiHitTest.PixelHit(null, None, bb, new XnaVector2(50, 9)));   // above
    }

    [Fact]
    public void Top_left_edge_is_inclusive()
    {
        var bb = Box(10, 10, 100, 50);
        Assert.True(UiHitTest.PixelHit(null, None, bb, new XnaVector2(10, 10)));
    }

    [Fact]
    public void Right_and_bottom_edges_are_exclusive()
    {
        var bb = Box(10, 10, 100, 50);                 // covers x[10,110), y[10,60)
        Assert.False(UiHitTest.PixelHit(null, None, bb, new XnaVector2(110, 30))); // right edge
        Assert.False(UiHitTest.PixelHit(null, None, bb, new XnaVector2(50, 60)));  // bottom edge
        Assert.True(UiHitTest.PixelHit(null, None, bb, new XnaVector2(109, 59)));  // just inside
    }
}
