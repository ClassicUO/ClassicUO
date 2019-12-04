#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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

using System.Collections.Generic;

using ClassicUO.Configuration;
using ClassicUO.Game.Scenes;
using ClassicUO.IO;
using ClassicUO.IO.Audio;
using ClassicUO.Utility.Logging;
using Microsoft.Xna.Framework.Audio;

namespace ClassicUO.Game.Managers
{
    internal class AudioManager
    {
        private UOMusic _currentMusic;
        private List<UOSound> _currentSounds;

        private bool _canReproduceAudio = true;

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

            _currentSounds = new List<UOSound>();

            CUOEnviroment.Client.Activated += OnWindowActivated;
            CUOEnviroment.Client.Deactivated += OnWindowDeactivated;
        }

        private void OnWindowDeactivated(object sender, System.EventArgs e)
        {
            if (ProfileManager.Current == null || ProfileManager.Current.ReproduceSoundsInBackground) return;

            for (int i = 0; i < _currentSounds.Count; i++)
            {
                _currentSounds[i].Mute();
            }
        }

        private void OnWindowActivated(object sender, System.EventArgs e)
        {
            if (ProfileManager.Current == null || ProfileManager.Current.ReproduceSoundsInBackground) return;

            for (int i = 0; i < _currentSounds.Count; i++)
            {
                _currentSounds[i].Unmute();
            }
        }

        public void PlaySound(int index, AudioEffects effect = AudioEffects.None)
        {
            if (!_canReproduceAudio)
                return;

            if (ProfileManager.Current == null || !ProfileManager.Current.EnableSound)
                return;

            float volume = ProfileManager.Current.SoundVolume / Constants.SOUND_DELTA;

            if (CUOEnviroment.Client.IsActive)
            {
                if (!ProfileManager.Current.ReproduceSoundsInBackground) volume = ProfileManager.Current.SoundVolume / Constants.SOUND_DELTA;
            }
            else if (!ProfileManager.Current.ReproduceSoundsInBackground) volume = 0;


            if (volume < -1 || volume > 1f)
                return;

            UOSound sound = (UOSound)UOFileManager.Sounds.GetSound(index);

            if (sound != null)
            {
                sound.Play(true, effect, volume, 0.0f);
                _currentSounds.Add(sound);
            }
        }

        public void PlaySoundWithDistance(int index, float volume, float distanceFactor = 0.0f)
        {
            if (!_canReproduceAudio)
                return;

            if (ProfileManager.Current == null || !ProfileManager.Current.EnableSound || !CUOEnviroment.Client.IsActive && !ProfileManager.Current.ReproduceSoundsInBackground)
                return;

            if (!CUOEnviroment.Client.IsActive && !ProfileManager.Current.ReproduceSoundsInBackground) volume = 0;

            if (volume < -1 || volume > 1f)
                return;

            UOSound sound = (UOSound)UOFileManager.Sounds.GetSound(index);

            if (sound != null)
            {
                sound.Play(true, AudioEffects.None, volume, distanceFactor);
                _currentSounds.Add(sound);
            }
        }

        public void PlayMusic(int music)
        {
            if (!_canReproduceAudio)
                return;

            if (music >= Constants.MAX_MUSIC_DATA_INDEX_COUNT)
                return;

            float volume;

            if (CUOEnviroment.Client.Scene is LoginScene)
            {
                if (!Settings.GlobalSettings.LoginMusic)
                    return;

                volume = Settings.GlobalSettings.LoginMusicVolume / Constants.SOUND_DELTA;
            }
            else
            {
                if (ProfileManager.Current == null || !ProfileManager.Current.EnableMusic)
                    return;

                volume = ProfileManager.Current.MusicVolume / Constants.SOUND_DELTA;
            }


            if (volume < -1 || volume > 1f)
                return;

            Sound m = UOFileManager.Sounds.GetMusic(music);

            if (m == null && _currentMusic != null)
                StopMusic();
            else if (m != null && m != _currentMusic)
            {
                StopMusic();
                _currentMusic = (UOMusic) m;
                _currentMusic.Play(false, volume: volume);
            }
        }

        public void UpdateCurrentMusicVolume()
        {
            if (!_canReproduceAudio)
                return;

            if (_currentMusic != null)
            {
                if (ProfileManager.Current == null || !ProfileManager.Current.EnableMusic)
                    return;

                float volume = ProfileManager.Current.MusicVolume / Constants.SOUND_DELTA;

                if (volume < -1 || volume > 1f)
                    return;

                _currentMusic.Volume = volume;
            }
        }

        public void UpdateCurrentSoundsVolume()
        {
            if (!_canReproduceAudio)
                return;

            if (ProfileManager.Current == null || !ProfileManager.Current.EnableSound)
                return;

            float volume = ProfileManager.Current.SoundVolume / Constants.SOUND_DELTA;

            if (volume < -1 || volume > 1f)
                return;

            for (int i = 0; i < _currentSounds.Count; i++)
            {
                _currentSounds[i].Volume = volume;
            }
        }

        public void StopMusic()
        {
            if (_currentMusic != null)
            {
                _currentMusic.Stop();
                _currentMusic.Dispose();
                _currentMusic = null;
            }
        }

        public void StopSounds()
        {
            for (int i = 0; i < _currentSounds.Count; i++)
            {
                var sound = _currentSounds[i];
                sound.Stop();
                sound.Dispose();
                _currentSounds.RemoveAt(i--);
            }
        }

        public void Update()
        {
            if (!_canReproduceAudio)
                return;

            if (_currentMusic != null && ProfileManager.Current != null)
            {
                if (CUOEnviroment.Client.IsActive)
                {
                    if (!ProfileManager.Current.ReproduceSoundsInBackground) _currentMusic.Volume = ProfileManager.Current.MusicVolume / Constants.SOUND_DELTA;
                }
                else if (!ProfileManager.Current.ReproduceSoundsInBackground && _currentMusic.Volume != 0.0f) _currentMusic.Volume = 0;
            }

            _currentMusic?.Update();

            for (int i = 0; i < _currentSounds.Count; i++)
            {
                var sound = _currentSounds[i];

                if (!sound.IsPlaying())
                {
                    _currentSounds.RemoveAt(i--);
                }
            }
        }
    }
}