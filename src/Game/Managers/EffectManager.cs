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

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Interfaces;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    internal class EffectManager : IUpdateable
    {
        private GameEffect _root;

        public void Update(double totalTime, double frameTime)
        {
            GameEffect f = _root;

            while (f != null)
            {
                LinkedObject n = f.Next;

                f.Update(totalTime, frameTime);

                if (!f.IsDestroyed && f.Distance > World.ClientViewRange)
                {
                    RemoveEffect(f);
                }

                if (f.IsDestroyed)
                {
                    if (f.Children.Count != 0)
                    {
                        foreach (GameEffect child in f.Children)
                        {
                            if (!child.IsDestroyed)
                            {
                                Add(child);
                            }
                        }

                        f.Children.Clear();
                    }
                }

                f = (GameEffect) n;
            }
        }

        public void Add
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

            GameEffect effect = null;

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

                    if (speed == 0)
                    {
                        speed++;
                    }

                    effect = new MovingEffect
                    (
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
                        speed
                    )
                    {
                        Blend = blendmode
                    };

                    if (doesExplode)
                    {
                        effect.AddChildEffect
                        (
                            new AnimatedItemEffect
                            (
                                target,
                                targetX,
                                targetY,
                                targetZ,
                                0x36Cb,
                                hue,
                                9,
                                speed
                            )
                        );
                    }

                    break;

                case GraphicEffectType.Lightning:
                    effect = new LightningEffect
                    (
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

                    effect = new AnimatedItemEffect
                    (
                        srcX,
                        srcY,
                        srcZ,
                        graphic,
                        hue,
                        duration,
                        speed
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

                    effect = new AnimatedItemEffect
                    (
                        source,
                        srcX,
                        srcY,
                        srcZ,
                        graphic,
                        hue,
                        duration,
                        speed
                    )
                    {
                        Blend = blendmode
                    };

                    break;

                case GraphicEffectType.ScreenFade:
                    Log.Warn("Unhandled 'Screen Fade' effect.");

                    break;

                default:
                    Log.Warn("Unhandled effect.");

                    return;
            }


            Add(effect);
        }

        public void Add(GameEffect effect)
        {
            if (effect != null)
            {
                if (_root == null)
                {
                    _root = effect;
                    effect.Previous = null;
                    effect.Next = null;
                }
                else
                {
                    effect.Next = _root;
                    _root.Previous = effect;
                    effect.Previous = null;
                    _root = effect;
                }
            }
        }

        public void Clear()
        {
            while (_root != null)
            {
                LinkedObject n = _root.Next;

                foreach (GameEffect child in _root.Children)
                {
                    RemoveEffect(child);
                }

                _root.Children.Clear();

                RemoveEffect(_root);

                _root = (GameEffect) n;
            }
        }


        public void RemoveEffect(GameEffect effect)
        {
            if (effect == null || effect.IsDestroyed)
            {
                return;
            }

            if (effect.Previous == null)
            {
                _root = (GameEffect) effect.Next;

                if (_root != null)
                {
                    _root.Previous = null;
                }
            }
            else
            {
                effect.Previous.Next = effect.Next;

                if (effect.Next != null)
                {
                    effect.Next.Previous = effect.Previous;
                }
            }

            effect.Next = null;
            effect.Previous = null;
            effect.Destroy();
        }
    }
}