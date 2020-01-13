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

namespace ClassicUO.IO.Audio.MP3Sharp.Decoding
{
    /// <summary>
    ///     The SampleBuffer class implements an output buffer
    ///     that provides storage for a fixed size block of samples.
    /// </summary>
    internal class SampleBuffer : ABuffer
    {
        private readonly short[] buffer;
        private readonly int[] bufferp;
        private readonly int channels;

        /// <summary>
        ///     Constructor
        /// </summary>
        public SampleBuffer(int sample_frequency, int number_of_channels)
        {
            buffer = new short[OBUFFERSIZE];
            bufferp = new int[MAXCHANNELS];
            channels = number_of_channels;
            SampleFrequency = sample_frequency;

            for (int i = 0; i < number_of_channels; ++i)
                bufferp[i] = (short) i;
        }

        public virtual int ChannelCount => channels;

        public virtual int SampleFrequency { get; }

        public virtual short[] Buffer => buffer;

        public virtual int BufferLength => bufferp[0];

        /// <summary>
        ///     Takes a 16 Bit PCM sample.
        /// </summary>
        public override void Append(int channel, short valueRenamed)
        {
            buffer[bufferp[channel]] = valueRenamed;
            bufferp[channel] += channels;
        }

        public override void AppendSamples(int channel, float[] samples)
        {
            int pos = bufferp[channel];

            short s;
            float fs;

            for (int i = 0; i < 32;)
            {
                fs = samples[i++];
                fs = fs > 32767.0f ? 32767.0f : fs < -32767.0f ? -32767.0f : fs;

                //UPGRADE_WARNING: Narrowing conversions may produce unexpected results in C#. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1042"'
                s = (short) fs;
                buffer[pos] = s;
                pos += channels;
            }

            bufferp[channel] = pos;
        }

        /// <summary>
        ///     Write the samples to the file (Random Acces).
        /// </summary>
        public override void WriteBuffer(int val)
        {
            // for (int i = 0; i < channels; ++i) 
            // bufferp[i] = (short)i;
        }

        public override void Close()
        {
        }

        /// <summary>
        ///     *
        /// </summary>
        public override void ClearBuffer()
        {
            for (int i = 0; i < channels; ++i)
                bufferp[i] = (short) i;
        }

        /// <summary>
        ///     *
        /// </summary>
        public override void SetStopFlag()
        {
        }
    }
}