﻿#region license
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
using System.Runtime.InteropServices;

namespace ClassicUO.IO
{
    internal struct UOFileIndex
    {
        public UOFileIndex(IntPtr address, uint fileSize, long offset, int length, int decompressed, short width = 0, short height = 0, ushort hue = 0)
        {
            Address = address;
            FileSize = fileSize;
            Offset = offset;
            Length = length;
            DecompressedLength = decompressed;
            Width = width;
            Height = height;
            Hue = hue;

            AnimOffset = 0;
        }

        public IntPtr Address;
        public uint FileSize;
        public long Offset;
        public int Length;
        public int DecompressedLength;
        public short Width;
        public short Height;
        public ushort Hue;
        public sbyte AnimOffset;

        public static UOFileIndex Invalid = new UOFileIndex(IntPtr.Zero, 0, 0, 0, 0, 0, 0);
    }

    internal struct UOFileIndex5D
    {
        public UOFileIndex5D(uint file, uint index, uint offset, uint length, uint extra = 0)
        {
            FileID = file;
            BlockID = index;
            Position = offset;
            Length = length;
            GumpData = extra;
        }

        public uint FileID;
        public uint BlockID;
        public uint Position;
        public uint Length;
        public uint GumpData;
    }
}