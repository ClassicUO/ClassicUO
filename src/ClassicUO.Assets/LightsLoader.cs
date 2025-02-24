// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.IO;
using ClassicUO.Utility;
using System;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public sealed class LightsLoader : UOFileLoader
    {
        private UOFileMul _file;

        public const int MAX_LIGHTS_DATA_INDEX_COUNT = 100;

        public LightsLoader(UOFileManager fileManager) : base(fileManager) { }

        public UOFileMul File => _file;

        public override void Load()
        {
            string path = FileManager.GetUOFilePath("light.mul");
            string pathidx = FileManager.GetUOFilePath("lightidx.mul");

            FileSystemHelper.EnsureFileExists(path);
            FileSystemHelper.EnsureFileExists(pathidx);

            _file = new UOFileMul(path, pathidx);
            _file.FillEntries();
        }

        public LightInfo GetLight(uint idx)
        {
            ref var entry = ref _file.GetValidRefEntry((int)idx);

            if (entry.Width == 0 && entry.Height == 0)
            {
                return default;
            }

            _file.Seek(entry.Offset, System.IO.SeekOrigin.Begin);
            var buffer = new uint[entry.Width * entry.Height];

            for (int i = 0; i < entry.Height; i++)
            {
                int pos = i * entry.Width;

                for (int j = 0; j < entry.Width; j++)
                {
                    ushort val = _file.ReadUInt8();
                    // Light can be from -31 to 31. When they are below 0 they are bit inverted
                    if (val > 0x1F)
                    {
                        val = (ushort)(~val & 0x1F);
                    }
                    uint rgb24 = (uint)((val << 19) | (val << 11) | (val << 3));

                    if (val != 0)
                    {
                        buffer[pos + j] = rgb24 | 0xFF_00_00_00;
                    }
                }
            }

            return new LightInfo()
            {
                Pixels = buffer,
                Width = entry.Width,
                Height = entry.Height
            };
        }
    }

    public ref struct LightInfo
    {
        public Span<uint> Pixels;
        public int Width;
        public int Height;
    }
}
