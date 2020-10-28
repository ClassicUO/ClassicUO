namespace ClassicUO.Renderer
{
    internal static class Fonts
    {
        static Fonts()
        {
            Regular = SpriteFont.Create("ClassicUO.Renderer.fonts.regular_font.xnb");
            Bold = SpriteFont.Create("ClassicUO.Renderer.fonts.bold_font.xnb");

            Map1 = SpriteFont.Create("ClassicUO.Renderer.fonts.map1_font.xnb");
            Map2 = SpriteFont.Create("ClassicUO.Renderer.fonts.map2_font.xnb");
            Map3 = SpriteFont.Create("ClassicUO.Renderer.fonts.map3_font.xnb");
            Map4 = SpriteFont.Create("ClassicUO.Renderer.fonts.map4_font.xnb");
            Map5 = SpriteFont.Create("ClassicUO.Renderer.fonts.map5_font.xnb");
            Map6 = SpriteFont.Create("ClassicUO.Renderer.fonts.map6_font.xnb");
        }

        public static SpriteFont Regular { get; }
        public static SpriteFont Bold { get; }
        public static SpriteFont Map1 { get; }
        public static SpriteFont Map2 { get; }
        public static SpriteFont Map3 { get; }
        public static SpriteFont Map4 { get; }
        public static SpriteFont Map5 { get; }
        public static SpriteFont Map6 { get; }
    }
}