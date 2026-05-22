// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using ClassicUO.Game.Events;
using ClassicUO.IO.Audio;
using ClassicUO.Utility;

namespace ClassicUO.Game.Audio
{
    /// <summary>
    /// Linked-list of in-flight <see cref="UOSound"/> instances. Receives
    /// <see cref="EventSink.SoundPlay"/> and routes to
    /// <see cref="PlayWithDistance"/> against the bound world; finished
    /// effects are reaped in <see cref="Update"/>.
    /// </summary>
    internal sealed class SoundPlayer : ISoundPlayer
    {
        private readonly LinkedList<UOSound> _currentSounds = new();
        private readonly IAudioHardware _hardware;
        private readonly IAudioPreferences _prefs;
        private World _world;

        public SoundPlayer(IAudioHardware hardware, IAudioPreferences prefs)
        {
            _hardware = hardware;
            _prefs = prefs;
        }

        public void SetWorld(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.SoundPlay += OnSoundPlay;
        }

        public void Unsubscribe()
        {
            EventSink.SoundPlay -= OnSoundPlay;
        }

        private void OnSoundPlay(SoundPlayArgs e)
        {
            if (_world != null)
            {
                PlayWithDistance(_world, e.Index, e.X, e.Y);
            }
        }

        public void Play(int index)
        {
            if (!_hardware.IsAvailable || !_prefs.HasProfile)
            {
                return;
            }

            float volume = _prefs.SoundVolume;

            if (Client.Game.IsActive)
            {
                if (!_prefs.BackgroundPlayback)
                {
                    volume = _prefs.SoundVolume;
                }
            }
            else if (!_prefs.BackgroundPlayback)
            {
                volume = 0;
            }

            if (volume < -1 || volume > 1f)
            {
                return;
            }

            if (!_prefs.SoundEnabled || (!Client.Game.IsActive && !_prefs.BackgroundPlayback))
            {
                volume = 0;
            }

            UOSound sound = (UOSound)Client.Game.UO.Sounds.GetSound(index);

            if (sound != null && sound.Play(Time.Ticks, volume))
            {
                sound.X = -1;
                sound.Y = -1;
                sound.CalculateByDistance = false;

                _currentSounds.AddLast(sound);
            }
        }

        public void PlayWithDistance(World world, int index, int x, int y)
        {
            if (!_hardware.IsAvailable || !world.InGame)
            {
                return;
            }

            int distX = Math.Abs(x - world.Player.X);
            int distY = Math.Abs(y - world.Player.Y);
            int distance = Math.Max(distX, distY);

            float volume = _prefs.SoundVolume;
            float distanceFactor = 0.0f;

            if (distance >= 1)
            {
                float volumeByDist = volume / (world.ClientViewRange + 1);
                distanceFactor = volumeByDist * distance;
            }

            if (distance > world.ClientViewRange)
            {
                volume = 0;
            }

            if (volume < -1 || volume > 1f)
            {
                return;
            }

            if (!_prefs.HasProfile || !_prefs.SoundEnabled || (!Client.Game.IsActive && !_prefs.BackgroundPlayback))
            {
                volume = 0;
            }

            UOSound sound = (UOSound)Client.Game.UO.Sounds.GetSound(index);

            if (sound != null && sound.Play(Time.Ticks, volume, distanceFactor))
            {
                sound.X = x;
                sound.Y = y;
                sound.CalculateByDistance = true;

                _currentSounds.AddLast(sound);
            }
        }

        public void Update()
        {
            if (!_hardware.IsAvailable)
            {
                return;
            }

            LinkedListNode<UOSound> first = _currentSounds.First;

            while (first != null)
            {
                LinkedListNode<UOSound> next = first.Next;

                if (!first.Value.IsPlaying(Time.Ticks))
                {
                    first.Value.Stop();
                    _currentSounds.Remove(first);
                }

                first = next;
            }
        }

        public void StopAll()
        {
            LinkedListNode<UOSound> first = _currentSounds.First;

            while (first != null)
            {
                LinkedListNode<UOSound> next = first.Next;

                first.Value.Stop();

                _currentSounds.Remove(first);

                first = next;
            }
        }

        public void UpdateCurrentSoundsVolume()
        {
            if (!_hardware.IsAvailable)
            {
                return;
            }

            float volume = !_prefs.SoundEnabled ? 0f : _prefs.SoundVolume;

            if (volume < -1 || volume > 1f)
            {
                return;
            }

            for (LinkedListNode<UOSound> soundNode = _currentSounds.First; soundNode != null; soundNode = soundNode.Next)
            {
                soundNode.Value.Volume = volume;
            }
        }
    }
}
