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
    ///     Class for layer I subbands in stereo mode.
    /// </summary>
    internal class SubbandLayer1Stereo : SubbandLayer1
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        public SubbandLayer1Stereo(int subbandnumber) : base(subbandnumber)
        {
        }

        protected internal int channel2_allocation;
        protected internal float channel2_factor, channel2_offset;
        protected internal float channel2_sample;
        protected internal int channel2_samplelength;
        protected internal float channel2_scalefactor;

        /// <summary>
        ///     *
        /// </summary>
        public override void read_allocation(Bitstream stream, Header header, Crc16 crc)
        {
            allocation = stream.GetBitsFromBuffer(4);
            channel2_allocation = stream.GetBitsFromBuffer(4);

            if (crc != null)
            {
                crc.add_bits(allocation, 4);
                crc.add_bits(channel2_allocation, 4);
            }

            if (allocation != 0)
            {
                samplelength = allocation + 1;
                factor = TableFactor[allocation];
                offset = TableOffset[allocation];
            }

            if (channel2_allocation != 0)
            {
                channel2_samplelength = channel2_allocation + 1;
                channel2_factor = TableFactor[channel2_allocation];
                channel2_offset = TableOffset[channel2_allocation];
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

            if (channel2_allocation != 0)
            {
                channel2_scalefactor = ScaleFactors[stream.GetBitsFromBuffer(6)];
            }
        }

        /// <summary>
        ///     *
        /// </summary>
        public override bool read_sampledata(Bitstream stream)
        {
            bool returnvalue = base.read_sampledata(stream);

            if (channel2_allocation != 0)
            {
                channel2_sample = stream.GetBitsFromBuffer(channel2_samplelength);
            }

            return returnvalue;
        }

        /// <summary>
        ///     *
        /// </summary>
        public override bool put_next_sample(int channels, SynthesisFilter filter1, SynthesisFilter filter2)
        {
            base.put_next_sample(channels, filter1, filter2);

            if (channel2_allocation != 0 && channels != OutputChannels.LEFT_CHANNEL)
            {
                float sample2 = (channel2_sample * channel2_factor + channel2_offset) * channel2_scalefactor;

                if (channels == OutputChannels.BOTH_CHANNELS)
                {
                    filter2.input_sample(sample2, subbandnumber);
                }
                else
                {
                    filter1.input_sample(sample2, subbandnumber);
                }
            }

            return true;
        }
    }
}