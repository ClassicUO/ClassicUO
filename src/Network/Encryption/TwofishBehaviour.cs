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

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace ClassicUO.Network.Encryption
{
    internal class TwofishEncryption : TwofishBase, ICryptoTransform
    {
        private byte[] _cipher_table, _xor_data;
        private ushort _rect_pos;
        private byte _send_pos;

        // not worked out this property yet - placing break points here just don't get caught.

        // I normally set this to false when block encrypting so that I can work on one block at a time
        // but for compression and stream type ciphers this can be set to true so that you get all the data

        private EncryptionDirection encryptionDirection;


        // need to have this method due to IDisposable - just can't think of a reason to use it for in this class
        public void Dispose()
        {
        }


        /// <summary>
        ///     Transform a block depending on whether we are encrypting or decrypting
        /// </summary>
        /// <param name="inputBuffer"></param>
        /// <param name="inputOffset"></param>
        /// <param name="inputCount"></param>
        /// <param name="outputBuffer"></param>
        /// <param name="outputOffset"></param>
        /// <returns></returns>
        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            Span<uint> x = stackalloc uint[4];

            // load it up
            for (int i = 0; i < 4; i++)
            {
                x[i] = (uint) (inputBuffer[i * 4 + 3 + inputOffset] << 24) | (uint) (inputBuffer[i * 4 + 2 + inputOffset] << 16) | (uint) (inputBuffer[i * 4 + 1 + inputOffset] << 8) | inputBuffer[i * 4 + 0 + inputOffset];
            }

            if (encryptionDirection == EncryptionDirection.Encrypting)
            {
                blockEncrypt(ref x);
            }
            else
            {
                blockDecrypt(ref x);
            }


            // load it up
            for (int i = 0; i < 4; i++)
            {
                outputBuffer[i * 4 + 0 + outputOffset] = b0(x[i]);
                outputBuffer[i * 4 + 1 + outputOffset] = b1(x[i]);
                outputBuffer[i * 4 + 2 + outputOffset] = b2(x[i]);
                outputBuffer[i * 4 + 3 + outputOffset] = b3(x[i]);
            }


            return inputCount;
        }

        public unsafe byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            byte[] outputBuffer; // = new byte[0];

            if (inputCount > 0)
            {
                outputBuffer = new byte[16]; // blocksize
                Span<uint> x = stackalloc uint[4];

                // load it up
                for (int i = 0; i < 4; i++) // should be okay as we have already said to pad with zeros
                {
                    x[i] = (uint) (inputBuffer[i * 4 + 3 + inputOffset] << 24) | (uint) (inputBuffer[i * 4 + 2 + inputOffset] << 16) | (uint) (inputBuffer[i * 4 + 1 + inputOffset] << 8) | inputBuffer[i * 4 + 0 + inputOffset];
                }

                if (encryptionDirection == EncryptionDirection.Encrypting)
                {
                    blockEncrypt(ref x);
                }
                else
                {
                    blockDecrypt(ref x);
                }

                // load it up
                for (int i = 0; i < 4; i++)
                {
                    outputBuffer[i * 4 + 0] = b0(x[i]);
                    outputBuffer[i * 4 + 1] = b1(x[i]);
                    outputBuffer[i * 4 + 2] = b2(x[i]);
                    outputBuffer[i * 4 + 3] = b3(x[i]);
                }
            }
            else
            {
                outputBuffer = new byte[0]; // the .NET framework doesn't like it if you return null - this calms it down
            }

            return outputBuffer;
        }

        public bool CanReuseTransform { get; } = true;

        public bool CanTransformMultipleBlocks { get; } = false;

        public int InputBlockSize => inputBlockSize;

        public int OutputBlockSize => outputBlockSize;


        public unsafe void Initialize(uint seed, bool use_md5)
        {
            int keyLen = 128;
            _cipher_table = new byte[0x100];
            Span<byte> key = stackalloc byte[16];
            key[0] = key[4] = key[8] = key[12] = (byte) ((seed >> 24) & 0xff);
            key[1] = key[5] = key[9] = key[13] = (byte) ((seed >> 16) & 0xff);
            key[2] = key[6] = key[10] = key[14] = (byte) ((seed >> 8) & 0xff);
            key[3] = key[7] = key[11] = key[15] = (byte) (seed & 0xff);

            byte[] iv = new byte[0];


            // convert our key into an array of ints
            for (int i = 0; i < key.Length / 4; i++)
            {
                Key[i] = (uint) (key[i * 4 + 3] << 24) | (uint) (key[i * 4 + 2] << 16) | (uint) (key[i * 4 + 1] << 8) | key[i * 4 + 0];
            }

            cipherMode = CipherMode.ECB;

            // we only need to convert our IV if we are using CBC
            if (cipherMode == CipherMode.CBC)
            {
                for (int i = 0; i < 4; i++)
                {
                    IV[i] = (uint) (iv[i * 4 + 3] << 24) | (uint) (iv[i * 4 + 2] << 16) | (uint) (iv[i * 4 + 1] << 8) | iv[i * 4 + 0];
                }
            }

            encryptionDirection = EncryptionDirection.Decrypting;
            reKey(keyLen, ref Key);

            for (int i = 0; i < 256; ++i)
            {
                _cipher_table[i] = (byte) i;
            }


            _send_pos = 0;

            refreshCipherTable();

            if (use_md5)
            {
                MD5 md5 = new MD5CryptoServiceProvider();
                _xor_data = md5.ComputeHash(_cipher_table, 0, 256);
                md5.Dispose();
            }
        }

        public void Encrypt(ref byte[] src, ref byte[] dst, int size)
        {
            for (int i = 0; i < size; ++i)
            {
                // Recalculate table
                if (_rect_pos >= 0x100)
                {
                    refreshCipherTable();
                }

                // Simple XOR operation
                dst[i] = (byte) (src[i] ^ _cipher_table[_rect_pos++]);
            }
        }

        public void Decrypt(ref byte[] src, ref byte[] dst, int size)
        {
            for (int i = 0; i < size; ++i)
            {
                dst[i] = (byte) (src[i] ^ _xor_data[_send_pos]);
                _send_pos++;
                _send_pos &= 0x0F; // Maximum Value is 0xF = 15, then 0xF + 1 = 0 again
            }
        }

        private void refreshCipherTable()
        {
            Span<uint> cache = stackalloc uint[4];
            Span<byte> table = _cipher_table;

            for (int i = 0; i < 256; i += 16)
            {
                table.Slice(i, 16).CopyTo(MemoryMarshal.AsBytes(cache));

                blockEncrypt(ref cache);

                MemoryMarshal.AsBytes(cache).CopyTo(table.Slice(i, 16));
            }

            _rect_pos = 0;
        }
    }

    /// <summary>
    ///     Summary description for TwofishBase.
    /// </summary>
    internal class TwofishBase
    {
        public enum EncryptionDirection
        {
            Encrypting,
            Decrypting
        }

        private int keyLength;

        private readonly int[] numRounds = { 0, ROUNDS_128, ROUNDS_192, ROUNDS_256 };
        private int rounds;

        protected CipherMode cipherMode = CipherMode.ECB;

        protected int inputBlockSize = BLOCK_SIZE / 8;
        protected uint[] IV = { 0, 0, 0, 0 };              // this should be one block size
        protected uint[] Key = { 0, 0, 0, 0, 0, 0, 0, 0 }; //new int[MAX_KEY_BITS/32];
        protected int outputBlockSize = BLOCK_SIZE / 8;

        protected uint[] sboxKeys = new uint[MAX_KEY_BITS / 64]; /* key bits used for S-boxes */
        protected uint[] subKeys = new uint[TOTAL_SUBKEYS];      /* round subkeys, input/output whitening bits */

        /*
        +*****************************************************************************
        *
        * Function Name:	f32
        *
        * Function:			Run four bytes through keyed S-boxes and apply MDS matrix
        *
        * Arguments:		x			=	input to f function
        *					k32			=	pointer to key dwords
        *					keyLen		=	total key length (k32 --> keyLey/2 bits)
        *
        * Return:			The output of the keyed permutation applied to x.
        *
        * Notes:
        *	This function is a keyed 32-bit permutation.  It is the major building
        *	block for the Twofish round function, including the four keyed 8x8 
        *	permutations and the 4x4 MDS matrix multiply.  This function is used
        *	both for generating round subkeys and within the round function on the
        *	block being encrypted.  
        *
        *	This version is fairly slow and pedagogical, although a smartcard would
        *	probably perform the operation exactly this way in firmware.   For
        *	ultimate performance, the entire operation can be completed with four
        *	lookups into four 256x32-bit tables, with three dword xors.
        *
        *	The MDS matrix is defined in TABLE.H.  To multiply by Mij, just use the
        *	macro Mij(x).
        *
        -****************************************************************************/
        private static unsafe uint f32(uint x, ref uint[] k32, int keyLen)
        {
            byte* b = stackalloc byte[4];
            b[0] = b0(x);
            b[1] = b1(x);
            b[2] = b2(x);
            b[3] = b3(x);

            /* Run each byte thru 8x8 S-boxes, xoring with key byte at each stage. */
            /* Note that each byte goes through a different combination of S-boxes.*/

            //*((DWORD *)b) = Bswap(x);	/* make b[0] = LSB, b[3] = MSB */
            switch (((keyLen + 63) / 64) & 3)
            {
                case 0: /* 256 bits of key */
                    b[0] = (byte) (P8x8[P_04, b[0]] ^ b0(k32[3]));
                    b[1] = (byte) (P8x8[P_14, b[1]] ^ b1(k32[3]));
                    b[2] = (byte) (P8x8[P_24, b[2]] ^ b2(k32[3]));
                    b[3] = (byte) (P8x8[P_34, b[3]] ^ b3(k32[3]));
                    /* fall thru, having pre-processed b[0]..b[3] with k32[3] */
                    goto case 3;

                case 3: /* 192 bits of key */
                    b[0] = (byte) (P8x8[P_03, b[0]] ^ b0(k32[2]));
                    b[1] = (byte) (P8x8[P_13, b[1]] ^ b1(k32[2]));
                    b[2] = (byte) (P8x8[P_23, b[2]] ^ b2(k32[2]));
                    b[3] = (byte) (P8x8[P_33, b[3]] ^ b3(k32[2]));
                    /* fall thru, having pre-processed b[0]..b[3] with k32[2] */
                    goto case 2;

                case 2: /* 128 bits of key */
                    b[0] = P8x8[P_00, P8x8[P_01, P8x8[P_02, b[0]] ^ b0(k32[1])] ^ b0(k32[0])];
                    b[1] = P8x8[P_10, P8x8[P_11, P8x8[P_12, b[1]] ^ b1(k32[1])] ^ b1(k32[0])];
                    b[2] = P8x8[P_20, P8x8[P_21, P8x8[P_22, b[2]] ^ b2(k32[1])] ^ b2(k32[0])];
                    b[3] = P8x8[P_30, P8x8[P_31, P8x8[P_32, b[3]] ^ b3(k32[1])] ^ b3(k32[0])];

                    break;
            }


            /* Now perform the MDS matrix multiply inline. */
            return (uint) (M00(b[0]) ^ M01(b[1]) ^ M02(b[2]) ^ M03(b[3])) ^ (uint) ((M10(b[0]) ^ M11(b[1]) ^ M12(b[2]) ^ M13(b[3])) << 8) ^ (uint) ((M20(b[0]) ^ M21(b[1]) ^ M22(b[2]) ^ M23(b[3])) << 16) ^ (uint) ((M30(b[0]) ^ M31(b[1]) ^ M32(b[2]) ^ M33(b[3])) << 24);
        }

        /*
        +*****************************************************************************
        *
        * Function Name:	reKey
        *
        * Function:			Initialize the Twofish key schedule from key32
        *
        * Arguments:		key			=	ptr to keyInstance to be initialized
        *
        * Return:			TRUE on success
        *
        * Notes:
        *	Here we precompute all the round subkeys, although that is not actually
        *	required.  For example, on a smartcard, the round subkeys can 
        *	be generated on-the-fly	using f32()
        *
        -****************************************************************************/
        protected bool reKey(int keyLen, ref uint[] key32)
        {
            int i, k64Cnt;
            keyLength = keyLen;
            rounds = numRounds[(keyLen - 1) / 64];
            int subkeyCnt = ROUND_SUBKEYS + 2 * rounds;
            uint A, B;
            uint[] k32e = new uint[MAX_KEY_BITS / 64];
            uint[] k32o = new uint[MAX_KEY_BITS / 64]; /* even/odd key dwords */

            k64Cnt = (keyLen + 63) / 64; /* round up to next multiple of 64 bits */

            for (i = 0; i < k64Cnt; i++)
            {
                /* split into even/odd key dwords */
                k32e[i] = key32[2 * i];
                k32o[i] = key32[2 * i + 1];
                /* compute S-box keys using (12,8) Reed-Solomon code over GF(256) */
                sboxKeys[k64Cnt - 1 - i] = RS_MDS_Encode(k32e[i], k32o[i]); /* reverse order */
            }

            for (i = 0; i < subkeyCnt / 2; i++) /* compute round subkeys for PHT */
            {
                A = f32((uint) (i * SK_STEP), ref k32e, keyLen);           /* A uses even key dwords */
                B = f32((uint) (i * SK_STEP + SK_BUMP), ref k32o, keyLen); /* B uses odd  key dwords */
                B = ROL(B, 8);
                subKeys[2 * i] = A + B; /* combine with a PHT */
                subKeys[2 * i + 1] = ROL(A + 2 * B, SK_ROTL);
            }

            return true;
        }

        protected unsafe void blockDecrypt(ref Span<uint> x)
        {
            uint t0, t1;
            Span<uint> xtemp = stackalloc uint[4];

            if (cipherMode == CipherMode.CBC)
            {
                x.CopyTo(xtemp);
            }

            for (int i = 0; i < BLOCK_HALF_SIZE; i++) /* copy in the block, add whitening */
            {
                x[i] ^= subKeys[OUTPUT_WHITEN + i];
            }

            for (int r = rounds - 1; r >= 0; r--) /* main Twofish decryption loop */
            {
                t0 = f32(x[0], ref sboxKeys, keyLength);
                t1 = f32(ROL(x[1], 8), ref sboxKeys, keyLength);

                x[2] = ROL(x[2], 1);
                x[2] ^= t0 + t1 + subKeys[ROUND_SUBKEYS + 2 * r]; /* PHT, round keys */
                x[3] ^= t0 + 2 * t1 + subKeys[ROUND_SUBKEYS + 2 * r + 1];
                x[3] = ROR(x[3], 1);

                if (r > 0) /* unswap, except for last round */
                {
                    t0 = x[0];
                    x[0] = x[2];
                    x[2] = t0;
                    t1 = x[1];
                    x[1] = x[3];
                    x[3] = t1;
                }
            }

            for (int i = 0; i < BLOCK_HALF_SIZE; i++) /* copy out, with whitening */
            {
                x[i] ^= subKeys[INPUT_WHITEN + i];

                if (cipherMode == CipherMode.CBC)
                {
                    x[i] ^= IV[i];
                    IV[i] = xtemp[i];
                }
            }
        }

        public unsafe void blockEncrypt(ref Span<uint> x)
        {
            uint t0, t1, tmp;

            for (int i = 0; i < BLOCK_HALF_SIZE; i++) /* copy in the block, add whitening */
            {
                x[i] ^= subKeys[INPUT_WHITEN + i];

                if (cipherMode == CipherMode.CBC)
                {
                    x[i] ^= IV[i];
                }
            }

            for (int r = 0; r < rounds; r++) /* main Twofish encryption loop */ // 16==rounds
            {
#if FEISTEL
				t0 = f32(ROR(x[0],  (r+1)/2),ref sboxKeys,keyLength);
				t1 = f32(ROL(x[1],8+(r+1)/2),ref sboxKeys,keyLength);
											/* PHT, round keys */
				x[2] ^= ROL(t0 +   t1 + subKeys[ROUND_SUBKEYS+2*r  ], r    /2);
				x[3] ^= ROR(t0 + 2*t1 + subKeys[ROUND_SUBKEYS+2*r+1],(r+2) /2);

#else
                t0 = f32(x[0], ref sboxKeys, keyLength);
                t1 = f32(ROL(x[1], 8), ref sboxKeys, keyLength);

                x[3] = ROL(x[3], 1);
                x[2] ^= t0 + t1 + subKeys[ROUND_SUBKEYS + 2 * r]; /* PHT, round keys */
                x[3] ^= t0 + 2 * t1 + subKeys[ROUND_SUBKEYS + 2 * r + 1];
                x[2] = ROR(x[2], 1);

#endif
                if (r < rounds - 1) /* swap for next round */
                {
                    tmp = x[0];
                    x[0] = x[2];
                    x[2] = tmp;
                    tmp = x[1];
                    x[1] = x[3];
                    x[3] = tmp;
                }
            }
#if FEISTEL
			x[0] = ROR(x[0],8);                     /* "final permutation" */
			x[1] = ROL(x[1],8);
			x[2] = ROR(x[2],8);
			x[3] = ROL(x[3],8);
#endif
            for (int i = 0; i < BLOCK_HALF_SIZE; i++) /* copy out, with whitening */
            {
                x[i] ^= subKeys[OUTPUT_WHITEN + i];

                if (cipherMode == CipherMode.CBC)
                {
                    IV[i] = x[i];
                }
            }
        }

        /*
        +*****************************************************************************
        *
        * Function Name:	RS_MDS_Encode
        *
        * Function:			Use (12,8) Reed-Solomon code over GF(256) to produce
        *					a key S-box dword from two key material dwords.
        *
        * Arguments:		k0	=	1st dword
        *					k1	=	2nd dword
        *
        * Return:			Remainder polynomial generated using RS code
        *
        * Notes:
        *	Since this computation is done only once per reKey per 64 bits of key,
        *	the performance impact of this routine is imperceptible. The RS code
        *	chosen has "simple" coefficients to allow smartcard/hardware implementation
        *	without lookup tables.
        *
        -****************************************************************************/
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint RS_MDS_Encode(uint k0, uint k1)
        {
            uint i, j;
            uint r;

            for (i = r = 0; i < 2; i++)
            {
                r ^= i > 0 ? k0 : k1; /* merge in 32 more key bits */

                for (j = 0; j < 4; j++) /* shift one byte at a time */
                {
                    RS_rem(ref r);
                }
            }

            return r;
        }


        #region These are all the definitions that were found in AES.H

        private static readonly int BLOCK_SIZE = 128; /* number of bits per block */
        private static readonly int BLOCK_HALF_SIZE = BLOCK_SIZE >> 5;
        private static readonly int MAX_ROUNDS = 16;    /* max # rounds (for allocating subkey array) */
        private static readonly int ROUNDS_128 = 16;    /* default number of rounds for 128-bit keys*/
        private static readonly int ROUNDS_192 = 16;    /* default number of rounds for 192-bit keys*/
        private static readonly int ROUNDS_256 = 16;    /* default number of rounds for 256-bit keys*/
        private static readonly int MAX_KEY_BITS = 256; /* max number of bits of key */
        private static readonly int MIN_KEY_BITS = 128; /* min number of bits of key (zero pad) */

        //#define		VALID_SIG	 0x48534946	/* initialization signature ('FISH') */
        //#define		MCT_OUTER			400	/* MCT outer loop */
        //#define		MCT_INNER		  10000	/* MCT inner loop */
        //#define		REENTRANT			  1	/* nonzero forces reentrant code (slightly slower) */

        private static readonly int INPUT_WHITEN = 0; /* subkey array indices */
        private static readonly int OUTPUT_WHITEN = INPUT_WHITEN + BLOCK_SIZE / 32;
        private static readonly int ROUND_SUBKEYS = OUTPUT_WHITEN + BLOCK_SIZE / 32; /* use 2 * (# rounds) */
        private static readonly int TOTAL_SUBKEYS = ROUND_SUBKEYS + 2 * MAX_ROUNDS;

        #endregion

        #region These are all the definitions that were found in TABLE.H that we need

        /* for computing subkeys */
        private static readonly uint SK_STEP = 0x02020202u;
        private static readonly uint SK_BUMP = 0x01010101u;
        private static readonly int SK_ROTL = 9;

        /* Reed-Solomon code parameters: (12,8) reversible code
        g(x) = x**4 + (a + 1/a) x**3 + a x**2 + (a + 1/a) x + 1
        where a = primitive root of field generator 0x14D */
        private static readonly uint RS_GF_FDBK = 0x14D; /* field generator */

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RS_rem(ref uint x)
        {
            byte b = (byte) (x >> 24);
            // TODO: maybe change g2 and g3 to bytes			 
            uint g2 = (uint) (((b << 1) ^ ((b & 0x80) == 0x80 ? RS_GF_FDBK : 0)) & 0xFF);
            uint g3 = (uint) (((b >> 1) & 0x7F) ^ ((b & 1) == 1 ? RS_GF_FDBK >> 1 : 0) ^ g2);
            x = (x << 8) ^ (g3 << 24) ^ (g2 << 16) ^ (g3 << 8) ^ b;
        }

        /*	Macros for the MDS matrix
        *	The MDS matrix is (using primitive polynomial 169):
        *      01  EF  5B  5B
        *      5B  EF  EF  01
        *      EF  5B  01  EF
        *      EF  01  EF  5B
        *----------------------------------------------------------------
        * More statistical properties of this matrix (from MDS.EXE output):
        *
        * Min Hamming weight (one byte difference) =  8. Max=26.  Total =  1020.
        * Prob[8]:      7    23    42    20    52    95    88    94   121   128    91
        *             102    76    41    24     8     4     1     3     0     0     0
        * Runs[8]:      2     4     5     6     7     8     9    11
        * MSBs[8]:      1     4    15     8    18    38    40    43
        * HW= 8: 05040705 0A080E0A 14101C14 28203828 50407050 01499101 A080E0A0 
        * HW= 9: 04050707 080A0E0E 10141C1C 20283838 40507070 80A0E0E0 C6432020 07070504 
        *        0E0E0A08 1C1C1410 38382820 70705040 E0E0A080 202043C6 05070407 0A0E080E 
        *        141C101C 28382038 50704070 A0E080E0 4320C620 02924B02 089A4508 
        * Min Hamming weight (two byte difference) =  3. Max=28.  Total = 390150.
        * Prob[3]:      7    18    55   149   270   914  2185  5761 11363 20719 32079
        *           43492 51612 53851 52098 42015 31117 20854 11538  6223  2492  1033
        * MDS OK, ROR:   6+  7+  8+  9+ 10+ 11+ 12+ 13+ 14+ 15+ 16+
        *               17+ 18+ 19+ 20+ 21+ 22+ 23+ 24+ 25+ 26+
        */
        private static readonly int MDS_GF_FDBK = 0x169; /* primitive polynomial for GF(256)*/

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LFSR1(int x)
        {
            return (x >> 1) ^ ((x & 0x01) == 0x01 ? MDS_GF_FDBK / 2 : 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LFSR2(int x)
        {
            return (x >> 2) ^ ((x & 0x02) == 0x02 ? MDS_GF_FDBK / 2 : 0) ^ ((x & 0x01) == 0x01 ? MDS_GF_FDBK / 4 : 0);
        }

        // TODO: not the most efficient use of code but it allows us to update the code a lot quicker we can possibly optimize this code once we have got it all working

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Mx_1(int x)
        {
            return x; /* force result to int so << will work */
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Mx_X(int x)
        {
            return x ^ LFSR2(x); /* 5B */
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Mx_Y(int x)
        {
            return x ^ LFSR1(x) ^ LFSR2(x); /* EF */
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int M00(int x)
        {
            return Mul_1(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int M01(int x)
        {
            return Mul_Y(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int M02(int x)
        {
            return Mul_X(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int M03(int x)
        {
            return Mul_X(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int M10(int x)
        {
            return Mul_X(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int M11(int x)
        {
            return Mul_Y(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int M12(int x)
        {
            return Mul_Y(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int M13(int x)
        {
            return Mul_1(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int M20(int x)
        {
            return Mul_Y(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int M21(int x)
        {
            return Mul_X(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int M22(int x)
        {
            return Mul_1(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int M23(int x)
        {
            return Mul_Y(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int M30(int x)
        {
            return Mul_Y(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int M31(int x)
        {
            return Mul_1(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int M32(int x)
        {
            return Mul_Y(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int M33(int x)
        {
            return Mul_X(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Mul_1(int x)
        {
            return Mx_1(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Mul_X(int x)
        {
            return Mx_X(x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Mul_Y(int x)
        {
            return Mx_Y(x);
        }

        /*	Define the fixed p0/p1 permutations used in keyed S-box lookup.  
            By changing the following constant definitions for P_ij, the S-boxes will
            automatically get changed in all the Twofish source code. Note that P_i0 is
            the "outermost" 8x8 permutation applied.  See the f32() function to see
            how these constants are to be  used.
        */
        private static readonly int P_00 = 1; /* "outermost" permutation */
        private static readonly int P_01 = 0;
        private static readonly int P_02 = 0;
        private static readonly int P_03 = P_01 ^ 1; /* "extend" to larger key sizes */
        private static readonly int P_04 = 1;

        private static readonly int P_10 = 0;
        private static readonly int P_11 = 0;
        private static readonly int P_12 = 1;
        private static readonly int P_13 = P_11 ^ 1;
        private static readonly int P_14 = 0;

        private static readonly int P_20 = 1;
        private static readonly int P_21 = 1;
        private static readonly int P_22 = 0;
        private static readonly int P_23 = P_21 ^ 1;
        private static readonly int P_24 = 0;

        private static readonly int P_30 = 0;
        private static readonly int P_31 = 1;
        private static readonly int P_32 = 1;
        private static readonly int P_33 = P_31 ^ 1;
        private static readonly int P_34 = 1;

        /* fixed 8x8 permutation S-boxes */

        /***********************************************************************
        *  07:07:14  05/30/98  [4x4]  TestCnt=256. keySize=128. CRC=4BD14D9E.
        * maxKeyed:  dpMax = 18. lpMax =100. fixPt =  8. skXor =  0. skDup =  6. 
        * log2(dpMax[ 6..18])=   --- 15.42  1.33  0.89  4.05  7.98 12.05
        * log2(lpMax[ 7..12])=  9.32  1.01  1.16  4.23  8.02 12.45
        * log2(fixPt[ 0.. 8])=  1.44  1.44  2.44  4.06  6.01  8.21 11.07 14.09 17.00
        * log2(skXor[ 0.. 0])
        * log2(skDup[ 0.. 6])=   ---  2.37  0.44  3.94  8.36 13.04 17.99
        ***********************************************************************/
        private static readonly byte[,] P8x8 =
        {
            /*  p0:   */
            /*  dpMax      = 10.  lpMax      = 64.  cycleCnt=   1  1  1  0.         */
            /* 817D6F320B59ECA4.ECB81235F4A6709D.BA5E6D90C8F32471.D7F4126E9B3085CA. */
            /* Karnaugh maps:
            *  0111 0001 0011 1010. 0001 1001 1100 1111. 1001 1110 0011 1110. 1101 0101 1111 1001. 
            *  0101 1111 1100 0100. 1011 0101 0010 0000. 0101 1000 1100 0101. 1000 0111 0011 0010. 
            *  0000 1001 1110 1101. 1011 1000 1010 0011. 0011 1001 0101 0000. 0100 0010 0101 1011. 
            *  0111 0100 0001 0110. 1000 1011 1110 1001. 0011 0011 1001 1101. 1101 0101 0000 1100. 
            */
            {
                0xA9, 0x67, 0xB3, 0xE8, 0x04, 0xFD, 0xA3, 0x76,
                0x9A, 0x92, 0x80, 0x78, 0xE4, 0xDD, 0xD1, 0x38,
                0x0D, 0xC6, 0x35, 0x98, 0x18, 0xF7, 0xEC, 0x6C,
                0x43, 0x75, 0x37, 0x26, 0xFA, 0x13, 0x94, 0x48,
                0xF2, 0xD0, 0x8B, 0x30, 0x84, 0x54, 0xDF, 0x23,
                0x19, 0x5B, 0x3D, 0x59, 0xF3, 0xAE, 0xA2, 0x82,
                0x63, 0x01, 0x83, 0x2E, 0xD9, 0x51, 0x9B, 0x7C,
                0xA6, 0xEB, 0xA5, 0xBE, 0x16, 0x0C, 0xE3, 0x61,
                0xC0, 0x8C, 0x3A, 0xF5, 0x73, 0x2C, 0x25, 0x0B,
                0xBB, 0x4E, 0x89, 0x6B, 0x53, 0x6A, 0xB4, 0xF1,
                0xE1, 0xE6, 0xBD, 0x45, 0xE2, 0xF4, 0xB6, 0x66,
                0xCC, 0x95, 0x03, 0x56, 0xD4, 0x1C, 0x1E, 0xD7,
                0xFB, 0xC3, 0x8E, 0xB5, 0xE9, 0xCF, 0xBF, 0xBA,
                0xEA, 0x77, 0x39, 0xAF, 0x33, 0xC9, 0x62, 0x71,
                0x81, 0x79, 0x09, 0xAD, 0x24, 0xCD, 0xF9, 0xD8,
                0xE5, 0xC5, 0xB9, 0x4D, 0x44, 0x08, 0x86, 0xE7,
                0xA1, 0x1D, 0xAA, 0xED, 0x06, 0x70, 0xB2, 0xD2,
                0x41, 0x7B, 0xA0, 0x11, 0x31, 0xC2, 0x27, 0x90,
                0x20, 0xF6, 0x60, 0xFF, 0x96, 0x5C, 0xB1, 0xAB,
                0x9E, 0x9C, 0x52, 0x1B, 0x5F, 0x93, 0x0A, 0xEF,
                0x91, 0x85, 0x49, 0xEE, 0x2D, 0x4F, 0x8F, 0x3B,
                0x47, 0x87, 0x6D, 0x46, 0xD6, 0x3E, 0x69, 0x64,
                0x2A, 0xCE, 0xCB, 0x2F, 0xFC, 0x97, 0x05, 0x7A,
                0xAC, 0x7F, 0xD5, 0x1A, 0x4B, 0x0E, 0xA7, 0x5A,
                0x28, 0x14, 0x3F, 0x29, 0x88, 0x3C, 0x4C, 0x02,
                0xB8, 0xDA, 0xB0, 0x17, 0x55, 0x1F, 0x8A, 0x7D,
                0x57, 0xC7, 0x8D, 0x74, 0xB7, 0xC4, 0x9F, 0x72,
                0x7E, 0x15, 0x22, 0x12, 0x58, 0x07, 0x99, 0x34,
                0x6E, 0x50, 0xDE, 0x68, 0x65, 0xBC, 0xDB, 0xF8,
                0xC8, 0xA8, 0x2B, 0x40, 0xDC, 0xFE, 0x32, 0xA4,
                0xCA, 0x10, 0x21, 0xF0, 0xD3, 0x5D, 0x0F, 0x00,
                0x6F, 0x9D, 0x36, 0x42, 0x4A, 0x5E, 0xC1, 0xE0
            },
            /*  p1:   */
            /*  dpMax      = 10.  lpMax      = 64.  cycleCnt=   2  0  0  1.         */
            /* 28BDF76E31940AC5.1E2B4C376DA5F908.4C75169A0ED82B3F.B951C3DE647F208A. */
            /* Karnaugh maps:
            *  0011 1001 0010 0111. 1010 0111 0100 0110. 0011 0001 1111 0100. 1111 1000 0001 1100. 
            *  1100 1111 1111 1010. 0011 0011 1110 0100. 1001 0110 0100 0011. 0101 0110 1011 1011. 
            *  0010 0100 0011 0101. 1100 1000 1000 1110. 0111 1111 0010 0110. 0000 1010 0000 0011. 
            *  1101 1000 0010 0001. 0110 1001 1110 0101. 0001 0100 0101 0111. 0011 1011 1111 0010. 
            */
            {
                0x75, 0xF3, 0xC6, 0xF4, 0xDB, 0x7B, 0xFB, 0xC8,
                0x4A, 0xD3, 0xE6, 0x6B, 0x45, 0x7D, 0xE8, 0x4B,
                0xD6, 0x32, 0xD8, 0xFD, 0x37, 0x71, 0xF1, 0xE1,
                0x30, 0x0F, 0xF8, 0x1B, 0x87, 0xFA, 0x06, 0x3F,
                0x5E, 0xBA, 0xAE, 0x5B, 0x8A, 0x00, 0xBC, 0x9D,
                0x6D, 0xC1, 0xB1, 0x0E, 0x80, 0x5D, 0xD2, 0xD5,
                0xA0, 0x84, 0x07, 0x14, 0xB5, 0x90, 0x2C, 0xA3,
                0xB2, 0x73, 0x4C, 0x54, 0x92, 0x74, 0x36, 0x51,
                0x38, 0xB0, 0xBD, 0x5A, 0xFC, 0x60, 0x62, 0x96,
                0x6C, 0x42, 0xF7, 0x10, 0x7C, 0x28, 0x27, 0x8C,
                0x13, 0x95, 0x9C, 0xC7, 0x24, 0x46, 0x3B, 0x70,
                0xCA, 0xE3, 0x85, 0xCB, 0x11, 0xD0, 0x93, 0xB8,
                0xA6, 0x83, 0x20, 0xFF, 0x9F, 0x77, 0xC3, 0xCC,
                0x03, 0x6F, 0x08, 0xBF, 0x40, 0xE7, 0x2B, 0xE2,
                0x79, 0x0C, 0xAA, 0x82, 0x41, 0x3A, 0xEA, 0xB9,
                0xE4, 0x9A, 0xA4, 0x97, 0x7E, 0xDA, 0x7A, 0x17,
                0x66, 0x94, 0xA1, 0x1D, 0x3D, 0xF0, 0xDE, 0xB3,
                0x0B, 0x72, 0xA7, 0x1C, 0xEF, 0xD1, 0x53, 0x3E,
                0x8F, 0x33, 0x26, 0x5F, 0xEC, 0x76, 0x2A, 0x49,
                0x81, 0x88, 0xEE, 0x21, 0xC4, 0x1A, 0xEB, 0xD9,
                0xC5, 0x39, 0x99, 0xCD, 0xAD, 0x31, 0x8B, 0x01,
                0x18, 0x23, 0xDD, 0x1F, 0x4E, 0x2D, 0xF9, 0x48,
                0x4F, 0xF2, 0x65, 0x8E, 0x78, 0x5C, 0x58, 0x19,
                0x8D, 0xE5, 0x98, 0x57, 0x67, 0x7F, 0x05, 0x64,
                0xAF, 0x63, 0xB6, 0xFE, 0xF5, 0xB7, 0x3C, 0xA5,
                0xCE, 0xE9, 0x68, 0x44, 0xE0, 0x4D, 0x43, 0x69,
                0x29, 0x2E, 0xAC, 0x15, 0x59, 0xA8, 0x0A, 0x9E,
                0x6E, 0x47, 0xDF, 0x34, 0x35, 0x6A, 0xCF, 0xDC,
                0x22, 0xC9, 0xC0, 0x9B, 0x89, 0xD4, 0xED, 0xAB,
                0x12, 0xA2, 0x0D, 0x52, 0xBB, 0x02, 0x2F, 0xA9,
                0xD7, 0x61, 0x1E, 0xB4, 0x50, 0x04, 0xF6, 0xC2,
                0x16, 0x25, 0x86, 0x56, 0x55, 0x09, 0xBE, 0x91
            }
        };

        #endregion

        #region These are all the definitions that were found in PLATFORM.H that we need

        // left rotation
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ROL(uint x, int n)
        {
            return (x << (n & 0x1F)) | (x >> (32 - (n & 0x1F)));
        }

        // right rotation
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ROR(uint x, int n)
        {
            return (x >> (n & 0x1F)) | (x << (32 - (n & 0x1F)));
        }

        // first byte
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static byte b0(uint x)
        {
            return (byte) x; //& 0xFF);
        }

        // second byte
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static byte b1(uint x)
        {
            return (byte) (x >> 8); // & (0xFF));
        }

        // third byte
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static byte b2(uint x)
        {
            return (byte) (x >> 16); // & (0xFF));
        }

        // fourth byte
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static byte b3(uint x)
        {
            return (byte) (x >> 24); // & (0xFF));
        }

        #endregion
    }
}