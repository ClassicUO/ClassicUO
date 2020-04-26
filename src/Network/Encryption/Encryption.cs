using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Configuration;
using ClassicUO.Data;
using ClassicUO.Network.Encryption;
using ClassicUO.Utility;

namespace ClassicUO.Network
{
    enum ENCRYPTION_TYPE
    {
        NONE,
        OLD_BFISH,
        BLOWFISH__1_25_36,
        BLOWFISH,
        BLOWFISH__2_0_3,
        TWOFISH_MD5
    }

    static class EncryptionHelper
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

                int temp = ((a << 9 | b) << 10 | c) ^ ((c * c) << 5);

                KEY_2 = (uint) ((temp << 4) ^ (b * b) ^ (b * 0x0B000000) ^ (c * 0x380000) ^ 0x2C13A5FD);
                temp = (((a << 9 | c) << 10 | b) * 8) ^ (c * c * 0x0c00);
                KEY_3 = (uint) (temp ^ (b * b) ^ (b * 0x6800000) ^ (c * 0x1c0000) ^ 0x0A31D527F);
                KEY_1 = KEY_2 - 1;


                if (version < (ClientVersion) (((1 & 0xFF) << 24) |
                                               ((25 & 0xFF) << 16) |
                                               ((35 & 0xFF) << 8) |
                                               (0 & 0xFF)))
                {
                    Type = ENCRYPTION_TYPE.OLD_BFISH;
                }


                else if (version == (ClientVersion) (((1 & 0xFF) << 24) |
                                                     ((25 & 0xFF) << 16) |
                                                     ((36 & 0xFF) << 8) |
                                                     (0 & 0xFF)))
                {
                    Type = ENCRYPTION_TYPE.BLOWFISH__1_25_36;
                }


                else if (version < ClientVersion.CV_200)
                {
                    Type = ENCRYPTION_TYPE.BLOWFISH;
                }

                else if (version <= (ClientVersion) (((2 & 0xFF) << 24) |
                                                     ((0 & 0xFF) << 16) |
                                                     ((3 & 0xFF) << 8) |
                                                     (0 & 0xFF)))
                {
                    Type = ENCRYPTION_TYPE.BLOWFISH__2_0_3;
                }
                else
                    Type = ENCRYPTION_TYPE.TWOFISH_MD5;
            }
        }

        
        public static void Initialize(bool is_login, uint seed, ENCRYPTION_TYPE encryption)
        {
            if (encryption == ENCRYPTION_TYPE.NONE)
                return;

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

        
        public static void Encrypt(bool is_login, ref byte[] src, ref byte[] dst, int size)
        {
            if (Type == ENCRYPTION_TYPE.NONE)
            {
                return;
            }

            if (is_login)
            {
                if (Type == ENCRYPTION_TYPE.OLD_BFISH)
                {
                    _loginCrypt.Encrypt_OLD(ref src, ref dst, size);
                }
                else if (Type == ENCRYPTION_TYPE.BLOWFISH__1_25_36)
                {
                    _loginCrypt.Encrypt_1_25_36(ref src, ref dst, size);
                }
                else if (Type != ENCRYPTION_TYPE.NONE)
                {
                    _loginCrypt.Encrypt(ref src, ref dst, size);
                }
            }
            else if (Type == ENCRYPTION_TYPE.BLOWFISH__2_0_3)
            {
                int index_s = 0, index_d = 0;

                _blowfishEncryption.Encrypt(ref src, ref dst, size, ref index_s, ref index_d);
                _twoFishBehaviour.Encrypt(ref dst, ref dst, size);
            }
            else if (Type == ENCRYPTION_TYPE.TWOFISH_MD5)
            {
                _twoFishBehaviour.Encrypt(ref src, ref dst, size);
            }
            else
            {
                int index_s = 0, index_d = 0;
                _blowfishEncryption.Encrypt(ref src, ref dst, size, ref index_s, ref index_d);
            }
        }

        public static void Decrypt(ref byte[] src, ref byte[] dst, int size)
        {
            if (Type == ENCRYPTION_TYPE.TWOFISH_MD5)
            {
                _twoFishBehaviour.Decrypt(ref src, ref dst, size);
            }
        }
    }
}
