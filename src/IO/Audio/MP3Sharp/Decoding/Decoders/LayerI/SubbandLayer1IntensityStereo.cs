namespace ClassicUO.IO.Audio.MP3Sharp.Decoding.Decoders.LayerI
{
    /// <summary>
    ///     Class for layer I subbands in joint stereo mode.
    /// </summary>
    internal class SubbandLayer1IntensityStereo : SubbandLayer1
    {
        protected internal float channel2_scalefactor;

        /// <summary>
        ///     Constructor
        /// </summary>
        public SubbandLayer1IntensityStereo(int subbandnumber)
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
        public override void read_scalefactor(Bitstream stream, Header header)
        {
            if (allocation != 0)
            {
                scalefactor = ScaleFactors[stream.GetBitsFromBuffer(6)];
                channel2_scalefactor = ScaleFactors[stream.GetBitsFromBuffer(6)];
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
                sample = sample * factor + offset; // requantization

                if (channels == OutputChannels.BOTH_CHANNELS)
                {
                    float sample1 = sample * scalefactor, sample2 = sample * channel2_scalefactor;
                    filter1.input_sample(sample1, subbandnumber);
                    filter2.input_sample(sample2, subbandnumber);
                }
                else if (channels == OutputChannels.LEFT_CHANNEL)
                {
                    float sample1 = sample * scalefactor;
                    filter1.input_sample(sample1, subbandnumber);
                }
                else
                {
                    float sample2 = sample * channel2_scalefactor;
                    filter1.input_sample(sample2, subbandnumber);
                }
            }

            return true;
        }
    }
}