using System;
using System.Collections.Generic;
using ClassicUO.Resources;
using FontStashSharp;
using FontStashSharp.RichText;
using TinyEcs;
using TinyEcs.Bevy;

namespace ClassicUO.Ecs;

internal readonly struct FontsPlugin : IPlugin
{
    public void Build(App app)
    {
        // scheduler.AddResource(new FontCache());

        app
            .AddSystem(Stage.Startup, (w) =>
            {
                void registerFont(string name, ReadOnlySpan<byte> fontData)
                {
                    var fontSystem = new FontSystem();
                    fontSystem.AddFont(fontData.ToArray());
                    FontCache.Register(name, fontSystem);
                }

                registerFont("medium", TTFFontsLoader.Medium());
                registerFont("regular", TTFFontsLoader.Regular());
                registerFont("bold", TTFFontsLoader.Bold());
                registerFont("bold-italic", TTFFontsLoader.BoldItalic());
                registerFont("italic", TTFFontsLoader.MediumItalic());
            });
    }
}

internal static class FontCache
{
    private static readonly Dictionary<string, FontSystem> _fontsCache = new();
    private static readonly List<FontSystem> _fonts = new();

    static FontCache()
    {
        RichTextDefaults.FontResolver = p =>
        {
            // Parse font name and size
            var args = p.Split(',');
            var fontName = args[0].Trim();
            var fontSize = int.Parse(args[1].Trim());
            // _fontCache is field of type Dictionary<string, FontSystem>
            // It is used to cache fonts
            if (!_fontsCache.TryGetValue(fontName, out var fontSystem))
            {
                // Load and cache the font system
                fontSystem = new FontSystem();
                fontSystem.AddFont(TTFFontsLoader.GetFont(fontName).ToArray());
                _fontsCache[fontName] = fontSystem;
            }
            // Return the required font
            return fontSystem.GetFont(fontSize);
        };
    }

    public static void Register(string fontName, FontSystem fontSystem)
    {
        ArgumentNullException.ThrowIfNull(fontName);
        _fontsCache[fontName] = fontSystem ?? throw new ArgumentNullException(nameof(fontSystem));
        _fonts.Add(fontSystem);
    }


    public static FontSystem GetFont(int fontIndex)
    {
        if (fontIndex < 0 || fontIndex >= _fonts.Count)
            throw new ArgumentOutOfRangeException(nameof(fontIndex), "Font index is out of range.");

        return _fonts[fontIndex];
    }
}

public enum FontType
{
    Regular,
    Bold,
    Italic,
    BoldItalic,
    Medium,
    MediumItalic,
}
