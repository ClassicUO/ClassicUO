using Xunit;

namespace ClassicUO.Ecs.Tests;

// SelectedEntity is the single sink for every world-object pick (tiles, statics,
// bodies, equipment all call Set). Enabled is the viewport gate: when the mouse
// is outside the game scene Bounds the renderer flips it off and no pick lands.
// Set keeps the highest-depth (visually topmost) entity; Clear promotes the
// frame's pick to Entity for downstream systems to read.
public class SelectedEntityTests
{
    private const ulong A = 100;
    private const ulong B = 200;

    [Fact]
    public void Enabled_set_then_clear_exposes_the_pick()
    {
        var sel = new SelectedEntity();
        sel.Set(A, 5f);
        sel.Clear();
        Assert.Equal(A, sel.Entity);
    }

    [Fact]
    public void Disabled_set_is_ignored()
    {
        var sel = new SelectedEntity { Enabled = false };
        sel.Set(A, 5f);
        sel.Clear();
        Assert.Equal(0ul, sel.Entity); // mouse outside viewport -> nothing picked
    }

    [Fact]
    public void Higher_depth_pick_wins_over_lower()
    {
        var sel = new SelectedEntity();
        sel.Set(A, 5f);
        sel.Set(B, 8f); // rendered later / topmost
        sel.Clear();
        Assert.Equal(B, sel.Entity);
    }

    [Fact]
    public void Lower_depth_pick_does_not_displace_higher()
    {
        var sel = new SelectedEntity();
        sel.Set(A, 5f);
        sel.Set(B, 3f); // behind A
        sel.Clear();
        Assert.Equal(A, sel.Entity);
    }

    [Fact]
    public void Re_enabling_restores_picking()
    {
        var sel = new SelectedEntity { Enabled = false };
        sel.Set(A, 5f);
        sel.Clear();
        Assert.Equal(0ul, sel.Entity);

        sel.Enabled = true;
        sel.Set(B, 5f);
        sel.Clear();
        Assert.Equal(B, sel.Entity);
    }
}
