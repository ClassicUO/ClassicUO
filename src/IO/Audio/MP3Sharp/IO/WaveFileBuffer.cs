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