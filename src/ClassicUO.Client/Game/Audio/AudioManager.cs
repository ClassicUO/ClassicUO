// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Audio;
using ClassicUO.IO.Audio;

namespace ClassicUO.Game.Managers
{
    /// <summary>
    /// Facade over the four <see cref="Audio"/> collaborators: keeps the
    /// existing public surface (<c>Client.Game.Audio.X</c>) so call sites
    /// do not change. Owns the lifetime of <see cref="IAudioHardware"/>,
    /// <see cref="IAudioPreferences"/>, <see cref="IMusicPlayer"/> and
    /// <see cref="ISoundPlayer"/>, and propagates Subscribe/Unsubscribe to
    /// the children (which are themselves <c>IEventListener</c>s).
    /// </summary>
    internal sealed class AudioManager
    {
        private readonly IAudioHardware _hardware;
        private readonly IAudioPreferences _prefs;
        private readonly IMusicPlayer _music;
        private readonly ISoundPlayer _sound;

        /// <summary>Production composition root.</summary>
        public AudioManager()
            : this(new AudioHardware(), new AudioPreferences())
        {
        }

        private AudioManager(IAudioHardware hardware, IAudioPreferences prefs)
            : this(hardware, prefs, new MusicPlayer(hardware, prefs), new SoundPlayer(hardware, prefs))
        {
        }

        /// <summary>Full DI seam — inject all collaborators.</summary>
        internal AudioManager(IAudioHardware hardware, IAudioPreferences prefs, IMusicPlayer music, ISoundPlayer sound)
        {
            _hardware = hardware;
            _prefs = prefs;
            _music = music;
            _sound = sound;
        }

        // ---- Lifetime ----

        public void Initialize() => _hardware.Initialize();

        public void Subscribe(World world)
        {
            _sound.SetWorld(world);
            _music.Subscribe();
            _sound.Subscribe();
        }

        public void Unsubscribe()
        {
            _music.Unsubscribe();
            _sound.Unsubscribe();
            _sound.SetWorld(null);
        }

        // ---- Hardware facade ----

        public int LoginMusicIndex => _hardware.LoginMusicIndex;

        public int DeathMusicIndex { get; } = 42;

        // ---- Music facade ----

        public void PlayMusic(int music, bool iswarmode = false, bool is_login = false)
            => _music.Play(music, iswarmode, is_login);

        public void StopMusic() => _music.Stop();

        public void StopWarMusic() => _music.StopWarMusic();

        public void UpdateCurrentMusicVolume(bool isLogin = false)
            => _music.UpdateCurrentMusicVolume(isLogin);

        public UOMusic GetCurrentMusic() => _music.GetCurrentMusic();

        // ---- Sound facade ----

        public void PlaySound(int index) => _sound.Play(index);

        public void PlaySoundWithDistance(World world, int index, int x, int y)
            => _sound.PlayWithDistance(world, index, x, y);

        public void StopSounds() => _sound.StopAll();

        public void UpdateCurrentSoundsVolume() => _sound.UpdateCurrentSoundsVolume();

        // ---- Per-frame ----

        public void Update()
        {
            _music.Update();
            _sound.Update();
        }
    }
}
