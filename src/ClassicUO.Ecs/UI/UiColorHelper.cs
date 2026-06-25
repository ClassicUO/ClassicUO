using ClassicUO.Assets;
using ClayColor = Clay.Color;

namespace ClassicUO.Ecs;

/// UO hue → ClayColor for tinting white unicode glyphs (cell 30, +1 wire
/// convention). Shared by ServerGumpPlugin + TextEntryDialogPlugin (was a
/// byte-identical private copy in each). hue==0 / invalid → black.
internal static class UiColorHelper
{
    // OOP RenderedText defaults cell=30 (RenderedText.Create cell=30 arg →
    // GenerateUnicode), the brightest end of the hue gradient. The tinted
    // ClayColor multiplies a white glyph drawn from the shared atlas
    // (UoFontRenderer.Draw → SHADER_RGB_TINT), so it keeps per-glyph alpha
    // exactly like OOP — a bright gold hue reads as bright gold, not the
    // muted olive cell=4 produced. Mirrors HueToTint (wrapped-text path),
    // which already uses 30. parts[3] + 1 mirrors OOP's Label ctor "+1".
    public static ClayColor HueToClayColor(HuesLoader hues, ushort hue)
    {
        if (hue == 0) return ClayColor.Black;
        var packed = hues.GetPolygoneColor(30, (ushort)(hue + 1));
        if (packed == 0 || packed == 0xFF010101) return ClayColor.Black;
        byte r = (byte)(packed & 0xFF);
        byte g = (byte)((packed >> 8) & 0xFF);
        byte b = (byte)((packed >> 16) & 0xFF);
        return new ClayColor(r, g, b, 255);
    }
}
