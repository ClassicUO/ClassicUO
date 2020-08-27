﻿#region license
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

using System.Collections.Generic;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Interfaces;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    internal class EffectManager : IUpdateable
    {
        private GameEffect _root;

        public void Update(double totalMS, double frameMS)
        {
            GameEffect f = _root;

            while (f != null)
            {
                LinkedObject n = f.Next;

                f.Update(totalMS, frameMS);

                if (!f.IsDestroyed && f.Distance > World.ClientViewRange)
                    RemoveEffect(f);

                if (f.IsDestroyed)
                {
                    if (f.Children.Count != 0)
                    {
                        foreach (GameEffect child in f.Children)
                        {
                            if (!child.IsDestroyed)
                                Add(child);
                        }

                        f.Children.Clear();
                    }
                }

                f = (GameEffect) n;
            }
        }

        public void Add(GraphicEffectType type, 
                        uint source, uint target, 
                        ushort graphic, 
                        ushort hue, 
                        ushort srcX, ushort srcY, sbyte srcZ,
                        ushort targetX, ushort targetY, sbyte targetZ,
                        byte speed, int duration, bool fixedDir, bool doesExplode, bool hasparticles, GraphicEffectBlendMode blendmode)
        {
            if (hasparticles) Log.Warn( "Unhandled particles in an effects packet.");
            GameEffect effect = null;

            if (hue != 0)
                hue++;

            duration *= Constants.ITEM_EFFECT_ANIMATION_DELAY;

            switch (type)
            {
                case GraphicEffectType.Moving:
                    if (graphic <= 0)
                        return;

                    if (speed == 0)
                        speed++;
                    
                    effect = new MovingEffect(source, target, srcX, srcY, srcZ, targetX, targetY, targetZ, graphic, hue, fixedDir, speed)
                    {
                        Blend = blendmode,
                    };

                    if (doesExplode)
                        effect.AddChildEffect(new AnimatedItemEffect(target, targetX, targetY, targetZ, 0x36Cb, hue, 9, speed));

                    break;

                case GraphicEffectType.Lightning:
                    effect = new LightningEffect(source, srcX, srcY, srcZ, hue);

                    break;

                case GraphicEffectType.FixedXYZ:

                    if (graphic <= 0)
                        return;

                    effect = new AnimatedItemEffect(srcX, srcY, srcZ, graphic, hue, duration, speed)
                    {
                        Blend = blendmode
                    };

                    break;

                case GraphicEffectType.FixedFrom:

                    if (graphic <= 0)
                        return;

                    effect = new AnimatedItemEffect(source, srcX, srcY, srcZ, graphic, hue, duration, speed)
                    {
                        Blend = blendmode,
                    };

                    break;

                case GraphicEffectType.ScreenFade:
                    Log.Warn( "Unhandled 'Screen Fade' effect.");

                    break;

                default:
                    Log.Warn( "Unhandled effect.");

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
                return;

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