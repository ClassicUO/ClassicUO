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
        protected AudioChannels Channels = AudioChannels.Mono;

        protected int Frequency = 22050;
        private string m_Name;
        private float m_volume = 1.0f;
        private float m_volumeFactor = 0.0f;
        protected DynamicSoundEffectInstance _sound_instance;
        private uint _lastPlayedTime;
        protected uint Delay = 250;



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
        public double DurationTime { get; private set; }

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

        public bool IsPlaying => _sound_instance != null && (_sound_instance.State == SoundState.Playing && DurationTime > Time.Ticks);

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
        public bool Play(float volume = 1.0f, float volumeFactor = 0.0f, bool spamCheck = false)
        {
            if (_lastPlayedTime > Time.Ticks)
                return false;

            BeforePlay();

            if (_sound_instance != null && !_sound_instance.IsDisposed)
            {
                _sound_instance.Stop();
            }    
            else
            {
                _sound_instance = new DynamicSoundEffectInstance(Frequency, Channels);
            }
     

            byte[] buffer = GetBuffer();

            if (buffer != null && buffer.Length > 0)
            {
                _lastPlayedTime = Time.Ticks + Delay;

                _sound_instance.BufferNeeded += OnBufferNeeded;
                _sound_instance.SubmitBuffer(buffer, 0, buffer.Length);
                VolumeFactor = volumeFactor;
                Volume = volume;
                DurationTime = Time.Ticks + _sound_instance.GetSampleDuration(buffer.Length).TotalMilliseconds;
                _sound_instance.Play();

                return true;
            }

            return false;
        }

        public void Stop()
        {            
            if (_sound_instance != null)
            {
                _sound_instance.BufferNeeded -= OnBufferNeeded;
                _sound_instance.Stop();
            }

            AfterStop();
        }
    }
}