// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Configuration;
using ClassicUO.IO.Audio;
using Microsoft.Xna.Framework.Audio;
using ClassicUO.Sdk;
using ClassicUO.Game.Services;

namespace ClassicUO.Game.Managers
{
    internal sealed class AudioManager
    {
        const float SOUND_DELTA = 250;

        private bool _canReproduceAudio = true;
        private readonly LinkedList<UOSound> _currentSounds = new LinkedList<UOSound>();
        private readonly UOMusic?[] _currentMusic = { null, null };
        private readonly int[] _currentMusicIndices = { 0, 0 };
        public int LoginMusicIndex { get; private set; }
        public int DeathMusicIndex { get; } = 42;

        public void Initialize()
        {
            try
            {
                new DynamicSoundEffectInstance(0, AudioChannels.Stereo).Dispose();
            }
            catch (NoAudioHardwareException ex)
            {
                Log.Warn(ex.ToString());
                _canReproduceAudio = false;
            }

            LoginMusicIndex = ServiceProvider.Get<UOService>().Version switch
            {
                >= ClientVersion.CV_7000 => 78, // LoginLoop
                > ClientVersion.CV_308Z => 0,
                _ => 8 // stones2
            };

            ServiceProvider.Get<UOService>().Activated += OnWindowActivated;
            ServiceProvider.Get<UOService>().Deactivated += OnWindowDeactivated;
        }

        private void OnWindowDeactivated(object? sender, EventArgs e)
        {
            if (!_canReproduceAudio || ProfileManager.CurrentProfile == null || ProfileManager.CurrentProfile.ReproduceSoundsInBackground)
            {
                return;
            }

            SoundEffect.MasterVolume = 0;
        }

        private void OnWindowActivated(object? sender, EventArgs e)
        {
            if (!_canReproduceAudio || ProfileManager.CurrentProfile == null || ProfileManager.CurrentProfile.ReproduceSoundsInBackground)
            {
                return;
            }

            SoundEffect.MasterVolume = 1;
        }

        public void PlaySound(int index)
        {
            Profile currentProfile = ProfileManager.CurrentProfile;

            if (!_canReproduceAudio || currentProfile == null)
            {
                return;
            }

            float volume = currentProfile.SoundVolume / SOUND_DELTA;

            if (ServiceProvider.Get<UOService>().IsActive)
            {
                if (!currentProfile.ReproduceSoundsInBackground)
                {
                    volume = currentProfile.SoundVolume / SOUND_DELTA;
                }
            }
            else if (!currentProfile.ReproduceSoundsInBackground)
            {
                volume = 0;
            }

            if (volume < -1 || volume > 1f)
            {
                return;
            }

            if (!currentProfile.EnableSound || !ServiceProvider.Get<UOService>().IsActive && !currentProfile.ReproduceSoundsInBackground)
            {
                volume = 0;
            }

            var sound = (UOSound?) ServiceProvider.Get<UOService>().Sounds.GetSound(index);

            if (sound != null && sound.Play(Time.Ticks, volume))
            {
                sound.X = -1;
                sound.Y = -1;
                sound.CalculateByDistance = false;

                _currentSounds.AddLast(sound);
            }
        }

        public void PlaySoundWithDistance(World world, int index, int x, int y)
        {
            if (!_canReproduceAudio || !world.InGame)
            {
                return;
            }

            int distX = Math.Abs(x - world.Player.X);
            int distY = Math.Abs(y - world.Player.Y);
            int distance = Math.Max(distX, distY);

            Profile currentProfile = ProfileManager.CurrentProfile;
            float volume = currentProfile.SoundVolume / SOUND_DELTA;
            float distanceFactor = 0.0f;

            if (distance >= 1)
            {
                float volumeByDist = volume / (world.ClientViewRange + 1);
                distanceFactor = volumeByDist * distance;
            }

            if (distance > world.ClientViewRange)
            {
                volume = 0;
            }

            if (volume < -1 || volume > 1f)
            {
                return;
            }

            if (currentProfile == null || !currentProfile.EnableSound || !ServiceProvider.Get<UOService>().IsActive && !currentProfile.ReproduceSoundsInBackground)
            {
                volume = 0;
            }

            var sound = (UOSound?)ServiceProvider.Get<UOService>().Sounds.GetSound(index);

            if (sound != null && sound.Play(Time.Ticks, volume, distanceFactor))
            {
                sound.X = x;
                sound.Y = y;
                sound.CalculateByDistance = true;

                _currentSounds.AddLast(sound);
            }
        }

        public void PlayMusic(int music, bool iswarmode = false, bool is_login = false)
        {
            if (!_canReproduceAudio)
            {
                return;
            }

            if (music >= Constants.MAX_MUSIC_DATA_INDEX_COUNT)
            {
                return;
            }

            float volume;

            if (is_login)
            {
                volume = Settings.GlobalSettings.LoginMusic ? Settings.GlobalSettings.LoginMusicVolume / SOUND_DELTA : 0;
            }
            else
            {
                Profile currentProfile = ProfileManager.CurrentProfile;

                if (currentProfile == null || !currentProfile.EnableMusic)
                {
                    volume = 0;
                }
                else
                {
                    volume = currentProfile.MusicVolume / SOUND_DELTA;
                }

                if (currentProfile != null && !currentProfile.EnableCombatMusic && iswarmode)
                {
                    return;
                }
            }


            if (volume < -1 || volume > 1f)
            {
                return;
            }

            var m = ServiceProvider.Get<UOService>().Sounds.GetMusic(music);

            if (m == null && _currentMusic[0] != null)
            {
                StopMusic();
            }
            else if (m != null && (m != _currentMusic[0] || iswarmode))
            {
                StopMusic();

                int idx = iswarmode ? 1 : 0;
                _currentMusicIndices[idx] = music;
                _currentMusic[idx] = (UOMusic) m;
                m.Play(Time.Ticks, volume);
            }
        }

        public void UpdateCurrentMusicVolume(bool isLogin = false)
        {
            if (!_canReproduceAudio)
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
                        volume = Settings.GlobalSettings.LoginMusic ? Settings.GlobalSettings.LoginMusicVolume / SOUND_DELTA : 0;
                    }
                    else
                    {
                        Profile currentProfile = ProfileManager.CurrentProfile;

                        volume = currentProfile == null || !currentProfile.EnableMusic ? 0 : currentProfile.MusicVolume / SOUND_DELTA;
                    }


                    if (volume < -1 || volume > 1f)
                    {
                        return;
                    }

                    var m = _currentMusic[i];
                    if (m == null)
                        return;

                    m.Volume = i == 0 && _currentMusic[1] != null ? 0 : volume;
                }
            }
        }

        public void UpdateCurrentSoundsVolume()
        {
            if (!_canReproduceAudio)
            {
                return;
            }

            Profile currentProfile = ProfileManager.CurrentProfile;

            float volume = currentProfile == null || !currentProfile.EnableSound ? 0 : currentProfile.SoundVolume / SOUND_DELTA;

            if (volume < -1 || volume > 1f)
            {
                return;
            }

            for (var soundNode = _currentSounds.First; soundNode != null; soundNode = soundNode.Next)
            {
                soundNode.Value.Volume = volume;
            }
        }

        public void StopMusic()
        {
            for (int i = 0; i < _currentMusic.Length; i++)
            {
                var m = _currentMusic[i];
                if (m != null)
                {
                    m.Stop();
                    m.Dispose();
                    _currentMusic[i] = null;
                }
            }
        }

        public void StopWarMusic()
        {
            PlayMusic(_currentMusicIndices[0]);
        }

        public void StopSounds()
        {
            var first = _currentSounds.First;

            while (first != null)
            {
                var next = first.Next;

                first.Value.Stop();

                _currentSounds.Remove(first);

                first = next;
            }
        }

        public void Update()
        {
            if (!_canReproduceAudio)
            {
                return;
            }

            bool runninWarMusic = _currentMusic[1] != null;
            var currentProfile = ProfileManager.CurrentProfile;

            var i = 0;
            foreach (var m in _currentMusic)
            {
                if (m != null && currentProfile != null)
                {
                    if (ServiceProvider.Get<UOService>().IsActive)
                    {
                        if (!currentProfile.ReproduceSoundsInBackground)
                        {
                            m.Volume = i == 0 && runninWarMusic || !currentProfile.EnableMusic ? 0 : currentProfile.MusicVolume / SOUND_DELTA;
                        }
                    }
                    else if (!currentProfile.ReproduceSoundsInBackground && m.Volume != 0.0f)
                    {
                        m.Volume = 0;
                    }
                }

                m?.Update();
                i++;
            }


            var first = _currentSounds.First;

            while (first != null)
            {
                var next = first.Next;

                if (!first.Value.IsPlaying(Time.Ticks))
                {
                    first.Value.Stop();
                    _currentSounds.Remove(first);
                }

                first = next;
            }
        }

        public UOMusic? GetCurrentMusic()
        {
            foreach (var m in _currentMusic)
            {
                if (m != null && m.IsPlaying(Time.Ticks))
                {
                    return m;
                }
            }
            return null;
        }
    }
}