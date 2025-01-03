// SPDX-License-Identifier: BSD-2-Clause

namespace ZLibNative
{
    public class Adler32
    {
        private const int _base = 65521;
        private const int _nmax = 5550;
        private uint a = 1;
        private uint b;
        private int pend;

        public void Update(byte data)
        {
            if (pend >= _nmax)
            {
                UpdateModulus();
            }

            a += data;
            b += a;
            pend++;
        }

        public void Update(byte[] data)
        {
            Update(data, 0, data.Length);
        }

        public void Update(byte[] data, int offset, int length)
        {
            int nextJToComputeModulus = _nmax - pend;

            for (int j = 0; j < length; j++)
            {
                if (j == nextJToComputeModulus)
                {
                    UpdateModulus();
                    nextJToComputeModulus = j + _nmax;
                }

                unchecked
                {
                    a += data[j + offset];
                }

                b += a;
                pend++;
            }
        }

        public void Reset()
        {
            a = 1;
            b = 0;
            pend = 0;
        }

        private void UpdateModulus()
        {
            a %= _base;
            b %= _base;
            pend = 0;
        }

        public uint GetValue()
        {
            if (pend > 0)
            {
                UpdateModulus();
            }

            return (b << 16) | a;
        }
    }
}