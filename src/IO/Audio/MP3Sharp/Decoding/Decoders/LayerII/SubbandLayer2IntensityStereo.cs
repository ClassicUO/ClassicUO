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

namespace ClassicUO.IO.Audio.MP3Sharp.Decoding.Decoders.LayerII
{
    /// <summary>
    ///     Class for layer II subbands in joint stereo mode.
    /// </summary>
    internal class SubbandLayer2IntensityStereo : SubbandLayer2
    {
        protected internal float channel2_scalefactor1, channel2_scalefactor2, channel2_scalefactor3;
        protected internal int channel2_scfsi;

        /// <summary>
        ///     Constructor
        /// </summary>
        public SubbandLayer2IntensityStereo(int subbandnumber)
            : base(subbandnumber)
        {
        }

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

                        channel2_scalefactor1 =
                            channel2_scalefactor2 = channel2_scalefactor3 = ScaleFactors[stream.GetBitsFromBuffer(6)];

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
                    sample = (sample + d[0]) * c[0];

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
                        sample *= scalefactor1;
                    else if (groupnumber <= 8)
                        sample *= scalefactor2;
                    else
                        sample *= scalefactor3;
                    filter1.input_sample(sample, subbandnumber);
                }
                else
                {
                    if (groupnumber <= 4)
                        sample *= channel2_scalefactor1;
                    else if (groupnumber <= 8)
                        sample *= channel2_scalefactor2;
                    else
                        sample *= channel2_scalefactor3;
                    filter1.input_sample(sample, subbandnumber);
                }
            }

            if (++samplenumber == 3)
                return true;

            return false;
        }
    }
}