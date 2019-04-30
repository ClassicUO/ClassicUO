namespace ClassicUO.Renderer
{
    internal static class Fonts
    {
        public static SpriteFont Regular { get; private set; }
        public static SpriteFont Bold { get; private set; }

        public static void Load()
        {
            Regular = SpriteFont.Create("ClassicUO.Renderer.fonts.regular_font.xnb");
            Bold = SpriteFont.Create("ClassicUO.Renderer.fonts.bold_font.xnb");
        }
    }
}