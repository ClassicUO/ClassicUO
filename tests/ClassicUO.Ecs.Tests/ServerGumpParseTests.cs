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

    [Fact]
    public void HtmlInnerRect_plain_is_unchanged()
        => Assert.Equal((10, 20, 100, 50), ServerGumpPlugin.HtmlInnerRect(10, 20, 100, 50, hasBg: false, hasScroll: false));

    [Fact]
    public void HtmlInnerRect_scrollbar_reserves_16px_width()
        => Assert.Equal((10, 20, 84, 50), ServerGumpPlugin.HtmlInnerRect(10, 20, 100, 50, hasBg: false, hasScroll: true));

    [Fact]
    public void HtmlInnerRect_bg_insets_4px_each_side()
        => Assert.Equal((14, 24, 92, 42), ServerGumpPlugin.HtmlInnerRect(10, 20, 100, 50, hasBg: true, hasScroll: false));

    [Fact]
    public void HtmlInnerRect_bg_and_scroll_combine_insets()
        => Assert.Equal((14, 24, 76, 42), ServerGumpPlugin.HtmlInnerRect(10, 20, 100, 50, hasBg: true, hasScroll: true));

    // --- Per-command numeric arg parsing (ServerGumpCommand) -----------------
    // Each Try* mirrors the exact inline guard BuildGump used to carry: same
    // minimum token count, same int/ushort widths, same field order. A
    // malformed (short / non-numeric) command must return false and spawn
    // nothing, not throw — these run on raw server bytes.

    static string[] T(params string[] s) => s;

    [Fact]
    public void TryResizePic_parses_x_y_id_w_h()
    {
        Assert.True(ServerGumpCommand.TryResizePic(T("resizepic", "10", "20", "9200", "100", "50"), out var a));
        Assert.Equal(new ServerGumpCommand.ResizePic(10, 20, 9200, 100, 50), a);
    }

    [Theory]
    [InlineData("resizepic", "10", "20", "9200", "100")]      // one short
    [InlineData("resizepic", "x", "20", "9200", "100", "50")] // non-numeric x
    public void TryResizePic_rejects_malformed(params string[] gp)
        => Assert.False(ServerGumpCommand.TryResizePic(gp, out _));

    [Fact]
    public void TryGumpPic_parses_pos_and_id()
    {
        Assert.True(ServerGumpCommand.TryGumpPic(T("gumppic", "5", "6", "100"), out var a));
        Assert.Equal(new ServerGumpCommand.PicAt(5, 6, 100), a);
    }

    [Fact]
    public void TryGumpPic_rejects_short()
        => Assert.False(ServerGumpCommand.TryGumpPic(T("gumppic", "5", "6"), out _));

    [Fact]
    public void TryGumpPicHued_requires_five_tokens()
    {
        Assert.False(ServerGumpCommand.TryGumpPicHued(T("gumppichued", "5", "6", "100"), out _));
        Assert.True(ServerGumpCommand.TryGumpPicHued(T("gumppichued", "5", "6", "100", "33"), out var a));
        Assert.Equal(new ServerGumpCommand.PicAt(5, 6, 100), a);
    }

    [Fact]
    public void TryGumpPicTiled_field_order_is_x_y_w_h_id()
    {
        Assert.True(ServerGumpCommand.TryGumpPicTiled(T("gumppictiled", "1", "2", "30", "40", "9000"), out var a));
        Assert.Equal(new ServerGumpCommand.GumpPicTiled(1, 2, 30, 40, 9000), a);
    }

    [Fact]
    public void TryTilePic_parses_pos_and_id()
    {
        Assert.True(ServerGumpCommand.TryTilePic(T("tilepic", "8", "9", "3850"), out var a));
        Assert.Equal(new ServerGumpCommand.PicAt(8, 9, 3850), a);
    }

    [Fact]
    public void TryButton_defaults_optional_action_fields_to_zero()
    {
        Assert.True(ServerGumpCommand.TryButton(T("button", "10", "20", "247", "248"), out var a));
        Assert.Equal(new ServerGumpCommand.Button(10, 20, 247, 248, 0, 0, 0), a);
    }

    [Fact]
    public void TryButton_reads_action_topage_btnId()
    {
        Assert.True(ServerGumpCommand.TryButton(T("button", "10", "20", "247", "248", "1", "0", "7"), out var a));
        Assert.Equal(new ServerGumpCommand.Button(10, 20, 247, 248, 1, 0, 7), a);
    }

    [Fact]
    public void TryButton_rejects_short()
        => Assert.False(ServerGumpCommand.TryButton(T("button", "10", "20", "247"), out _));

    [Fact]
    public void TryText_parses_x_y_hue_lineId()
    {
        Assert.True(ServerGumpCommand.TryText(T("text", "4", "5", "996", "0"), out var a));
        Assert.Equal(new ServerGumpCommand.Text(4, 5, 996, 0), a);
    }

    [Fact]
    public void TryCroppedText_parses_rect_hue_lineId()
    {
        Assert.True(ServerGumpCommand.TryCroppedText(T("croppedtext", "4", "5", "60", "20", "996", "2"), out var a));
        Assert.Equal(new ServerGumpCommand.CroppedText(4, 5, 60, 20, 996, 2), a);
    }

    [Fact]
    public void TryHtmlBlock_flags_off_without_optional_tokens()
    {
        Assert.True(ServerGumpCommand.TryHtmlBlock(T("htmlgump", "0", "0", "200", "100", "3"), out var a));
        Assert.Equal(new ServerGumpCommand.HtmlBlock(0, 0, 200, 100, HasBg: false, HasScroll: false), a);
    }

    [Fact]
    public void TryHtmlBlock_reads_bg_and_scroll_flags()
    {
        // gp[6]=="1" → hasBg; gp[7]!="0" → hasScroll
        Assert.True(ServerGumpCommand.TryHtmlBlock(T("htmlgump", "0", "0", "200", "100", "3", "1", "1"), out var a));
        Assert.True(a.HasBg);
        Assert.True(a.HasScroll);

        Assert.True(ServerGumpCommand.TryHtmlBlock(T("htmlgump", "0", "0", "200", "100", "3", "0", "0"), out var b));
        Assert.False(b.HasBg);
        Assert.False(b.HasScroll);
    }

    [Fact]
    public void TryXmfHtmlTok_requires_nine_tokens_and_reads_flags_at_5_6()
    {
        Assert.False(ServerGumpCommand.TryXmfHtmlTok(T("xmfhtmltok", "0", "0", "200", "100", "1", "1", "0"), out _));
        Assert.True(ServerGumpCommand.TryXmfHtmlTok(T("xmfhtmltok", "0", "0", "200", "100", "1", "1", "0", "1234", "@arg@"), out var a));
        Assert.Equal(new ServerGumpCommand.HtmlBlock(0, 0, 200, 100, HasBg: true, HasScroll: true), a);
    }

    [Fact]
    public void TryCheck_initial_state_picks_checked_asset()
    {
        Assert.True(ServerGumpCommand.TryCheck(T("checkbox", "3", "4", "210", "211", "1"), out var on));
        Assert.True(on.Initial);
        Assert.True(ServerGumpCommand.TryCheck(T("checkbox", "3", "4", "210", "211"), out var off));
        Assert.False(off.Initial);
        Assert.Equal(new ServerGumpCommand.Check(3, 4, 210, 211, false), off);
    }

    [Fact]
    public void TryPicInPic_reads_dest_size_from_tokens_6_7()
    {
        Assert.True(ServerGumpCommand.TryPicInPic(T("picinpic", "1", "2", "100", "0", "0", "40", "30"), out var a));
        Assert.Equal(new ServerGumpCommand.PicInPic(1, 2, 100, 40, 30), a);
    }

    [Fact]
    public void TryRect_parses_x_y_w_h()
    {
        Assert.True(ServerGumpCommand.TryRect(T("checkertrans", "5", "6", "70", "80"), out var a));
        Assert.Equal(new ServerGumpCommand.Rect(5, 6, 70, 80), a);
    }

    [Fact]
    public void TryRect_rejects_short()
        => Assert.False(ServerGumpCommand.TryRect(T("checkertrans", "5", "6", "70"), out _));
}
