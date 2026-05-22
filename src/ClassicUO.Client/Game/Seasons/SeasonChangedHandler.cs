// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;

namespace ClassicUO.Game.Seasons
{
    internal sealed class SeasonChangedHandler : IEventListener
    {
        private readonly World _world;

        public SeasonChangedHandler(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.SeasonChanged += OnSeasonChanged;
        }

        public void Unsubscribe()
        {
            EventSink.SeasonChanged -= OnSeasonChanged;
        }

        private void OnSeasonChanged(SeasonChangedArgs e)
        {
            if (_world.Player == null) return;

            byte season = e.Season;
            if (season > 4) season = 0;

            if (_world.Player.IsDead && season == 4) return;

            _world.OldSeason = (Season)season;
            _world.OldMusicIndex = e.MusicCue;

            if (_world.Season == Season.Desolation)
            {
                _world.OldMusicIndex = 42;
            }

            _world.ChangeSeason((Season)season, e.MusicCue);
        }
    }
}
