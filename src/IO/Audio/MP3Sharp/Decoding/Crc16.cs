using ClassicUO.IO.Audio.MP3Sharp.Support;

namespace ClassicUO.IO.Audio.MP3Sharp.Decoding
{
    /// <summary>
    ///     16-Bit CRC checksum
    /// </summary>
    internal sealed class Crc16
    {
        private static readonly short Polynomial;
        private short m_Crc;

        static Crc16()
        {
            Polynomial = (short) SupportClass.Identity(0x8005);
        }

        /// <summary>
        ///     Dummy Constructor
        /// </summary>
        public Crc16()
        {
            m_Crc = (short) SupportClass.Identity(0xFFFF);
        }

        /// <summary>
        ///     Feed a bitstring to the crc calculation (length between 0 and 32, not inclusive).
        /// </summary>
        public void add_bits(int bitstring, int length)
        {
            int bitmask = 1 << (length - 1);

            do
            {
                if (((m_Crc & 0x8000) == 0) ^ ((bitstring & bitmask) == 0))
                {
                    m_Crc <<= 1;
                    m_Crc ^= Polynomial;
                }
                else
                    m_Crc <<= 1;
            } while ((bitmask = SupportClass.URShift(bitmask, 1)) != 0);
        }

        /// <summary>
        ///     Return the calculated checksum.
        ///     Erase it for next calls to add_bits().
        /// </summary>
        public short Checksum()
        {
            short sum = m_Crc;
            m_Crc = (short) SupportClass.Identity(0xFFFF);

            return sum;
        }
    }
}