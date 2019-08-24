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

        public void Add(GraphicEffectType type, Serial source, Serial target, Graphic graphic, Hue hue, Position srcPos, Position targPos, byte speed, int duration, bool fixedDir, bool doesExplode, bool hasparticles, GraphicEffectBlendMode blendmode)
        {
            if (hasparticles) Log.Message(LogTypes.Warning, "Unhandled particles in an effects packet.");
            GameEffect effect = null;

            switch (type)
            {
                case GraphicEffectType.Moving:

                    if (graphic <= 0)
                        return;

                    if (speed == 0)
                        speed++;

                    effect = new MovingEffect(source, target, srcPos.X, srcPos.Y, srcPos.Z, targPos.X, targPos.Y, targPos.Z, graphic, hue, fixedDir)
                    {
                        Blend = blendmode,
                        MovingDelay = (byte) (speed)
                    };

                    if (doesExplode)
                        effect.AddChildEffect(new AnimatedItemEffect(target, targPos.X, targPos.Y, targPos.Z, 0x36Cb, hue, 9));

                    effect.Update(Engine.Ticks, 0);
                    break;

                case GraphicEffectType.Lightning:
                    effect = new LightningEffect(source, srcPos.X, srcPos.Y, srcPos.Z, hue);

                    break;

                case GraphicEffectType.FixedXYZ:

                    if (graphic <= 0)
                        return;

                    effect = new AnimatedItemEffect(srcPos.X, srcPos.Y, srcPos.Z, graphic, hue, duration)
                    {
                        Blend = blendmode
                    };

                    break;

                case GraphicEffectType.FixedFrom:

                    if (graphic <= 0)
                        return;

                    effect = new AnimatedItemEffect(source, srcPos.X, srcPos.Y, srcPos.Z, graphic, hue, duration)
                    {
                        Blend = blendmode
                    };

                    break;

                case GraphicEffectType.ScreenFade:
                    Log.Message(LogTypes.Warning, "Unhandled 'Screen Fade' effect.");

                    break;

                default:
                    Log.Message(LogTypes.Warning, "Unhandled effect.");

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