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

using ClassicUO.IO.Audio.MP3Sharp.Decoding.Decoders.LayerIII;
using ClassicUO.IO.Audio.MP3Sharp.Support;

namespace ClassicUO.IO.Audio.MP3Sharp.Decoding.Decoders
{
    /// <summary>
    ///     Implements decoding of MPEG Audio Layer 3 frames.
    /// </summary>
    internal sealed class LayerIIIDecoder : IFrameDecoder
    {
        private const int SSLIMIT = 18;
        private const int SBLIMIT = 32;

        private static readonly int[][] slen =
        {
            new[] {0, 0, 0, 0, 3, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 4},
            new[] {0, 1, 2, 3, 0, 1, 2, 3, 1, 2, 3, 1, 2, 3, 2, 3}
        };

        public static readonly int[] pretab = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 3, 3, 3, 2, 0};

        public static readonly float[] two_to_negative_half_pow =
        {
            1.0000000000e+00f, 7.0710678119e-01f, 5.0000000000e-01f, 3.5355339059e-01f, 2.5000000000e-01f,
            1.7677669530e-01f, 1.2500000000e-01f, 8.8388347648e-02f, 6.2500000000e-02f, 4.4194173824e-02f,
            3.1250000000e-02f, 2.2097086912e-02f, 1.5625000000e-02f, 1.1048543456e-02f, 7.8125000000e-03f,
            5.5242717280e-03f, 3.9062500000e-03f, 2.7621358640e-03f, 1.9531250000e-03f, 1.3810679320e-03f,
            9.7656250000e-04f, 6.9053396600e-04f, 4.8828125000e-04f, 3.4526698300e-04f, 2.4414062500e-04f,
            1.7263349150e-04f, 1.2207031250e-04f, 8.6316745750e-05f, 6.1035156250e-05f, 4.3158372875e-05f,
            3.0517578125e-05f, 2.1579186438e-05f, 1.5258789062e-05f, 1.0789593219e-05f, 7.6293945312e-06f,
            5.3947966094e-06f, 3.8146972656e-06f, 2.6973983047e-06f, 1.9073486328e-06f, 1.3486991523e-06f,
            9.5367431641e-07f, 6.7434957617e-07f, 4.7683715820e-07f, 3.3717478809e-07f, 2.3841857910e-07f,
            1.6858739404e-07f, 1.1920928955e-07f, 8.4293697022e-08f, 5.9604644775e-08f, 4.2146848511e-08f,
            2.9802322388e-08f, 2.1073424255e-08f, 1.4901161194e-08f, 1.0536712128e-08f, 7.4505805969e-09f,
            5.2683560639e-09f, 3.7252902985e-09f, 2.6341780319e-09f, 1.8626451492e-09f, 1.3170890160e-09f,
            9.3132257462e-10f, 6.5854450798e-10f, 4.6566128731e-10f, 3.2927225399e-10f
        };

        public static readonly float[] t_43;

        public static readonly float[][] io =
        {
            new[]
            {
                1.0000000000e+00f, 8.4089641526e-01f, 7.0710678119e-01f, 5.9460355751e-01f, 5.0000000001e-01f,
                4.2044820763e-01f, 3.5355339060e-01f, 2.9730177876e-01f, 2.5000000001e-01f, 2.1022410382e-01f,
                1.7677669530e-01f, 1.4865088938e-01f, 1.2500000000e-01f, 1.0511205191e-01f, 8.8388347652e-02f,
                7.4325444691e-02f, 6.2500000003e-02f, 5.2556025956e-02f, 4.4194173826e-02f, 3.7162722346e-02f,
                3.1250000002e-02f, 2.6278012978e-02f, 2.2097086913e-02f, 1.8581361173e-02f, 1.5625000001e-02f,
                1.3139006489e-02f, 1.1048543457e-02f, 9.2906805866e-03f, 7.8125000006e-03f, 6.5695032447e-03f,
                5.5242717285e-03f, 4.6453402934e-03f
            },
            new[]
            {
                1.0000000000e+00f, 7.0710678119e-01f, 5.0000000000e-01f, 3.5355339060e-01f, 2.5000000000e-01f,
                1.7677669530e-01f, 1.2500000000e-01f, 8.8388347650e-02f, 6.2500000001e-02f, 4.4194173825e-02f,
                3.1250000001e-02f, 2.2097086913e-02f, 1.5625000000e-02f, 1.1048543456e-02f, 7.8125000002e-03f,
                5.5242717282e-03f, 3.9062500001e-03f, 2.7621358641e-03f, 1.9531250001e-03f, 1.3810679321e-03f,
                9.7656250004e-04f, 6.9053396603e-04f, 4.8828125002e-04f, 3.4526698302e-04f, 2.4414062501e-04f,
                1.7263349151e-04f, 1.2207031251e-04f, 8.6316745755e-05f, 6.1035156254e-05f, 4.3158372878e-05f,
                3.0517578127e-05f, 2.1579186439e-05f
            }
        };

        public static readonly float[] TAN12 =
        {
            0.0f, 0.26794919f, 0.57735027f, 1.0f, 1.73205081f, 3.73205081f, 9.9999999e10f, -3.73205081f, -1.73205081f,
            -1.0f, -0.57735027f, -0.26794919f, 0.0f, 0.26794919f, 0.57735027f, 1.0f
        };

        private static int[][] reorder_table; // Generated on demand

        private static readonly float[] cs =
        {
            0.857492925712f, 0.881741997318f, 0.949628649103f, 0.983314592492f, 0.995517816065f, 0.999160558175f,
            0.999899195243f, 0.999993155067f
        };

        private static readonly float[] ca =
        {
            -0.5144957554270f, -0.4717319685650f, -0.3133774542040f, -0.1819131996110f, -0.0945741925262f,
            -0.0409655828852f, -0.0141985685725f, -0.00369997467375f
        };

        public static readonly float[][] win =
        {
            new[]
            {
                -1.6141214951e-02f, -5.3603178919e-02f, -1.0070713296e-01f, -1.6280817573e-01f, -4.9999999679e-01f,
                -3.8388735032e-01f, -6.2061144372e-01f, -1.1659756083e+00f, -3.8720752656e+00f, -4.2256286556e+00f,
                -1.5195289984e+00f, -9.7416483388e-01f, -7.3744074053e-01f, -1.2071067773e+00f, -5.1636156596e-01f,
                -4.5426052317e-01f, -4.0715656898e-01f, -3.6969460527e-01f, -3.3876269197e-01f, -3.1242222492e-01f,
                -2.8939587111e-01f, -2.6880081906e-01f, -5.0000000266e-01f, -2.3251417468e-01f, -2.1596714708e-01f,
                -2.0004979098e-01f, -1.8449493497e-01f, -1.6905846094e-01f, -1.5350360518e-01f, -1.3758624925e-01f,
                -1.2103922149e-01f, -2.0710679058e-01f, -8.4752577594e-02f, -6.4157525656e-02f, -4.1131172614e-02f,
                -1.4790705759e-02f
            },
            new[]
            {
                -1.6141214951e-02f, -5.3603178919e-02f, -1.0070713296e-01f, -1.6280817573e-01f, -4.9999999679e-01f,
                -3.8388735032e-01f, -6.2061144372e-01f, -1.1659756083e+00f, -3.8720752656e+00f, -4.2256286556e+00f,
                -1.5195289984e+00f, -9.7416483388e-01f, -7.3744074053e-01f, -1.2071067773e+00f, -5.1636156596e-01f,
                -4.5426052317e-01f, -4.0715656898e-01f, -3.6969460527e-01f, -3.3908542600e-01f, -3.1511810350e-01f,
                -2.9642226150e-01f, -2.8184548650e-01f, -5.4119610000e-01f, -2.6213228100e-01f, -2.5387916537e-01f,
                -2.3296291359e-01f, -1.9852728987e-01f, -1.5233534808e-01f, -9.6496400054e-02f, -3.3423828516e-02f,
                0.0000000000e+00f, 0.0000000000e+00f, 0.0000000000e+00f, 0.0000000000e+00f, 0.0000000000e+00f,
                0.0000000000e+00f
            },
            new[]
            {
                -4.8300800645e-02f, -1.5715656932e-01f, -2.8325045177e-01f, -4.2953747763e-01f, -1.2071067795e+00f,
                -8.2426483178e-01f, -1.1451749106e+00f, -1.7695290101e+00f, -4.5470225061e+00f, -3.4890531002e+00f,
                -7.3296292804e-01f, -1.5076514758e-01f, 0.0000000000e+00f, 0.0000000000e+00f, 0.0000000000e+00f,
                0.0000000000e+00f, 0.0000000000e+00f, 0.0000000000e+00f, 0.0000000000e+00f, 0.0000000000e+00f,
                0.0000000000e+00f, 0.0000000000e+00f, 0.0000000000e+00f, 0.0000000000e+00f, 0.0000000000e+00f,
                0.0000000000e+00f, 0.0000000000e+00f, 0.0000000000e+00f, 0.0000000000e+00f, 0.0000000000e+00f,
                0.0000000000e+00f, 0.0000000000e+00f, 0.0000000000e+00f, 0.0000000000e+00f, 0.0000000000e+00f,
                0.0000000000e+00f
            },
            new[]
            {
                0.0000000000e+00f, 0.0000000000e+00f, 0.0000000000e+00f, 0.0000000000e+00f, 0.0000000000e+00f,
                0.0000000000e+00f, -1.5076513660e-01f, -7.3296291107e-01f, -3.4890530566e+00f, -4.5470224727e+00f,
                -1.7695290031e+00f, -1.1451749092e+00f, -8.3137738100e-01f, -1.3065629650e+00f, -5.4142014250e-01f,
                -4.6528974900e-01f, -4.1066990750e-01f, -3.7004680800e-01f, -3.3876269197e-01f, -3.1242222492e-01f,
                -2.8939587111e-01f, -2.6880081906e-01f, -5.0000000266e-01f, -2.3251417468e-01f, -2.1596714708e-01f,
                -2.0004979098e-01f,
                -1.8449493497e-01f, -1.6905846094e-01f, -1.5350360518e-01f, -1.3758624925e-01f, -1.2103922149e-01f,
                -2.0710679058e-01f, -8.4752577594e-02f, -6.4157525656e-02f, -4.1131172614e-02f, -1.4790705759e-02f
            }
        };

        public static readonly int[][][] nr_of_sfb_block =
        {
            new[] {new[] {6, 5, 5, 5}, new[] {9, 9, 9, 9}, new[] {6, 9, 9, 9}},
            new[] {new[] {6, 5, 7, 3}, new[] {9, 9, 12, 6}, new[] {6, 9, 12, 6}},
            new[] {new[] {11, 10, 0, 0}, new[] {18, 18, 0, 0}, new[] {15, 18, 0, 0}},
            new[] {new[] {7, 7, 7, 0}, new[] {12, 12, 12, 0}, new[] {6, 15, 12, 0}},
            new[] {new[] {6, 6, 6, 3}, new[] {12, 9, 9, 6}, new[] {6, 12, 9, 6}},
            new[] {new[] {8, 8, 5, 0}, new[] {15, 12, 9, 0}, new[] {6, 18, 9, 0}}
        };

        private readonly ABuffer buffer;
        private readonly int channels;
        private readonly SynthesisFilter filter1;
        private readonly SynthesisFilter filter2;
        private readonly int first_channel;
        private readonly Header header;
        private readonly ScaleFactorData[] III_scalefac_t;
        private readonly int[] is_1d;
        private readonly float[][] k;
        private readonly int last_channel;
        private readonly float[][][] lr;
        private readonly Layer3SideInfo m_SideInfo;

        private readonly int max_gr;
        private readonly int[] nonzero;
        private readonly float[] out_1d;
        private readonly float[][] prevblck;
        private readonly float[][][] ro;
        private readonly ScaleFactorData[] scalefac;
        private readonly SBI[] sfBandIndex; // Init in the constructor.
        private readonly int sfreq;
        private readonly Bitstream stream;
        private readonly int which_channels;

        private int CheckSumHuff;
        private int counter;
        private int frame_start;

        internal int[] is_pos;

        internal float[] is_ratio;
        private BitReserve m_BitReserve;

        // MDM: new_slen is fully initialized before use, no need
        // to reallocate array.
        private int[] new_slen;

        private int part2_start;
        internal float[] rawout;

        // subband samples are buffered and passed to the
        // SynthesisFilter in one go.
        private float[] samples1;

        private float[] samples2;
        public int[] scalefac_buffer;
        public ScaleFactorTable sftable;

        // MDM: tsOutCopy and rawout do not need initializing, so the arrays
        // can be reused.
        internal float[] tsOutCopy;

        internal int[] v = {0};
        internal int[] w = {0};

        /// <summary>
        ///     *
        /// </summary>
        internal int[] x = {0};

        internal int[] y = {0};

        static LayerIIIDecoder()
        {
            t_43 = create_t_43();
        }

        /// <summary>
        ///     Constructor.
        ///     REVIEW: these constructor arguments should be moved to the decodeFrame() method.
        /// </summary>
        public LayerIIIDecoder(Bitstream stream, Header header, SynthesisFilter filtera, SynthesisFilter filterb, ABuffer buffer, int whichCh)
        {
            Huffman.Initialize();

            InitBlock();
            is_1d = new int[SBLIMIT * SSLIMIT + 4];
            ro = new float[2][][];

            for (int i = 0; i < 2; i++)
            {
                ro[i] = new float[SBLIMIT][];
                for (int i2 = 0; i2 < SBLIMIT; i2++) ro[i][i2] = new float[SSLIMIT];
            }

            lr = new float[2][][];

            for (int i3 = 0; i3 < 2; i3++)
            {
                lr[i3] = new float[SBLIMIT][];
                for (int i4 = 0; i4 < SBLIMIT; i4++) lr[i3][i4] = new float[SSLIMIT];
            }

            out_1d = new float[SBLIMIT * SSLIMIT];
            prevblck = new float[2][];
            for (int i5 = 0; i5 < 2; i5++) prevblck[i5] = new float[SBLIMIT * SSLIMIT];
            k = new float[2][];
            for (int i6 = 0; i6 < 2; i6++) k[i6] = new float[SBLIMIT * SSLIMIT];
            nonzero = new int[2];

            //III_scalefact_t
            III_scalefac_t = new ScaleFactorData[2];
            III_scalefac_t[0] = new ScaleFactorData();
            III_scalefac_t[1] = new ScaleFactorData();
            scalefac = III_scalefac_t;
            // L3TABLE INIT

            sfBandIndex = new SBI[9]; // SZD: MPEG2.5 +3 indices

            int[] l0 =
            {
                0, 6, 12, 18, 24, 30, 36, 44, 54, 66, 80, 96, 116, 140, 168, 200, 238, 284, 336, 396, 464, 522, 576
            };
            int[] s0 = {0, 4, 8, 12, 18, 24, 32, 42, 56, 74, 100, 132, 174, 192};

            int[] l1 =
            {
                0, 6, 12, 18, 24, 30, 36, 44, 54, 66, 80, 96, 114, 136, 162, 194, 232, 278, 330, 394, 464, 540, 576
            };
            int[] s1 = {0, 4, 8, 12, 18, 26, 36, 48, 62, 80, 104, 136, 180, 192};

            int[] l2 =
            {
                0, 6, 12, 18, 24, 30, 36, 44, 54, 66, 80, 96, 116, 140, 168, 200, 238, 284, 336, 396, 464, 522, 576
            };
            int[] s2 = {0, 4, 8, 12, 18, 26, 36, 48, 62, 80, 104, 134, 174, 192};

            int[] l3 =
            {
                0, 4, 8, 12, 16, 20, 24, 30, 36, 44, 52, 62, 74, 90, 110, 134, 162, 196, 238, 288, 342, 418, 576
            };
            int[] s3 = {0, 4, 8, 12, 16, 22, 30, 40, 52, 66, 84, 106, 136, 192};

            int[] l4 =
            {
                0, 4, 8, 12, 16, 20, 24, 30, 36, 42, 50, 60, 72, 88, 106, 128, 156, 190, 230, 276, 330, 384, 576
            };
            int[] s4 = {0, 4, 8, 12, 16, 22, 28, 38, 50, 64, 80, 100, 126, 192};

            int[] l5 =
            {
                0, 4, 8, 12, 16, 20, 24, 30, 36, 44, 54, 66, 82, 102, 126, 156, 194, 240, 296, 364, 448, 550,
                576
            };
            int[] s5 = {0, 4, 8, 12, 16, 22, 30, 42, 58, 78, 104, 138, 180, 192};

            // SZD: MPEG2.5
            int[] l6 =
            {
                0, 6, 12, 18, 24, 30, 36, 44, 54, 66, 80, 96, 116, 140, 168, 200, 238, 284, 336, 396, 464, 522,
                576
            };
            int[] s6 = {0, 4, 8, 12, 18, 26, 36, 48, 62, 80, 104, 134, 174, 192};

            int[] l7 =
            {
                0, 6, 12, 18, 24, 30, 36, 44, 54, 66, 80, 96, 116, 140, 168, 200, 238, 284, 336, 396, 464, 522,
                576
            };
            int[] s7 = {0, 4, 8, 12, 18, 26, 36, 48, 62, 80, 104, 134, 174, 192};

            int[] l8 =
            {
                0, 12, 24, 36, 48, 60, 72, 88, 108, 132, 160, 192, 232, 280, 336, 400, 476, 566, 568, 570, 572,
                574, 576
            };
            int[] s8 = {0, 8, 16, 24, 36, 52, 72, 96, 124, 160, 162, 164, 166, 192};

            sfBandIndex[0] = new SBI(l0, s0);
            sfBandIndex[1] = new SBI(l1, s1);
            sfBandIndex[2] = new SBI(l2, s2);

            sfBandIndex[3] = new SBI(l3, s3);
            sfBandIndex[4] = new SBI(l4, s4);
            sfBandIndex[5] = new SBI(l5, s5);
            //SZD: MPEG2.5
            sfBandIndex[6] = new SBI(l6, s6);
            sfBandIndex[7] = new SBI(l7, s7);
            sfBandIndex[8] = new SBI(l8, s8);
            // END OF L3TABLE INIT

            if (reorder_table == null)
            {
                // SZD: generate LUT
                reorder_table = new int[9][];

                for (int i = 0; i < 9; i++)
                    reorder_table[i] = Reorder(sfBandIndex[i].s);
            }

            // Sftable
            int[] ll0 = {0, 6, 11, 16, 21};
            int[] ss0 = {0, 6, 12};
            sftable = new ScaleFactorTable(this, ll0, ss0);
            // END OF Sftable

            // scalefac_buffer
            scalefac_buffer = new int[54];
            // END OF scalefac_buffer

            this.stream = stream;
            this.header = header;
            filter1 = filtera;
            filter2 = filterb;
            this.buffer = buffer;
            which_channels = whichCh;

            frame_start = 0;
            channels = this.header.mode() == Header.SINGLE_CHANNEL ? 1 : 2;
            max_gr = this.header.version() == Header.MPEG1 ? 2 : 1;

            sfreq = this.header.sample_frequency() +
                    (this.header.version() == Header.MPEG1 ? 3 : this.header.version() == Header.MPEG25_LSF ? 6 : 0); // SZD

            if (channels == 2)
            {
                switch (which_channels)
                {
                    case (int) OutputChannelsEnum.LEFT_CHANNEL:
                    case (int) OutputChannelsEnum.DOWNMIX_CHANNELS:
                        first_channel = last_channel = 0;

                        break;

                    case (int) OutputChannelsEnum.RIGHT_CHANNEL:
                        first_channel = last_channel = 1;

                        break;

                    case (int) OutputChannelsEnum.BOTH_CHANNELS:
                    default:
                        first_channel = 0;
                        last_channel = 1;

                        break;
                }
            }
            else
                first_channel = last_channel = 0;

            for (int ch = 0; ch < 2; ch++)
            {
                for (int j = 0; j < 576; j++)
                    prevblck[ch][j] = 0.0f;
            }

            nonzero[0] = nonzero[1] = 576;

            m_BitReserve = new BitReserve();
            m_SideInfo = new Layer3SideInfo();
        }

        public void DecodeFrame()
        {
            Decode();
        }

        private void InitBlock()
        {
            rawout = new float[36];
            tsOutCopy = new float[18];
            is_ratio = new float[576];
            is_pos = new int[576];
            new_slen = new int[4];
            samples2 = new float[32];
            samples1 = new float[32];
        }

        /// <summary>
        ///     Notify decoder that a seek is being made.
        /// </summary>
        public void seek_notify()
        {
            frame_start = 0;

            for (int ch = 0; ch < 2; ch++)
            {
                for (int j = 0; j < 576; j++)
                    prevblck[ch][j] = 0.0f;
            }

            m_BitReserve = new BitReserve();
        }

        public void Decode()
        {
            int nSlots = header.slots();
            int flush_main;
            int gr, ch, ss, sb, sb18;
            int main_data_end;
            int bytes_to_discard;
            int i;

            ReadSideInfo();

            for (i = 0; i < nSlots; i++)
                m_BitReserve.hputbuf(stream.GetBitsFromBuffer(8));

            main_data_end = SupportClass.URShift(m_BitReserve.hsstell(), 3); // of previous frame

            if ((flush_main = m_BitReserve.hsstell() & 7) != 0)
            {
                m_BitReserve.ReadBits(8 - flush_main);
                main_data_end++;
            }

            bytes_to_discard = frame_start - main_data_end - m_SideInfo.MainDataBegin;

            frame_start += nSlots;

            if (bytes_to_discard < 0)
                return;

            if (main_data_end > 4096)
            {
                frame_start -= 4096;
                m_BitReserve.RewindStreamBytes(4096);
            }

            for (; bytes_to_discard > 0; bytes_to_discard--)
                m_BitReserve.ReadBits(8);

            for (gr = 0; gr < max_gr; gr++)
            {
                for (ch = 0; ch < channels; ch++)
                {
                    part2_start = m_BitReserve.hsstell();

                    if (header.version() == Header.MPEG1)
                        ReadScaleFactors(ch, gr);
                    // MPEG-2 LSF, SZD: MPEG-2.5 LSF
                    else
                        get_LSF_scale_factors(ch, gr);

                    HuffmanDecode(ch, gr);
                    // System.out.println("CheckSum HuffMan = " + CheckSumHuff);
                    dequantize_sample(ro[ch], ch, gr);
                }

                stereo(gr);

                if (which_channels == OutputChannels.DOWNMIX_CHANNELS && channels > 1)
                    doDownMix();

                for (ch = first_channel; ch <= last_channel; ch++)
                {
                    Reorder(lr[ch], ch, gr);
                    Antialias(ch, gr);
                    //for (int hb = 0;hb<576;hb++) CheckSumOut1d = CheckSumOut1d + out_1d[hb];
                    //System.out.println("CheckSumOut1d = "+CheckSumOut1d);

                    Hybrid(ch, gr);

                    //for (int hb = 0;hb<576;hb++) CheckSumOut1d = CheckSumOut1d + out_1d[hb];
                    //System.out.println("CheckSumOut1d = "+CheckSumOut1d);

                    for (sb18 = 18; sb18 < 576; sb18 += 36)
                        // Frequency inversion
                    {
                        for (ss = 1; ss < SSLIMIT; ss += 2)
                            out_1d[sb18 + ss] = -out_1d[sb18 + ss];
                    }

                    if (ch == 0 || which_channels == OutputChannels.RIGHT_CHANNEL)
                    {
                        for (ss = 0; ss < SSLIMIT; ss++)
                        {
                            // Polyphase synthesis
                            sb = 0;

                            for (sb18 = 0; sb18 < 576; sb18 += 18)
                            {
                                samples1[sb] = out_1d[sb18 + ss];
                                //filter1.input_sample(out_1d[sb18+ss], sb);
                                sb++;
                            }

                            //buffer.appendSamples(0, samples1);
                            //Console.WriteLine("Adding samples right into output buffer");
                            filter1.input_samples(samples1);
                            filter1.calculate_pcm_samples(buffer);
                        }
                    }
                    else
                    {
                        for (ss = 0; ss < SSLIMIT; ss++)
                        {
                            // Polyphase synthesis
                            sb = 0;

                            for (sb18 = 0; sb18 < 576; sb18 += 18)
                            {
                                samples2[sb] = out_1d[sb18 + ss];
                                //filter2.input_sample(out_1d[sb18+ss], sb);
                                sb++;
                            }

                            //buffer.appendSamples(1, samples2);
                            //Console.WriteLine("Adding samples right into output buffer");
                            filter2.input_samples(samples2);
                            filter2.calculate_pcm_samples(buffer);
                        }
                    }
                }

                // channels
            }
            // granule

            // System.out.println("Counter = ................................."+counter);
            //if (counter <  609)
            //{
            counter++;
            buffer.WriteBuffer(1);
            //}
            //else if (counter == 609)
            //{
            //    buffer.close();
            //    counter++;
            //}
            //else
            //{
            //}
        }

        /// <summary>
        ///     Reads the side info from the stream, assuming the entire.
        ///     frame has been read already.
        ///     Mono   : 136 bits (= 17 bytes)
        ///     Stereo : 256 bits (= 32 bytes)
        /// </summary>
        private bool ReadSideInfo()
        {
            int ch, gr;

            if (header.version() == Header.MPEG1)
            {
                m_SideInfo.MainDataBegin = stream.GetBitsFromBuffer(9);

                if (channels == 1)
                    m_SideInfo.PrivateBits = stream.GetBitsFromBuffer(5);
                else
                    m_SideInfo.PrivateBits = stream.GetBitsFromBuffer(3);

                for (ch = 0; ch < channels; ch++)
                {
                    m_SideInfo.Channels[ch].ScaleFactorBits[0] = stream.GetBitsFromBuffer(1);
                    m_SideInfo.Channels[ch].ScaleFactorBits[1] = stream.GetBitsFromBuffer(1);
                    m_SideInfo.Channels[ch].ScaleFactorBits[2] = stream.GetBitsFromBuffer(1);
                    m_SideInfo.Channels[ch].ScaleFactorBits[3] = stream.GetBitsFromBuffer(1);
                }

                for (gr = 0; gr < 2; gr++)
                {
                    for (ch = 0; ch < channels; ch++)
                    {
                        m_SideInfo.Channels[ch].Granules[gr].Part23Length = stream.GetBitsFromBuffer(12);
                        m_SideInfo.Channels[ch].Granules[gr].BigValues = stream.GetBitsFromBuffer(9);
                        m_SideInfo.Channels[ch].Granules[gr].GlobalGain = stream.GetBitsFromBuffer(8);
                        m_SideInfo.Channels[ch].Granules[gr].ScaleFacCompress = stream.GetBitsFromBuffer(4);
                        m_SideInfo.Channels[ch].Granules[gr].WindowSwitchingFlag = stream.GetBitsFromBuffer(1);

                        if (m_SideInfo.Channels[ch].Granules[gr].WindowSwitchingFlag != 0)
                        {
                            m_SideInfo.Channels[ch].Granules[gr].BlockType = stream.GetBitsFromBuffer(2);
                            m_SideInfo.Channels[ch].Granules[gr].MixedBlockFlag = stream.GetBitsFromBuffer(1);

                            m_SideInfo.Channels[ch].Granules[gr].TableSelect[0] = stream.GetBitsFromBuffer(5);
                            m_SideInfo.Channels[ch].Granules[gr].TableSelect[1] = stream.GetBitsFromBuffer(5);

                            m_SideInfo.Channels[ch].Granules[gr].SubblockGain[0] = stream.GetBitsFromBuffer(3);
                            m_SideInfo.Channels[ch].Granules[gr].SubblockGain[1] = stream.GetBitsFromBuffer(3);
                            m_SideInfo.Channels[ch].Granules[gr].SubblockGain[2] = stream.GetBitsFromBuffer(3);

                            // Set region_count parameters since they are implicit in this case.

                            if (m_SideInfo.Channels[ch].Granules[gr].BlockType == 0)
                            {
                                // Side info bad: block_type == 0 in split block
                                return false;
                            }

                            if (m_SideInfo.Channels[ch].Granules[gr].BlockType == 2 && m_SideInfo.Channels[ch].Granules[gr].MixedBlockFlag == 0)
                                m_SideInfo.Channels[ch].Granules[gr].Region0Count = 8;
                            else
                                m_SideInfo.Channels[ch].Granules[gr].Region0Count = 7;
                            m_SideInfo.Channels[ch].Granules[gr].Region1Count = 20 - m_SideInfo.Channels[ch].Granules[gr].Region0Count;
                        }
                        else
                        {
                            m_SideInfo.Channels[ch].Granules[gr].TableSelect[0] = stream.GetBitsFromBuffer(5);
                            m_SideInfo.Channels[ch].Granules[gr].TableSelect[1] = stream.GetBitsFromBuffer(5);
                            m_SideInfo.Channels[ch].Granules[gr].TableSelect[2] = stream.GetBitsFromBuffer(5);
                            m_SideInfo.Channels[ch].Granules[gr].Region0Count = stream.GetBitsFromBuffer(4);
                            m_SideInfo.Channels[ch].Granules[gr].Region1Count = stream.GetBitsFromBuffer(3);
                            m_SideInfo.Channels[ch].Granules[gr].BlockType = 0;
                        }

                        m_SideInfo.Channels[ch].Granules[gr].Preflag = stream.GetBitsFromBuffer(1);
                        m_SideInfo.Channels[ch].Granules[gr].ScaleFacScale = stream.GetBitsFromBuffer(1);
                        m_SideInfo.Channels[ch].Granules[gr].Count1TableSelect = stream.GetBitsFromBuffer(1);
                    }
                }
            }
            else
            {
                // MPEG-2 LSF, SZD: MPEG-2.5 LSF

                m_SideInfo.MainDataBegin = stream.GetBitsFromBuffer(8);

                if (channels == 1)
                    m_SideInfo.PrivateBits = stream.GetBitsFromBuffer(1);
                else
                    m_SideInfo.PrivateBits = stream.GetBitsFromBuffer(2);

                for (ch = 0; ch < channels; ch++)
                {
                    m_SideInfo.Channels[ch].Granules[0].Part23Length = stream.GetBitsFromBuffer(12);
                    m_SideInfo.Channels[ch].Granules[0].BigValues = stream.GetBitsFromBuffer(9);
                    m_SideInfo.Channels[ch].Granules[0].GlobalGain = stream.GetBitsFromBuffer(8);
                    m_SideInfo.Channels[ch].Granules[0].ScaleFacCompress = stream.GetBitsFromBuffer(9);
                    m_SideInfo.Channels[ch].Granules[0].WindowSwitchingFlag = stream.GetBitsFromBuffer(1);

                    if (m_SideInfo.Channels[ch].Granules[0].WindowSwitchingFlag != 0)
                    {
                        m_SideInfo.Channels[ch].Granules[0].BlockType = stream.GetBitsFromBuffer(2);
                        m_SideInfo.Channels[ch].Granules[0].MixedBlockFlag = stream.GetBitsFromBuffer(1);
                        m_SideInfo.Channels[ch].Granules[0].TableSelect[0] = stream.GetBitsFromBuffer(5);
                        m_SideInfo.Channels[ch].Granules[0].TableSelect[1] = stream.GetBitsFromBuffer(5);

                        m_SideInfo.Channels[ch].Granules[0].SubblockGain[0] = stream.GetBitsFromBuffer(3);
                        m_SideInfo.Channels[ch].Granules[0].SubblockGain[1] = stream.GetBitsFromBuffer(3);
                        m_SideInfo.Channels[ch].Granules[0].SubblockGain[2] = stream.GetBitsFromBuffer(3);

                        // Set region_count parameters since they are implicit in this case.

                        if (m_SideInfo.Channels[ch].Granules[0].BlockType == 0)
                        {
                            // Side info bad: block_type == 0 in split block
                            return false;
                        }

                        if (m_SideInfo.Channels[ch].Granules[0].BlockType == 2 && m_SideInfo.Channels[ch].Granules[0].MixedBlockFlag == 0)
                            m_SideInfo.Channels[ch].Granules[0].Region0Count = 8;
                        else
                        {
                            m_SideInfo.Channels[ch].Granules[0].Region0Count = 7;
                            m_SideInfo.Channels[ch].Granules[0].Region1Count = 20 - m_SideInfo.Channels[ch].Granules[0].Region0Count;
                        }
                    }
                    else
                    {
                        m_SideInfo.Channels[ch].Granules[0].TableSelect[0] = stream.GetBitsFromBuffer(5);
                        m_SideInfo.Channels[ch].Granules[0].TableSelect[1] = stream.GetBitsFromBuffer(5);
                        m_SideInfo.Channels[ch].Granules[0].TableSelect[2] = stream.GetBitsFromBuffer(5);
                        m_SideInfo.Channels[ch].Granules[0].Region0Count = stream.GetBitsFromBuffer(4);
                        m_SideInfo.Channels[ch].Granules[0].Region1Count = stream.GetBitsFromBuffer(3);
                        m_SideInfo.Channels[ch].Granules[0].BlockType = 0;
                    }

                    m_SideInfo.Channels[ch].Granules[0].ScaleFacScale = stream.GetBitsFromBuffer(1);
                    m_SideInfo.Channels[ch].Granules[0].Count1TableSelect = stream.GetBitsFromBuffer(1);
                }

                // for(ch=0; ch<channels; ch++)
            }

            // if (header.version() == MPEG1)
            return true;
        }

        /// <summary>
        ///     *
        /// </summary>
        private void ReadScaleFactors(int ch, int gr)
        {
            int sfb, window;
            GranuleInfo gr_info = m_SideInfo.Channels[ch].Granules[gr];
            int scale_comp = gr_info.ScaleFacCompress;
            int length0 = slen[0][scale_comp];
            int length1 = slen[1][scale_comp];

            if (gr_info.WindowSwitchingFlag != 0 && gr_info.BlockType == 2)
            {
                if (gr_info.MixedBlockFlag != 0)
                {
                    // MIXED
                    for (sfb = 0; sfb < 8; sfb++)
                        scalefac[ch].l[sfb] = m_BitReserve.ReadBits(slen[0][gr_info.ScaleFacCompress]);

                    for (sfb = 3; sfb < 6; sfb++)
                    {
                        for (window = 0; window < 3; window++)
                            scalefac[ch].s[window][sfb] = m_BitReserve.ReadBits(slen[0][gr_info.ScaleFacCompress]);
                    }

                    for (sfb = 6; sfb < 12; sfb++)
                    {
                        for (window = 0; window < 3; window++)
                            scalefac[ch].s[window][sfb] = m_BitReserve.ReadBits(slen[1][gr_info.ScaleFacCompress]);
                    }

                    for (sfb = 12, window = 0; window < 3; window++)
                        scalefac[ch].s[window][sfb] = 0;
                }
                else
                {
                    // SHORT

                    scalefac[ch].s[0][0] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].s[1][0] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].s[2][0] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].s[0][1] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].s[1][1] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].s[2][1] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].s[0][2] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].s[1][2] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].s[2][2] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].s[0][3] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].s[1][3] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].s[2][3] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].s[0][4] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].s[1][4] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].s[2][4] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].s[0][5] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].s[1][5] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].s[2][5] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].s[0][6] = m_BitReserve.ReadBits(length1);
                    scalefac[ch].s[1][6] = m_BitReserve.ReadBits(length1);
                    scalefac[ch].s[2][6] = m_BitReserve.ReadBits(length1);
                    scalefac[ch].s[0][7] = m_BitReserve.ReadBits(length1);
                    scalefac[ch].s[1][7] = m_BitReserve.ReadBits(length1);
                    scalefac[ch].s[2][7] = m_BitReserve.ReadBits(length1);
                    scalefac[ch].s[0][8] = m_BitReserve.ReadBits(length1);
                    scalefac[ch].s[1][8] = m_BitReserve.ReadBits(length1);
                    scalefac[ch].s[2][8] = m_BitReserve.ReadBits(length1);
                    scalefac[ch].s[0][9] = m_BitReserve.ReadBits(length1);
                    scalefac[ch].s[1][9] = m_BitReserve.ReadBits(length1);
                    scalefac[ch].s[2][9] = m_BitReserve.ReadBits(length1);
                    scalefac[ch].s[0][10] = m_BitReserve.ReadBits(length1);
                    scalefac[ch].s[1][10] = m_BitReserve.ReadBits(length1);
                    scalefac[ch].s[2][10] = m_BitReserve.ReadBits(length1);
                    scalefac[ch].s[0][11] = m_BitReserve.ReadBits(length1);
                    scalefac[ch].s[1][11] = m_BitReserve.ReadBits(length1);
                    scalefac[ch].s[2][11] = m_BitReserve.ReadBits(length1);
                    scalefac[ch].s[0][12] = 0;
                    scalefac[ch].s[1][12] = 0;
                    scalefac[ch].s[2][12] = 0;
                }

                // SHORT
            }
            else
            {
                // LONG types 0,1,3

                if (m_SideInfo.Channels[ch].ScaleFactorBits[0] == 0 || gr == 0)
                {
                    scalefac[ch].l[0] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].l[1] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].l[2] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].l[3] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].l[4] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].l[5] = m_BitReserve.ReadBits(length0);
                }

                if (m_SideInfo.Channels[ch].ScaleFactorBits[1] == 0 || gr == 0)
                {
                    scalefac[ch].l[6] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].l[7] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].l[8] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].l[9] = m_BitReserve.ReadBits(length0);
                    scalefac[ch].l[10] = m_BitReserve.ReadBits(length0);
                }

                if (m_SideInfo.Channels[ch].ScaleFactorBits[2] == 0 || gr == 0)
                {
                    scalefac[ch].l[11] = m_BitReserve.ReadBits(length1);
                    scalefac[ch].l[12] = m_BitReserve.ReadBits(length1);
                    scalefac[ch].l[13] = m_BitReserve.ReadBits(length1);
                    scalefac[ch].l[14] = m_BitReserve.ReadBits(length1);
                    scalefac[ch].l[15] = m_BitReserve.ReadBits(length1);
                }

                if (m_SideInfo.Channels[ch].ScaleFactorBits[3] == 0 || gr == 0)
                {
                    scalefac[ch].l[16] = m_BitReserve.ReadBits(length1);
                    scalefac[ch].l[17] = m_BitReserve.ReadBits(length1);
                    scalefac[ch].l[18] = m_BitReserve.ReadBits(length1);
                    scalefac[ch].l[19] = m_BitReserve.ReadBits(length1);
                    scalefac[ch].l[20] = m_BitReserve.ReadBits(length1);
                }

                scalefac[ch].l[21] = 0;
                scalefac[ch].l[22] = 0;
            }
        }

        private void get_LSF_scale_data(int ch, int gr)
        {
            int scalefac_comp, int_scalefac_comp;
            int mode_ext = header.mode_extension();
            int m;
            int blocktypenumber;
            int blocknumber = 0;

            GranuleInfo grInfo = m_SideInfo.Channels[ch].Granules[gr];

            scalefac_comp = grInfo.ScaleFacCompress;

            if (grInfo.BlockType == 2)
            {
                if (grInfo.MixedBlockFlag == 0)
                    blocktypenumber = 1;
                else if (grInfo.MixedBlockFlag == 1)
                    blocktypenumber = 2;
                else
                    blocktypenumber = 0;
            }
            else
                blocktypenumber = 0;

            if (!((mode_ext == 1 || mode_ext == 3) && ch == 1))
            {
                if (scalefac_comp < 400)
                {
                    new_slen[0] = SupportClass.URShift(scalefac_comp, 4) / 5;
                    new_slen[1] = SupportClass.URShift(scalefac_comp, 4) % 5;
                    new_slen[2] = SupportClass.URShift(scalefac_comp & 0xF, 2);
                    new_slen[3] = scalefac_comp & 3;
                    m_SideInfo.Channels[ch].Granules[gr].Preflag = 0;
                    blocknumber = 0;
                }
                else if (scalefac_comp < 500)
                {
                    new_slen[0] = SupportClass.URShift(scalefac_comp - 400, 2) / 5;
                    new_slen[1] = SupportClass.URShift(scalefac_comp - 400, 2) % 5;
                    new_slen[2] = (scalefac_comp - 400) & 3;
                    new_slen[3] = 0;
                    m_SideInfo.Channels[ch].Granules[gr].Preflag = 0;
                    blocknumber = 1;
                }
                else if (scalefac_comp < 512)
                {
                    new_slen[0] = (scalefac_comp - 500) / 3;
                    new_slen[1] = (scalefac_comp - 500) % 3;
                    new_slen[2] = 0;
                    new_slen[3] = 0;
                    m_SideInfo.Channels[ch].Granules[gr].Preflag = 1;
                    blocknumber = 2;
                }
            }

            if ((mode_ext == 1 || mode_ext == 3) && ch == 1)
            {
                int_scalefac_comp = SupportClass.URShift(scalefac_comp, 1);

                if (int_scalefac_comp < 180)
                {
                    new_slen[0] = int_scalefac_comp / 36;
                    new_slen[1] = int_scalefac_comp % 36 / 6;
                    new_slen[2] = int_scalefac_comp % 36 % 6;
                    new_slen[3] = 0;
                    m_SideInfo.Channels[ch].Granules[gr].Preflag = 0;
                    blocknumber = 3;
                }
                else if (int_scalefac_comp < 244)
                {
                    new_slen[0] = SupportClass.URShift((int_scalefac_comp - 180) & 0x3F, 4);
                    new_slen[1] = SupportClass.URShift((int_scalefac_comp - 180) & 0xF, 2);
                    new_slen[2] = (int_scalefac_comp - 180) & 3;
                    new_slen[3] = 0;
                    m_SideInfo.Channels[ch].Granules[gr].Preflag = 0;
                    blocknumber = 4;
                }
                else if (int_scalefac_comp < 255)
                {
                    new_slen[0] = (int_scalefac_comp - 244) / 3;
                    new_slen[1] = (int_scalefac_comp - 244) % 3;
                    new_slen[2] = 0;
                    new_slen[3] = 0;
                    m_SideInfo.Channels[ch].Granules[gr].Preflag = 0;
                    blocknumber = 5;
                }
            }

            for (int x = 0; x < 45; x++)
                // why 45, not 54?
                scalefac_buffer[x] = 0;

            m = 0;

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < nr_of_sfb_block[blocknumber][blocktypenumber][i]; j++)
                {
                    scalefac_buffer[m] = new_slen[i] == 0 ? 0 : m_BitReserve.ReadBits(new_slen[i]);
                    m++;
                }

                // for (unint32 j ...
            }

            // for (uint32 i ...
        }

        /// <summary>
        ///     *
        /// </summary>
        private void get_LSF_scale_factors(int ch, int gr)
        {
            int m = 0;
            int sfb;
            GranuleInfo grInfo = m_SideInfo.Channels[ch].Granules[gr];

            get_LSF_scale_data(ch, gr);

            if (grInfo.WindowSwitchingFlag != 0 && grInfo.BlockType == 2)
            {
                int window;

                if (grInfo.MixedBlockFlag != 0)
                {
                    // MIXED
                    for (sfb = 0; sfb < 8; sfb++)
                    {
                        scalefac[ch].l[sfb] = scalefac_buffer[m];
                        m++;
                    }

                    for (sfb = 3; sfb < 12; sfb++)
                    {
                        for (window = 0; window < 3; window++)
                        {
                            scalefac[ch].s[window][sfb] = scalefac_buffer[m];
                            m++;
                        }
                    }

                    for (window = 0; window < 3; window++)
                        scalefac[ch].s[window][12] = 0;
                }
                else
                {
                    // SHORT

                    for (sfb = 0; sfb < 12; sfb++)
                    {
                        for (window = 0; window < 3; window++)
                        {
                            scalefac[ch].s[window][sfb] = scalefac_buffer[m];
                            m++;
                        }
                    }

                    for (window = 0; window < 3; window++)
                        scalefac[ch].s[window][12] = 0;
                }
            }
            else
            {
                // LONG types 0,1,3

                for (sfb = 0; sfb < 21; sfb++)
                {
                    scalefac[ch].l[sfb] = scalefac_buffer[m];
                    m++;
                }

                scalefac[ch].l[21] = 0; // Jeff
                scalefac[ch].l[22] = 0;
            }
        }

        private void HuffmanDecode(int ch, int gr)
        {
            x[0] = 0;
            y[0] = 0;
            v[0] = 0;
            w[0] = 0;

            int part2_3_end = part2_start + m_SideInfo.Channels[ch].Granules[gr].Part23Length;
            int num_bits;
            int region1Start;
            int region2Start;
            int index;

            int buf, buf1;

            Huffman h;

            // Find region boundary for short block case

            if (m_SideInfo.Channels[ch].Granules[gr].WindowSwitchingFlag != 0 && m_SideInfo.Channels[ch].Granules[gr].BlockType == 2)
            {
                // Region2.
                //MS: Extrahandling for 8KHZ
                region1Start = sfreq == 8 ? 72 : 36; // sfb[9/3]*3=36 or in case 8KHZ = 72
                region2Start = 576; // No Region2 for short block case
            }
            else
            {
                // Find region boundary for long block case

                buf = m_SideInfo.Channels[ch].Granules[gr].Region0Count + 1;
                buf1 = buf + m_SideInfo.Channels[ch].Granules[gr].Region1Count + 1;

                if (buf1 > sfBandIndex[sfreq].l.Length - 1)
                    buf1 = sfBandIndex[sfreq].l.Length - 1;

                region1Start = sfBandIndex[sfreq].l[buf];
                region2Start = sfBandIndex[sfreq].l[buf1]; /* MI */
            }

            index = 0;

            // Read bigvalues area
            for (int i = 0; i < m_SideInfo.Channels[ch].Granules[gr].BigValues << 1; i += 2)
            {
                if (i < region1Start)
                    h = Huffman.ht[m_SideInfo.Channels[ch].Granules[gr].TableSelect[0]];
                else if (i < region2Start)
                    h = Huffman.ht[m_SideInfo.Channels[ch].Granules[gr].TableSelect[1]];
                else
                    h = Huffman.ht[m_SideInfo.Channels[ch].Granules[gr].TableSelect[2]];

                Huffman.Decode(h, x, y, v, w, m_BitReserve);

                is_1d[index++] = x[0];
                is_1d[index++] = y[0];
                CheckSumHuff = CheckSumHuff + x[0] + y[0];
                // System.out.println("x = "+x[0]+" y = "+y[0]);
            }

            // Read count1 area
            h = Huffman.ht[m_SideInfo.Channels[ch].Granules[gr].Count1TableSelect + 32];
            num_bits = m_BitReserve.hsstell();

            while (num_bits < part2_3_end && index < 576)
            {
                Huffman.Decode(h, x, y, v, w, m_BitReserve);

                is_1d[index++] = v[0];
                is_1d[index++] = w[0];
                is_1d[index++] = x[0];
                is_1d[index++] = y[0];
                CheckSumHuff = CheckSumHuff + v[0] + w[0] + x[0] + y[0];
                // System.out.println("v = "+v[0]+" w = "+w[0]);
                // System.out.println("x = "+x[0]+" y = "+y[0]);
                num_bits = m_BitReserve.hsstell();
            }

            if (num_bits > part2_3_end)
            {
                m_BitReserve.RewindStreamBits(num_bits - part2_3_end);
                index -= 4;
            }

            num_bits = m_BitReserve.hsstell();

            // Dismiss stuffing bits
            if (num_bits < part2_3_end)
                m_BitReserve.ReadBits(part2_3_end - num_bits);

            // Zero out rest

            if (index < 576)
                nonzero[ch] = index;
            else
                nonzero[ch] = 576;

            if (index < 0)
                index = 0;

            // may not be necessary
            for (; index < 576; index++)
                is_1d[index] = 0;
        }

        /// <summary>
        ///     *
        /// </summary>
        private void i_stereo_k_values(int is_pos, int io_type, int i)
        {
            if (is_pos == 0)
            {
                k[0][i] = 1.0f;
                k[1][i] = 1.0f;
            }
            else if ((is_pos & 1) != 0)
            {
                k[0][i] = io[io_type][SupportClass.URShift(is_pos + 1, 1)];
                k[1][i] = 1.0f;
            }
            else
            {
                k[0][i] = 1.0f;
                k[1][i] = io[io_type][SupportClass.URShift(is_pos, 1)];
            }
        }

        /// <summary>
        ///     *
        /// </summary>
        private void dequantize_sample(float[][] xr, int ch, int gr)
        {
            GranuleInfo gr_info = m_SideInfo.Channels[ch].Granules[gr];
            int cb = 0;
            int next_cb_boundary;
            int cb_begin = 0;
            int cb_width = 0;
            int index = 0, t_index, j;
            float g_gain;
            float[][] xr_1d = xr;

            // choose correct scalefactor band per block type, initalize boundary

            if (gr_info.WindowSwitchingFlag != 0 && gr_info.BlockType == 2)
            {
                if (gr_info.MixedBlockFlag != 0)
                    next_cb_boundary = sfBandIndex[sfreq].l[1];
                // LONG blocks: 0,1,3
                else
                {
                    cb_width = sfBandIndex[sfreq].s[1];
                    next_cb_boundary = (cb_width << 2) - cb_width;
                    cb_begin = 0;
                }
            }
            else
                next_cb_boundary = sfBandIndex[sfreq].l[1]; // LONG blocks: 0,1,3

            // Compute overall (global) scaling.

            g_gain = (float) Math.Pow(2.0, 0.25 * (gr_info.GlobalGain - 210.0));

            for (j = 0; j < nonzero[ch]; j++)
            {
                // Modif E.B 02/22/99
                int reste = j % SSLIMIT;
                int quotien = (j - reste) / SSLIMIT;

                if (is_1d[j] == 0)
                    xr_1d[quotien][reste] = 0.0f;
                else
                {
                    int abv = is_1d[j];

                    double d43 = (4.0 / 3.0);
                    if (abv < t_43.Length)
                    {
                        if (is_1d[j] > 0)
                        {
                            xr_1d[quotien][reste] = g_gain * t_43[abv];
                        }
                        else if (-abv < t_43.Length)
                        {
                            xr_1d[quotien][reste] = -g_gain * t_43[-abv];
                        }
                        else
                        {
                            xr_1d[quotien][reste] = -g_gain * (float) Math.Pow(-abv, d43);
                        }
                    }
                    else if (is_1d[j] > 0)
                    {
                        xr_1d[quotien][reste] = g_gain * (float) Math.Pow(abv, d43);
                    }
                    else
                    {
                        xr_1d[quotien][reste] = -g_gain * (float) Math.Pow(-abv, d43);
                    }
                }
            }

            // apply formula per block type

            for (j = 0; j < nonzero[ch]; j++)
            {
                // Modif E.B 02/22/99
                int reste = j % SSLIMIT;
                int quotien = (j - reste) / SSLIMIT;

                if (index == next_cb_boundary)
                {
                    /* Adjust critical band boundary */
                    if (gr_info.WindowSwitchingFlag != 0 && gr_info.BlockType == 2)
                    {
                        if (gr_info.MixedBlockFlag != 0)
                        {
                            if (index == sfBandIndex[sfreq].l[8])
                            {
                                next_cb_boundary = sfBandIndex[sfreq].s[4];
                                next_cb_boundary = (next_cb_boundary << 2) - next_cb_boundary;
                                cb = 3;
                                cb_width = sfBandIndex[sfreq].s[4] - sfBandIndex[sfreq].s[3];

                                cb_begin = sfBandIndex[sfreq].s[3];
                                cb_begin = (cb_begin << 2) - cb_begin;
                            }
                            else if (index < sfBandIndex[sfreq].l[8])
                                next_cb_boundary = sfBandIndex[sfreq].l[++cb + 1];
                            else
                            {
                                next_cb_boundary = sfBandIndex[sfreq].s[++cb + 1];
                                next_cb_boundary = (next_cb_boundary << 2) - next_cb_boundary;

                                cb_begin = sfBandIndex[sfreq].s[cb];
                                cb_width = sfBandIndex[sfreq].s[cb + 1] - cb_begin;
                                cb_begin = (cb_begin << 2) - cb_begin;
                            }
                        }
                        else
                        {
                            next_cb_boundary = sfBandIndex[sfreq].s[++cb + 1];
                            next_cb_boundary = (next_cb_boundary << 2) - next_cb_boundary;

                            cb_begin = sfBandIndex[sfreq].s[cb];
                            cb_width = sfBandIndex[sfreq].s[cb + 1] - cb_begin;
                            cb_begin = (cb_begin << 2) - cb_begin;
                        }
                    }
                    else
                    {
                        // long blocks

                        next_cb_boundary = sfBandIndex[sfreq].l[++cb + 1];
                    }
                }

                // Do long/short dependent scaling operations

                if (gr_info.WindowSwitchingFlag != 0 &&
                    (gr_info.BlockType == 2 && gr_info.MixedBlockFlag == 0 ||
                     gr_info.BlockType == 2 && gr_info.MixedBlockFlag != 0 && j >= 36))
                {
                    t_index = (index - cb_begin) / cb_width;
                    /*            xr[sb][ss] *= pow(2.0, ((-2.0 * gr_info.subblock_gain[t_index])
                    -(0.5 * (1.0 + gr_info.scalefac_scale)
                    * scalefac[ch].s[t_index][cb]))); */
                    int idx = scalefac[ch].s[t_index][cb] << gr_info.ScaleFacScale;
                    idx += gr_info.SubblockGain[t_index] << 2;

                    xr_1d[quotien][reste] *= two_to_negative_half_pow[idx];
                }
                else
                {
                    // LONG block types 0,1,3 & 1st 2 subbands of switched blocks
                    /* xr[sb][ss] *= pow(2.0, -0.5 * (1.0+gr_info.scalefac_scale)
                    * (scalefac[ch].l[cb]
                    + gr_info.preflag * pretab[cb])); */
                    int idx = scalefac[ch].l[cb];

                    if (gr_info.Preflag != 0)
                        idx += pretab[cb];

                    idx = idx << gr_info.ScaleFacScale;
                    xr_1d[quotien][reste] *= two_to_negative_half_pow[idx];
                }

                index++;
            }

            for (j = nonzero[ch]; j < 576; j++)
            {
                // Modif E.B 02/22/99
                int reste = j % SSLIMIT;
                int quotien = (j - reste) / SSLIMIT;

                if (reste < 0)
                    reste = 0;

                if (quotien < 0)
                    quotien = 0;
                xr_1d[quotien][reste] = 0.0f;
            }
        }

        /// <summary>
        ///     *
        /// </summary>
        private void Reorder(float[][] xr, int ch, int gr)
        {
            GranuleInfo gr_info = m_SideInfo.Channels[ch].Granules[gr];
            int freq, freq3;
            int index;
            int sfb, sfb_start, sfb_lines;
            int src_line, des_line;
            float[][] xr_1d = xr;

            if (gr_info.WindowSwitchingFlag != 0 && gr_info.BlockType == 2)
            {
                for (index = 0; index < 576; index++)
                    out_1d[index] = 0.0f;

                if (gr_info.MixedBlockFlag != 0)
                {
                    // NO REORDER FOR LOW 2 SUBBANDS
                    for (index = 0; index < 36; index++)
                    {
                        // Modif E.B 02/22/99
                        int reste = index % SSLIMIT;
                        int quotien = (index - reste) / SSLIMIT;
                        out_1d[index] = xr_1d[quotien][reste];
                    }

                    // REORDERING FOR REST SWITCHED SHORT
                    for (sfb = 3, sfb_start = sfBandIndex[sfreq].s[3], sfb_lines = sfBandIndex[sfreq].s[4] - sfb_start;
                         sfb < 13;
                         sfb++, sfb_start = sfBandIndex[sfreq].s[sfb],
                         sfb_lines = sfBandIndex[sfreq].s[sfb + 1] - sfb_start)
                    {
                        int sfb_start3 = (sfb_start << 2) - sfb_start;

                        for (freq = 0, freq3 = 0; freq < sfb_lines; freq++, freq3 += 3)
                        {
                            src_line = sfb_start3 + freq;
                            des_line = sfb_start3 + freq3;
                            // Modif E.B 02/22/99
                            int reste = src_line % SSLIMIT;
                            int quotien = (src_line - reste) / SSLIMIT;

                            out_1d[des_line] = xr_1d[quotien][reste];
                            src_line += sfb_lines;
                            des_line++;

                            reste = src_line % SSLIMIT;
                            quotien = (src_line - reste) / SSLIMIT;

                            out_1d[des_line] = xr_1d[quotien][reste];
                            src_line += sfb_lines;
                            des_line++;

                            reste = src_line % SSLIMIT;
                            quotien = (src_line - reste) / SSLIMIT;

                            out_1d[des_line] = xr_1d[quotien][reste];
                        }
                    }
                }
                else
                {
                    // pure short
                    for (index = 0; index < 576; index++)
                    {
                        int j = reorder_table[sfreq][index];
                        int reste = j % SSLIMIT;
                        int quotien = (j - reste) / SSLIMIT;
                        out_1d[index] = xr_1d[quotien][reste];
                    }
                }
            }
            else
            {
                // long blocks
                for (index = 0; index < 576; index++)
                {
                    // Modif E.B 02/22/99
                    int reste = index % SSLIMIT;
                    int quotien = (index - reste) / SSLIMIT;
                    out_1d[index] = xr_1d[quotien][reste];
                }
            }
        }

        private void stereo(int gr)
        {
            int sb, ss;

            if (channels == 1)
            {
                // mono , bypass xr[0][][] to lr[0][][]

                for (sb = 0; sb < SBLIMIT; sb++)
                {
                    for (ss = 0; ss < SSLIMIT; ss += 3)
                    {
                        lr[0][sb][ss] = ro[0][sb][ss];
                        lr[0][sb][ss + 1] = ro[0][sb][ss + 1];
                        lr[0][sb][ss + 2] = ro[0][sb][ss + 2];
                    }
                }
            }
            else
            {
                GranuleInfo gr_info = m_SideInfo.Channels[0].Granules[gr];
                int mode_ext = header.mode_extension();
                int sfb;
                int i;
                int lines, temp, temp2;

                bool ms_stereo = header.mode() == Header.JOINT_STEREO && (mode_ext & 0x2) != 0;
                bool i_stereo = header.mode() == Header.JOINT_STEREO && (mode_ext & 0x1) != 0;
                bool lsf = header.version() == Header.MPEG2_LSF || header.version() == Header.MPEG25_LSF; // SZD

                int io_type = gr_info.ScaleFacCompress & 1;

                // initialization

                for (i = 0; i < 576; i++)
                {
                    is_pos[i] = 7;

                    is_ratio[i] = 0.0f;
                }

                if (i_stereo)
                {
                    if (gr_info.WindowSwitchingFlag != 0 && gr_info.BlockType == 2)
                    {
                        if (gr_info.MixedBlockFlag != 0)
                        {
                            int max_sfb = 0;

                            for (int j = 0; j < 3; j++)
                            {
                                int sfbcnt;
                                sfbcnt = 2;

                                for (sfb = 12; sfb >= 3; sfb--)
                                {
                                    i = sfBandIndex[sfreq].s[sfb];
                                    lines = sfBandIndex[sfreq].s[sfb + 1] - i;
                                    i = (i << 2) - i + (j + 1) * lines - 1;

                                    while (lines > 0)
                                    {
                                        if (ro[1][i / 18][i % 18] != 0.0f)
                                        {
                                            // MDM: in java, array access is very slow.
                                            // Is quicker to compute div and mod values.
                                            //if (ro[1][ss_div[i]][ss_mod[i]] != 0.0f) {
                                            sfbcnt = sfb;
                                            sfb = -10;
                                            lines = -10;
                                        }

                                        lines--;
                                        i--;
                                    } // while (lines > 0)
                                }

                                // for (sfb=12 ...
                                sfb = sfbcnt + 1;

                                if (sfb > max_sfb)
                                    max_sfb = sfb;

                                while (sfb < 12)
                                {
                                    temp = sfBandIndex[sfreq].s[sfb];
                                    sb = sfBandIndex[sfreq].s[sfb + 1] - temp;
                                    i = (temp << 2) - temp + j * sb;

                                    for (; sb > 0; sb--)
                                    {
                                        is_pos[i] = scalefac[1].s[j][sfb];

                                        if (is_pos[i] != 7)
                                        {
                                            if (lsf)
                                                i_stereo_k_values(is_pos[i], io_type, i);
                                            else
                                                is_ratio[i] = TAN12[is_pos[i]];
                                        }

                                        i++;
                                    }

                                    // for (; sb>0...
                                    sfb++;
                                } // while (sfb < 12)

                                sfb = sfBandIndex[sfreq].s[10];
                                sb = sfBandIndex[sfreq].s[11] - sfb;
                                sfb = (sfb << 2) - sfb + j * sb;
                                temp = sfBandIndex[sfreq].s[11];
                                sb = sfBandIndex[sfreq].s[12] - temp;
                                i = (temp << 2) - temp + j * sb;

                                for (; sb > 0; sb--)
                                {
                                    is_pos[i] = is_pos[sfb];

                                    if (lsf)
                                    {
                                        k[0][i] = k[0][sfb];
                                        k[1][i] = k[1][sfb];
                                    }
                                    else
                                        is_ratio[i] = is_ratio[sfb];

                                    i++;
                                }

                                // for (; sb > 0 ...
                            }

                            if (max_sfb <= 3)
                            {
                                i = 2;
                                ss = 17;
                                sb = -1;

                                while (i >= 0)
                                {
                                    if (ro[1][i][ss] != 0.0f)
                                    {
                                        sb = (i << 4) + (i << 1) + ss;
                                        i = -1;
                                    }
                                    else
                                    {
                                        ss--;

                                        if (ss < 0)
                                        {
                                            i--;
                                            ss = 17;
                                        }
                                    }

                                    // if (ro ...
                                } // while (i>=0)

                                i = 0;

                                while (sfBandIndex[sfreq].l[i] <= sb)
                                    i++;
                                sfb = i;
                                i = sfBandIndex[sfreq].l[i];

                                for (; sfb < 8; sfb++)
                                {
                                    sb = sfBandIndex[sfreq].l[sfb + 1] - sfBandIndex[sfreq].l[sfb];

                                    for (; sb > 0; sb--)
                                    {
                                        is_pos[i] = scalefac[1].l[sfb];

                                        if (is_pos[i] != 7)
                                        {
                                            if (lsf)
                                                i_stereo_k_values(is_pos[i], io_type, i);
                                            else
                                                is_ratio[i] = TAN12[is_pos[i]];
                                        }

                                        i++;
                                    }

                                    // for (; sb>0 ...
                                }

                                // for (; sfb<8 ...
                            }

                            // for (j=0 ...
                        }
                        else
                        {
                            // if (gr_info.mixed_block_flag)
                            for (int j = 0; j < 3; j++)
                            {
                                int sfbcnt;
                                sfbcnt = -1;

                                for (sfb = 12; sfb >= 0; sfb--)
                                {
                                    temp = sfBandIndex[sfreq].s[sfb];
                                    lines = sfBandIndex[sfreq].s[sfb + 1] - temp;
                                    i = (temp << 2) - temp + (j + 1) * lines - 1;

                                    while (lines > 0)
                                    {
                                        if (ro[1][i / 18][i % 18] != 0.0f)
                                        {
                                            // MDM: in java, array access is very slow.
                                            // Is quicker to compute div and mod values.
                                            //if (ro[1][ss_div[i]][ss_mod[i]] != 0.0f) {
                                            sfbcnt = sfb;
                                            sfb = -10;
                                            lines = -10;
                                        }

                                        lines--;
                                        i--;
                                    } // while (lines > 0) */
                                }

                                // for (sfb=12 ...
                                sfb = sfbcnt + 1;

                                while (sfb < 12)
                                {
                                    temp = sfBandIndex[sfreq].s[sfb];
                                    sb = sfBandIndex[sfreq].s[sfb + 1] - temp;
                                    i = (temp << 2) - temp + j * sb;

                                    for (; sb > 0; sb--)
                                    {
                                        is_pos[i] = scalefac[1].s[j][sfb];

                                        if (is_pos[i] != 7)
                                        {
                                            if (lsf)
                                                i_stereo_k_values(is_pos[i], io_type, i);
                                            else
                                                is_ratio[i] = TAN12[is_pos[i]];
                                        }

                                        i++;
                                    }

                                    // for (; sb>0 ...
                                    sfb++;
                                } // while (sfb<12)

                                temp = sfBandIndex[sfreq].s[10];
                                temp2 = sfBandIndex[sfreq].s[11];
                                sb = temp2 - temp;
                                sfb = (temp << 2) - temp + j * sb;
                                sb = sfBandIndex[sfreq].s[12] - temp2;
                                i = (temp2 << 2) - temp2 + j * sb;

                                for (; sb > 0; sb--)
                                {
                                    is_pos[i] = is_pos[sfb];

                                    if (lsf)
                                    {
                                        k[0][i] = k[0][sfb];
                                        k[1][i] = k[1][sfb];
                                    }
                                    else
                                        is_ratio[i] = is_ratio[sfb];

                                    i++;
                                }

                                // for (; sb>0 ...
                            }

                            // for (sfb=12
                        }

                        // for (j=0 ...
                    }
                    else
                    {
                        // if (gr_info.window_switching_flag ...
                        i = 31;
                        ss = 17;
                        sb = 0;

                        while (i >= 0)
                        {
                            if (ro[1][i][ss] != 0.0f)
                            {
                                sb = (i << 4) + (i << 1) + ss;
                                i = -1;
                            }
                            else
                            {
                                ss--;

                                if (ss < 0)
                                {
                                    i--;
                                    ss = 17;
                                }
                            }
                        }

                        i = 0;

                        while (sfBandIndex[sfreq].l[i] <= sb)
                            i++;

                        sfb = i;
                        i = sfBandIndex[sfreq].l[i];

                        for (; sfb < 21; sfb++)
                        {
                            sb = sfBandIndex[sfreq].l[sfb + 1] - sfBandIndex[sfreq].l[sfb];

                            for (; sb > 0; sb--)
                            {
                                is_pos[i] = scalefac[1].l[sfb];

                                if (is_pos[i] != 7)
                                {
                                    if (lsf)
                                        i_stereo_k_values(is_pos[i], io_type, i);
                                    else
                                        is_ratio[i] = TAN12[is_pos[i]];
                                }

                                i++;
                            }
                        }

                        sfb = sfBandIndex[sfreq].l[20];

                        for (sb = 576 - sfBandIndex[sfreq].l[21]; sb > 0 && i < 576; sb--)
                        {
                            is_pos[i] = is_pos[sfb]; // error here : i >=576

                            if (lsf)
                            {
                                k[0][i] = k[0][sfb];
                                k[1][i] = k[1][sfb];
                            }
                            else
                                is_ratio[i] = is_ratio[sfb];

                            i++;
                        }

                        // if (gr_info.mixed_block_flag)
                    }

                    // if (gr_info.window_switching_flag ...
                }
                // if (i_stereo)

                i = 0;

                for (sb = 0; sb < SBLIMIT; sb++)
                {
                    for (ss = 0; ss < SSLIMIT; ss++)
                    {
                        if (is_pos[i] == 7)
                        {
                            if (ms_stereo)
                            {
                                lr[0][sb][ss] = (ro[0][sb][ss] + ro[1][sb][ss]) * 0.707106781f;
                                lr[1][sb][ss] = (ro[0][sb][ss] - ro[1][sb][ss]) * 0.707106781f;
                            }
                            else
                            {
                                lr[0][sb][ss] = ro[0][sb][ss];
                                lr[1][sb][ss] = ro[1][sb][ss];
                            }
                        }
                        else if (i_stereo)
                        {
                            if (lsf)
                            {
                                lr[0][sb][ss] = ro[0][sb][ss] * k[0][i];
                                lr[1][sb][ss] = ro[0][sb][ss] * k[1][i];
                            }
                            else
                            {
                                lr[1][sb][ss] = ro[0][sb][ss] / (1 + is_ratio[i]);
                                lr[0][sb][ss] = lr[1][sb][ss] * is_ratio[i];
                            }
                        }

                        /* else {
                        System.out.println("Error in stereo processing\n");
                        } */
                        i++;
                    }
                }
            }

            // channels == 2
        }

        /// <summary>
        ///     *
        /// </summary>
        private void Antialias(int ch, int gr)
        {
            int sb18, ss, sb18lim;
            GranuleInfo gr_info = m_SideInfo.Channels[ch].Granules[gr];
            // 31 alias-reduction operations between each pair of sub-bands
            // with 8 butterflies between each pair

            if (gr_info.WindowSwitchingFlag != 0 && gr_info.BlockType == 2 && !(gr_info.MixedBlockFlag != 0))
                return;

            if (gr_info.WindowSwitchingFlag != 0 && gr_info.MixedBlockFlag != 0 && gr_info.BlockType == 2)
                sb18lim = 18;
            else
                sb18lim = 558;

            for (sb18 = 0; sb18 < sb18lim; sb18 += 18)
            {
                for (ss = 0; ss < 8; ss++)
                {
                    int src_idx1 = sb18 + 17 - ss;
                    int src_idx2 = sb18 + 18 + ss;
                    float bu = out_1d[src_idx1];
                    float bd = out_1d[src_idx2];
                    out_1d[src_idx1] = bu * cs[ss] - bd * ca[ss];
                    out_1d[src_idx2] = bd * cs[ss] + bu * ca[ss];
                }
            }
        }

        private void Hybrid(int ch, int gr)
        {
            int bt;
            int sb18;
            GranuleInfo gr_info = m_SideInfo.Channels[ch].Granules[gr];
            float[] tsOut;

            float[][] prvblk;

            for (sb18 = 0; sb18 < 576; sb18 += 18)
            {
                bt = gr_info.WindowSwitchingFlag != 0 && gr_info.MixedBlockFlag != 0 && sb18 < 36
                         ? 0
                         : gr_info.BlockType;

                tsOut = out_1d;

                // Modif E.B 02/22/99
                for (int cc = 0; cc < 18; cc++)
                    tsOutCopy[cc] = tsOut[cc + sb18];

                InverseMDCT(tsOutCopy, rawout, bt);

                for (int cc = 0; cc < 18; cc++)
                    tsOut[cc + sb18] = tsOutCopy[cc];
                // Fin Modif

                // overlap addition
                prvblk = prevblck;

                tsOut[0 + sb18] = rawout[0] + prvblk[ch][sb18 + 0];
                prvblk[ch][sb18 + 0] = rawout[18];
                tsOut[1 + sb18] = rawout[1] + prvblk[ch][sb18 + 1];
                prvblk[ch][sb18 + 1] = rawout[19];
                tsOut[2 + sb18] = rawout[2] + prvblk[ch][sb18 + 2];
                prvblk[ch][sb18 + 2] = rawout[20];
                tsOut[3 + sb18] = rawout[3] + prvblk[ch][sb18 + 3];
                prvblk[ch][sb18 + 3] = rawout[21];
                tsOut[4 + sb18] = rawout[4] + prvblk[ch][sb18 + 4];
                prvblk[ch][sb18 + 4] = rawout[22];
                tsOut[5 + sb18] = rawout[5] + prvblk[ch][sb18 + 5];
                prvblk[ch][sb18 + 5] = rawout[23];
                tsOut[6 + sb18] = rawout[6] + prvblk[ch][sb18 + 6];
                prvblk[ch][sb18 + 6] = rawout[24];
                tsOut[7 + sb18] = rawout[7] + prvblk[ch][sb18 + 7];
                prvblk[ch][sb18 + 7] = rawout[25];
                tsOut[8 + sb18] = rawout[8] + prvblk[ch][sb18 + 8];
                prvblk[ch][sb18 + 8] = rawout[26];
                tsOut[9 + sb18] = rawout[9] + prvblk[ch][sb18 + 9];
                prvblk[ch][sb18 + 9] = rawout[27];
                tsOut[10 + sb18] = rawout[10] + prvblk[ch][sb18 + 10];
                prvblk[ch][sb18 + 10] = rawout[28];
                tsOut[11 + sb18] = rawout[11] + prvblk[ch][sb18 + 11];
                prvblk[ch][sb18 + 11] = rawout[29];
                tsOut[12 + sb18] = rawout[12] + prvblk[ch][sb18 + 12];
                prvblk[ch][sb18 + 12] = rawout[30];
                tsOut[13 + sb18] = rawout[13] + prvblk[ch][sb18 + 13];
                prvblk[ch][sb18 + 13] = rawout[31];
                tsOut[14 + sb18] = rawout[14] + prvblk[ch][sb18 + 14];
                prvblk[ch][sb18 + 14] = rawout[32];
                tsOut[15 + sb18] = rawout[15] + prvblk[ch][sb18 + 15];
                prvblk[ch][sb18 + 15] = rawout[33];
                tsOut[16 + sb18] = rawout[16] + prvblk[ch][sb18 + 16];
                prvblk[ch][sb18 + 16] = rawout[34];
                tsOut[17 + sb18] = rawout[17] + prvblk[ch][sb18 + 17];
                prvblk[ch][sb18 + 17] = rawout[35];
            }
        }

        /// <summary>
        ///     *
        /// </summary>
        private void doDownMix()
        {
            for (int sb = 0; sb < SSLIMIT; sb++)
            {
                for (int ss = 0; ss < SSLIMIT; ss += 3)
                {
                    lr[0][sb][ss] = (lr[0][sb][ss] + lr[1][sb][ss]) * 0.5f;
                    lr[0][sb][ss + 1] = (lr[0][sb][ss + 1] + lr[1][sb][ss + 1]) * 0.5f;
                    lr[0][sb][ss + 2] = (lr[0][sb][ss + 2] + lr[1][sb][ss + 2]) * 0.5f;
                }
            }
        }

        /// <summary>
        ///     Fast Inverse Modified discrete cosine transform.
        /// </summary>
        public void InverseMDCT(float[] inValues, float[] outValues, int blockType)
        {
            float tmpf_0, tmpf_1, tmpf_2, tmpf_3, tmpf_4, tmpf_5, tmpf_6, tmpf_7, tmpf_8, tmpf_9;
            float tmpf_10, tmpf_11, tmpf_12, tmpf_13, tmpf_14, tmpf_15, tmpf_16, tmpf_17;

            tmpf_0 = tmpf_1 = tmpf_2 = tmpf_3 = tmpf_4 = tmpf_5 = tmpf_6 = tmpf_7 =
                                                                               tmpf_8 = tmpf_9 = tmpf_10 = tmpf_11 = tmpf_12 = tmpf_13 = tmpf_14 = tmpf_15 =
                                                                                                                                                       tmpf_16 = tmpf_17 = 0.0f;

            if (blockType == 2)
            {
                /*
                *
                * Under MicrosoftVM 2922, This causes a GPF, or
                * At best, an ArrayIndexOutOfBoundsExceptin.
                for(int p=0;p<36;p+=9)
                {
                out[p]   = out[p+1] = out[p+2] = out[p+3] =
                out[p+4] = out[p+5] = out[p+6] = out[p+7] =
                out[p+8] = 0.0f;
                }
                */
                outValues[0] = 0.0f;
                outValues[1] = 0.0f;
                outValues[2] = 0.0f;
                outValues[3] = 0.0f;
                outValues[4] = 0.0f;
                outValues[5] = 0.0f;
                outValues[6] = 0.0f;
                outValues[7] = 0.0f;
                outValues[8] = 0.0f;
                outValues[9] = 0.0f;
                outValues[10] = 0.0f;
                outValues[11] = 0.0f;
                outValues[12] = 0.0f;
                outValues[13] = 0.0f;
                outValues[14] = 0.0f;
                outValues[15] = 0.0f;
                outValues[16] = 0.0f;
                outValues[17] = 0.0f;
                outValues[18] = 0.0f;
                outValues[19] = 0.0f;
                outValues[20] = 0.0f;
                outValues[21] = 0.0f;
                outValues[22] = 0.0f;
                outValues[23] = 0.0f;
                outValues[24] = 0.0f;
                outValues[25] = 0.0f;
                outValues[26] = 0.0f;
                outValues[27] = 0.0f;
                outValues[28] = 0.0f;
                outValues[29] = 0.0f;
                outValues[30] = 0.0f;
                outValues[31] = 0.0f;
                outValues[32] = 0.0f;
                outValues[33] = 0.0f;
                outValues[34] = 0.0f;
                outValues[35] = 0.0f;

                int six_i = 0;

                int i;

                for (i = 0; i < 3; i++)
                {
                    // 12 point IMDCT
                    // Begin 12 point IDCT
                    // Input aliasing for 12 pt IDCT
                    inValues[15 + i] += inValues[12 + i];
                    inValues[12 + i] += inValues[9 + i];
                    inValues[9 + i] += inValues[6 + i];
                    inValues[6 + i] += inValues[3 + i];
                    inValues[3 + i] += inValues[0 + i];

                    // Input aliasing on odd indices (for 6 point IDCT)
                    inValues[15 + i] += inValues[9 + i];
                    inValues[9 + i] += inValues[3 + i];

                    // 3 point IDCT on even indices
                    float pp1, pp2, sum;
                    pp2 = inValues[12 + i] * 0.500000000f;
                    pp1 = inValues[6 + i] * 0.866025403f;
                    sum = inValues[0 + i] + pp2;
                    tmpf_1 = inValues[0 + i] - inValues[12 + i];
                    tmpf_0 = sum + pp1;
                    tmpf_2 = sum - pp1;

                    // End 3 point IDCT on even indices
                    // 3 point IDCT on odd indices (for 6 point IDCT)
                    pp2 = inValues[15 + i] * 0.500000000f;
                    pp1 = inValues[9 + i] * 0.866025403f;
                    sum = inValues[3 + i] + pp2;
                    tmpf_4 = inValues[3 + i] - inValues[15 + i];
                    tmpf_5 = sum + pp1;
                    tmpf_3 = sum - pp1;
                    // End 3 point IDCT on odd indices
                    // Twiddle factors on odd indices (for 6 point IDCT)

                    tmpf_3 *= 1.931851653f;
                    tmpf_4 *= 0.707106781f;
                    tmpf_5 *= 0.517638090f;

                    // Output butterflies on 2 3 point IDCT's (for 6 point IDCT)
                    float save = tmpf_0;
                    tmpf_0 += tmpf_5;
                    tmpf_5 = save - tmpf_5;
                    save = tmpf_1;
                    tmpf_1 += tmpf_4;
                    tmpf_4 = save - tmpf_4;
                    save = tmpf_2;
                    tmpf_2 += tmpf_3;
                    tmpf_3 = save - tmpf_3;

                    // End 6 point IDCT
                    // Twiddle factors on indices (for 12 point IDCT)

                    tmpf_0 *= 0.504314480f;
                    tmpf_1 *= 0.541196100f;
                    tmpf_2 *= 0.630236207f;
                    tmpf_3 *= 0.821339815f;
                    tmpf_4 *= 1.306562965f;
                    tmpf_5 *= 3.830648788f;

                    // End 12 point IDCT

                    // Shift to 12 point modified IDCT, multiply by window type 2
                    tmpf_8 = -tmpf_0 * 0.793353340f;
                    tmpf_9 = -tmpf_0 * 0.608761429f;
                    tmpf_7 = -tmpf_1 * 0.923879532f;
                    tmpf_10 = -tmpf_1 * 0.382683432f;
                    tmpf_6 = -tmpf_2 * 0.991444861f;
                    tmpf_11 = -tmpf_2 * 0.130526192f;

                    tmpf_0 = tmpf_3;
                    tmpf_1 = tmpf_4 * 0.382683432f;
                    tmpf_2 = tmpf_5 * 0.608761429f;

                    tmpf_3 = -tmpf_5 * 0.793353340f;
                    tmpf_4 = -tmpf_4 * 0.923879532f;
                    tmpf_5 = -tmpf_0 * 0.991444861f;

                    tmpf_0 *= 0.130526192f;

                    outValues[six_i + 6] += tmpf_0;
                    outValues[six_i + 7] += tmpf_1;
                    outValues[six_i + 8] += tmpf_2;
                    outValues[six_i + 9] += tmpf_3;
                    outValues[six_i + 10] += tmpf_4;
                    outValues[six_i + 11] += tmpf_5;
                    outValues[six_i + 12] += tmpf_6;
                    outValues[six_i + 13] += tmpf_7;
                    outValues[six_i + 14] += tmpf_8;
                    outValues[six_i + 15] += tmpf_9;
                    outValues[six_i + 16] += tmpf_10;
                    outValues[six_i + 17] += tmpf_11;

                    six_i += 6;
                }
            }
            else
            {
                // 36 point IDCT
                // input aliasing for 36 point IDCT
                inValues[17] += inValues[16];
                inValues[16] += inValues[15];
                inValues[15] += inValues[14];
                inValues[14] += inValues[13];
                inValues[13] += inValues[12];
                inValues[12] += inValues[11];
                inValues[11] += inValues[10];
                inValues[10] += inValues[9];
                inValues[9] += inValues[8];
                inValues[8] += inValues[7];
                inValues[7] += inValues[6];
                inValues[6] += inValues[5];
                inValues[5] += inValues[4];
                inValues[4] += inValues[3];
                inValues[3] += inValues[2];
                inValues[2] += inValues[1];
                inValues[1] += inValues[0];

                // 18 point IDCT for odd indices
                // input aliasing for 18 point IDCT
                inValues[17] += inValues[15];
                inValues[15] += inValues[13];
                inValues[13] += inValues[11];
                inValues[11] += inValues[9];
                inValues[9] += inValues[7];
                inValues[7] += inValues[5];
                inValues[5] += inValues[3];
                inValues[3] += inValues[1];

                float tmp0, tmp1, tmp2, tmp3, tmp4, tmp0_, tmp1_, tmp2_, tmp3_;
                float tmp0o, tmp1o, tmp2o, tmp3o, tmp4o, tmp0_o, tmp1_o, tmp2_o, tmp3_o;

                // Fast 9 Point Inverse Discrete Cosine Transform
                //
                // By  Francois-Raymond Boyer
                //         mailto:boyerf@iro.umontreal.ca
                //         http://www.iro.umontreal.ca/~boyerf
                //
                // The code has been optimized for Intel processors
                //  (takes a lot of time to convert float to and from iternal FPU representation)
                //
                // It is a simple "factorization" of the IDCT matrix.

                // 9 point IDCT on even indices

                // 5 points on odd indices (not realy an IDCT)
                float i00 = inValues[0] + inValues[0];
                float iip12 = i00 + inValues[12];

                tmp0 = iip12 + inValues[4] * 1.8793852415718f + inValues[8] * 1.532088886238f +
                       inValues[16] * 0.34729635533386f;
                tmp1 = i00 + inValues[4] - inValues[8] - inValues[12] - inValues[12] - inValues[16];

                tmp2 = iip12 - inValues[4] * 0.34729635533386f - inValues[8] * 1.8793852415718f +
                       inValues[16] * 1.532088886238f;

                tmp3 = iip12 - inValues[4] * 1.532088886238f + inValues[8] * 0.34729635533386f -
                       inValues[16] * 1.8793852415718f;
                tmp4 = inValues[0] - inValues[4] + inValues[8] - inValues[12] + inValues[16];

                // 4 points on even indices
                float i66_ = inValues[6] * 1.732050808f; // Sqrt[3]

                tmp0_ = inValues[2] * 1.9696155060244f + i66_ + inValues[10] * 1.2855752193731f +
                        inValues[14] * 0.68404028665134f;
                tmp1_ = (inValues[2] - inValues[10] - inValues[14]) * 1.732050808f;

                tmp2_ = inValues[2] * 1.2855752193731f - i66_ - inValues[10] * 0.68404028665134f +
                        inValues[14] * 1.9696155060244f;

                tmp3_ = inValues[2] * 0.68404028665134f - i66_ + inValues[10] * 1.9696155060244f -
                        inValues[14] * 1.2855752193731f;

                // 9 point IDCT on odd indices
                // 5 points on odd indices (not realy an IDCT)
                float i0 = inValues[0 + 1] + inValues[0 + 1];
                float i0p12 = i0 + inValues[12 + 1];

                tmp0o = i0p12 + inValues[4 + 1] * 1.8793852415718f + inValues[8 + 1] * 1.532088886238f +
                        inValues[16 + 1] * 0.34729635533386f;

                tmp1o = i0 + inValues[4 + 1] - inValues[8 + 1] - inValues[12 + 1] - inValues[12 + 1] -
                        inValues[16 + 1];

                tmp2o = i0p12 - inValues[4 + 1] * 0.34729635533386f - inValues[8 + 1] * 1.8793852415718f +
                        inValues[16 + 1] * 1.532088886238f;

                tmp3o = i0p12 - inValues[4 + 1] * 1.532088886238f + inValues[8 + 1] * 0.34729635533386f -
                        inValues[16 + 1] * 1.8793852415718f;

                tmp4o = (inValues[0 + 1] - inValues[4 + 1] + inValues[8 + 1] - inValues[12 + 1] +
                         inValues[16 + 1]) * 0.707106781f; // Twiddled

                // 4 points on even indices
                float i6_ = inValues[6 + 1] * 1.732050808f; // Sqrt[3]

                tmp0_o = inValues[2 + 1] * 1.9696155060244f + i6_ + inValues[10 + 1] * 1.2855752193731f +
                         inValues[14 + 1] * 0.68404028665134f;
                tmp1_o = (inValues[2 + 1] - inValues[10 + 1] - inValues[14 + 1]) * 1.732050808f;

                tmp2_o = inValues[2 + 1] * 1.2855752193731f - i6_ - inValues[10 + 1] * 0.68404028665134f +
                         inValues[14 + 1] * 1.9696155060244f;

                tmp3_o = inValues[2 + 1] * 0.68404028665134f - i6_ + inValues[10 + 1] * 1.9696155060244f -
                         inValues[14 + 1] * 1.2855752193731f;

                // Twiddle factors on odd indices
                // and
                // Butterflies on 9 point IDCT's
                // and
                // twiddle factors for 36 point IDCT

                float e, o;
                e = tmp0 + tmp0_;
                o = (tmp0o + tmp0_o) * 0.501909918f;
                tmpf_0 = e + o;
                tmpf_17 = e - o;
                e = tmp1 + tmp1_;
                o = (tmp1o + tmp1_o) * 0.517638090f;
                tmpf_1 = e + o;
                tmpf_16 = e - o;
                e = tmp2 + tmp2_;
                o = (tmp2o + tmp2_o) * 0.551688959f;
                tmpf_2 = e + o;
                tmpf_15 = e - o;
                e = tmp3 + tmp3_;
                o = (tmp3o + tmp3_o) * 0.610387294f;
                tmpf_3 = e + o;
                tmpf_14 = e - o;
                tmpf_4 = tmp4 + tmp4o;
                tmpf_13 = tmp4 - tmp4o;
                e = tmp3 - tmp3_;
                o = (tmp3o - tmp3_o) * 0.871723397f;
                tmpf_5 = e + o;
                tmpf_12 = e - o;
                e = tmp2 - tmp2_;
                o = (tmp2o - tmp2_o) * 1.183100792f;
                tmpf_6 = e + o;
                tmpf_11 = e - o;
                e = tmp1 - tmp1_;
                o = (tmp1o - tmp1_o) * 1.931851653f;
                tmpf_7 = e + o;
                tmpf_10 = e - o;
                e = tmp0 - tmp0_;
                o = (tmp0o - tmp0_o) * 5.736856623f;
                tmpf_8 = e + o;
                tmpf_9 = e - o;

                // end 36 point IDCT */
                // shift to modified IDCT
                float[] win_bt = win[blockType];

                outValues[0] = -tmpf_9 * win_bt[0];
                outValues[1] = -tmpf_10 * win_bt[1];
                outValues[2] = -tmpf_11 * win_bt[2];
                outValues[3] = -tmpf_12 * win_bt[3];
                outValues[4] = -tmpf_13 * win_bt[4];
                outValues[5] = -tmpf_14 * win_bt[5];
                outValues[6] = -tmpf_15 * win_bt[6];
                outValues[7] = -tmpf_16 * win_bt[7];
                outValues[8] = -tmpf_17 * win_bt[8];
                outValues[9] = tmpf_17 * win_bt[9];
                outValues[10] = tmpf_16 * win_bt[10];
                outValues[11] = tmpf_15 * win_bt[11];
                outValues[12] = tmpf_14 * win_bt[12];
                outValues[13] = tmpf_13 * win_bt[13];
                outValues[14] = tmpf_12 * win_bt[14];
                outValues[15] = tmpf_11 * win_bt[15];
                outValues[16] = tmpf_10 * win_bt[16];
                outValues[17] = tmpf_9 * win_bt[17];
                outValues[18] = tmpf_8 * win_bt[18];
                outValues[19] = tmpf_7 * win_bt[19];
                outValues[20] = tmpf_6 * win_bt[20];
                outValues[21] = tmpf_5 * win_bt[21];
                outValues[22] = tmpf_4 * win_bt[22];
                outValues[23] = tmpf_3 * win_bt[23];
                outValues[24] = tmpf_2 * win_bt[24];
                outValues[25] = tmpf_1 * win_bt[25];
                outValues[26] = tmpf_0 * win_bt[26];
                outValues[27] = tmpf_0 * win_bt[27];
                outValues[28] = tmpf_1 * win_bt[28];
                outValues[29] = tmpf_2 * win_bt[29];
                outValues[30] = tmpf_3 * win_bt[30];
                outValues[31] = tmpf_4 * win_bt[31];
                outValues[32] = tmpf_5 * win_bt[32];
                outValues[33] = tmpf_6 * win_bt[33];
                outValues[34] = tmpf_7 * win_bt[34];
                outValues[35] = tmpf_8 * win_bt[35];
            }
        }

        private static float[] create_t_43()
        {
            float[] t43 = new float[8192];
            double d43 = 4.0 / 3.0;

            for (int i = 0; i < 8192; i++) t43[i] = (float) Math.Pow(i, d43);

            return t43;
        }

        internal static int[] Reorder(int[] scalefac_band)
        {
            // SZD: converted from LAME
            int j = 0;
            int[] ix = new int[576];

            for (int sfb = 0; sfb < 13; sfb++)
            {
                int start = scalefac_band[sfb];
                int end = scalefac_band[sfb + 1];

                for (int window = 0; window < 3; window++)
                {
                    for (int i = start; i < end; i++)
                        ix[3 * i + window] = j++;
                }
            }

            return ix;
        }
    }
}