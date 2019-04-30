﻿using System;

namespace ClassicUO.IO.Audio
{
    internal class UOSound : Sound
    {
        private readonly byte[] m_WaveBuffer;

        public UOSound(string name, int index, byte[] buffer)
            : base(name, index)
        {
            m_WaveBuffer = buffer;
        }

        protected override void OnBufferNeeded(object sender, EventArgs e)
        {
            // not needed.
        }

        protected override byte[] GetBuffer()
        {
            return m_WaveBuffer;
        }
    }
}