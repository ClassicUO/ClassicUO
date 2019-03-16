using System;
using System.Collections.Generic;

using ClassicUO.Utility;

using Microsoft.Xna.Framework.Audio;

namespace ClassicUO.IO.Audio
{
    abstract class Sound : IDisposable
    {
        string m_Name;
        public string Name
        {
            get { return m_Name; }
            private set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    m_Name = value.Replace(".mp3", "");
                }
                else
                {
                    m_Name = string.Empty;
                }
            }
        }
        public DateTime LastPlayed = DateTime.MinValue;
        public static TimeSpan MinimumDelay = TimeSpan.FromSeconds(1d);

        abstract protected byte[] GetBuffer();
        abstract protected void OnBufferNeeded(object sender, EventArgs e);
        virtual protected void AfterStop() { }
        virtual protected void BeforePlay() { }

        static readonly List<Tuple<DynamicSoundEffectInstance, double>> m_EffectInstances;
        static readonly List<Tuple<DynamicSoundEffectInstance, double>> m_MusicInstances;
        protected DynamicSoundEffectInstance m_ThisInstance;

        protected int Frequency = 22050;
        protected AudioChannels Channels = AudioChannels.Mono;

        static Sound()
        {
            m_EffectInstances = new List<Tuple<DynamicSoundEffectInstance, double>>();
            m_MusicInstances = new List<Tuple<DynamicSoundEffectInstance, double>>();
        }

        public Sound(string name)
        {
            Name = name;         
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

        /// <summary>
        /// Plays the effect.
        /// </summary>
        /// <param name="asEffect">Set to false for music, true for sound effects.</param>
        public void Play(bool asEffect, AudioEffects effect = AudioEffects.None, float volume = 1.0f, bool spamCheck = false)
        {
            double now = Engine.Ticks;
            CullExpiredEffects(now);

            if (spamCheck && (LastPlayed + MinimumDelay > DateTime.Now))
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
            
            LastPlayed = DateTime.Now;

            byte[] buffer = GetBuffer();
            if (buffer != null && buffer.Length > 0)
            {
                m_ThisInstance.BufferNeeded += OnBufferNeeded;
                m_ThisInstance.SubmitBuffer(buffer);
                m_ThisInstance.Volume = volume;
                m_ThisInstance.Play();
                List<Tuple<DynamicSoundEffectInstance, double>> list = (asEffect) ? m_EffectInstances : m_MusicInstances;
                double ms = m_ThisInstance.GetSampleDuration(buffer.Length).TotalMilliseconds;
                list.Add(new Tuple<DynamicSoundEffectInstance, double>(m_ThisInstance, now + ms));
            }
        }

        public void Stop()
        {
            AfterStop();
        }

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
            List<Tuple<DynamicSoundEffectInstance, double>> list = (asEffect) ? m_EffectInstances : m_MusicInstances;
            int maxInstances = (asEffect) ? 32 : 2;
            if (list.Count >= maxInstances)
                return null;
            else
                return new DynamicSoundEffectInstance(Frequency, Channels); // shouldn't we be recycling these?
        }
    }
}
