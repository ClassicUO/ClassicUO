using ClassicUO.Game.Scenes;
using ClassicUO.IO;
using ClassicUO.IO.Audio;

namespace ClassicUO.Game.Managers
{
    internal class AudioManager
    {
        private UOMusic _currentMusic;
        private int _lastMusicVolume;

        public void PlaySound(int index, AudioEffects effect = AudioEffects.None, bool spamCheck = false)
        {
            if (Engine.Profile == null || Engine.Profile.Current == null || !Engine.Profile.Current.EnableSound)
                return;

            float volume = (Engine.Profile.Current.SoundVolume / Constants.SOUND_DELTA);
            
            if (Engine.Instance.IsActive)
            {
                if (!Engine.Profile.Current.ReproduceSoundsInBackground)
                {
                    volume = Engine.Profile.Current.SoundVolume / Constants.SOUND_DELTA;
                }
            }
            else if (!Engine.Profile.Current.ReproduceSoundsInBackground)
            {
                volume = 0;
            }
            

            if (volume < -1 || volume > 1f)
                return;

            FileManager.Sounds.GetSound(index)?.Play(true, effect, volume, spamCheck);
        }

        public void PlaySoundWithDistance(int index, float volume, bool spamCheck = false)
        {
            if (Engine.Profile == null || Engine.Profile.Current == null || !Engine.Profile.Current.EnableSound || (!Engine.Instance.IsActive && !Engine.Profile.Current.ReproduceSoundsInBackground))
                return;

            if (Engine.Instance.IsActive)
            {
                if (!Engine.Profile.Current.ReproduceSoundsInBackground)
                {
                    volume = Engine.Profile.Current.SoundVolume / Constants.SOUND_DELTA;
                }
            }
            else if (!Engine.Profile.Current.ReproduceSoundsInBackground)
            {
                volume = 0;
            }

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

                volume = Engine.GlobalSettings.LoginMusicVolume / Constants.SOUND_DELTA;
            }
            else
            {
                if (Engine.Profile == null || Engine.Profile.Current == null || !Engine.Profile.Current.EnableMusic)
                    return;

                volume = Engine.Profile.Current.MusicVolume / Constants.SOUND_DELTA;
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

                float volume = Engine.Profile.Current.MusicVolume / Constants.SOUND_DELTA;
                if (volume < -1 || volume > 1f)
                    return;

                _currentMusic.Volume = volume;
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
            if (_currentMusic != null && Engine.Profile.Current != null)
            {
                if (Engine.Instance.IsActive)
                {
                    if (!Engine.Profile.Current.ReproduceSoundsInBackground)
                    {
                        _currentMusic.Volume = Engine.Profile.Current.MusicVolume / Constants.SOUND_DELTA;
                    }
                }
                else if (!Engine.Profile.Current.ReproduceSoundsInBackground && _currentMusic.Volume != 0)
                {
                    _currentMusic.Volume = 0;
                }
            }

            _currentMusic?.Update();
        }
    }
}
