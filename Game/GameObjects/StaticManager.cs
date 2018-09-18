using ClassicUO.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using static ClassicUO.Interfaces.IUpdateable;

namespace ClassicUO.Game.GameObjects
{
    public class StaticManager : IUpdateable
    {
        private readonly List<Static> _activeStatics = new List<Static>();

        public void Add(Static stat)
        {
            if (!stat.IsDisposed && stat.OverHeads.Count > 0)
                _activeStatics.Add(stat);
        }

        public void Update(double totalMS, double frameMS)
        {
            for (int i = 0; i < _activeStatics.Count; i++)
            {
                _activeStatics[i].Update(totalMS, frameMS);
                if (_activeStatics[i].IsDisposed || _activeStatics[i].OverHeads.Count <= 0)
                    _activeStatics.RemoveAt(i);
            }
        }
    }
}
