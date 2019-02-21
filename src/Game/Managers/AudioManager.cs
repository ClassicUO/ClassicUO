using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.Scenes;
using ClassicUO.IO;
using ClassicUO.IO.Audio;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Managers
{
    internal class AudioManager
    {
        private UOMusic _currentMusic;
        private int _lastMusicVolume;

        private const float SOUND_DELTA = 2500f;

        public void PlaySound(int index, AudioEffects effect = AudioEffects.None, bool spamCheck = false)
        {
            if (Engine.Profile == null || Engine.Profile.Current == null || !Engine.Profile.Current.EnableSound)
                return;

            float volume = (Engine.Profile.Current.SoundVolume / SOUND_DELTA);

            if (volume < -1 || volume > 1f)
                return;

            FileManager.Sounds.GetSound(index)?.Play(true, effect, volume, spamCheck);
        }

        public void PlaySoundWithDistance(int index, float volume, bool spamCheck = false)
        {
            if (Engine.Profile == null || Engine.Profile.Current == null || !Engine.Profile.Current.EnableSound)
                return;

            if (volume < -1 || volume > 1f)
                return;

            FileManager.Sounds.GetSound(index)?.Play(true, AudioEffects.PitchVariation, volume, spamCheck);
        }

        public void PlayMusic(int music)
        {
            float volume;

            if (Engine.SceneManager.CurrentScene is LoginScene)
            {
                if (!Engine.GlobalSettings.LoginMusic)
                    return;

                volume = Engine.GlobalSettings.LoginMusicVolume / SOUND_DELTA;
            }
            else
            {
                if (Engine.Profile == null || Engine.Profile.Current == null || !Engine.Profile.Current.EnableMusic)
                    return;

                volume = Engine.Profile.Current.MusicVolume / SOUND_DELTA;
            }


            if (volume < -1 || volume > 1f)
                return;

            Sound m = FileManager.Sounds.GetMusic(music);

            if( m == null && _currentMusic != null )
            {
                StopMusic();
            }
            else if (m != null && m != _currentMusic)
            {
                StopMusic();
                _currentMusic = (UOMusic) m;
                _currentMusic.Play(false, volume: volume);
            }
        }

        public void UpdateCurrentMusicVolume()
        {
            if (_currentMusic != null)
            {
                if (Engine.Profile == null || Engine.Profile.Current == null || !Engine.Profile.Current.EnableMusic)
                    return;

                float volume = Engine.Profile.Current.MusicVolume / SOUND_DELTA;
                if (volume < -1 || volume > 1f)
                    return;

                _currentMusic.SetVolume(volume);
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

        public void Update()
        {
            _currentMusic?.Update();
        }
    }
}
