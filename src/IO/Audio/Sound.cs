#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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
        public static TimeSpan MinimumDelay = TimeSpan.FromMilliseconds(250d);

        private static readonly List<Tuple<DynamicSoundEffectInstance, double>> m_EffectInstances;
        private static readonly List<Tuple<DynamicSoundEffectInstance, double>> m_MusicInstances;
        protected AudioChannels Channels = AudioChannels.Mono;

        protected int Frequency = 22050;
        public DateTime LastPlayed = DateTime.MinValue;
        private string m_Name;
        protected DynamicSoundEffectInstance m_ThisInstance;

        static Sound()
        {
            m_EffectInstances = new List<Tuple<DynamicSoundEffectInstance, double>>();
            m_MusicInstances = new List<Tuple<DynamicSoundEffectInstance, double>>();
        }

        public Sound(string name, int index)
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
            get => m_ThisInstance.Volume;
            set
            {
                if (value < 0.0f)
                    value = 0f;
                else if (value > 1f)
                    value = 1f;

                m_ThisInstance.Volume = value;
            }
        }

        public int CompareTo(Sound other)
        {
            return other == null ? -1 : Index.CompareTo(other.Index);
        }

        public void Dispose()
        {
            if (m_ThisInstance != null)
            {
                m_ThisInstance.BufferNeeded -= OnBufferNeeded;

                if (!m_ThisInstance.IsDisposed)
                {
                    m_ThisInstance.Stop();
                    m_ThisInstance.Dispose();
                }

                m_ThisInstance = null;
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
        public void Play(bool asEffect, AudioEffects effect = AudioEffects.None, float volume = 1.0f, bool spamCheck = false)
        {
            double now = Engine.Ticks;
            CullExpiredEffects(now);

            if (spamCheck && LastPlayed + MinimumDelay > Engine.CurrDateTime)
                return;

            BeforePlay();
            m_ThisInstance = GetNewInstance(asEffect);

            if (m_ThisInstance == null)
            {
                Dispose();

                return;
            }

            switch (effect)
            {
                case AudioEffects.PitchVariation:
                    float pitch = RandomHelper.GetValue(-5, 5) * .025f;
                    m_ThisInstance.Pitch = pitch;

                    break;
            }

            LastPlayed = Engine.CurrDateTime;

            byte[] buffer = GetBuffer();

            if (buffer != null && buffer.Length > 0)
            {
                m_ThisInstance.BufferNeeded += OnBufferNeeded;
                m_ThisInstance.SubmitBuffer(buffer);
                m_ThisInstance.Volume = volume;
                m_ThisInstance.Play();
                List<Tuple<DynamicSoundEffectInstance, double>> list = asEffect ? m_EffectInstances : m_MusicInstances;
                double ms = m_ThisInstance.GetSampleDuration(buffer.Length).TotalMilliseconds;
                list.Add(new Tuple<DynamicSoundEffectInstance, double>(m_ThisInstance, now + ms));
            }
        }

        public void Stop()
        {
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