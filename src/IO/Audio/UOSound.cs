using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.IO.Audio
{
    class UOSound : ASound
    {
        private readonly byte[] m_WaveBuffer;

        public UOSound(string name, byte[] buffer)
            : base(name)
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
    };
}
