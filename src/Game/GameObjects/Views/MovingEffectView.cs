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

using ClassicUO.Configuration;
using ClassicUO.Game.Scenes;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class MovingEffect
    {
        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY)
        {
            if (IsDestroyed || !AllowedToDraw)
                return false;

            ResetHueVector();

            ushort hue = Hue;

            if (ProfileManager.Current.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
            {
                hue = Constants.OUT_RANGE_COLOR;
            }
            else if (World.Player.IsDead && ProfileManager.Current.EnableBlackWhiteEffect)
            {
                hue = Constants.DEAD_RANGE_COLOR;
            }

            ShaderHuesTraslator.GetHueVector(ref HueVector, hue);

            //Engine.DebugInfo.EffectsRendered++;

            if (FixedDir)
            {
                DrawStatic(batcher, AnimationGraphic, posX, posY, ref HueVector);
            }
            else
            {
                posX += (int) Offset.X;
                posY += (int) (Offset.Y + Offset.Z);

                DrawStaticRotated(batcher, AnimationGraphic, posX, posY, 0, 0, AngleToTarget, ref HueVector);
            }


            ref StaticTiles data = ref TileDataLoader.Instance.StaticData[AnimationGraphic];

            if (data.IsLight && Source != null)
            {
                Client.Game.GetScene<GameScene>()
                      .AddLight(Source, Source, posX + 22, posY + 22);
            }

            return true;
        }
    }
}