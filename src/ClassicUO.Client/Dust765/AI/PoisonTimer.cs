using System;

namespace ClassicUO.Game.Cheats.AIBot
{
    internal delegate void PoisonTick();

    internal class PoisonTimer
    {
        public const int MAX_TICK_COUNT = 6;

        private DateTime _lastTick = DateTime.Now;

        private DateTime LastTick => _lastTick;
        private DateTime NextTick => _lastTick + TimeSpan.FromSeconds( 3.0 );
        private TimeSpan Elapsed => NextTick - LastTick;
        public bool HasElapsed => DateTime.Now > NextTick;

        public bool IsActive {  get { return Automation.Bot.Target.IsPoisoned; } }

        public PoisonTick OnTick { get; set; }

        private int _count;
        public int Count
        {
            get { return _count; }
            set
            {
                _count = value;
                 Tick();
            }
        }

        private void Tick()
        {
            _lastTick = DateTime.Now;

            if (_count > 0)
            {
                if (OnTick != null)
                    OnTick();

                if (_count >= PoisonTimer.MAX_TICK_COUNT)
                    _count = 0;
            }
        }
    }
}
