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

using ClassicUO.IO.Audio.MP3Sharp.Decoding.Decoders.LayerII;

namespace ClassicUO.IO.Audio.MP3Sharp.Decoding.Decoders
{
    /// <summary>
    ///     Implements decoding of MPEG Audio Layer II frames.
    /// </summary>
    internal class LayerIIDecoder : LayerIDecoder, IFrameDecoder
    {
        protected internal override void CreateSubbands()
        {
            int i;

            if (mode == Header.SINGLE_CHANNEL)
            {
                for (i = 0; i < num_subbands; ++i)
                    subbands[i] = new SubbandLayer2(i);
            }
            else if (mode == Header.JOINT_STEREO)
            {
                for (i = 0; i < header.intensity_stereo_bound(); ++i)
                    subbands[i] = new SubbandLayer2Stereo(i);

                for (; i < num_subbands; ++i)
                    subbands[i] = new SubbandLayer2IntensityStereo(i);
            }
            else
            {
                for (i = 0; i < num_subbands; ++i)
                    subbands[i] = new SubbandLayer2Stereo(i);
            }
        }

        protected internal override void ReadScaleFactorSelection()
        {
            for (int i = 0; i < num_subbands; ++i)
                ((SubbandLayer2) subbands[i]).read_scalefactor_selection(stream, crc);
        }
    }
}