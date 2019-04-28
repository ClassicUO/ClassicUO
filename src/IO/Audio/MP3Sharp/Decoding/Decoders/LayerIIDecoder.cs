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