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
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.GameObjects
{
    internal class DragEffect : GameEffect
    {
        private uint _lastMoveTime;

        public DragEffect
        (
            EffectManager manager,
            uint src,
            uint trg,
            int xSource,
            int ySource,
            int zSource,
            int xTarget,
            int yTarget,
            int zTarget,
            ushort graphic,
            ushort hue,
            int duration,
            byte speed
        ) 
            : base(manager, graphic, hue, duration, speed)
        {
            Entity source = World.Get(src);

            if (SerialHelper.IsValid(src) && source != null)
            {
                SetSource(source);
            }
            else
            {
                SetSource(xSource, ySource, zSource);
            }


            Entity target = World.Get(trg);

            if (SerialHelper.IsValid(trg) && target != null)
            {
                SetTarget(target);
            }
            else
            {
                SetTarget(xTarget, yTarget, zTarget);
            }

            Hue = hue;
            Graphic = graphic;
        }

        public override void Update(double totalTime, double frameTime)
        {
            if (_lastMoveTime > Time.Ticks)
            {
                return;
            }

            Offset.X += 8;
            Offset.Y += 8;

            _lastMoveTime = Time.Ticks + 20;

            base.Update(totalTime, frameTime);
        }

        public override bool Draw(UltimaBatcher2D batcher, int posX, int posY, float depth)
        {
            if (IsDestroyed)
            {
                return false;
            }

            ushort hue;
            if (ProfileManager.CurrentProfile.NoColorObjectsOutOfRange && Distance > World.ClientViewRange)
            {
                hue = Constants.OUT_RANGE_COLOR;
            }
            else if (World.Player.IsDead && ProfileManager.CurrentProfile.EnableBlackWhiteEffect)
            {
                hue = Constants.DEAD_RANGE_COLOR;
            }
            else
            {
                hue = Hue;
            }

            Vector3 hueVec = ShaderHueTranslator.GetHueVector(hue);

            //Engine.DebugInfo.EffectsRendered++;

            DrawStatic
            (
                batcher,
                AnimationGraphic,
                posX - ((int) Offset.X + 22),
                posY - ((int) -Offset.Y + 22),
                hueVec,
                depth
            );

            ref StaticTiles data = ref TileDataLoader.Instance.StaticData[Graphic];

            if (data.IsLight && Source != null)
            {
                Client.Game.GetScene<GameScene>().AddLight(Source, Source, posX + 22, posY + 22);
            }

            return true;
        }
    }
}