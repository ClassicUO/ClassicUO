using System;

namespace FontStashSharp.RichText
{
    public class RichTextSettings
    {
        public Func<string, SpriteFontBase> FontResolver { get; set; }
        public Func<string, IRenderable> ImageResolver { get; set; }

        public RichTextSettings()
        {
            FontResolver = RichTextDefaults.FontResolver;
            ImageResolver = RichTextDefaults.ImageResolver;
        }
    }
}