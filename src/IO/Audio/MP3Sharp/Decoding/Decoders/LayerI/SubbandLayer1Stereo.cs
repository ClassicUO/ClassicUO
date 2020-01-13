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

namespace ClassicUO.IO.Audio.MP3Sharp.Decoding.Decoders.LayerI
{
    /// <summary>
    ///     Class for layer I subbands in stereo mode.
    /// </summary>
    internal class SubbandLayer1Stereo : SubbandLayer1
    {
        protected internal int channel2_allocation;
        protected internal float channel2_factor, channel2_offset;
        protected internal float channel2_sample;
        protected internal int channel2_samplelength;
        protected internal float channel2_scalefactor;

        /// <summary>
        ///     Constructor
        /// </summary>
        public SubbandLayer1Stereo(int subbandnumber)
            : base(subbandnumber)
        {
        }

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
                scalefactor = ScaleFactors[stream.GetBitsFromBuffer(6)];

            if (channel2_allocation != 0)
                channel2_scalefactor = ScaleFactors[stream.GetBitsFromBuffer(6)];
        }

        /// <summary>
        ///     *
        /// </summary>
        public override bool read_sampledata(Bitstream stream)
        {
            bool returnvalue = base.read_sampledata(stream);
            if (channel2_allocation != 0) channel2_sample = stream.GetBitsFromBuffer(channel2_samplelength);

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
                    filter2.input_sample(sample2, subbandnumber);
                else
                    filter1.input_sample(sample2, subbandnumber);
            }

            return true;
        }
    }
}