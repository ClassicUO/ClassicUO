#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
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
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using ClassicUO.Utility;

namespace ClassicUO.IO.Resources
{
    internal class AnimDataLoader : ResourceLoader
    {
        private UOFileMul _file;

        public override Task Load()
        {
            return Task.Run(() => 
            {
                string path = Path.Combine(FileManager.UoFolderPath, "animdata.mul");

                if (File.Exists(path))
                {
                    _file = new UOFileMul(path);
                }
            });
        }

        public override void CleanResources()
        {
            //
        }

        public AnimDataFrame2 CalculateCurrentGraphic(ushort graphic)
        {
            IntPtr address = _file.StartAddress;

            if (address != IntPtr.Zero)
            {
                IntPtr addr = address + (graphic * 68 + 4 * ((graphic >> 3) + 1));

                //Stopwatch sw = Stopwatch.StartNew();
                //for (int i = 0; i < 2000000; i++)
                //{
                //    AnimDataFrame pad = Marshal.PtrToStructure<AnimDataFrame>(addr);
                //}

                //Console.WriteLine("Marshal: {0} ms", sw.ElapsedMilliseconds);

                //sw.Restart();
                //for (int i = 0; i < 2000000; i++)
                //{
                //    
                //}

                //Console.WriteLine("Custom: {0} ms", sw.ElapsedMilliseconds);

                //if (pad.FrameCount == 0)
                //{
                //    pad.FrameCount = 1;
                //    pad.FrameData[0] = 0;
                //}

                //if (pad.FrameInterval == 0)
                //    pad.FrameInterval = 1;
                AnimDataFrame2 a = UnsafeMemoryManager.ToStruct<AnimDataFrame2>(addr);

                return a;
            }

            return default;
        }
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal unsafe struct AnimDataFrame2
    {
        public fixed sbyte FrameData[64];
        public byte Unknown;
        public byte FrameCount;
        public byte FrameInterval;
        public byte FrameStart;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal readonly struct AnimDataFrame
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly sbyte[] FrameData;
        public readonly byte Unknown;
        public readonly byte FrameCount;
        public readonly byte FrameInterval;
        public readonly byte FrameStart;
    }
}