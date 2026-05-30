using System.Collections.Generic;
using ClassicUO.Ecs;
using Microsoft.Xna.Framework;
using Xunit;

namespace ClassicUO.Ecs.Tests;

// The pure tokenizing/parsing helpers behind the server-pushed gump DSL
// (packets 0xB0 / 0xDD). These run on raw, server-controlled strings, so the
// edge cases (missing args, '#' cliloc prefix, HTML markup, white-hue
// sentinels) are exactly where a malformed packet would otherwise crash or
// mis-render. No ECS world needed — they take strings and return values.
public class ServerGumpParseTests
{
    [Theory]
    [InlineData("5", 5)]
    [InlineData("-3", -3)]
    [InlineData("", 0)]
    [InlineData("garbage", 0)]
    [InlineData(null, 0)]
    public void SafeInt_falls_back_to_zero_on_non_numeric(string s, int expected)
        => Assert.Equal(expected, ServerGumpPlugin.SafeInt(s));

    [Fact]
    public void SafeLine_returns_the_line_in_range()
        => Assert.Equal("b", ServerGumpPlugin.SafeLine(new[] { "a", "b", "c" }, 1));

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void SafeLine_out_of_range_is_empty(int idx)
        => Assert.Equal("", ServerGumpPlugin.SafeLine(new[] { "a", "b", "c" }, idx));

    [Fact]
    public void SafeLine_null_entry_becomes_empty()
        => Assert.Equal("", ServerGumpPlugin.SafeLine(new string[] { null }, 0));

    [Theory]
    [InlineData("#1234", 1234)]   // cliloc rows arrive '#'-prefixed
    [InlineData("1234", 1234)]
    [InlineData("", 0)]
    [InlineData(null, 0)]
    [InlineData("##5", 5)]        // all '#' stripped
    public void ParseClilocId_strips_hash(string s, int expected)
        => Assert.Equal(expected, ServerGumpPlugin.ParseClilocId(s));

    [Fact]
    public void ParseHueArg_finds_hue_token()
    {
        var gp = new List<string> { "gumppic", "0", "0", "hue=0x44" };
        Assert.Equal(0x44, ServerGumpPlugin.ParseHueArg(gp, 1));
    }

    [Fact]
    public void ParseHueArg_decimal_value()
    {
        var gp = new List<string> { "x", "hue=10" };
        Assert.Equal(10, ServerGumpPlugin.ParseHueArg(gp, 0));
    }

    [Fact]
    public void ParseHueArg_absent_is_zero()
    {
        var gp = new List<string> { "gumppic", "0", "0" };
        Assert.Equal(0, ServerGumpPlugin.ParseHueArg(gp, 0));
    }

    [Fact]
    public void ParseHueArg_respects_start_index()
    {
        var gp = new List<string> { "hue=99", "hue=7" };
        Assert.Equal(7, ServerGumpPlugin.ParseHueArg(gp, 1)); // earlier hue= skipped
    }

    [Fact]
    public void StripTags_drops_markup_keeps_text()
        => Assert.Equal("Hello world", ServerGumpPlugin.StripTags("<basefont color=#fff>Hello <br>world</basefont>"));

    [Theory]
    [InlineData("plain", "plain")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("<b></b>", "")]
    public void StripTags_edge_cases(string s, string expected)
        => Assert.Equal(expected, ServerGumpPlugin.StripTags(s));

    [Theory]
    [InlineData(0x00FFFFFF)]
    [InlineData(0xFFFF)]
    [InlineData(0xFF)]
    public void HtmlStartColor_white_sentinels_map_to_full_white(int hue)
        => Assert.Equal(0xFFFFFFFEu, ServerGumpPlugin.HtmlStartColor(hue, hasBg: false, hasScroll: false));

    [Fact]
    public void HtmlStartColor_real_hue_keeps_opaque_alpha()
    {
        var c = ServerGumpPlugin.HtmlStartColor(33, hasBg: false, hasScroll: false);
        Assert.Equal(0xFFu, c & 0xFF); // opaque low byte
    }

    [Fact]
    public void HtmlStartColor_no_bg_with_scroll_is_white()
        => Assert.Equal(0xFFFFFFFFu, ServerGumpPlugin.HtmlStartColor(0, hasBg: false, hasScroll: true));

    [Fact]
    public void HtmlStartColor_no_bg_no_scroll_is_near_black()
        => Assert.Equal(0x010101FFu, ServerGumpPlugin.HtmlStartColor(0, hasBg: false, hasScroll: false));

    [Fact]
    public void HtmlStartColor_with_bg_is_near_black()
        => Assert.Equal(0x010101FFu, ServerGumpPlugin.HtmlStartColor(0, hasBg: true, hasScroll: true));

    [Fact]
    public void ToShaderHue_zero_is_passthrough_sentinel()
        => Assert.Equal(Vector3.UnitZ, ServerGumpPlugin.ToShaderHue(0));

    [Fact]
    public void ToShaderHue_nonzero_packs_hue_into_x()
        => Assert.Equal(new Vector3(42, 1f, 1f), ServerGumpPlugin.ToShaderHue(42));

    [Theory]
    [InlineData("Button", "button", true)]
    [InlineData("PAGE", "page", true)]
    [InlineData("text", "html", false)]
    public void Eq_is_case_insensitive(string a, string b, bool expected)
        => Assert.Equal(expected, ServerGumpPlugin.Eq(a, b));
}
