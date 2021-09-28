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

using System;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    internal class EffectManager : LinkedObject
    {
        public void Update(double totalTime, double frameTime)
        {
            for (GameEffect f = (GameEffect) Items; f != null;)
            {
                GameEffect next = (GameEffect) f.Next;

                f.Update(totalTime, frameTime);

                if (!f.IsDestroyed && f.Distance > World.ClientViewRange)
                {
                    f.Destroy();
                }

                f = next;
            }
        }


        public void CreateEffect
        (
            GraphicEffectType type,
            uint source,
            uint target,
            ushort graphic,
            ushort hue,
            ushort srcX,
            ushort srcY,
            sbyte srcZ,
            ushort targetX,
            ushort targetY,
            sbyte targetZ,
            byte speed,
            int duration,
            bool fixedDir,
            bool doesExplode,
            bool hasparticles,
            GraphicEffectBlendMode blendmode
        )
        {
            if (hasparticles)
            {
                Log.Warn("Unhandled particles in an effects packet.");
            }

            GameEffect effect;

            if (hue != 0)
            {
                hue++;
            }

            duration *= Constants.ITEM_EFFECT_ANIMATION_DELAY;

            switch (type)
            {
                case GraphicEffectType.Moving:
                    if (graphic <= 0)
                    {
                        return;
                    }

                    // TODO: speed == 0 means run at standard frameInterval got from anim.mul?
                    if (speed == 0)
                    {
                        speed++;
                    }

                    effect = new MovingEffect
                    (
                        this,
                        source,
                        target,
                        srcX,
                        srcY,
                        srcZ,
                        targetX,
                        targetY,
                        targetZ,
                        graphic,
                        hue,
                        fixedDir,
                        duration,
                        speed
                    )
                    {
                        Blend = blendmode,
                        CanCreateExplosionEffect = doesExplode
                    };

                    break;

                case GraphicEffectType.DragEffect:

                    if (graphic <= 0)
                    {
                        return;
                    }

                    if (speed == 0)
                    {
                        speed++;
                    }

                    effect = new DragEffect
                    (
                        this,
                        source,
                        target,
                        srcX,
                        srcY,
                        srcZ,
                        targetX,
                        targetY,
                        targetZ,
                        graphic,
                        hue,
                        duration,
                        speed
                    )
                    {
                        Blend = blendmode,
                        CanCreateExplosionEffect = doesExplode
                    };

                    break;

                case GraphicEffectType.Lightning:
                    effect = new LightningEffect
                    (
                        this,
                        source,
                        srcX,
                        srcY,
                        srcZ,
                        hue
                    );

                    break;

                case GraphicEffectType.FixedXYZ:

                    if (graphic <= 0)
                    {
                        return;
                    }

                    effect = new FixedEffect
                    (
                        this,
                        srcX,
                        srcY,
                        srcZ,
                        graphic,
                        hue,
                        duration,
                        0 //speed [use 50ms]
                    )
                    {
                        Blend = blendmode
                    };

                    break;

                case GraphicEffectType.FixedFrom:

                    if (graphic <= 0)
                    {
                        return;
                    }

                    effect = new FixedEffect
                    (
                        this,
                        source,
                        srcX,
                        srcY,
                        srcZ,
                        graphic,
                        hue,
                        duration,
                        0 //speed [use 50ms]
                    )
                    {
                        Blend = blendmode
                    };

                    break;

                case GraphicEffectType.ScreenFade:
                    Log.Warn("Unhandled 'Screen Fade' effect.");

                    return;

                default:
                    Log.Warn("Unhandled effect.");

                    return;
            }


            PushToBack(effect);
        }

        public new void Clear()
        {
            GameEffect first = (GameEffect) Items;

            while (first != null)
            {
                LinkedObject n = first.Next;

                first.Destroy();

                first = (GameEffect) n;
            }

            Items = null;
        }
    }
}