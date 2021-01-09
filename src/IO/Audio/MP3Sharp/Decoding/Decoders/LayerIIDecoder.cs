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
                {
                    subbands[i] = new SubbandLayer2(i);
                }
            }
            else if (mode == Header.JOINT_STEREO)
            {
                for (i = 0; i < header.intensity_stereo_bound(); ++i)
                {
                    subbands[i] = new SubbandLayer2Stereo(i);
                }

                for (; i < num_subbands; ++i)
                {
                    subbands[i] = new SubbandLayer2IntensityStereo(i);
                }
            }
            else
            {
                for (i = 0; i < num_subbands; ++i)
                {
                    subbands[i] = new SubbandLayer2Stereo(i);
                }
            }
        }

        protected internal override void ReadScaleFactorSelection()
        {
            for (int i = 0; i < num_subbands; ++i)
            {
                ((SubbandLayer2) subbands[i]).read_scalefactor_selection(stream, crc);
            }
        }
    }
}