using System;
using System.IO;

using ClassicUO.IO.Audio.MP3Sharp.Decoding;

namespace ClassicUO.IO.Audio.MP3Sharp.IO
{
    /// <summary> Implements an Obuffer by writing the data to a file in RIFF WAVE format.</summary>
    internal class WaveFileBuffer : ABuffer
    {
        private readonly short[] m_Buffer;
        private readonly short[] m_Bufferp;
        private readonly int m_Channels;
        private readonly WaveFile m_OutWave;

        public WaveFileBuffer(int numberOfChannels, int freq, string fileName)
        {
            if (fileName == null)
                throw new NullReferenceException("FileName");

            m_Buffer = new short[OBUFFERSIZE];
            m_Bufferp = new short[MAXCHANNELS];
            m_Channels = numberOfChannels;

            for (int i = 0; i < numberOfChannels; ++i)
                m_Bufferp[i] = (short) i;

            m_OutWave = new WaveFile();

            int rc = m_OutWave.OpenForWrite(fileName, null, freq, 16, (short) m_Channels);
        }

        public WaveFileBuffer(int numberOfChannels, int freq, Stream stream)
        {
            m_Buffer = new short[OBUFFERSIZE];
            m_Bufferp = new short[MAXCHANNELS];
            m_Channels = numberOfChannels;

            for (int i = 0; i < numberOfChannels; ++i)
                m_Bufferp[i] = (short) i;

            m_OutWave = new WaveFile();

            int rc = m_OutWave.OpenForWrite(null, stream, freq, 16, (short) m_Channels);
        }

        /// <summary>
        ///     Takes a 16 Bit PCM sample.
        /// </summary>
        public override void Append(int channel, short valueRenamed)
        {
            m_Buffer[m_Bufferp[channel]] = valueRenamed;
            m_Bufferp[channel] = (short) (m_Bufferp[channel] + m_Channels);
        }

        public override void WriteBuffer(int val)
        {
            int rc = m_OutWave.WriteData(m_Buffer, m_Bufferp[0]);

            for (int i = 0; i < m_Channels; ++i)
                m_Bufferp[i] = (short) i;
        }

        public void close(bool justWriteLengthBytes)
        {
            m_OutWave.Close(justWriteLengthBytes);
        }

        public override void Close()
        {
            m_OutWave.Close();
        }

        /// <summary>
        ///     *
        /// </summary>
        public override void ClearBuffer()
        {
        }

        /// <summary>
        ///     *
        /// </summary>
        public override void SetStopFlag()
        {
        }
    }
}