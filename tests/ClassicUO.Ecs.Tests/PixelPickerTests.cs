using ClassicUO.Renderer;
using Xunit;

namespace ClassicUO.Ecs.Tests;

// The CPU transparency mask behind every pixel-perfect UO hit-test: Gump/Art
// PixelCheck just compute a key + delegate to PixelPicker. AssetsServer needs
// UO files + a GPU, but THIS — the RLE span walk that decides opaque vs
// transparent — is pure and takes a synthetic ARGB buffer. pixel == 0 is
// transparent; anything else is opaque.
//
// Two boundary quirks are INHERENT to the ported span walk (`target < current`
// is strict, and the loop never runs for target 0): linear index 0 always
// reads miss, and the first opaque pixel immediately after a transparent run
// reads miss. Real UO sprites carry a transparent border, so deep-interior
// pixels are what hit-testing relies on — those are exact. The quirks are
// pinned below so a future change to them is a conscious decision.
public class PixelPickerTests
{
    private const uint O = 0xFFFFFFFF; // opaque
    private const uint T = 0;          // transparent

    [Fact]
    public void Interior_opaque_pixels_hit_transparent_ones_miss()
    {
        // T O O O T  — indices 2,3 are deep inside the opaque run.
        var p = new PixelPicker();
        p.Set(1, 5, 1, new[] { T, O, O, O, T });
        Assert.True(p.Get(1, 2, 0));
        Assert.True(p.Get(1, 3, 0));
        Assert.False(p.Get(1, 0, 0)); // transparent
        Assert.False(p.Get(1, 4, 0)); // transparent
    }

    [Fact]
    public void Interior_of_a_2d_solid_sprite_hits()
    {
        var p = new PixelPicker();
        p.Set(2, 3, 3, new[] { O, O, O,
                               O, O, O,
                               O, O, O });
        Assert.True(p.Get(2, 1, 1));
        Assert.True(p.Get(2, 2, 2));
    }

    [Fact]
    public void Quirk_linear_index_zero_always_misses_even_when_opaque()
    {
        var p = new PixelPicker();
        p.Set(3, 1, 1, new[] { O });
        Assert.False(p.Get(3, 0, 0)); // documented boundary quirk
    }

    [Fact]
    public void Quirk_first_opaque_pixel_after_transparent_run_misses()
    {
        // T O : index 1 is opaque but sits on the run boundary.
        var p = new PixelPicker();
        p.Set(4, 2, 1, new[] { T, O });
        Assert.False(p.Get(4, 1, 0)); // documented boundary quirk
    }

    [Fact]
    public void Get_out_of_bounds_is_false()
    {
        var p = new PixelPicker();
        p.Set(5, 3, 3, new[] { O, O, O, O, O, O, O, O, O });
        Assert.False(p.Get(5, -1, 0));
        Assert.False(p.Get(5, 0, -1));
        Assert.False(p.Get(5, 3, 0)); // x == width
        Assert.False(p.Get(5, 0, 3)); // y == height
    }

    [Fact]
    public void Get_unknown_texture_id_is_false()
        => Assert.False(new PixelPicker().Get(999, 1, 1));

    [Fact]
    public void Fully_transparent_sprite_never_hits()
    {
        var p = new PixelPicker();
        p.Set(6, 2, 2, new[] { T, T, T, T });
        Assert.False(p.Get(6, 1, 1));
        Assert.False(p.Get(6, 0, 1));
    }

    [Fact]
    public void GetDimensions_reports_stored_size()
    {
        var p = new PixelPicker();
        p.Set(7, 12, 5, new uint[12 * 5]);
        p.GetDimensions(7, out var w, out var h);
        Assert.Equal(12, w);
        Assert.Equal(5, h);
    }

    [Fact]
    public void GetDimensions_unknown_id_is_zero()
    {
        new PixelPicker().GetDimensions(404, out var w, out var h);
        Assert.Equal(0, w);
        Assert.Equal(0, h);
    }

    [Fact]
    public void Set_is_idempotent_first_write_wins()
    {
        // Re-Set on the same id is ignored (Has guard), so the cached mask is
        // never silently rebuilt with a different graphic.
        var p = new PixelPicker();
        p.Set(8, 1, 1, new[] { O });
        p.Set(8, 4, 4, new uint[16]); // ignored
        p.GetDimensions(8, out var w, out var h);
        Assert.Equal(1, w);
        Assert.Equal(1, h);
    }
}
