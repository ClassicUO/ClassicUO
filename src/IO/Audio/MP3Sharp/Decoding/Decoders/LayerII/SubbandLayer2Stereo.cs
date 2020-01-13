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
    ///     Class for layer II subbands in stereo mode.
    /// </summary>
    internal class SubbandLayer2Stereo : SubbandLayer2
    {
        protected internal int channel2_allocation;
        protected internal float[] channel2_c = {0};
        //protected boolean channel2_grouping;  ???? Never used!
        protected internal int[] channel2_codelength = {0};
        protected internal float[] channel2_d = {0};
        //protected float[][] channel2_groupingtable = {{0},{0}};
        protected internal float[] channel2_factor = {0};
        protected internal float[] channel2_samples;
        protected internal float channel2_scalefactor1, channel2_scalefactor2, channel2_scalefactor3;
        protected internal int channel2_scfsi;

        /// <summary>
        ///     Constructor
        /// </summary>
        public SubbandLayer2Stereo(int subbandnumber)
            : base(subbandnumber)
        {
            channel2_samples = new float[3];
        }

        /// <summary>
        ///     *
        /// </summary>
        public override void read_allocation(Bitstream stream, Header header, Crc16 crc)
        {
            int length = get_allocationlength(header);
            allocation = stream.GetBitsFromBuffer(length);
            channel2_allocation = stream.GetBitsFromBuffer(length);

            if (crc != null)
            {
                crc.add_bits(allocation, length);
                crc.add_bits(channel2_allocation, length);
            }
        }

        /// <summary>
        ///     *
        /// </summary>
        public override void read_scalefactor_selection(Bitstream stream, Crc16 crc)
        {
            if (allocation != 0)
            {
                scfsi = stream.GetBitsFromBuffer(2);

                if (crc != null)
                    crc.add_bits(scfsi, 2);
            }

            if (channel2_allocation != 0)
            {
                channel2_scfsi = stream.GetBitsFromBuffer(2);

                if (crc != null)
                    crc.add_bits(channel2_scfsi, 2);
            }
        }

        /// <summary>
        ///     *
        /// </summary>
        public override void read_scalefactor(Bitstream stream, Header header)
        {
            base.read_scalefactor(stream, header);

            if (channel2_allocation != 0)
            {
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

                prepare_sample_reading(header, channel2_allocation, 1, channel2_factor, channel2_codelength,
                                       channel2_c, channel2_d);
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
                if (groupingtable[1] != null)
                {
                    int samplecode = stream.GetBitsFromBuffer(channel2_codelength[0]);
                    // create requantized samples:
                    samplecode += samplecode << 1;
                    /*
                    float[] target = channel2_samples;
                    float[] source = channel2_groupingtable[0];
                    int tmp = 0;
                    int temp = 0;
                    target[tmp++] = source[samplecode + temp];
                    temp++;
                    target[tmp++] = source[samplecode + temp];
                    temp++;
                    target[tmp] = source[samplecode + temp];
                    // memcpy (channel2_samples, channel2_groupingtable + samplecode, 3 * sizeof (real));
                    */
                    float[] target = channel2_samples;
                    float[] source = groupingtable[1];
                    int tmp = 0;
                    int temp = samplecode;
                    target[tmp] = source[temp];
                    temp++;
                    tmp++;
                    target[tmp] = source[temp];
                    temp++;
                    tmp++;
                    target[tmp] = source[temp];
                }
                else
                {
                    channel2_samples[0] =
                        (float) (stream.GetBitsFromBuffer(channel2_codelength[0]) * channel2_factor[0] - 1.0);

                    channel2_samples[1] =
                        (float) (stream.GetBitsFromBuffer(channel2_codelength[0]) * channel2_factor[0] - 1.0);

                    channel2_samples[2] =
                        (float) (stream.GetBitsFromBuffer(channel2_codelength[0]) * channel2_factor[0] - 1.0);
                }
            }

            return returnvalue;
        }

        /// <summary>
        ///     *
        /// </summary>
        public override bool put_next_sample(int channels, SynthesisFilter filter1, SynthesisFilter filter2)
        {
            bool returnvalue = base.put_next_sample(channels, filter1, filter2);

            if (channel2_allocation != 0 && channels != OutputChannels.LEFT_CHANNEL)
            {
                float sample = channel2_samples[samplenumber - 1];

                if (groupingtable[1] == null)
                    sample = (sample + channel2_d[0]) * channel2_c[0];

                if (groupnumber <= 4)
                    sample *= channel2_scalefactor1;
                else if (groupnumber <= 8)
                    sample *= channel2_scalefactor2;
                else
                    sample *= channel2_scalefactor3;

                if (channels == OutputChannels.BOTH_CHANNELS)
                    filter2.input_sample(sample, subbandnumber);
                else
                    filter1.input_sample(sample, subbandnumber);
            }

            return returnvalue;
        }
    }
}