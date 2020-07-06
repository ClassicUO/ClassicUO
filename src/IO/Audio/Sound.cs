#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;

using ClassicUO.Utility;

using Microsoft.Xna.Framework.Audio;

using static System.String;

namespace ClassicUO.IO.Audio
{
    internal abstract class Sound : IComparable<Sound>, IDisposable
    {
        private static readonly List<Tuple<DynamicSoundEffectInstance, double>> m_EffectInstances;
        private static readonly List<Tuple<DynamicSoundEffectInstance, double>> m_MusicInstances;
        protected AudioChannels Channels = AudioChannels.Mono;

        protected int Frequency = 22050;
        private string m_Name;
        private float m_volume = 1.0f;
        private float m_volumeFactor = 0.0f;
        protected DynamicSoundEffectInstance _sound_instance;
        private uint _lastPlayedTime;
        protected uint Delay = 250;

        static Sound()
        {
            m_EffectInstances = new List<Tuple<DynamicSoundEffectInstance, double>>();
            m_MusicInstances = new List<Tuple<DynamicSoundEffectInstance, double>>();
        }

        protected Sound(string name, int index)
        {
            Name = name;
            Index = index;
        }

        public string Name
        {
            get => m_Name;
            private set
            {
                if (!IsNullOrEmpty(value))
                    m_Name = value.Replace(".mp3", "");
                else
                    m_Name = Empty;
            }
        }

        public int Index { get; }

        public float Volume
        {
            get => m_volume;
            set
            {
                if (value < 0.0f)
                    value = 0f;
                else if (value > 1f)
                    value = 1f;

                m_volume = value;

                float instanceVolume = Math.Max(value - VolumeFactor, 0.0f);

                if (_sound_instance != null && !_sound_instance.IsDisposed)
                    _sound_instance.Volume = instanceVolume;
            }
        }

        public float VolumeFactor
        {
            get => m_volumeFactor;
            set
            {
                m_volumeFactor = value;
                Volume = m_volume;
            }
        }

        public bool IsPlaying => _sound_instance != null && (_sound_instance.State == SoundState.Playing || _sound_instance.PendingBufferCount > 0);

        public int CompareTo(Sound other)
        {
            return other == null ? -1 : Index.CompareTo(other.Index);
        }

        public void Dispose()
        {
            if (_sound_instance != null)
            {
                _sound_instance.BufferNeeded -= OnBufferNeeded;

                if (!_sound_instance.IsDisposed)
                {
                    _sound_instance.Stop();
                    _sound_instance.Dispose();
                }

                _sound_instance = null;
            }
        }

        public void Mute()
        {
            if (_sound_instance != null)
            {
                _sound_instance.Volume = 0.0f;
            }
        }

        public void Unmute()
        {
            Volume = m_volume;
        }

        protected abstract byte[] GetBuffer();
        protected abstract void OnBufferNeeded(object sender, EventArgs e);

        protected virtual void AfterStop()
        {
        }

        protected virtual void BeforePlay()
        {
        }

        /// <summary>
        ///     Plays the effect.
        /// </summary>
        /// <param name="asEffect">Set to false for music, true for sound effects.</param>
        public bool Play(bool asEffect, AudioEffects effect = AudioEffects.None, float volume = 1.0f, float volumeFactor = 0.0f, bool spamCheck = false)
        {
            uint now = Time.Ticks;
            CullExpiredEffects(now);

            if (_lastPlayedTime > now)
                return false;

            BeforePlay();

            _sound_instance = GetNewInstance(asEffect);

            if (_sound_instance == null)
            {
                Dispose();

                return false;
            }

            switch (effect)
            {
                case AudioEffects.PitchVariation:
                    float pitch = RandomHelper.GetValue(-5, 5) * .025f;
                    _sound_instance.Pitch = pitch;

                    break;
            }

            _lastPlayedTime = now + Delay;

            byte[] buffer = GetBuffer();

            if (buffer != null && buffer.Length > 0)
            {
                _sound_instance.BufferNeeded += OnBufferNeeded;
                _sound_instance.SubmitBuffer(buffer);
                VolumeFactor = volumeFactor;
                Volume = volume;
                _sound_instance.Play();
                List<Tuple<DynamicSoundEffectInstance, double>> list = asEffect ? m_EffectInstances : m_MusicInstances;
                double ms = _sound_instance.GetSampleDuration(buffer.Length).TotalMilliseconds;
                list.Add(new Tuple<DynamicSoundEffectInstance, double>(_sound_instance, now + ms));
            }

            return true;
        }

        public void Stop()
        {
            //m_ThisInstance?.Stop();
            //m_ThisInstance?.Dispose();

            CullExpiredEffects(Time.Ticks);

            _sound_instance?.Stop();
            _sound_instance?.Dispose();


            //foreach (Tuple<DynamicSoundEffectInstance, double> sound in m_EffectInstances)
            //{
            //    sound.Item1.Stop();
            //    sound.Item1.Dispose();
            //}

            //foreach (Tuple<DynamicSoundEffectInstance, double> music in m_MusicInstances)
            //{
            //    music.Item1.Stop();
            //    music.Item1.Dispose();
            //}

            AfterStop();
        }

        private void CullExpiredEffects(double now)
        {
            // Check to see if any existing instances have stopped playing. If they have, remove the
            // reference to them so the garbage collector can collect them.
            for (int i = 0; i < m_EffectInstances.Count; i++)
            {
                if (m_EffectInstances[i].Item1.IsDisposed || m_EffectInstances[i].Item1.State == SoundState.Stopped || m_EffectInstances[i].Item2 <= now)
                {
                    m_EffectInstances[i].Item1.Dispose();
                    m_EffectInstances.RemoveAt(i);
                    i--;
                }
            }

            for (int i = 0; i < m_MusicInstances.Count; i++)
            {
                if (m_MusicInstances[i].Item1.IsDisposed || m_MusicInstances[i].Item1.State == SoundState.Stopped)
                {
                    m_MusicInstances[i].Item1.Dispose();
                    m_MusicInstances.RemoveAt(i);
                    i--;
                }
            }
        }

        private DynamicSoundEffectInstance GetNewInstance(bool asEffect)
        {
            List<Tuple<DynamicSoundEffectInstance, double>> list = asEffect ? m_EffectInstances : m_MusicInstances;
            int maxInstances = asEffect ? 32 : 2;

            return list.Count >= maxInstances ? null : new DynamicSoundEffectInstance(Frequency, Channels);
        }
    }
}