#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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

using System;
using System.Collections.Generic;

using ClassicUO.Configuration;
using ClassicUO.Game.Scenes;
using ClassicUO.IO.Audio;
using ClassicUO.IO.Resources;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework.Audio;

namespace ClassicUO.Game.Managers
{
    internal class AudioManager
    {
        private UOMusic[] _currentMusic = { null, null };
        private LinkedList<UOSound> _current_sounds = new LinkedList<UOSound>();
        private bool _canReproduceAudio = true;
        private int[] _currentMusicIndices = { 0, 0 };


        public void Initialize()
        {
            try
            {
                new DynamicSoundEffectInstance(0, AudioChannels.Stereo).Dispose();
            }
            catch (NoAudioHardwareException ex)
            {
                Log.Warn( ex.ToString());
                _canReproduceAudio = false;
            }

            Client.Game.Activated += OnWindowActivated;
            Client.Game.Deactivated += OnWindowDeactivated;
        }

        private void OnWindowDeactivated(object sender, System.EventArgs e)
        {
            if (ProfileManager.Current == null || ProfileManager.Current.ReproduceSoundsInBackground) return;

            SoundEffect.MasterVolume = 0;
        }

        private void OnWindowActivated(object sender, System.EventArgs e)
        {
            if (ProfileManager.Current == null || ProfileManager.Current.ReproduceSoundsInBackground) return;

            SoundEffect.MasterVolume = 1;
        }

        public void PlaySound(int index)
        {
            if (!_canReproduceAudio || ProfileManager.Current == null)
                return;

            float volume = ProfileManager.Current.SoundVolume / Constants.SOUND_DELTA;

            if (Client.Game.IsActive)
            {
                if (!ProfileManager.Current.ReproduceSoundsInBackground) 
                    volume = ProfileManager.Current.SoundVolume / Constants.SOUND_DELTA;
            }
            else if (!ProfileManager.Current.ReproduceSoundsInBackground)
                volume = 0;

            if (volume < -1 || volume > 1f)
                return;

            if (!ProfileManager.Current.EnableSound || !Client.Game.IsActive && !ProfileManager.Current.ReproduceSoundsInBackground)
                volume = 0;

            UOSound sound = (UOSound)SoundsLoader.Instance.GetSound(index);

            if (sound != null && sound.Play(volume))
            {
                sound.X = -1;
                sound.Y = -1;
                sound.CalculateByDistance = false;

                _current_sounds.AddLast(sound);
            }
        }

        public void PlaySoundWithDistance(int index, int x, int y)
        {
            if (!_canReproduceAudio || !World.InGame)
                return;

            int distX = Math.Abs(x - World.Player.X);
            int distY = Math.Abs(y - World.Player.Y);
            int distance = Math.Max(distX, distY);

            float volume = ProfileManager.Current.SoundVolume / Constants.SOUND_DELTA;
            float distanceFactor = 0.0f;

            if (distance >= 1)
            {
                float volumeByDist = volume / (World.ClientViewRange + 1);
                distanceFactor = volumeByDist * distance;
            }

            if (distance > World.ClientViewRange)
            {
                volume = 0;
            }

            if (volume < -1 || volume > 1f)
                return;

            if (ProfileManager.Current == null || !ProfileManager.Current.EnableSound || !Client.Game.IsActive && !ProfileManager.Current.ReproduceSoundsInBackground)
                volume = 0;

            UOSound sound = (UOSound) SoundsLoader.Instance.GetSound(index);

            if (sound != null && sound.Play(volume, distanceFactor))
            {
                sound.X = x;
                sound.Y = y;
                sound.CalculateByDistance = true;

                _current_sounds.AddLast(sound);
            }
        }

        public void PlayMusic(int music, bool iswarmode = false, bool is_login = false)
        {
            if (!_canReproduceAudio)
                return;

            if (music >= Constants.MAX_MUSIC_DATA_INDEX_COUNT)
                return;

            float volume;

            if (is_login)
            {
                volume = Settings.GlobalSettings.LoginMusic ? Settings.GlobalSettings.LoginMusicVolume / Constants.SOUND_DELTA : 0;
            }
            else
            {
                if (ProfileManager.Current == null || !ProfileManager.Current.EnableMusic)
                {
                    volume = 0;
                }
                else
                {
                    volume = ProfileManager.Current.MusicVolume / Constants.SOUND_DELTA;
                }

                if (ProfileManager.Current != null && !ProfileManager.Current.EnableCombatMusic && iswarmode)
                {
                    return;
                }
            }


            if (volume < -1 || volume > 1f)
                return;

            Sound m = SoundsLoader.Instance.GetMusic(music);

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
                _currentMusic[idx].Play(volume);
            }
        }

        public void UpdateCurrentMusicVolume(bool is_login = false)
        {
            if (!_canReproduceAudio)
                return;

            for (int i = 0; i < 2; i++)
            {
                if (_currentMusic[i] != null)
                {
                    float volume;

                    if (is_login)
                    {
                        volume = Settings.GlobalSettings.LoginMusic ? Settings.GlobalSettings.LoginMusicVolume / Constants.SOUND_DELTA : 0;
                    }
                    else
                    {
                        volume = ProfileManager.Current == null || !ProfileManager.Current.EnableMusic ?
                                     0 :
                                     ProfileManager.Current.MusicVolume / Constants.SOUND_DELTA;
                    }
                    

                    if (volume < -1 || volume > 1f)
                        return;

                    _currentMusic[i].Volume = i == 0 && _currentMusic[1] != null ? 0 : volume;
                }
            }      
        }

        public void UpdateCurrentSoundsVolume()
        {
            if (!_canReproduceAudio)
                return;

            float volume = ProfileManager.Current == null || !ProfileManager.Current.EnableSound ? 0 : ProfileManager.Current.SoundVolume / Constants.SOUND_DELTA;

            if (volume < -1 || volume > 1f)
                return;

            for (var s = _current_sounds.First; s != null; s = s.Next)
            {
                s.Value.Volume = volume;
            }
        }

        public void StopMusic()
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

        public void StopWarMusic()
        {
            PlayMusic(_currentMusicIndices[0]);
        }

        public void StopSounds()
        {
            var first = _current_sounds.First;

            while (first != null)
            {
                var next = first.Next;

                first.Value.Stop();

                _current_sounds.Remove(first);

                first = next;
            }
        }

        public void Update()
        {
            if (!_canReproduceAudio)
                return;

            bool runninWarMusic = _currentMusic[1] != null;

            for (int i = 0; i < 2; i++)
            {
                if (_currentMusic[i] != null && ProfileManager.Current != null)
                {
                    if (Client.Game.IsActive)
                    {
                        if (!ProfileManager.Current.ReproduceSoundsInBackground)
                            _currentMusic[i].Volume = (i == 0 && runninWarMusic) || !ProfileManager.Current.EnableMusic ? 0 : ProfileManager.Current.MusicVolume / Constants.SOUND_DELTA;
                    }
                    else if (!ProfileManager.Current.ReproduceSoundsInBackground && _currentMusic[i].Volume != 0.0f)
                        _currentMusic[i].Volume = 0;
                }

                _currentMusic[i]?.Update();
            }

            
            var first = _current_sounds.First;

            while (first != null)
            {
                var next = first.Next;

                if (!first.Value.IsPlaying)
                {
                    first.Value.Stop();
                    _current_sounds.Remove(first);
                }

                first = next;
            }
        }
    }
}