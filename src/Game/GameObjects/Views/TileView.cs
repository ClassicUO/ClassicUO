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

using System.Runtime.CompilerServices;

using ClassicUO.Configuration;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class Land
    {
        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY)
        {
            if (!AllowedToDraw || IsDestroyed)
                return false;

            //Engine.DebugInfo.LandsRendered++;

            ResetHueVector();

            ushort hue = Hue;

            if (ProfileManager.Current.HighlightGameObjects && SelectedObject.LastObject == this)
            {
                hue = Constants.HIGHLIGHT_CURRENT_OBJECT_HUE;
            }
            else if (ProfileManager.Current.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
            {
                hue = Constants.OUT_RANGE_COLOR;
            }
            else if (World.Player.IsDead && ProfileManager.Current.EnableBlackWhiteEffect)
            {
                hue = Constants.DEAD_RANGE_COLOR;
            }


            if (hue != 0)
            {
                HueVector.X = hue - 1;
                HueVector.Y = IsStretched ? ShaderHuesTraslator.SHADER_LAND_HUED : ShaderHuesTraslator.SHADER_HUED;
            }
            else
            {
                HueVector.Y = IsStretched ? ShaderHuesTraslator.SHADER_LAND : ShaderHuesTraslator.SHADER_NONE;
            }

            if (IsStretched)
            {
                posY += (Z << 2);

                DrawLand(
                    batcher,
                    Graphic, posX, posY, 
                    ref Rectangle, ref Normal0, ref Normal1, ref Normal2, ref Normal3,
                    ref HueVector);

                if (SelectedObject.IsPointInStretchedLand(ref Rectangle, posX, posY))
                    SelectedObject.Object = this;
            }
            else
            {
                DrawLand(batcher, Graphic, posX, posY, ref HueVector);

                if (SelectedObject.IsPointInLand(posX, posY))
                    SelectedObject.Object = this;
            }

            return true;
        }
    }
}