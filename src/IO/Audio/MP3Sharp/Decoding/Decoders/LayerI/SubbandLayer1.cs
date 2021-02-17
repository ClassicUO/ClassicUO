#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

namespace ClassicUO.IO.Audio.MP3Sharp.Decoding.Decoders.LayerI
{
    /// <summary>
    ///     Class for layer I subbands in single channel mode.
    ///     Used for single channel mode
    ///     and in derived class for intensity stereo mode
    /// </summary>
    internal class SubbandLayer1 : ASubband
    {
        // Factors and offsets for sample requantization
        public static readonly float[] TableFactor =
        {
            0.0f, 1.0f / 2.0f * (4.0f / 3.0f), 1.0f / 4.0f * (8.0f / 7.0f), 1.0f / 8.0f * (16.0f / 15.0f),
            1.0f / 16.0f * (32.0f / 31.0f), 1.0f / 32.0f * (64.0f / 63.0f), 1.0f / 64.0f * (128.0f / 127.0f),
            1.0f / 128.0f * (256.0f / 255.0f), 1.0f / 256.0f * (512.0f / 511.0f), 1.0f / 512.0f * (1024.0f / 1023.0f),
            1.0f / 1024.0f * (2048.0f / 2047.0f), 1.0f / 2048.0f * (4096.0f / 4095.0f),
            1.0f / 4096.0f * (8192.0f / 8191.0f),
            1.0f / 8192.0f * (16384.0f / 16383.0f), 1.0f / 16384.0f * (32768.0f / 32767.0f)
        };

        public static readonly float[] TableOffset =
        {
            0.0f, (1.0f / 2.0f - 1.0f) * (4.0f / 3.0f), (1.0f / 4.0f - 1.0f) * (8.0f / 7.0f),
            (1.0f / 8.0f - 1.0f) * (16.0f / 15.0f), (1.0f / 16.0f - 1.0f) * (32.0f / 31.0f),
            (1.0f / 32.0f - 1.0f) * (64.0f / 63.0f), (1.0f / 64.0f - 1.0f) * (128.0f / 127.0f),
            (1.0f / 128.0f - 1.0f) * (256.0f / 255.0f), (1.0f / 256.0f - 1.0f) * (512.0f / 511.0f),
            (1.0f / 512.0f - 1.0f) * (1024.0f / 1023.0f), (1.0f / 1024.0f - 1.0f) * (2048.0f / 2047.0f),
            (1.0f / 2048.0f - 1.0f) * (4096.0f / 4095.0f), (1.0f / 4096.0f - 1.0f) * (8192.0f / 8191.0f),
            (1.0f / 8192.0f - 1.0f) * (16384.0f / 16383.0f), (1.0f / 16384.0f - 1.0f) * (32768.0f / 32767.0f)
        };

        /// <summary>
        ///     Construtor.
        /// </summary>
        public SubbandLayer1(int subbandnumber)
        {
            this.subbandnumber = subbandnumber;
            samplenumber = 0;
        }

        protected int allocation;
        protected float factor, offset;
        protected float sample;
        protected int samplelength;
        protected int samplenumber;
        protected float scalefactor;
        protected int subbandnumber;

        /// <summary>
        ///     *
        /// </summary>
        public override void read_allocation(Bitstream stream, Header header, Crc16 crc)
        {
            if ((allocation = stream.GetBitsFromBuffer(4)) == 15)
            {
            }

            // cerr << "WARNING: stream contains an illegal allocation!\n";
            // MPEG-stream is corrupted!
            if (crc != null)
            {
                crc.add_bits(allocation, 4);
            }

            if (allocation != 0)
            {
                samplelength = allocation + 1;
                factor = TableFactor[allocation];
                offset = TableOffset[allocation];
            }
        }

        /// <summary>
        ///     *
        /// </summary>
        public override void read_scalefactor(Bitstream stream, Header header)
        {
            if (allocation != 0)
            {
                scalefactor = ScaleFactors[stream.GetBitsFromBuffer(6)];
            }
        }

        /// <summary>
        ///     *
        /// </summary>
        public override bool read_sampledata(Bitstream stream)
        {
            if (allocation != 0)
            {
                sample = stream.GetBitsFromBuffer(samplelength);
            }

            if (++samplenumber == 12)
            {
                samplenumber = 0;

                return true;
            }

            return false;
        }

        /// <summary>
        ///     *
        /// </summary>
        public override bool put_next_sample(int channels, SynthesisFilter filter1, SynthesisFilter filter2)
        {
            if (allocation != 0 && channels != OutputChannels.RIGHT_CHANNEL)
            {
                float scaled_sample = (sample * factor + offset) * scalefactor;
                filter1.input_sample(scaled_sample, subbandnumber);
            }

            return true;
        }
    }
}