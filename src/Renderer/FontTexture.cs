using ClassicUO.IO.Resources;
using ClassicUO.Utility.Collections;

namespace ClassicUO.Renderer
{
    internal class FontTexture : UOTexture
    {
        public FontTexture(int width, int height, int lineCount, RawList<WebLinkRect> links)
            : base(width, height)
        {
            LineCount = lineCount;
            Links = links;
        }

        public int LineCount { get; set; }

        public RawList<WebLinkRect> Links { get; }
    }
}