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

using System.Collections.Generic;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Interfaces;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    internal class EffectManager : IUpdateable
    {
        private readonly List<GameEffect> _effects = new List<GameEffect>();

        public void Update(double totalMS, double frameMS)
        {
            for (int i = 0; i < _effects.Count; i++)
            {
                GameEffect effect = _effects[i];
                effect.Update(totalMS, frameMS);

                if (effect.IsDestroyed)
                {
                    _effects.RemoveAt(i--);

                    if (effect.Children.Count > 0)
                    {
                        foreach (GameEffect t in effect.Children)
                            _effects.Add(t);
                    }
                }
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
                _effects.Add(effect);
        }

        public void Clear()
        {
            _effects.ForEach(s => s.Destroy());
            _effects.Clear();
        }
    }
}