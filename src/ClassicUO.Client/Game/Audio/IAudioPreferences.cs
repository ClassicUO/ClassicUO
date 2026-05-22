// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Audio
{
    /// <summary>
    /// Thin read-only view over the audio knobs in
    /// <c>ProfileManager.CurrentProfile</c> and <c>Settings.GlobalSettings</c>.
    /// Centralises volume normalisation against <c>SOUND_DELTA = 250f</c>
    /// so the players don't repeat that math.
    /// </summary>
    internal interface IAudioPreferences
    {
        /// <summary>True when a <see cref="Configuration.ProfileManager.CurrentProfile"/>
        /// is loaded. Some legacy gates intentionally do nothing when no profile is set
        /// rather than treat null as "all-defaults".</summary>
        bool HasProfile { get; }

        /// <summary>True when a profile exists and the user enabled sounds.</summary>
        bool SoundEnabled { get; }

        /// <summary>True when a profile exists and the user enabled music.</summary>
        bool MusicEnabled { get; }

        /// <summary>True when a profile exists and the user enabled war-mode music.</summary>
        bool CombatMusicEnabled { get; }

        /// <summary>True when audio should keep playing while the window is unfocused.</summary>
        bool BackgroundPlayback { get; }

        /// <summary>Effects volume in [0..1].</summary>
        float SoundVolume { get; }

        /// <summary>Music volume in [0..1].</summary>
        float MusicVolume { get; }

        /// <summary>Login-screen music volume in [0..1].</summary>
        float LoginVolume { get; }
    }
}
