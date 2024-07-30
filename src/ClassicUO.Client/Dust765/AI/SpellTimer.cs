using ClassicUO.Dust765.Managers;
using System;

namespace ClassicUO.Game.Cheats.AIBot
{
    internal class SpellTimer
    {
        public static TimeSpan CursorDuration { get; } = TimeSpan.FromSeconds( 30 );

        private SpellAction _spellID = SpellAction.Unknown;
        private DateTime _lastCreation = DateTime.Now;

        public DateTime Created => _lastCreation;
        public TimeSpan Elapsed => DateTime.Now - _lastCreation;

        public SpellAction Action
        {
            get { return _spellID; }
            set
            {
                _spellID = value;
                _lastCreation = DateTime.Now;

                if ( OnSpellChange != null )
                    OnSpellChange();
            }
        }

        public event Action OnSpellChange;
        public bool IsCasting => Action != SpellAction.Unknown && Elapsed < SpellCaster.GetCursorTime( this.Action );
        public bool IsActive => Action != SpellAction.Unknown && !IsCasting && Elapsed < SpellTimer.CursorDuration;
    }
}