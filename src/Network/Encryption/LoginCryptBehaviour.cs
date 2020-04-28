using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Network.Encryption
{
    sealed class LoginCryptBehaviour
    {
        private uint _k1, _k2, _k3;
        private uint[] _key = new uint[2];
        private byte[] _seed = new byte[4];



        public void Initialize(uint seed, uint k1, uint k2, uint k3)
        {
            _seed[0] = (byte) (seed >> 24);
            _seed[1] = (byte) (seed >> 16);
            _seed[2] = (byte) (seed >> 8);
            _seed[3] = (byte) seed;

            _k1 = k1;
            _k2 = k2;
            _k3 = k3;

            const uint seed_key = 0x0000_1357;

            _key[0] = (((~seed) ^ seed_key) << 16) | ((seed ^ 0xffffaaaa) & 0x0000ffff);
            _key[1] = ((seed ^ 0x43210000) >> 16) | (((~seed) ^ 0xabcdffff) & 0xffff0000);
        }


        public void Encrypt(ref byte[] src, ref byte[] dst, int size)
        {
            for (int i = 0; i < size; i++)
            {
                dst[i] = (byte) (src[i] ^ _key[0]);

                uint table0 = _key[0];
                uint table1 = _key[1];

                _key[1] = (((((table1 >> 1) | (table0 << 31)) ^ _k1) >> 1) | (table0 << 31)) ^ _k2;
                _key[0] = ((table0 >> 1) | (table1 << 31)) ^ _k3;
            }
        }

        public void Encrypt_OLD(ref byte[] src, ref byte[] dst, int size)
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

        public void Encrypt_1_25_36(ref byte[] src, ref byte[] dst, int size)
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
