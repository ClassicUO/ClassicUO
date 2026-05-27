// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Network
{
    internal sealed class PacketsTable
    {
        private readonly short[] _packetsTable =
        {
            0x0068, // 0x00
            0x0005, // 0x01
            0x0007, // 0x02
            -1,     // 0x03
            0x0002, // 0x04
            0x0005, // 0x05
            0x0005, // 0x06
            0x0007, // 0x07
            0x000E, // 0x08
            0x0005, // 0x09
            0x000B, // 0x0A
            0x010A, // 0x0B
            -1,     // 0x0C
            0x0003, // 0x0D
            -1,     // 0x0E
            0x003D, // 0x0F
            0x00D7, // 0x10
            -1,     // 0x11
            -1,     // 0x12
            0x000A, // 0x13
            0x0006, // 0x14
            0x0009, // 0x15
            0x0001, // 0x16
            -1,     // 0x17
            -1,     // 0x18
            -1,     // 0x19
            -1,     // 0x1A
            0x0025, // 0x1B
            -1,     // 0x1C
            0x0005, // 0x1D
            0x0004, // 0x1E
            0x0008, // 0x1F
            0x0013, // 0x20
            0x0008, // 0x21
            0x0003, // 0x22
            0x001A, // 0x23
            0x0007, // 0x24
            0x0014, // 0x25
            0x0005, // 0x26
            0x0002, // 0x27
            0x0005, // 0x28
            0x0001, // 0x29
            0x0005, // 0x2A
            0x0002, // 0x2B
            0x0002, // 0x2C
            0x0011, // 0x2D
            0x000F, // 0x2E
            0x000A, // 0x2F
            0x0005, // 0x30
            0x0001, // 0x31
            0x0002, // 0x32
            0x0002, // 0x33
            0x000A, // 0x34
            0x028D, // 0x35
            -1,     // 0x36
            0x0008, // 0x37
            0x0007, // 0x38
            0x0009, // 0x39
            -1,     // 0x3A
            -1,     // 0x3B
            -1,     // 0x3C
            0x0002, // 0x3D
            0x0025, // 0x3E
            -1,     // 0x3F
            0x00C9, // 0x40
            -1,     // 0x41
            -1,     // 0x42
            0x0229, // 0x43
            0x02C9, // 0x44
            0x0005, // 0x45
            -1,     // 0x46
            0x000B, // 0x47
            0x0049, // 0x48
            0x005D, // 0x49
            0x0005, // 0x4A
            0x0009, // 0x4B
            -1,     // 0x4C
            -1,     // 0x4D
            0x0006, // 0x4E
            0x0002, // 0x4F
            -1,     // 0x50
            -1,     // 0x51
            -1,     // 0x52
            0x0002, // 0x53
            0x000C, // 0x54
            0x0001, // 0x55
            0x000B, // 0x56
            0x006E, // 0x57
            0x006A, // 0x58
            -1,     // 0x59
            -1,     // 0x5A
            0x0004, // 0x5B
            0x0002, // 0x5C
            0x0049, // 0x5D
            -1,     // 0x5E
            0x0031, // 0x5F
            0x0005, // 0x60
            0x0009, // 0x61
            0x000F, // 0x62
            0x000D, // 0x63
            0x0001, // 0x64
            0x0004, // 0x65
            -1,     // 0x66
            0x0015, // 0x67
            -1,     // 0x68
            -1,     // 0x69
            0x0003, // 0x6A
            0x0009, // 0x6B
            0x0013, // 0x6C
            0x0003, // 0x6D
            0x000E, // 0x6E
            -1,     // 0x6F
            0x001C, // 0x70
            -1,     // 0x71
            0x0005, // 0x72
            0x0002, // 0x73
            -1,     // 0x74
            0x0023, // 0x75
            0x0010, // 0x76
            0x0011, // 0x77
            -1,     // 0x78
            0x0009, // 0x79
            -1,     // 0x7A
            0x0002, // 0x7B
            -1,     // 0x7C
            0x000D, // 0x7D
            0x0002, // 0x7E
            -1,     // 0x7F
            0x003E, // 0x80
            -1,     // 0x81
            0x0002, // 0x82
            0x0027, // 0x83
            0x0045, // 0x84
            0x0002, // 0x85
            -1,     // 0x86
            -1,     // 0x87
            0x0042, // 0x88
            -1,     // 0x89
            -1,     // 0x8A
            -1,     // 0x8B
            0x000B, // 0x8C
            -1,     // 0x8D
            -1,     // 0x8E
            -1,     // 0x8F
            0x0013, // 0x90
            0x0041, // 0x91
            -1,     // 0x92
            0x0063, // 0x93
            -1,     // 0x94
            0x0009, // 0x95
            -1,     // 0x96
            0x0002, // 0x97
            -1,     // 0x98
            0x001A, // 0x99
            -1,     // 0x9A
            0x0102, // 0x9B
            0x0135, // 0x9C
            0x0033, // 0x9D
            -1,     // 0x9E
            -1,     // 0x9F
            0x0003, // 0xA0
            0x0009, // 0xA1
            0x0009, // 0xA2
            0x0009, // 0xA3
            0x0095, // 0xA4
            -1,     // 0xA5
            -1,     // 0xA6
            0x0004, // 0xA7
            -1,     // 0xA8
            -1,     // 0xA9
            0x0005, // 0xAA
            -1,     // 0xAB
            -1,     // 0xAC
            -1,     // 0xAD
            -1,     // 0xAE
            0x000D, // 0xAF
            -1,     // 0xB0
            -1,     // 0xB1
            -1,     // 0xB2
            -1,     // 0xB3
            -1,     // 0xB4
            0x0040, // 0xB5
            0x0009, // 0xB6
            -1,     // 0xB7
            -1,     // 0xB8
            0x0003, // 0xB9
            0x0006, // 0xBA
            0x0009, // 0xBB
            0x0003, // 0xBC
            -1,     // 0xBD
            -1,     // 0xBE
            -1,     // 0xBF
            0x0024, // 0xC0
            -1,     // 0xC1
            -1,     // 0xC2
            -1,     // 0xC3
            0x0006, // 0xC4
            0x00CB, // 0xC5
            0x0001, // 0xC6
            0x0031, // 0xC7
            0x0002, // 0xC8
            0x0006, // 0xC9
            0x0006, // 0xCA
            0x0007, // 0xCB
            -1,     // 0xCC
            0x0001, // 0xCD
            -1,     // 0xCE
            0x004E, // 0xCF
            -1,     // 0xD0
            0x0002, // 0xD1
            0x0019, // 0xD2
            -1,     // 0xD3
            -1,     // 0xD4
            -1,     // 0xD5
            -1,     // 0xD6
            -1,     // 0xD7
            -1,     // 0xD8
            0x010C, // 0xD9
            -1,     // 0xDA
            -1,     // 0xDB
            0x09,   // dc
            -1,     // dd
            -1,     // de
            -1,     // df
            -1,     // e0
            -1,     // e1
            0x0A,   // e2
            -1,     // e3
            -1,     // e4
            -1,     // e5
            0x05,   // e6
            0x0C,   // e7
            0x0D,   // e8
            0x4B,   // e9
            0x03,   // ea
            -1,     // eb
            -1,     // ec
            -1,     // ed
            0x0A,   // ee
            0x0015, // ef
            -1,     // f0
            0x09,   // f1
            0x19,   // f2
            0x1A,   // f3
            -1,     // f4
            0x15,   // f5
            -1,     // f6
            -1,     // f7
            0x6A,   // f8
            -1,     // f9
            -1,     // fa -> UOStore
            -1,     // fb -> public house content
            -1,     // fc
            -1,     // fd
            -1      // ff
        };

        public PacketsTable(ClientVersion version)
        {
            Log.Trace("Network calibration...");

            if (version >= ClientVersion.CV_500A)
            {
                _packetsTable[0x0B] = 0x07;
                _packetsTable[0x16] = -1;
                _packetsTable[0x31] = -1;
            }
            else
            {
                _packetsTable[0x0B] = 0x10A;
                _packetsTable[0x16] = 0x01;
                _packetsTable[0x31] = 0x01;
            }

            if (version >= ClientVersion.CV_5090)
            {
                _packetsTable[0xE1] = -1;
            }
            else
            {
                _packetsTable[0xE1] = 0x09;
            }

            if (version >= ClientVersion.CV_6013)
            {
                _packetsTable[0xE3] = -1;
                _packetsTable[0xE6] = 0x05;
                _packetsTable[0xE7] = 0x0C;
                _packetsTable[0xE8] = 0x0D;
                _packetsTable[0xE9] = 0x4B;
                _packetsTable[0xEA] = 0x03;
            }
            else
            {
                _packetsTable[0xE3] = 0x4D;
                _packetsTable[0xE6] = -1;
                _packetsTable[0xE7] = -1;
                _packetsTable[0xE8] = -1;
                _packetsTable[0xE9] = -1;
                _packetsTable[0xEA] = -1;
            }

            if (version >= ClientVersion.CV_6017)
            {
                _packetsTable[0x08] = 0x0F;
                _packetsTable[0x25] = 0x15;
            }
            else
            {
                _packetsTable[0x08] = 0x0E;
                _packetsTable[0x25] = 0x14;
            }

            if (version >= ClientVersion.CV_6060)
            {
                _packetsTable[0xEE] = 0x2000;
                _packetsTable[0xEF] = 0x2000;
                _packetsTable[0xF1] = 0x09;
            }
            else
            {
                _packetsTable[0xEE] = -1;
                _packetsTable[0xEF] = 0x15;
                _packetsTable[0xF1] = -1;
            }

            if (version >= ClientVersion.CV_60142)
            {
                _packetsTable[0xB9] = 0x05;
            }
            else
            {
                _packetsTable[0xB9] = 0x03;
            }

            if (version >= ClientVersion.CV_7000)
            {
                _packetsTable[0xEE] = 0x0A; //0x2000;
                _packetsTable[0xEF] = 0x15; //0x2000;
            }
            else
            {
                _packetsTable[0xEE] = -1;
                _packetsTable[0xEF] = 0x15;
            }

            if (version >= ClientVersion.CV_7090)
            {
                _packetsTable[0x24] = 0x09;
                _packetsTable[0x99] = 0x1E;
                _packetsTable[0xBA] = 0x0A;
                _packetsTable[0xF3] = 0x1A;
                _packetsTable[0xF1] = 0x09;
                _packetsTable[0xF2] = 0x19;
            }
            else
            {
                _packetsTable[0x24] = 0x07;
                _packetsTable[0x99] = 0x1A;
                _packetsTable[0xBA] = 0x06;
                _packetsTable[0xF3] = 0x18;
                _packetsTable[0xF1] = -1;
                _packetsTable[0xF2] = -1;
            }

            if (version >= ClientVersion.CV_70180)
            {
                _packetsTable[0x00] = 0x6A;
            }
            else
            {
                _packetsTable[0x00] = 0x68;
            }

            if (version >= ClientVersion.CV_706400)
            {
                _packetsTable[0xFA] = 0x01;
                _packetsTable[0xFB] = 0x02;
            }

            if (version >= ClientVersion.CV_7010400)
            {
                _packetsTable[0xD5] = 0x09;
                _packetsTable[0xFD] = 2;
            }
        }

        public short GetPacketLength(int id)
        {
            return (short) (id >= 0xFF ? -1 : _packetsTable[id]);
        }
    }
}