#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

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
            {
                return false;
            }

            ResetHueVector();

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
                DrawStatic
                (
                    batcher,
                    AnimationGraphic,
                    posX,
                    posY,
                    ref HueVector
                );
            }
            else
            {
                posX += (int) Offset.X;
                posY += (int) (Offset.Y + Offset.Z);

                DrawStaticRotated
                (
                    batcher,
                    AnimationGraphic,
                    posX,
                    posY,
                    0,
                    0,
                    AngleToTarget,
                    ref HueVector
                );
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