using ClassicUO.Game.GameObjects;
using ClassicUO.Interfaces;
using ClassicUO.Utility.Collections;

namespace ClassicUO.Game.Managers
{
    internal class UseItemQueue : IUpdateable
    {
        private readonly Deque<uint> _actions = new Deque<uint>();
        private long _timer;


        public UseItemQueue()
        {
            _timer = Time.Ticks + 1000;
        }

        public void Update(double totalTime, double frameTime)
        {
            if (_timer < Time.Ticks)
            {
                _timer = Time.Ticks + 1000;

                if (_actions.Count == 0)
                {
                    return;
                }

                uint serial = _actions.RemoveFromFront();

                if (World.Get(serial) != null)
                {
                    if (SerialHelper.IsMobile(serial))
                    {
                        GameActions.OpenPaperdoll(serial);
                    }
                    else
                    {
                        GameActions.DoubleClick(serial);
                    }
                }
            }
        }

        public void Add(uint serial)
        {
            foreach (uint s in _actions)
            {
                if (serial == s)
                {
                    return;
                }
            }

            _actions.AddToBack(serial);
        }

        public void Clear()
        {
            _actions.Clear();
        }

        public void ClearCorpses()
        {
            for (int i = 0; i < _actions.Count; i++)
            {
                Entity entity = World.Get(_actions[i]);

                if (entity == null)
                {
                    continue;
                }

                if (entity is Item it && it.IsCorpse)
                {
                    _actions.RemoveAt(i--);
                }
            }
        }
    }
}