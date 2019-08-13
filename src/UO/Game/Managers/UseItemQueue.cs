using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Interfaces;
using ClassicUO.Utility;

namespace ClassicUO.Game.Managers
{
    class UseItemQueue : IUpdateable
    {
        private readonly Deque<Serial> _actions = new Deque<Serial>();
        private long _timer;

        public void Add(Serial action)
        {
            _actions.AddToBack(action);
        }

        public void Clear() => _actions.Clear();

        public void Update(double totalMS, double frameMS)
        {
            if (_timer <= totalMS)
            {
                _timer = (long) (totalMS + 1000);

                if (_actions.Count <= 0)
                    return;

                Serial serial = _actions.RemoveFromFront();

                if (World.Get(serial) != null)
                {
                    if (serial.IsMobile)
                        GameActions.OpenPaperdoll(serial);
                    else 
                        GameActions.DoubleClick(serial);
                }
            }
        }
    }
}
