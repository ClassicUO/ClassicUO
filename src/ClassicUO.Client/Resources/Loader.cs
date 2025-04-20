using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Resources
{
    public partial class Loader
    {
        [FileEmbed.FileEmbed("cuologo.png")]
        public static partial ReadOnlySpan<byte> GetCuoLogo();

        [FileEmbed.FileEmbed("game-background.png")]
        public static partial ReadOnlySpan<byte> GetBackgroundImage();
    }

    public partial class TTFFontsLoader
    {
        [FileEmbed.FileEmbed("Resources/Fonts/Roboto-Medium.ttf")]
        public static partial ReadOnlySpan<byte> Medium();

        [FileEmbed.FileEmbed("Resources/Fonts/Roboto-MediumItalic.ttf")]
        public static partial ReadOnlySpan<byte> MediumItalic();

        [FileEmbed.FileEmbed("Resources/Fonts/Roboto-Bold.ttf")]
        public static partial ReadOnlySpan<byte> Bold();

        [FileEmbed.FileEmbed("Resources/Fonts/Roboto-BoldItalic.ttf")]
        public static partial ReadOnlySpan<byte> BoldItalic();

        [FileEmbed.FileEmbed("Resources/Fonts/Roboto-Regular.ttf")]
        public static partial ReadOnlySpan<byte> Regular();


        public static ReadOnlySpan<byte> GetFont(string font)
        {
            return (font?.ToLowerInvariant() ?? "") switch
            {
                "italic" => MediumItalic(),
                "bold" => Bold(),
                "bold-italic" => BoldItalic(),
                "regular" => Regular(),
                _ => Medium(),
            };
        }
    }
}
