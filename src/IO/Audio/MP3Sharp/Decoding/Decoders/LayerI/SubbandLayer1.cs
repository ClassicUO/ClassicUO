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
            1.0f / 1024.0f * (2048.0f / 2047.0f), 1.0f / 2048.0f * (4096.0f / 4095.0f), 1.0f / 4096.0f * (8192.0f / 8191.0f),
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

        protected int allocation;
        protected float factor, offset;
        protected float sample;
        protected int samplelength;
        protected int samplenumber;
        protected float scalefactor;
        protected int subbandnumber;

        /// <summary>
        ///     Construtor.
        /// </summary>
        public SubbandLayer1(int subbandnumber)
        {
            this.subbandnumber = subbandnumber;
            samplenumber = 0;
        }

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
                crc.add_bits(allocation, 4);

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
                scalefactor = ScaleFactors[stream.GetBitsFromBuffer(6)];
        }

        /// <summary>
        ///     *
        /// </summary>
        public override bool read_sampledata(Bitstream stream)
        {
            if (allocation != 0) sample = stream.GetBitsFromBuffer(samplelength);

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