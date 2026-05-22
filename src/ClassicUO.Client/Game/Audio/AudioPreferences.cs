// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;

namespace ClassicUO.Game.Audio
{
    /// <summary>
    /// Read facade over <see cref="ProfileManager.CurrentProfile"/> and
    /// <see cref="Settings.GlobalSettings"/>. Volume getters already
    /// normalise by <c>SOUND_DELTA</c> so callers receive a [0..1] float
    /// ready to assign to a MonoGame sound instance.
    /// </summary>
    internal sealed class AudioPreferences : IAudioPreferences
    {
        private const float SOUND_DELTA = 250f;

        public bool HasProfile => ProfileManager.CurrentProfile != null;

        public bool SoundEnabled => ProfileManager.CurrentProfile?.EnableSound == true;

        public bool MusicEnabled => ProfileManager.CurrentProfile?.EnableMusic == true;

        public bool CombatMusicEnabled => ProfileManager.CurrentProfile?.EnableCombatMusic == true;

        public bool BackgroundPlayback => ProfileManager.CurrentProfile?.ReproduceSoundsInBackground == true;

        public float SoundVolume
        {
            get
            {
                Profile profile = ProfileManager.CurrentProfile;
                return profile == null ? 0f : profile.SoundVolume / SOUND_DELTA;
            }
        }

        public float MusicVolume
        {
            get
            {
                Profile profile = ProfileManager.CurrentProfile;
                return profile == null ? 0f : profile.MusicVolume / SOUND_DELTA;
            }
        }

        public float LoginVolume =>
            Settings.GlobalSettings.LoginMusic ? Settings.GlobalSettings.LoginMusicVolume / SOUND_DELTA : 0f;
    }
}
