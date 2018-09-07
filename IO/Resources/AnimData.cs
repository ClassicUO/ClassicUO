#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2015 ClassicUO Development Team)
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
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ClassicUO.IO.Resources
{
    public static class AnimData
    {
        private static UOFileMul _file;

        public static void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "animdata.mul");

            if (File.Exists(path)) _file = new UOFileMul(path);
        }


        public static AnimDataFrame CalculateCurrentGraphic(ushort graphic)
        {
            IntPtr address = _file.StartAddress;

            if (address != IntPtr.Zero)
            {
                int addr = graphic * 68 + 4 * (graphic / 8 + 1);

                AnimDataFrame pad = Marshal.PtrToStructure<AnimDataFrame>(address + addr);

                //if (pad.FrameCount == 0)
                //{
                //    pad.FrameCount = 1;
                //    pad.FrameData[0] = 0;
                //}

                //if (pad.FrameInterval == 0)
                //    pad.FrameInterval = 1;

                return pad;
            }

            return default;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct AnimDataFrame
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly sbyte[] FrameData;

        public readonly byte Unknown;
        public readonly byte FrameCount;
        public readonly byte FrameInterval;
        public readonly byte FrameStart;
    }
}