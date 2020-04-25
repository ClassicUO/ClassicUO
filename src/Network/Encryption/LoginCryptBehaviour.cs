using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Network.Encryption
{
    sealed class LoginCryptBehaviour
    {
        private readonly uint _k1, _k2, _k3;
        private readonly uint[] _key = new uint[2];
        private readonly byte[] _seed = new byte[4];

        public LoginCryptBehaviour(uint seed)
        {
            //for (int i = 0; i < 4; i++)
            //    _seed[i] = buff_seed[i];


            _k1 = EncryptionHelper.KEY_1;
            _k2 = EncryptionHelper.KEY_2;
            _k3 = EncryptionHelper.KEY_3;

            uint seed_key = 0;// TODO:

            _key[0] = (((~seed) ^ seed_key) << 16) | ((seed ^ 0xffffaaaa) & 0x0000ffff);
            _key[1] = ((seed ^ 0x43210000) >> 16) | (((~seed) ^ 0xabcdffff) & 0xffff0000);
        }


        public void Encrypt(byte[] src, byte[] dst, int size)
        {
            for (int i = 0; i < size; i++)
            {
                dst[i] = (byte) (src[i] ^ (byte) (_key[0]));

                uint table0 = _key[0];
                uint table1 = _key[1];

                _key[1] = (((((table1 >> 1) | (table0 << 31)) ^ _k1) >> 1) | (table0 << 31)) ^ _k2;
                _key[0] = ((table0 >> 1) | (table1 << 31)) ^ _k3;
            }
        }

        public void Encrypt_OLD(byte[] src, byte[] dst, int size)
        {
            for (int i = 0; i < size; i++)
            {
                dst[i] = (byte) (src[i] ^ (byte) (_key[0]));

                uint table0 = _key[0];
                uint table1 = _key[1];

                _key[0] = ((table0 >> 1) | (table1 << 31)) ^ _k2;
                _key[1] = ((table1 >> 1) | (table0 << 31)) ^ _k1;
            }
        }

        public void Encrypt_1_25_36(byte[] src, byte[] dst, int size)
        {
            for (int i = 0; i < size; i++)
            {
                dst[i] = (byte) (src[i] ^ (byte) (_key[0]));

                uint table0 = _key[0];
                uint table1 = _key[1];

                _key[0] = ((table0 >> 1) | (table1 << 31)) ^ _k2;
                _key[1] = ((table1 >> 1) | (table0 << 31)) ^ _k1;

                _key[1] = (_k1 >> (byte) ((5 * table1 * table1) & 0xFF)) + (table1 * _k1) + (table0 * table0 * 0x35ce9581) + 0x07afcc37;
                _key[0] = (_k2 >> (byte) ((3 * table0 * table0) & 0xFF)) + (table0 * _k2) + (_key[1] * _key[1] * 0x4c3a1353) + 0x16ef783f;
            }
        }
    }
}
