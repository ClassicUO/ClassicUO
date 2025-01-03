#region license

// BSD 2-Clause License
//
// Copyright (c) 2025, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//
// - Redistributions of source code must retain the above copyright notice, this
//   list of conditions and the following disclaimer.
//
// - Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using ClassicUO.IO;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public sealed class AnimDataLoader : UOFileLoader
    {
        private UOFileMul _file;

        public AnimDataLoader(UOFileManager fileManager) : base(fileManager)
        {
        }

        public UOFile AnimDataFile => _file;

        public override void Load()
        {
            var path = FileManager.GetUOFilePath("animdata.mul");

            if (File.Exists(path))
            {
                _file = new UOFileMul(path);
            }
        }

        public AnimDataFrame CalculateCurrentGraphic(ushort graphic)
        {
            if (_file == null)
                return default;

            var pos = (graphic * 68 + 4 * ((graphic >> 3) + 1));
            if (pos >= _file.Length)
            {
                return default;
            }

            _file.Seek(pos, SeekOrigin.Begin);

            Span<byte> buf = stackalloc byte[Unsafe.SizeOf<AnimDataFrame>()];
            _file.Read(buf);

            var span = MemoryMarshal.Cast<byte, AnimDataFrame>(buf);
            return span[0];

        }
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct AnimDataFrame
    {
        public fixed sbyte FrameData[64];
        public byte Unknown;
        public byte FrameCount;
        public byte FrameInterval;
        public byte FrameStart;
    }
}