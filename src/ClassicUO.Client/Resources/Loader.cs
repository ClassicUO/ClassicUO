using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Resources
{
    partial class Loader
    {
        [EmbedResourceCSharp.FileEmbed("cuologo.png")]
        public static partial ReadOnlySpan<byte> GetCuoLogo();

        [EmbedResourceCSharp.FileEmbed("game-background.png")]
        public static partial ReadOnlySpan<byte> GetBackgroundImage();
    }

    partial class TTFFontsLoader
    {
        [EmbedResourceCSharp.FileEmbed("Resources/Fonts/Roboto-Medium.ttf")]
        public static partial ReadOnlySpan<byte> Medium();

        [EmbedResourceCSharp.FileEmbed("Resources/Fonts/Roboto-MediumItalic.ttf")]
        public static partial ReadOnlySpan<byte> MediumItalic();

        [EmbedResourceCSharp.FileEmbed("Resources/Fonts/Roboto-Bold.ttf")]
        public static partial ReadOnlySpan<byte> Bold();

        [EmbedResourceCSharp.FileEmbed("Resources/Fonts/Roboto-BoldItalic.ttf")]
        public static partial ReadOnlySpan<byte> BoldItalic();

        [EmbedResourceCSharp.FileEmbed("Resources/Fonts/Roboto-Regular.ttf")]
        public static partial ReadOnlySpan<byte> Regular();


        public static ReadOnlySpan<byte> GetFont(string font)
        {
            switch (font?.ToLowerInvariant() ?? "")
            {
                default:
                case "medium": return Medium();
                case "italic": return MediumItalic();
                case "bold": return Bold();
                case "bold-italic": return BoldItalic();
                case "regular": return Regular();
            };
        }
    }
}
