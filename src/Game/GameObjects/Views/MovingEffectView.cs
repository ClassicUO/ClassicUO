#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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

using System;

using ClassicUO.Configuration;
using ClassicUO.Game.Scenes;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class MovingEffect
    {
        private Graphic _displayedGraphic = Graphic.INVALID;


        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY)
        {
            if (IsDestroyed)
                return false;

            ResetHueVector();

            if (AnimationGraphic != _displayedGraphic || Texture == null || Texture.IsDisposed)
            {
                _displayedGraphic = AnimationGraphic;
                Texture = FileManager.Art.GetTexture(AnimationGraphic);
                Bounds.X = -((Texture.Width >> 1) - 22);
                Bounds.Y = -(Texture.Height - 44);
                Bounds.Width = Texture.Width;
                Bounds.Height = Texture.Height;
            }


            posX += (int) Offset.X;
            posY += (int) (Offset.Y + Offset.Z);

            //posX += 22;
            //posY += 22;




            double normalized = Normalized();
            if (normalized >= 1.0)
                return false;

            int playerX = World.Player.X;
            int playerY = World.Player.Y;
            int playerZ = World.Player.Z;

            (int x1, int y1, int z1) = GetSource();

            int offsetX = 0, offsetY, offsetZ = 0;

            int x2 = x1 - playerX;
            int y2 = y1 - playerY;
            int z2 = z1 - playerZ;

            int sourceX = (ProfileManager.Current.GameWindowSize.X >> 1) + (x2 - y2) * 22;
            int sourceY = (ProfileManager.Current.GameWindowSize.Y >> 1) + (x2 + y2) * 22 - z2 * 4;
            sourceX += ProfileManager.Current.GameWindowPosition.X;
            sourceY += ProfileManager.Current.GameWindowPosition.Y;
            sourceX += (int) Offset.X;
            sourceY += (int) Offset.Y;


            (x2, y2, z2) = GetTarget();
            x2 -= playerX;
            y2 -= playerY;
            z2 -= playerZ;

            int targetX = (ProfileManager.Current.GameWindowSize.X >> 1) + (x2 - y2) * 22;
            int targetY = (ProfileManager.Current.GameWindowSize.Y >> 1) + (x2 + y2) * 22 - z2 * 4;
            targetX += ProfileManager.Current.GameWindowPosition.X;
            targetY += ProfileManager.Current.GameWindowPosition.Y;
            targetX += (int) Offset.X;
            targetY += (int) Offset.Y;

            posX = sourceX + (int) ((targetX - sourceX) * normalized);
            posY = sourceY + (int) ((targetY - sourceY) * normalized);

            AngleToTarget = (float) -(Math.Atan2(sourceY - targetY, sourceX - targetX) + Math.PI);


            if (ProfileManager.Current.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
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
                ShaderHuesTraslator.GetHueVector(ref HueVector, Hue);

            //Engine.DebugInfo.EffectsRendered++;

            if (FixedDir)
                batcher.DrawSprite(Texture, posX, posY, false, ref HueVector);
            else
                batcher.DrawSpriteRotated(Texture, posX, posY, Bounds.X, Bounds.Y, ref HueVector, AngleToTarget);

            //Select(posX, posY);
            Texture.Ticks = Time.Ticks;

            ref readonly StaticTiles data = ref FileManager.TileData.StaticData[_displayedGraphic];

            if (data.IsLight && Source != null)
            {
                CUOEnviroment.Client.GetScene<GameScene>()
                      .AddLight(Source, Source, posX + 22, posY + 22);
            }

            return true;
        }
    }
}