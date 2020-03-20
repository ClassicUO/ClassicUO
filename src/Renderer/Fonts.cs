#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

namespace ClassicUO.Renderer
{
    internal static class Fonts
    {
        public static SpriteFont Regular { get; private set; }
        public static SpriteFont Bold { get; private set; }
        public static SpriteFont Map1 { get; private set; }
        public static SpriteFont Map2 { get; private set; }
        public static SpriteFont Map3 { get; private set; }
        public static SpriteFont Map4 { get; private set; }
        public static SpriteFont Map5 { get; private set; }
        public static SpriteFont Map6 { get; private set; }

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
    }
}