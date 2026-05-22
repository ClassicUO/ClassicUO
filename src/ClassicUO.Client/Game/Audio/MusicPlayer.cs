// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;
using ClassicUO.IO.Audio;
using ClassicUO.Utility;

namespace ClassicUO.Game.Audio
{
    /// <summary>
    /// Two-slot music player: index 0 hosts the ambient/peace track,
    /// index 1 hosts the optional war-mode track that temporarily
    /// preempts slot 0. Listens on <see cref="EventSink.MusicPlay"/>
    /// and <see cref="EventSink.MusicStop"/>.
    /// </summary>
    internal sealed class MusicPlayer : IMusicPlayer
    {
        private readonly UOMusic[] _currentMusic = { null, null };
        private readonly int[] _currentMusicIndices = { 0, 0 };
        private readonly IAudioHardware _hardware;
        private readonly IAudioPreferences _prefs;

        public MusicPlayer(IAudioHardware hardware, IAudioPreferences prefs)
        {
            _hardware = hardware;
            _prefs = prefs;
        }

        public void Subscribe()
        {
            EventSink.MusicPlay += OnMusicPlay;
            EventSink.MusicStop += OnMusicStop;
        }

        public void Unsubscribe()
        {
            EventSink.MusicPlay -= OnMusicPlay;
            EventSink.MusicStop -= OnMusicStop;
        }

        private void OnMusicPlay(MusicPlayArgs e) => Play(e.Index);

        private void OnMusicStop(MusicStopArgs e) => Stop();

        public void Play(int index, bool isWarMode = false, bool isLogin = false)
        {
            if (!_hardware.IsAvailable)
            {
                return;
            }

            if (index >= Constants.MAX_MUSIC_DATA_INDEX_COUNT)
            {
                return;
            }

            float volume;

            if (isLogin)
            {
                volume = _prefs.LoginVolume;
            }
            else
            {
                if (!_prefs.MusicEnabled)
                {
                    volume = 0;
                }
                else
                {
                    volume = _prefs.MusicVolume;
                }

                // PreCombatMusicCheck: a profile with combat music disabled drops
                // the war-mode preempt entirely.
                if (!_prefs.CombatMusicEnabled && isWarMode)
                {
                    return;
                }
            }

            if (volume < -1 || volume > 1f)
            {
                return;
            }

            Sound m = Client.Game.UO.Sounds.GetMusic(index);

            if (m == null && _currentMusic[0] != null)
            {
                Stop();
            }
            else if (m != null && (m != _currentMusic[0] || isWarMode))
            {
                Stop();

                int idx = isWarMode ? 1 : 0;
                _currentMusicIndices[idx] = index;
                _currentMusic[idx] = (UOMusic)m;

                _currentMusic[idx].Play(Time.Ticks, volume);
            }
        }

        public void Stop()
        {
            for (int i = 0; i < 2; i++)
            {
                if (_currentMusic[i] != null)
                {
                    _currentMusic[i].Stop();
                    _currentMusic[i].Dispose();
                    _currentMusic[i] = null;
                }
            }
        }

        public void StopWarMusic() => Play(_currentMusicIndices[0]);

        public void UpdateCurrentMusicVolume(bool isLogin = false)
        {
            if (!_hardware.IsAvailable)
            {
                return;
            }

            for (int i = 0; i < 2; i++)
            {
                if (_currentMusic[i] != null)
                {
                    float volume;

                    if (isLogin)
                    {
                        volume = _prefs.LoginVolume;
                    }
                    else
                    {
                        volume = _prefs.MusicEnabled ? _prefs.MusicVolume : 0f;
                    }

                    if (volume < -1 || volume > 1f)
                    {
                        return;
                    }

                    _currentMusic[i].Volume = i == 0 && _currentMusic[1] != null ? 0 : volume;
                }
            }
        }

        public void Update()
        {
            if (!_hardware.IsAvailable)
            {
                return;
            }

            bool runningWarMusic = _currentMusic[1] != null;

            for (int i = 0; i < 2; i++)
            {
                if (_currentMusic[i] != null && _prefs.HasProfile)
                {
                    if (Client.Game.IsActive)
                    {
                        if (!_prefs.BackgroundPlayback)
                        {
                            float musicVolume = !_prefs.MusicEnabled ? 0f : _prefs.MusicVolume;
                            _currentMusic[i].Volume = (i == 0 && runningWarMusic) || !_prefs.MusicEnabled ? 0 : musicVolume;
                        }
                    }
                    else if (!_prefs.BackgroundPlayback && _currentMusic[i].Volume != 0.0f)
                    {
                        _currentMusic[i].Volume = 0;
                    }
                }

                _currentMusic[i]?.Update();
            }
        }

        public UOMusic GetCurrentMusic()
        {
            for (int i = 0; i < 2; i++)
            {
                if (_currentMusic[i] != null && _currentMusic[i].IsPlaying(Time.Ticks))
                {
                    return _currentMusic[i];
                }
            }
            return null;
        }
    }
}
