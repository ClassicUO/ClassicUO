using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassicUO.Game.Data;
using ClassicUO.Interfaces;

namespace ClassicUO.Game.GameObjects
{
    class EffectManager : IUpdateable
    {
        private readonly List<GameEffect> _effects = new List<GameEffect>();

        public void Add(GraphicEffectType type, Serial source, Serial target, Graphic graphic, Position srcPos,
            Position targPos, byte speed, byte duration, bool fixedDir, bool doesExplode)
        {

        }

        public void Add(GameEffect effect)
            => _effects.Add(effect);

        public void Update(double totalMS, double frameMS)
        {
            for (int i = 0; i < _effects.Count; i++)
            {
                GameEffect effect = _effects[i];

                effect.Update(totalMS, frameMS);

                if (effect.IsDisposed)
                {
                    _effects.RemoveAt(i--);

                    if (effect.Children.Count > 0)
                    {
                        for (int j = 0; j < effect.Children.Count; j++)
                            _effects.Add(effect.Children[j]);
                    }
                }
            }
        }
    }
}
