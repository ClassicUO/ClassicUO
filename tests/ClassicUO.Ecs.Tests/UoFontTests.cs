using ClassicUO.Assets;
using Xunit;

namespace ClassicUO.Ecs.Tests;

// Pure-logic coverage for the UO font pipeline: font id resolution, the ASCII
// hue packing contract, and the line-walk helpers shared by DrawGlyphs and
// BuildHitMask. The glyph rasterisation itself needs UO data files and is
// covered by the agent-harness screenshot loop, not unit tests.
public class UoFontTests
{
    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(1, 1, false)]
    [InlineData(19, 19, false)]
    [InlineData(20, UoFontRuntime.DefaultFont, false)] // >= 20 falls back
    [InlineData(0x7F, UoFontRuntime.DefaultFont, false)]
    public void Resolve_unicode_font_index(ushort fontId, byte expectedFont, bool expectedAscii)
    {
        var (font, ascii) = UoFontRuntime.Resolve(fontId);
        Assert.Equal(expectedFont, font);
        Assert.Equal(expectedAscii, ascii);
    }

    [Theory]
    [InlineData(UoFontRuntime.AsciiFlag | 2, 2)]
    [InlineData(UoFontRuntime.AsciiFlag | 5, 5)]
    [InlineData(UoFontRuntime.AsciiFlag | 9, 9)]
    [InlineData(UoFontRuntime.AsciiFlag | 25, UoFontRuntime.DefaultFont)]
    public void Resolve_ascii_flag_selects_ascii_set(int fontId, byte expectedFont)
    {
        var (font, ascii) = UoFontRuntime.Resolve((ushort)fontId);
        Assert.True(ascii);
        Assert.Equal(expectedFont, font);
    }

    // AsciiHue packs the UO hue into ClayColor R/G; UoFontRenderer.Draw decodes
    // it from the XnaColor tint as (tint.R | tint.G << 8). The roundtrip must
    // survive the ClayColor(float) → XnaColor(byte) conversion.
    [Theory]
    [InlineData((ushort)0)]
    [InlineData((ushort)1)]
    [InlineData((ushort)0x21)]
    [InlineData((ushort)0x0481)]
    [InlineData((ushort)0xFFFF)]
    public void AsciiHue_roundtrips_through_color_channels(ushort hue)
    {
        var c = UoFontRuntime.AsciiHue(hue);
        ushort decoded = (ushort)((byte)c.R | ((byte)c.G << 8));
        Assert.Equal(hue, decoded);
        Assert.Equal(0f, c.B);
        Assert.Equal(255f, c.A);
    }

    private static MultilinesFontInfo Line(TEXT_ALIGN_TYPE align, int width, int maxHeight = 20)
        => new() { Align = align, Width = width, MaxHeight = maxHeight };

    [Fact]
    public void LineAlignOffset_left_is_zero()
    {
        Assert.Equal(0, UoFontRenderer.LineAlignOffset(Line(TEXT_ALIGN_TYPE.TS_LEFT, 50), 200, isUnicode: true));
        Assert.Equal(0, UoFontRenderer.LineAlignOffset(Line(TEXT_ALIGN_TYPE.TS_LEFT, 50), 200, isUnicode: false));
    }

    [Fact]
    public void LineAlignOffset_center_differs_per_font_set()
    {
        // Unicode: (textWidth - 8) / 2 - lineWidth / 2; ASCII: (textWidth - lineWidth) >> 1.
        Assert.Equal((200 - 8) / 2 - 50 / 2, UoFontRenderer.LineAlignOffset(Line(TEXT_ALIGN_TYPE.TS_CENTER, 50), 200, isUnicode: true));
        Assert.Equal((200 - 50) >> 1, UoFontRenderer.LineAlignOffset(Line(TEXT_ALIGN_TYPE.TS_CENTER, 50), 200, isUnicode: false));
    }

    [Fact]
    public void LineAlignOffset_right_and_negative_clamp()
    {
        Assert.Equal(200 - 10 - 50, UoFontRenderer.LineAlignOffset(Line(TEXT_ALIGN_TYPE.TS_RIGHT, 50), 200, isUnicode: true));
        // A line wider than the block clamps to 0 instead of going negative.
        Assert.Equal(0, UoFontRenderer.LineAlignOffset(Line(TEXT_ALIGN_TYPE.TS_CENTER, 300), 200, isUnicode: true));
        Assert.Equal(0, UoFontRenderer.LineAlignOffset(Line(TEXT_ALIGN_TYPE.TS_RIGHT, 300), 200, isUnicode: false));
    }

    [Fact]
    public void LinePitch_ascii_font6_reduces_spacing_by_7()
    {
        var line = Line(TEXT_ALIGN_TYPE.TS_LEFT, 50, maxHeight: 20);
        Assert.Equal(13, UoFontRenderer.LinePitch(line, font: 6, isUnicode: false));
        Assert.Equal(20, UoFontRenderer.LinePitch(line, font: 6, isUnicode: true));
        Assert.Equal(20, UoFontRenderer.LinePitch(line, font: 3, isUnicode: false));
    }

    [Theory]
    [InlineData(0x0000, false, false, false)]
    [InlineData(0x0001, false, true, false)]  // UOFONT_SOLID
    [InlineData(0x0002, false, false, true)]  // UOFONT_ITALIC
    [InlineData(0x0008, true, false, false)]  // UOFONT_BLACK_BORDER
    [InlineData(0x000B, true, true, true)]
    public void HtmlCharStyle_decodes_flags(int flags, bool border, bool solid, bool italic)
    {
        var d = new MultilinesFontData(0xFFFFFFFF, (ushort)flags, font: 3, 'a', 0);
        var style = UoFontRenderer.HtmlCharStyle(in d);
        Assert.Equal((byte)3, style.Font);
        Assert.Equal(border, style.Border);
        Assert.Equal(solid, style.Solid);
        Assert.Equal(italic, style.Italic);
    }

    [Fact]
    public void StripUnderlineTags_removes_only_underline_tags()
    {
        Assert.Equal("hello world", UoFontRenderer.StripUnderlineTags("<U>hello</U> <u>world</u>"));
        Assert.Equal("<basefont color=#ff0000>hi</basefont>", UoFontRenderer.StripUnderlineTags("<basefont color=#ff0000><U>hi</U></basefont>"));
    }

    [Fact]
    public void StripUnderlineTags_no_tag_returns_same_instance()
    {
        var s = "plain text, no markup";
        Assert.Same(s, UoFontRenderer.StripUnderlineTags(s));
    }
}
