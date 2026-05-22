// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;

namespace ClassicUO.Game.Audio
{
    /// <summary>
    /// Owns the LRU of currently-playing sound effects, drives their
    /// per-frame Stop() when they finish, and listens for
    /// <see cref="EventSink.SoundPlay"/>.
    /// </summary>
    internal interface ISoundPlayer : IEventListener
    {
        /// <summary>The world used to compute listener-relative distance for
        /// <see cref="EventSink.SoundPlay"/>. Null disables distance gating.</summary>
        void SetWorld(World world);

        /// <summary>Fire-and-forget effect; no distance attenuation.</summary>
        void Play(int index);

        /// <summary>Effect with listener-distance attenuation.</summary>
        void PlayWithDistance(World world, int index, int x, int y);

        /// <summary>Per-frame update: removes finished effects from the LRU.</summary>
        void Update();

        /// <summary>Stop every active effect immediately.</summary>
        void StopAll();

        /// <summary>Recalculate volume on every currently-playing effect.</summary>
        void UpdateCurrentSoundsVolume();
    }
}
