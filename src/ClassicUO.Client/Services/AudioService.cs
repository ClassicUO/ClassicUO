using ClassicUO.Game;
using ClassicUO.IO.Audio;

namespace ClassicUO.Services
{
    internal class AudioService : IService
    {
        private readonly ClassicUO.Game.Managers.AudioManager _audio;

        public AudioService(ClassicUO.Game.Managers.AudioManager audio)
        {
            _audio = audio;
        }

        public int LoginMusicIndex => _audio.LoginMusicIndex;
        public ushort DeathMusicIndex { get; } = 0x4D2;


        public void PlayMusic(int index, bool isWarmode = false, bool isLogin = false)
        {
            _audio.PlayMusic(index, isWarmode, isLogin);
        }

        public void StopMusic()
        {
            _audio.StopMusic();
        }

        public void StopSounds()
        {
            _audio.StopSounds();
        }

        public void PlaySound(int sound)
        {
            _audio.PlaySound(sound);
        }

        public void PlaySoundWithDistance(World world, int sound, int x, int y)
        {
            _audio.PlaySoundWithDistance(world, sound, x, y);
        }

        public void UpdateCurrentMusicVolume(bool isLogin = false)
        {
            _audio.UpdateCurrentMusicVolume(isLogin);
        }

        public UOMusic? GetCurrentMusic()
        {
            return _audio.GetCurrentMusic();
        }

        public void StopWarMusic()
        {
            _audio.StopWarMusic();
        }
    }
}