// SPDX-License-Identifier: BSD-2-Clause

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