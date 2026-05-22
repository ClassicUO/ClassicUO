// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;
using ClassicUO.IO.Audio;

namespace ClassicUO.Game.Audio
{
    /// <summary>
    /// Owns the active music slots (<c>[0] = peace</c>, <c>[1] = war</c>)
    /// and the last-played-index history for resuming peace music after
    /// a war-music interruption. Subscribes to <see cref="EventSink.MusicPlay"/>
    /// and <see cref="EventSink.MusicStop"/>.
    /// </summary>
    internal interface IMusicPlayer : IEventListener
    {
        /// <summary>Play a music track. <paramref name="isWarMode"/> targets the
        /// war slot; <paramref name="isLogin"/> uses the login-volume knob.</summary>
        void Play(int index, bool isWarMode = false, bool isLogin = false);

        /// <summary>Stop and dispose both slots.</summary>
        void Stop();

        /// <summary>Stop the war slot and resume the last peace track.</summary>
        void StopWarMusic();

        /// <summary>Per-frame update; volume adjustment + asset tick.</summary>
        void Update();

        /// <summary>Recalculate the currently-playing volume from the prefs.</summary>
        void UpdateCurrentMusicVolume(bool isLogin = false);

        /// <summary>The currently-playing music, or null when both slots are idle.</summary>
        UOMusic GetCurrentMusic();
    }
}
