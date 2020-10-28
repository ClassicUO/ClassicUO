using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ClassicUO.Data;
using ClassicUO.Game;

namespace ClassicUO.IO.Resources
{
    internal class MultiLoader : UOFileLoader
    {
        private static MultiLoader _instance;
        private DataReader _reader;

        private MultiLoader()
        {
        }

        public static MultiLoader Instance => _instance ?? (_instance = new MultiLoader());

        public int Count { get; private set; }
        public UOFile File { get; private set; }

        public bool IsUOP { get; private set; }
        public int Offset { get; private set; }


        public override unsafe Task Load()
        {
            return Task.Run
            (
                () =>
                {
                    string uopPath = UOFileManager.GetUOFilePath("MultiCollection.uop");

                    if (Client.IsUOPInstallation && System.IO.File.Exists(uopPath))
                    {
                        Count = Constants.MAX_MULTI_DATA_INDEX_COUNT;
                        File = new UOFileUop(uopPath, "build/multicollection/{0:D6}.bin");
                        Entries = new UOFileIndex[Count];
                        _reader = new DataReader();
                        IsUOP = true;
                    }
                    else
                    {
                        string path = UOFileManager.GetUOFilePath("multi.mul");
                        string pathidx = UOFileManager.GetUOFilePath("multi.idx");

                        if (System.IO.File.Exists(path) && System.IO.File.Exists(pathidx))
                        {
                            File = new UOFileMul(path, pathidx, Constants.MAX_MULTI_DATA_INDEX_COUNT, 14);

                            Count = Offset = Client.Version >= ClientVersion.CV_7090 ?
                                sizeof(MultiBlockNew) + 2 :
                                sizeof(MultiBlock);
                        }
                    }

                    File.FillEntries(ref Entries);
                }
            );
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal ref struct MultiBlock
    {
        public ushort ID;
        public short X;
        public short Y;
        public short Z;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal ref struct MultiBlockNew
    {
        public ushort ID;
        public short X;
        public short Y;
        public short Z;
        public ushort Flags;
        public uint Unknown;
    }
}