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

using System;

using ClassicUO.Configuration;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Game.GameObjects
{
    internal sealed partial class LightningEffect
    {
        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY)
        {
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
            else
            {
                hue = 1150;
            }

            ShaderHuesTraslator.GetHueVector(ref HueVector, hue, false, 0);
            HueVector.Y = ShaderHuesTraslator.SHADER_LIGHTS;

            //Engine.DebugInfo.EffectsRendered++;

            ref var index = ref GumpsLoader.Instance.GetValidRefEntry(AnimationGraphic);

            posX -= (index.Width >> 1);
            posY -= index.Height;

            batcher.SetBlendState(BlendState.Additive);
            DrawGump(batcher, AnimationGraphic, posX, posY, ref HueVector);
            batcher.SetBlendState(null);

            return true;
        }
    }
}