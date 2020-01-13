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

using System;

using ClassicUO.IO.Audio.MP3Sharp.Decoding.Decoders;

namespace ClassicUO.IO.Audio.MP3Sharp.Decoding
{
    /// <summary>
    ///     Encapsulates the details of decoding an MPEG audio frame.
    /// </summary>
    internal class Decoder
    {
        private static readonly Params DEFAULT_PARAMS = new Params();
        private readonly Params params_Renamed;
        private Equalizer m_Equalizer;

        private bool m_IsInitialized;
        private LayerIDecoder m_L1Decoder;
        private LayerIIDecoder m_L2Decoder;
        private LayerIIIDecoder m_L3Decoder;

        private SynthesisFilter m_LeftChannelFilter;

        private ABuffer m_Output;

        private int m_OutputChannels;
        private int m_OutputFrequency;
        private SynthesisFilter m_RightChannelFilter;

        /// <summary>
        ///     Creates a new Decoder instance with default parameters.
        /// </summary>
        public Decoder() : this(null)
        {
            InitBlock();
        }

        /// <summary>
        ///     Creates a new Decoder instance with custom parameters.
        /// </summary>
        public Decoder(Params params0)
        {
            InitBlock();

            if (params0 == null)
                params0 = DEFAULT_PARAMS;

            params_Renamed = params0;

            Equalizer eq = params_Renamed.InitialEqualizerSettings;
            if (eq != null) m_Equalizer.FromEqualizer = eq;
        }

        public static Params DefaultParams => (Params) DEFAULT_PARAMS.Clone();

        public virtual Equalizer Equalizer
        {
            set
            {
                if (value == null)
                    value = Equalizer.PASS_THRU_EQ;

                m_Equalizer.FromEqualizer = value;

                float[] factors = m_Equalizer.BandFactors;

                if (m_LeftChannelFilter != null)
                    m_LeftChannelFilter.EQ = factors;

                if (m_RightChannelFilter != null)
                    m_RightChannelFilter.EQ = factors;
            }
        }

        /// <summary>
        ///     Changes the output buffer. This will take effect the next time
        ///     decodeFrame() is called.
        /// </summary>
        public virtual ABuffer OutputBuffer
        {
            set => m_Output = value;
        }

        /// <summary>
        ///     Retrieves the sample frequency of the PCM samples output
        ///     by this decoder. This typically corresponds to the sample
        ///     rate encoded in the MPEG audio stream.
        /// </summary>
        public virtual int OutputFrequency => m_OutputFrequency;

        /// <summary>
        ///     Retrieves the number of channels of PCM samples output by
        ///     this decoder. This usually corresponds to the number of
        ///     channels in the MPEG audio stream.
        /// </summary>
        public virtual int OutputChannels => m_OutputChannels;

        /// <summary>
        ///     Retrieves the maximum number of samples that will be written to
        ///     the output buffer when one frame is decoded. This can be used to
        ///     help calculate the size of other buffers whose size is based upon
        ///     the number of samples written to the output buffer. NB: this is
        ///     an upper bound and fewer samples may actually be written, depending
        ///     upon the sample rate and number of channels.
        /// </summary>
        public virtual int OutputBlockSize => ABuffer.OBUFFERSIZE;

        private void InitBlock()
        {
            m_Equalizer = new Equalizer();
        }

        /// <summary>
        ///     Decodes one frame from an MPEG audio bitstream.
        /// </summary>
        /// <param name="header">
        ///     Header describing the frame to decode.
        /// </param>
        /// <param name="stream">
        ///     Bistream that provides the bits for the body of the frame.
        /// </param>
        /// <returns>
        ///     A SampleBuffer containing the decoded samples.
        /// </returns>
        public virtual ABuffer DecodeFrame(Header header, Bitstream stream)
        {
            if (!m_IsInitialized) Initialize(header);

            int layer = header.layer();

            m_Output.ClearBuffer();

            IFrameDecoder decoder = RetrieveDecoder(header, stream, layer);

            decoder.DecodeFrame();

            m_Output.WriteBuffer(1);

            return m_Output;
        }

        protected internal virtual DecoderException NewDecoderException(int errorcode)
        {
            return new DecoderException(errorcode, null);
        }

        protected internal virtual DecoderException NewDecoderException(int errorcode, Exception throwable)
        {
            return new DecoderException(errorcode, throwable);
        }

        protected internal virtual IFrameDecoder RetrieveDecoder(Header header, Bitstream stream, int layer)
        {
            IFrameDecoder decoder = null;

            // REVIEW: allow channel output selection type
            // (LEFT, RIGHT, BOTH, DOWNMIX)
            switch (layer)
            {
                case 3:

                    if (m_L3Decoder == null)
                    {
                        m_L3Decoder = new LayerIIIDecoder(stream, header, m_LeftChannelFilter, m_RightChannelFilter, m_Output,
                                                          (int) OutputChannelsEnum.BOTH_CHANNELS);
                    }

                    decoder = m_L3Decoder;

                    break;

                case 2:

                    if (m_L2Decoder == null)
                    {
                        m_L2Decoder = new LayerIIDecoder();

                        m_L2Decoder.Create(stream, header, m_LeftChannelFilter, m_RightChannelFilter, m_Output,
                                           (int) OutputChannelsEnum.BOTH_CHANNELS);
                    }

                    decoder = m_L2Decoder;

                    break;

                case 1:

                    if (m_L1Decoder == null)
                    {
                        m_L1Decoder = new LayerIDecoder();

                        m_L1Decoder.Create(stream, header, m_LeftChannelFilter, m_RightChannelFilter, m_Output,
                                           (int) OutputChannelsEnum.BOTH_CHANNELS);
                    }

                    decoder = m_L1Decoder;

                    break;
            }

            if (decoder == null) throw NewDecoderException(DecoderErrors.UNSUPPORTED_LAYER, null);

            return decoder;
        }

        private void Initialize(Header header)
        {
            // REVIEW: allow customizable scale factor
            float scalefactor = 32700.0f;

            int mode = header.mode();
            int layer = header.layer();
            int channels = mode == Header.SINGLE_CHANNEL ? 1 : 2;

            // set up output buffer if not set up by client.
            if (m_Output == null)
                m_Output = new SampleBuffer(header.frequency(), channels);

            float[] factors = m_Equalizer.BandFactors;
            //Console.WriteLine("NOT CREATING SYNTHESIS FILTERS");
            m_LeftChannelFilter = new SynthesisFilter(0, scalefactor, factors);

            // REVIEW: allow mono output for stereo
            if (channels == 2)
                m_RightChannelFilter = new SynthesisFilter(1, scalefactor, factors);

            m_OutputChannels = channels;
            m_OutputFrequency = header.frequency();

            m_IsInitialized = true;
        }

        /// <summary>
        ///     The Params class presents the customizable
        ///     aspects of the decoder. Instances of this class are not thread safe.
        /// </summary>
        internal class Params : ICloneable
        {
            private OutputChannels m_OutputChannels;

            public virtual OutputChannels OutputChannels
            {
                get => m_OutputChannels;

                set
                {
                    if (value == null)
                        throw new NullReferenceException("out");

                    m_OutputChannels = value;
                }
            }

            /// <summary>
            ///     Retrieves the equalizer settings that the decoder's equalizer
            ///     will be initialized from.
            ///     The Equalizer instance returned
            ///     cannot be changed in real time to affect the
            ///     decoder output as it is used only to initialize the decoders
            ///     EQ settings. To affect the decoder's output in realtime,
            ///     use the Equalizer returned from the getEqualizer() method on
            ///     the decoder.
            /// </summary>
            /// <returns>
            ///     The Equalizer used to initialize the
            ///     EQ settings of the decoder.
            /// </returns>
            public virtual Equalizer InitialEqualizerSettings { get; }

            public object Clone()
            {
                try
                {
                    return MemberwiseClone();
                }
                catch (Exception ex)
                {
                    throw new ApplicationException(this + ": " + ex);
                }
            }
        }
    }
}