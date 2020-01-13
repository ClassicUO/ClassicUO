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

namespace ClassicUO.Game.Data
{
    enum GraphicEffectBlendMode
    {
        Normal = 0x00, // normal, black is transparent
        Multiply = 0x01, // darken
        Screen = 0x02, // lighten
        ScreenMore = 0x03, // lighten more (slightly)
        ScreenLess = 0x04, // lighten less
        NormalHalfTransparent = 0x05, // transparent but with black edges - 50% transparency?
        ShadowBlue = 0x06, // complete shadow with blue edges
        ScreenRed = 0x07 // transparent more red
    }
}