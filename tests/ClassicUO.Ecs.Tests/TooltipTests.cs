using ClassicUO.Ecs;
using ClassicUO.Game.Data;
using TinyEcs.Bevy.UI;
using Xunit;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;
using NumVector2 = System.Numerics.Vector2;

namespace ClassicUO.Ecs.Tests;

// Unit tests for the tooltip's pure pockets: the on-screen box clamp (cursor
// offset + edge containment) and the OPL -> HTML builder. TrackAndRender itself
// is hover/asset/render-driven; these are the value bits pulled out of it.
public class TooltipTests
{
    private static UiSurface Surface(float w, float h) => new UiSurface { LogicalSize = new NumVector2(w, h) };

    [Fact]
    public void ClampToScreen_offsets_down_right_when_it_fits()
    {
        var (x, y) = TooltipPlugin.ClampToScreen(new XnaVector2(100, 100), 50, 30, Surface(800, 600));
        Assert.Equal(108f, x); // +OffsetX 8
        Assert.Equal(122f, y); // +OffsetY 22
    }

    [Fact]
    public void ClampToScreen_pulls_the_box_in_at_the_right_and_bottom_edges()
    {
        var (x, y) = TooltipPlugin.ClampToScreen(new XnaVector2(790, 590), 50, 30, Surface(800, 600));
        Assert.Equal(750f, x); // 800 - 50
        Assert.Equal(570f, y); // 600 - 30
    }

    [Fact]
    public void ClampToScreen_never_goes_negative()
    {
        var (x, y) = TooltipPlugin.ClampToScreen(new XnaVector2(-20, -50), 50, 30, Surface(800, 600));
        Assert.Equal(0f, x);
        Assert.Equal(0f, y);
    }

    [Fact]
    public void BuildHtml_item_name_is_yellow_then_white_properties()
    {
        var e = new ObjectPropertyLists.Entry { Name = "Sword", Data = "Exceptional" };
        var html = TooltipPlugin.BuildHtml(0x4000_0001, in e, NotorietyFlag.Unknown); // item serial
        Assert.Equal("<basefont color=\"yellow\">Sword<basefont color=\"#FFFFFFFF\">\nExceptional", html);
    }

    [Fact]
    public void BuildHtml_name_only_has_no_trailing_newline()
    {
        var e = new ObjectPropertyLists.Entry { Name = "Sword", Data = "" };
        Assert.Equal("<basefont color=\"yellow\">Sword<basefont color=\"#FFFFFFFF\">",
            TooltipPlugin.BuildHtml(0x4000_0001, in e, NotorietyFlag.Unknown));
    }

    [Fact]
    public void BuildHtml_data_only_renders_plain()
    {
        var e = new ObjectPropertyLists.Entry { Name = "", Data = "just props" };
        Assert.Equal("just props", TooltipPlugin.BuildHtml(0x4000_0001, in e, NotorietyFlag.Unknown));
    }

    [Fact]
    public void BuildHtml_empty_entry_is_empty()
    {
        var e = new ObjectPropertyLists.Entry { Name = "", Data = "" };
        Assert.Equal("", TooltipPlugin.BuildHtml(0x4000_0001, in e, NotorietyFlag.Unknown));
    }
}
