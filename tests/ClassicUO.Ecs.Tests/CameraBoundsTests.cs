using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Xunit;

namespace ClassicUO.Ecs.Tests;

// World-object selection is confined to the game viewport: a click in the top
// bar or a side gump gutter (inside the window, outside Bounds) must not pick a
// tile/item/mobile. IsMouseInsideBounds is the gate; _mousePos is window space,
// same frame as Bounds, so this is a plain Rectangle.Contains.
public class CameraBoundsTests
{
    private static Camera WithMouse(Rectangle bounds, int mx, int my)
    {
        var cam = new Camera { Bounds = bounds };
        cam.Update(force: true, timeDelta: 0f, mousePos: new Point(mx, my));
        return cam;
    }

    [Fact]
    public void Mouse_inside_viewport_is_in_bounds()
    {
        var cam = WithMouse(new Rectangle(0, 27, 800, 573), 400, 300);
        Assert.True(cam.IsMouseInsideBounds());
    }

    [Fact]
    public void Mouse_above_viewport_top_is_out_of_bounds()
    {
        // y < Bounds.Y — the top bar region above a maximized game scene.
        var cam = WithMouse(new Rectangle(0, 27, 800, 573), 400, 10);
        Assert.False(cam.IsMouseInsideBounds());
    }

    [Fact]
    public void Mouse_left_of_windowed_viewport_is_out_of_bounds()
    {
        // Windowed game scene offset into the window; gutter to its left.
        var cam = WithMouse(new Rectangle(100, 100, 400, 300), 50, 200);
        Assert.False(cam.IsMouseInsideBounds());
    }

    [Fact]
    public void Mouse_past_right_and_bottom_edges_is_out_of_bounds()
    {
        var bounds = new Rectangle(0, 0, 200, 150);
        Assert.False(WithMouse(bounds, 200, 75).IsMouseInsideBounds());  // x == right edge (exclusive)
        Assert.False(WithMouse(bounds, 100, 150).IsMouseInsideBounds()); // y == bottom edge (exclusive)
    }

    [Fact]
    public void Top_left_corner_is_inside_bottom_right_corner_is_outside()
    {
        var bounds = new Rectangle(10, 20, 100, 80);
        Assert.True(WithMouse(bounds, 10, 20).IsMouseInsideBounds());    // inclusive top-left
        Assert.False(WithMouse(bounds, 110, 100).IsMouseInsideBounds()); // exclusive bottom-right
    }
}
