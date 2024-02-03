#region license

// Copyright (c) 2024, andreakarasho
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

using ClassicUO.Utility;
using System;

namespace ClassicUO.Network.Encryption
{
    internal enum ENCRYPTION_TYPE
    {
        NONE,
        OLD_BFISH,
        BLOWFISH__1_25_36,
        BLOWFISH,
        BLOWFISH__2_0_3,
        TWOFISH_MD5
    }

    internal static class EncryptionHelper
    {
        private static readonly LoginCryptBehaviour _loginCrypt = new LoginCryptBehaviour();
        private static readonly BlowfishEncryption _blowfishEncryption = new BlowfishEncryption();
        private static readonly TwofishEncryption _twoFishBehaviour = new TwofishEncryption();


        public static uint KEY_1, KEY_2, KEY_3;
        public static ENCRYPTION_TYPE Type;


        public static void CalculateEncryption(ClientVersion version)
        {
            if (version == ClientVersion.CV_200X)
            {
                KEY_1 = 0x2D13A5FC;
                KEY_2 = 0x2D13A5FD;
                KEY_3 = 0xA39D527F;

                Type = ENCRYPTION_TYPE.BLOWFISH__2_0_3;
            }
            else
            {
                int a = ((int) version >> 24) & 0xFF;
                int b = ((int) version >> 16) & 0xFF;
                int c = ((int) version >> 8) & 0xFF;

                int temp = ((((a << 9) | b) << 10) | c) ^ ((c * c) << 5);

                KEY_2 = (uint) ((temp << 4) ^ (b * b) ^ (b * 0x0B000000) ^ (c * 0x380000) ^ 0x2C13A5FD);
                temp = (((((a << 9) | c) << 10) | b) * 8) ^ (c * c * 0x0c00);
                KEY_3 = (uint) (temp ^ (b * b) ^ (b * 0x6800000) ^ (c * 0x1c0000) ^ 0x0A31D527F);
                KEY_1 = KEY_2 - 1;


                if (version < (ClientVersion) (((1 & 0xFF) << 24) | ((25 & 0xFF) << 16) | ((35 & 0xFF) << 8) | (0 & 0xFF)))
                {
                    Type = ENCRYPTION_TYPE.OLD_BFISH;
                }


                else if (version == (ClientVersion) (((1 & 0xFF) << 24) | ((25 & 0xFF) << 16) | ((36 & 0xFF) << 8) | (0 & 0xFF)))
                {
                    Type = ENCRYPTION_TYPE.BLOWFISH__1_25_36;
                }


                else if (version <= ClientVersion.CV_200)
                {
                    Type = ENCRYPTION_TYPE.BLOWFISH;
                }

                else if (version <= (ClientVersion) (((2 & 0xFF) << 24) | ((0 & 0xFF) << 16) | ((3 & 0xFF) << 8) | (0 & 0xFF)))
                {
                    Type = ENCRYPTION_TYPE.BLOWFISH__2_0_3;
                }
                else
                {
                    Type = ENCRYPTION_TYPE.TWOFISH_MD5;
                }
            }
        }


        public static void Initialize(bool is_login, uint seed, ENCRYPTION_TYPE encryption)
        {
            if (encryption == ENCRYPTION_TYPE.NONE)
            {
                return;
            }

            if (is_login)
            {
                _loginCrypt.Initialize(seed, KEY_1, KEY_2, KEY_3);
            }
            else
            {
                if (encryption >= ENCRYPTION_TYPE.OLD_BFISH && encryption < ENCRYPTION_TYPE.TWOFISH_MD5)
                {
                    _blowfishEncryption.Initialize();
                }

                if (encryption == ENCRYPTION_TYPE.BLOWFISH__2_0_3 || encryption == ENCRYPTION_TYPE.TWOFISH_MD5)
                {
                    _twoFishBehaviour.Initialize(seed, encryption == ENCRYPTION_TYPE.TWOFISH_MD5);
                }
            }
        }


        public static void Encrypt(bool is_login, Span<byte> src, Span<byte> dst, int size)
        {
            if (Type == ENCRYPTION_TYPE.NONE)
            {
                return;
            }

            if (is_login)
            {
                if (Type == ENCRYPTION_TYPE.OLD_BFISH)
                {
                    _loginCrypt.Encrypt_OLD(src, dst, size);
                }
                else if (Type == ENCRYPTION_TYPE.BLOWFISH__1_25_36)
                {
                    _loginCrypt.Encrypt_1_25_36(src, dst, size);
                }
                else if (Type != ENCRYPTION_TYPE.NONE)
                {
                    _loginCrypt.Encrypt(src, dst, size);
                }
            }
            else if (Type == ENCRYPTION_TYPE.BLOWFISH__2_0_3)
            {
                int index_s = 0, index_d = 0;

                _blowfishEncryption.Encrypt
                (
                    src,
                    dst,
                    size,
                    ref index_s,
                    ref index_d
                );

                _twoFishBehaviour.Encrypt(dst, dst, size);
            }
            else if (Type == ENCRYPTION_TYPE.TWOFISH_MD5)
            {
                _twoFishBehaviour.Encrypt(src, dst, size);
            }
            else
            {
                int index_s = 0, index_d = 0;

                _blowfishEncryption.Encrypt
                (
                    src,
                    dst,
                    size,
                    ref index_s,
                    ref index_d
                );
            }
        }

        public static void Decrypt(Span<byte> src, Span<byte> dst, int size)
        {
            if (Type == ENCRYPTION_TYPE.TWOFISH_MD5)
            {
                _twoFishBehaviour.Decrypt(src, dst, size);
            }
        }
    }
}