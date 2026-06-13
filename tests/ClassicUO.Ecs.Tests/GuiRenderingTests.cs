using ClassicUO.Ecs;
using Microsoft.Xna.Framework;
using Xunit;
using ClayColor = Clay.Color;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace ClassicUO.Ecs.Tests;

// Clay publishes colours as floats in a 0..255 range; the batcher needs bytes.
// ToXnaColor is the clamp+narrow on the render hot path — locked here so an
// out-of-range channel can't wrap instead of clamping.
public class GuiRenderingTests
{
    [Fact]
    public void ToXnaColor_narrows_in_range_channels()
    {
        var c = GuiRenderingPlugin.ToXnaColor(new ClayColor(10, 20, 30, 255));
        Assert.Equal(new XnaColor(10, 20, 30, 255), c);
    }

    [Fact]
    public void ToXnaColor_clamps_over_and_under_range()
    {
        // 300 -> 255, -5 -> 0 (clamp, never wrap to a wrong byte)
        var c = GuiRenderingPlugin.ToXnaColor(new ClayColor(300, -5, 128, 400));
        Assert.Equal(new XnaColor(255, 0, 128, 255), c);
    }

    // --- DrawCustom geometry helpers (Art / Animation / Land + paperdoll slot)

    [Fact]
    public void FitCenteredRect_art_smaller_than_box_is_native_and_centered()
        => Assert.Equal(new Rectangle(22, 17, 20, 20),
                        GuiRenderingPlugin.FitCenteredRect(10, 5, 44, 44, 20, 20));

    [Fact]
    public void FitCenteredRect_art_larger_fills_the_box()
        => Assert.Equal(new Rectangle(0, 0, 44, 44),
                        GuiRenderingPlugin.FitCenteredRect(0, 0, 44, 44, 60, 50));

    [Fact]
    public void FitCenteredRect_one_dim_larger_still_fills_the_box()
        => Assert.Equal(new Rectangle(0, 0, 44, 44),
                        GuiRenderingPlugin.FitCenteredRect(0, 0, 44, 44, 60, 20));

    [Fact]
    public void FitCenteredRect_zero_box_dim_falls_back_to_art_size()
        => Assert.Equal(new Rectangle(5, 5, 30, 30),
                        GuiRenderingPlugin.FitCenteredRect(5, 5, 0, 0, 30, 30));

    [Fact]
    public void RealBoundsRect_small_art_centers_in_box()
        => Assert.Equal(new Rectangle(17, 17, 10, 10),
                        GuiRenderingPlugin.RealBoundsRect(0, 0, 44, 44, new Rectangle(0, 0, 10, 10)));

    [Fact]
    public void RealBoundsRect_large_art_pins_to_corner_clamped_to_box()
        => Assert.Equal(new Rectangle(0, 0, 44, 44),
                        GuiRenderingPlugin.RealBoundsRect(0, 0, 44, 44, new Rectangle(0, 0, 60, 60)));

    [Fact]
    public void RealBoundsRect_zero_box_dim_falls_back_to_real_bounds()
        => Assert.Equal(new Rectangle(3, 3, 12, 12),
                        GuiRenderingPlugin.RealBoundsRect(3, 3, 0, 0, new Rectangle(0, 0, 12, 12)));
}
