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
// ## BEGIN - END ## //
using ClassicUO.Game.InteropServices.Runtime.UOClassicCombat;
// ## BEGIN - END ## //
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
            {
                return false;
            }

            ResetHueVector();

            // ## BEGIN - END ## //
            if (Graphic == 0x379F) //energy bolt
            {
                if (ProfileManager.CurrentProfile.EnergyBoltArtType != 0)
                    Graphic = UOClassicCombatCollection.EnergyBoltArt(Graphic);

                if (ProfileManager.CurrentProfile.ColorEnergyBolt || ProfileManager.CurrentProfile.EnergyBoltNeonType != 0)
                    Hue = UOClassicCombatCollection.EnergyBoltHue(Hue);
            }
            // ## BEGIN - END ## //

            ushort hue = Hue;

            if (ProfileManager.CurrentProfile.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
            {
                hue = Constants.OUT_RANGE_COLOR;
            }
            else if (World.Player.IsDead && ProfileManager.CurrentProfile.EnableBlackWhiteEffect)
            {
                hue = Constants.DEAD_RANGE_COLOR;
            }

            ShaderHueTranslator.GetHueVector(ref HueVector, hue);

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
                Client.Game.GetScene<GameScene>().AddLight(Source, Source, posX + 22, posY + 22);
            }

            return true;
        }
    }
}