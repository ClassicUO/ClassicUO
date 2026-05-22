// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Audio
{
    /// <summary>
    /// MonoGame audio bootstrap + window-focus master volume.
    /// One responsibility: probe the underlying audio hardware once,
    /// remember whether playback is possible, and mute/unmute the
    /// engine master volume when the window loses/regains focus.
    /// </summary>
    internal interface IAudioHardware
    {
        /// <summary>True once <see cref="Initialize"/> has run and the
        /// audio device probe succeeded.</summary>
        bool IsAvailable { get; }

        /// <summary>Probe the audio device, wire window focus events,
        /// and compute the current client's login-music index.</summary>
        void Initialize();

        /// <summary>Login music index resolved from the client version.</summary>
        int LoginMusicIndex { get; }
    }
}
