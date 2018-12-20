using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Renderer;

namespace ClassicUO.IO.Resources
{
    class AnimDataLoader : ResourceLoader
    {
        private UOFileMul _file;

        public override void Load()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "animdata.mul");
            if (File.Exists(path)) _file = new UOFileMul(path, false);
        }

        protected override void CleanResources()
        {
            throw new NotImplementedException();
        }

        public AnimDataFrame CalculateCurrentGraphic(ushort graphic)
        {
            IntPtr address = _file.StartAddress;

            if (address != IntPtr.Zero)
            {
                int addr = graphic * 68 + 4 * ((graphic >> 3) + 1);
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
