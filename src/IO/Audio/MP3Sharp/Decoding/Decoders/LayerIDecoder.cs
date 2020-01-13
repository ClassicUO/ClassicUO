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

using ClassicUO.IO.Audio.MP3Sharp.Decoding.Decoders.LayerI;

namespace ClassicUO.IO.Audio.MP3Sharp.Decoding.Decoders
{
    /// <summary>
    ///     Implements decoding of MPEG Audio Layer I frames.
    /// </summary>
    internal class LayerIDecoder : IFrameDecoder
    {
        protected internal ABuffer buffer;
        protected internal Crc16 crc;
        protected internal SynthesisFilter filter1, filter2;
        protected internal Header header;
        protected internal int mode;
        protected internal int num_subbands;
        protected internal Bitstream stream;
        protected internal ASubband[] subbands;
        protected internal int which_channels;
        // new Crc16[1] to enable CRC checking.

        public LayerIDecoder()
        {
            crc = new Crc16();
        }

        public virtual void DecodeFrame()
        {
            num_subbands = header.number_of_subbands();
            subbands = new ASubband[32];
            mode = header.mode();

            CreateSubbands();

            ReadAllocation();
            ReadScaleFactorSelection();

            if (crc != null || header.IsChecksumOK())
            {
                ReadScaleFactors();

                ReadSampleData();
            }
        }

        public virtual void Create(Bitstream stream0, Header header0, SynthesisFilter filtera, SynthesisFilter filterb,
                                   ABuffer buffer0, int whichCh0)
        {
            stream = stream0;
            header = header0;
            filter1 = filtera;
            filter2 = filterb;
            buffer = buffer0;
            which_channels = whichCh0;
        }

        protected internal virtual void CreateSubbands()
        {
            int i;

            if (mode == Header.SINGLE_CHANNEL)
            {
                for (i = 0; i < num_subbands; ++i)
                    subbands[i] = new SubbandLayer1(i);
            }
            else if (mode == Header.JOINT_STEREO)
            {
                for (i = 0; i < header.intensity_stereo_bound(); ++i)
                    subbands[i] = new SubbandLayer1Stereo(i);

                for (; i < num_subbands; ++i)
                    subbands[i] = new SubbandLayer1IntensityStereo(i);
            }
            else
            {
                for (i = 0; i < num_subbands; ++i)
                    subbands[i] = new SubbandLayer1Stereo(i);
            }
        }

        protected internal virtual void ReadAllocation()
        {
            // start to read audio data:
            for (int i = 0; i < num_subbands; ++i)
                subbands[i].read_allocation(stream, header, crc);
        }

        protected internal virtual void ReadScaleFactorSelection()
        {
            // scale factor selection not present for layer I. 
        }

        protected internal virtual void ReadScaleFactors()
        {
            for (int i = 0; i < num_subbands; ++i)
                subbands[i].read_scalefactor(stream, header);
        }

        protected internal virtual void ReadSampleData()
        {
            bool readReady = false;
            bool writeReady = false;
            int hdrMode = header.mode();

            do
            {
                int i;

                for (i = 0; i < num_subbands; ++i)
                    readReady = subbands[i].read_sampledata(stream);

                do
                {
                    for (i = 0; i < num_subbands; ++i)
                        writeReady = subbands[i].put_next_sample(which_channels, filter1, filter2);

                    filter1.calculate_pcm_samples(buffer);

                    if (which_channels == OutputChannels.BOTH_CHANNELS && hdrMode != Header.SINGLE_CHANNEL)
                        filter2.calculate_pcm_samples(buffer);
                } while (!writeReady);
            } while (!readReady);
        }
    }
}