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
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ClassicUO.IO.Resources
{
    internal class AnimDataLoader : UOFileLoader
    {
        private static AnimDataLoader _instance;
        private UOFileMul _file;

        private AnimDataLoader()
        {
        }

        public static AnimDataLoader Instance => _instance ?? (_instance = new AnimDataLoader());

        public UOFile AnimDataFile => _file;

        public override Task Load()
        {
            return Task.Run
            (
                () =>
                {
                    string path = UOFileManager.GetUOFilePath("animdata.mul");

                    if (File.Exists(path))
                    {
                        _file = new UOFileMul(path);
                    }
                }
            );
        }

        public unsafe AnimDataFrame CalculateCurrentGraphic(ushort graphic)
        {
            IntPtr address = _file?.StartAddress ?? IntPtr.Zero;

            if (address != IntPtr.Zero)
            {
                IntPtr addr = address + (graphic * 68 + 4 * ((graphic >> 3) + 1));

                if (addr.ToInt64() < address.ToInt64() + _file.Length)
                {
                    ref AnimDataFrame a = ref Unsafe.AsRef<AnimDataFrame>((void*)addr);

                    return a;
                }
            }

            return default;
        }
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal unsafe struct AnimDataFrame
    {
        public fixed sbyte FrameData[64];
        public byte Unknown;
        public byte FrameCount;
        public byte FrameInterval;
        public byte FrameStart;
    }
}