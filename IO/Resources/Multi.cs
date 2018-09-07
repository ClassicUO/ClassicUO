#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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
using ClassicUO.IO;
using System.IO;
using System.Runtime.InteropServices;

namespace ClassicUO.IO.Resources
{
    public static class Multi
    {
        private static UOFileMul _file;
        private static int _itemOffset;

        public static void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "Multi.mul");
            string pathidx = Path.Combine(FileManager.UoFolderPath, "Multi.idx");

            if (File.Exists(path) && File.Exists(pathidx))
                _file = new UOFileMul(path, pathidx, 0x2000, 14);
            else
                throw new FileNotFoundException();

            _itemOffset = FileManager.ClientVersion >= ClientVersions.CV_7090 ? Marshal.SizeOf<MultiBlockNew>() : Marshal.SizeOf<MultiBlock>();
        }

        public static unsafe MultiBlock GetMulti(int index)
        {
            return *((MultiBlock*)(_file.PositionAddress + index * _itemOffset));
        }


        public static int GetCount(int graphic)
        {
            graphic &= FileManager.GraphicMask;
            (int length, int extra, bool patcher) = _file.SeekByEntryIndex(graphic);
            int count = length / _itemOffset;
            return count;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct MultiBlock
    {
        public readonly ushort ID;
        public readonly short X;
        public readonly short Y;
        public readonly short Z;
        public readonly uint Flags;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct MultiBlockNew
    {
        public readonly ushort ID;
        public readonly short X;
        public readonly short Y;
        public readonly short Z;
        public readonly uint Flags;
        public readonly int Unknown;
    }
}