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
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using ClassicUO.Configuration;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using ClassicUO.Utility;

namespace ClassicUO.Dust765.Dust765
{
    internal static class BuffFilters
    {
        public static readonly List<ushort> ECBuffs = new List<ushort>();
        public static readonly List<ushort> ECDebuffs = new List<ushort>();
        public static readonly List<ushort> ECStates = new List<ushort>();
        public static readonly List<ushort> ModernBuffs = new List<ushort>();

        public static void Load()
        {
            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string buffs = Path.Combine(path, "ecbuffs.txt");
            string debuffs = Path.Combine(path, "ecdebuffs.txt");
            string states = Path.Combine(path, "ecstates.txt");
            string modern = Path.Combine(path, "modernbuffs.txt");

            //BUFFS
            if (!File.Exists(buffs))
            {
                using (StreamWriter writer = new StreamWriter(buffs))
                {
                    ushort[] buffList =
                    {
                        //UNKNOWN (COULD NOT FIND A ICON)
                        0x5D51,
                        //GREEN ICONS
                        0x094B, 0x094C, 0x094D, 0x094E, 0x5011, 0x5DDF, 0x5DE1, 0x5DE4, 0x5DE5, 0x5DE6, 0x753C, 0x753D, 0x753E, 0x753E, 0x753F,
                        0x7540, 0x7542, 0x7543, 0x7547, 0x754D, 0x754E, 0x7550, 0x7553, 0x7554, 0x7558, 0x7559, 0x755E, 0x7562, 0x7563, 0x7567,
                        0x758B, 0x758D, 0x7592, 0x7593, 0x7594, 0x7596, 0x7598, 0x759C, 0x759E, 0x759F, 0x75A3, 0x75A4, 0x75A7, 0x75C0, 0x75C2,
                        0x75C3, 0x75C4, 0x75C5, 0x75F2, 0x75F3, 0x75F4, 0x75F5, 0x75F6, 0x75F7, 0x75F8, 0x75F9, 0x75FA, 0x75FB, 0x75FC, 0x75FD,
                        0x75FE, 0x75FF, 0x7600, 0x7601, 0x7602, 0x7617, 0x7618, 0x7619, 0x761A, 0x761B, 0x9bb5, 0x9bb6, 0x9bb7, 0x9bb8, 0x9bb9,
                        0x9bba, 0x9bbb, 0x9bbc, 0x9bbd, 0x9bbe, 0x9bbf, 0x9bc0, 0x9bc1, 0x9bc2, 0x9bc3, 0x9bc4, 0x9bc5, 0x9bc6, 0x9bc7, 0x9bc8,
                        0x9bd0, 0x9bd1, 0x9bd2, 0x9bd4, 0x9bd5, 0x9bd6, 0x9bd7, 0x9bd8, 0x9bd9, 0x9bda, 0x9bdd, 0x9bdf, 0x9CDE, 0xC342, 0xC346,
                        0xC347, 0xC348, 0xC349, 0xC34A, 0xC34B, 0xC34C, 0xC34D, 0xC34E
                    };

                    for (int i = 0; i < buffList.Length; i++)
                    {
                        ushort g = buffList[i];

                        writer.WriteLine(g);
                    }
                }
            }

            TextFileParser buffParser = new TextFileParser(File.ReadAllText(buffs), new[] { ' ', '\t', ',' }, new[] { '#', ';' }, new[] { '"', '"' });

            while (!buffParser.IsEOF())
            {
                List<string> ss = buffParser.ReadTokens();

                if (ss != null && ss.Count != 0)
                {
                    if (ushort.TryParse(ss[0], out ushort graphic))
                    {
                        ECBuffs.Add(graphic);
                    }
                }
            }
            //DEBUFFS
            if (!File.Exists(debuffs))
            {
                using (StreamWriter writer = new StreamWriter(debuffs))
                {
                    ushort[] debuffList =
                    {
                        //RED ICONS
                        0x094F, 0x0950, 0x0951, 0x5DE3, 0x753A, 0x7540, 0x7544, 0x7545, 0x7546, 0x7548, 0x7549, 0x754A, 0x754C, 0x754F, 0x7551,
                        0x7552, 0x7555, 0x7556, 0x755A, 0x755B, 0x755C, 0x755D, 0x755F, 0x7560, 0x7564, 0x7566, 0x7568, 0x7569, 0x758A, 0x758C,
                        0x758F, 0x7590, 0x7591, 0x7599, 0x759B, 0x75A0, 0x75A1, 0x75A5, 0x75A6, 0x75A8, 0x75C1, 0x7603, 0x7604, 0x7605, 0x7606,
                        0x7607, 0x7608, 0x7609, 0x760A, 0x760B, 0x760C, 0x760D, 0x760E, 0x760F, 0x7610, 0x7611, 0x7612, 0x7613, 0x7614, 0x7615,
                        0x7616, 0x9bc9, 0x9bca, 0x9bcb, 0x9bcc, 0x9bcd, 0x9bce, 0x9bcf, 0x9bd3, 0x9bdb, 0x9bdc, 0x9bde, 0xC343, 0xC344, 0xC345
                    };

                    for (int i = 0; i < debuffList.Length; i++)
                    {
                        ushort g = debuffList[i];

                        writer.WriteLine(g);
                    }
                }
            }

            TextFileParser debuffParser = new TextFileParser(File.ReadAllText(debuffs), new[] { ' ', '\t', ',' }, new[] { '#', ';' }, new[] { '"', '"' });

            while (!debuffParser.IsEOF())
            {
                List<string> ss = debuffParser.ReadTokens();

                if (ss != null && ss.Count != 0)
                {
                    if (ushort.TryParse(ss[0], out ushort graphic))
                    {
                        ECDebuffs.Add(graphic);
                    }
                }
            }
            //STATES
            if (!File.Exists(states))
            {
                using (StreamWriter writer = new StreamWriter(states))
                {
                    ushort[] statesList =
                    {
                        //BLUE ICONS
                        0x753B, 0x754B, 0x7557, 0x7561, 0x7565, 0x758E, 0x7595
                    };

                    for (int i = 0; i < statesList.Length; i++)
                    {
                        ushort g = statesList[i];

                        writer.WriteLine(g);
                    }
                }
            }

            TextFileParser statesParser = new TextFileParser(File.ReadAllText(states), new[] { ' ', '\t', ',' }, new[] { '#', ';' }, new[] { '"', '"' });

            while (!statesParser.IsEOF())
            {
                List<string> ss = statesParser.ReadTokens();

                if (ss != null && ss.Count != 0)
                {
                    if (ushort.TryParse(ss[0], out ushort graphic))
                    {
                        ECStates.Add(graphic);
                    }
                }
            }
            //MODERNCCOLDOWNS
            if (!File.Exists(modern))
            {
                using (StreamWriter writer = new StreamWriter(modern))
                {
                    ushort[] modernList =
                    {
                        //UNKNOWN (COULD NOT FIND A ICON)
                        0x5D51,
                        //GREEN ICONS
                        0x094B, 0x094C, 0x094D, 0x094E, 0x5011, 0x5DDF, 0x5DE1, 0x5DE4, 0x5DE5, 0x5DE6, 0x753C, 0x753D, 0x753E, 0x753E, 0x753F,
                        0x7540, 0x7542, 0x7543, 0x7547, 0x754D, 0x754E, 0x7550, 0x7553, 0x7554, 0x7558, 0x7559, 0x755E, 0x7562, 0x7563, 0x7567,
                        0x758B, 0x758D, 0x7592, 0x7593, 0x7594, 0x7596, 0x7598, 0x759C, 0x759E, 0x759F, 0x75A3, 0x75A4, 0x75A7, 0x75C0, 0x75C2,
                        0x75C3, 0x75C4, 0x75C5, 0x75F2, 0x75F3, 0x75F4, 0x75F5, 0x75F6, 0x75F7, 0x75F8, 0x75F9, 0x75FA, 0x75FB, 0x75FC, 0x75FD,
                        0x75FE, 0x75FF, 0x7600, 0x7601, 0x7602, 0x7617, 0x7618, 0x7619, 0x761A, 0x761B, 0x9bb5, 0x9bb6, 0x9bb7, 0x9bb8, 0x9bb9,
                        0x9bba, 0x9bbb, 0x9bbc, 0x9bbd, 0x9bbe, 0x9bbf, 0x9bc0, 0x9bc1, 0x9bc2, 0x9bc3, 0x9bc4, 0x9bc5, 0x9bc6, 0x9bc7, 0x9bc8,
                        0x9bd0, 0x9bd1, 0x9bd2, 0x9bd4, 0x9bd5, 0x9bd6, 0x9bd7, 0x9bd8, 0x9bd9, 0x9bda, 0x9bdd, 0x9bdf, 0x9CDE, 0xC342, 0xC346,
                        0xC347, 0xC348, 0xC349, 0xC34A, 0xC34B, 0xC34C, 0xC34D, 0xC34E,
                        //RED ICONS
                        0x094F, 0x0950, 0x0951, 0x5DE3, 0x753A, 0x7540, 0x7544, 0x7545, 0x7546, 0x7548, 0x7549, 0x754A, 0x754C, 0x754F, 0x7551,
                        0x7552, 0x7555, 0x7556, 0x755A, 0x755B, 0x755C, 0x755D, 0x755F, 0x7560, 0x7564, 0x7566, 0x7568, 0x7569, 0x758A, 0x758C,
                        0x758F, 0x7590, 0x7591, 0x7599, 0x759B, 0x75A0, 0x75A1, 0x75A5, 0x75A6, 0x75A8, 0x75C1, 0x7603, 0x7604, 0x7605, 0x7606,
                        0x7607, 0x7608, 0x7609, 0x760A, 0x760B, 0x760C, 0x760D, 0x760E, 0x760F, 0x7610, 0x7611, 0x7612, 0x7613, 0x7614, 0x7615,
                        0x7616, 0x9bc9, 0x9bca, 0x9bcb, 0x9bcc, 0x9bcd, 0x9bce, 0x9bcf, 0x9bd3, 0x9bdb, 0x9bdc, 0x9bde, 0xC343, 0xC344, 0xC345,
                        //BLUE ICONS
                        0x753B, 0x754B, 0x7557, 0x7561, 0x7565, 0x758E, 0x7595
                    };

                    for (int i = 0; i < modernList.Length; i++)
                    {
                        ushort g = modernList[i];

                        writer.WriteLine(g);
                    }
                }
            }

            TextFileParser modernParser = new TextFileParser(File.ReadAllText(modern), new[] { ' ', '\t', ',' }, new[] { '#', ';' }, new[] { '"', '"' });

            while (!modernParser.IsEOF())
            {
                List<string> ss = modernParser.ReadTokens();

                if (ss != null && ss.Count != 0)
                {
                    if (ushort.TryParse(ss[0], out ushort graphic))
                    {
                        ModernBuffs.Add(graphic);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBuff(ushort g)
        {
            return ECBuffs.Contains(g);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDebuff(ushort g)
        {
            return ECDebuffs.Contains(g);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsState(ushort g)
        {
            return ECStates.Contains(g);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsModernBuff(ushort g)
        {
            return ModernBuffs.Contains(g);
        }
    }
}