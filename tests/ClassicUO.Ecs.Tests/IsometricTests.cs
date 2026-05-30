using ClassicUO.Ecs;
using Xunit;

namespace ClassicUO.Ecs.Tests;

// The tile→screen projection every world-render + pick path relies on. Pure
// integer math, no assets — guards the 22px half-tile constant and the
// z << 2 (4px per z-step) vertical offset against accidental edits.
public class IsometricTests
{
    [Theory]
    [InlineData(0, 0, 0, 0f, 0f)]
    [InlineData(1, 0, 0, 22f, 22f)]   // +1 east: right + down
    [InlineData(0, 1, 0, -22f, 22f)]  // +1 south: left + down
    [InlineData(1, 1, 0, 0f, 44f)]    // straight down one row
    public void IsoToScreen_projects_tile_to_screen(ushort x, ushort y, sbyte z, float ex, float ey)
    {
        var p = Isometric.IsoToScreen(x, y, z);
        Assert.Equal(ex, p.X);
        Assert.Equal(ey, p.Y);
    }

    [Fact]
    public void IsoToScreen_z_raises_by_four_pixels_per_step()
    {
        var ground = Isometric.IsoToScreen(5, 5, 0);
        var raised = Isometric.IsoToScreen(5, 5, 1);
        Assert.Equal(ground.X, raised.X);            // z does not move X
        Assert.Equal(ground.Y - 4f, raised.Y);       // up by (z << 2)
    }

    [Fact]
    public void IsoToScreen_negative_z_drops_below_ground()
    {
        var ground = Isometric.IsoToScreen(5, 5, 0);
        var sunk = Isometric.IsoToScreen(5, 5, -1);
        Assert.Equal(ground.Y + 4f, sunk.Y);
    }

    [Theory]
    [InlineData(0, 0, 0, 0f)]
    [InlineData(0, 0, 5, 0.05f)]      // priorityZ contributes 0.01 each
    [InlineData(1, 1, 0, 1.4142f)]    // (x+y) * 0.7071
    public void GetDepthZ_combines_xy_diagonal_and_priority(int x, int y, int pz, float expected)
    {
        Assert.Equal(expected, Isometric.GetDepthZ(x, y, pz), 4);
    }

    [Fact]
    public void GetDepthZ_priority_breaks_ties_at_same_tile()
    {
        var lo = Isometric.GetDepthZ(10, 10, 0);
        var hi = Isometric.GetDepthZ(10, 10, 1);
        Assert.True(hi > lo);
    }
}
