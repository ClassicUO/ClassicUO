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

namespace ClassicUO.IO.Audio.MP3Sharp.Decoding.Decoders.LayerII
{
    /// <summary>
    ///     Class for layer II subbands in joint stereo mode.
    /// </summary>
    internal class SubbandLayer2IntensityStereo : SubbandLayer2
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        public SubbandLayer2IntensityStereo(int subbandnumber) : base(subbandnumber)
        {
        }

        protected internal float channel2_scalefactor1, channel2_scalefactor2, channel2_scalefactor3;
        protected internal int channel2_scfsi;

        /// <summary>
        ///     *
        /// </summary>
        public override void read_allocation(Bitstream stream, Header header, Crc16 crc)
        {
            base.read_allocation(stream, header, crc);
        }

        /// <summary>
        ///     *
        /// </summary>
        public override void read_scalefactor_selection(Bitstream stream, Crc16 crc)
        {
            if (allocation != 0)
            {
                scfsi = stream.GetBitsFromBuffer(2);
                channel2_scfsi = stream.GetBitsFromBuffer(2);

                if (crc != null)
                {
                    crc.add_bits(scfsi, 2);
                    crc.add_bits(channel2_scfsi, 2);
                }
            }
        }

        /// <summary>
        ///     *
        /// </summary>
        public override void read_scalefactor(Bitstream stream, Header header)
        {
            if (allocation != 0)
            {
                base.read_scalefactor(stream, header);

                switch (channel2_scfsi)
                {
                    case 0:
                        channel2_scalefactor1 = ScaleFactors[stream.GetBitsFromBuffer(6)];
                        channel2_scalefactor2 = ScaleFactors[stream.GetBitsFromBuffer(6)];
                        channel2_scalefactor3 = ScaleFactors[stream.GetBitsFromBuffer(6)];

                        break;

                    case 1:
                        channel2_scalefactor1 = channel2_scalefactor2 = ScaleFactors[stream.GetBitsFromBuffer(6)];
                        channel2_scalefactor3 = ScaleFactors[stream.GetBitsFromBuffer(6)];

                        break;

                    case 2:

                        channel2_scalefactor1 = channel2_scalefactor2 = channel2_scalefactor3 = ScaleFactors[stream.GetBitsFromBuffer(6)];

                        break;

                    case 3:
                        channel2_scalefactor1 = ScaleFactors[stream.GetBitsFromBuffer(6)];
                        channel2_scalefactor2 = channel2_scalefactor3 = ScaleFactors[stream.GetBitsFromBuffer(6)];

                        break;
                }
            }
        }

        /// <summary>
        ///     *
        /// </summary>
        public override bool read_sampledata(Bitstream stream)
        {
            return base.read_sampledata(stream);
        }

        /// <summary>
        ///     *
        /// </summary>
        public override bool put_next_sample(int channels, SynthesisFilter filter1, SynthesisFilter filter2)
        {
            if (allocation != 0)
            {
                float sample = samples[samplenumber];

                if (groupingtable[0] == null)
                {
                    sample = (sample + d[0]) * c[0];
                }

                if (channels == OutputChannels.BOTH_CHANNELS)
                {
                    float sample2 = sample;

                    if (groupnumber <= 4)
                    {
                        sample *= scalefactor1;
                        sample2 *= channel2_scalefactor1;
                    }
                    else if (groupnumber <= 8)
                    {
                        sample *= scalefactor2;
                        sample2 *= channel2_scalefactor2;
                    }
                    else
                    {
                        sample *= scalefactor3;
                        sample2 *= channel2_scalefactor3;
                    }

                    filter1.input_sample(sample, subbandnumber);
                    filter2.input_sample(sample2, subbandnumber);
                }
                else if (channels == OutputChannels.LEFT_CHANNEL)
                {
                    if (groupnumber <= 4)
                    {
                        sample *= scalefactor1;
                    }
                    else if (groupnumber <= 8)
                    {
                        sample *= scalefactor2;
                    }
                    else
                    {
                        sample *= scalefactor3;
                    }

                    filter1.input_sample(sample, subbandnumber);
                }
                else
                {
                    if (groupnumber <= 4)
                    {
                        sample *= channel2_scalefactor1;
                    }
                    else if (groupnumber <= 8)
                    {
                        sample *= channel2_scalefactor2;
                    }
                    else
                    {
                        sample *= channel2_scalefactor3;
                    }

                    filter1.input_sample(sample, subbandnumber);
                }
            }

            if (++samplenumber == 3)
            {
                return true;
            }

            return false;
        }
    }
}