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

            if (Texture == null || Texture.IsDisposed)
            {
                if (IsStretched)
                    Texture = TexmapsLoader.Instance.GetTexture(TileData.TexID);
                else
                {
                    Texture = ArtLoader.Instance.GetLandTexture(Graphic);
                    Bounds.Width = 44;
                    Bounds.Height = 44;
                }
            }


            if (ProfileManager.Current.HighlightGameObjects && SelectedObject.LastObject == this)
            {
                HueVector.X = 0x0023;
                HueVector.Y = 1;
            }
            else if (ProfileManager.Current.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
            {
                HueVector.X = Constants.OUT_RANGE_COLOR;
                HueVector.Y = 1;
            }
            else if (World.Player.IsDead && ProfileManager.Current.EnableBlackWhiteEffect)
            {
                HueVector.X = Constants.DEAD_RANGE_COLOR;
                HueVector.Y = 1;
            }
            else
            {
                HueVector.X = Hue;

                if (Hue != 0)
                    HueVector.Y = IsStretched ? ShaderHuesTraslator.SHADER_LAND_HUED : ShaderHuesTraslator.SHADER_HUED;
                else
                    HueVector.Y = IsStretched ? ShaderHuesTraslator.SHADER_LAND : ShaderHuesTraslator.SHADER_NONE;
            }


            return IsStretched ? Draw3DStretched(batcher, posX, posY) : base.Draw(batcher, posX, posY);
        }


        private bool Draw3DStretched(UltimaBatcher2D batcher, int posX, int posY)
        {
            Texture.Ticks = Time.Ticks;
            batcher.DrawSpriteLand(Texture, posX, posY + (Z << 2), ref Rectangle, ref Normal0, ref Normal1, ref Normal2, ref Normal3, ref HueVector);
            Select(posX, posY);

            return true;
        }

        public override void Select(int x, int y)
        {
            if (SelectedObject.Object == this)
                return;

            if (IsStretched)
            {
                if (SelectedObject.IsPointInStretchedLand(ref Rectangle, x, y + (Z << 2)))
                    SelectedObject.Object = this;
            }
            else
            {
                if (SelectedObject.IsPointInLand(Texture, x - Bounds.X, y - Bounds.Y))
                    SelectedObject.Object = this;
            }
        }
    }
}