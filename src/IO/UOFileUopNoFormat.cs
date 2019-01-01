using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.IO.Resources;

namespace ClassicUO.IO
{
    internal class UOFileUopNoFormat : UOFile
    {
        private const uint UOP_MAGIC_NUMBER = 0x50594D;
        private readonly int _indexFile;


        public UOFileUopNoFormat(string path, int index) : base(path)
        {
            _indexFile = index;
        }

        public UOFileUopNoFormat(string path) : base(path)
        {
            Load();
        }


        protected override void Load(bool loadentries = true)
        {
            base.Load(loadentries);
            Seek(0);

            if (ReadInt() != UOP_MAGIC_NUMBER)
                throw new ArgumentException("Bad uop file");
            Skip(8);
            long nextblock = ReadLong();
            Skip(4);

            Entries = new UOFileIndex3D[ReadInt()];

            int idx = 0;
            do
            {
                Seek(nextblock);
                int fileCount = ReadInt();
                nextblock = ReadLong();

                for (int i = 0; i < fileCount; i++)
                {
                    long offset = ReadLong();
                    int headerLength = ReadInt();
                    int compressedLength = ReadInt();
                    int decompressedLength = ReadInt();
                    ulong hash = ReadULong();
                    Skip(6);

                    if (offset == 0)
                        continue;

                    Entries[idx++] = new UOFileIndex3D((uint)(offset + headerLength), compressedLength, decompressedLength);
                }
            } while (nextblock != 0);
        }

        internal void LoadEx(ref Dictionary<ulong, UopFileData> hashes)
        {
            Load();
            Seek(0);

            if (ReadInt() != UOP_MAGIC_NUMBER)
                throw new ArgumentException("Bad uop file");
            Skip(8);
            long nextblock = ReadLong();
            Skip(4);

            do
            {
                Seek(nextblock);
                int fileCount = ReadInt();
                nextblock = ReadLong();

                for (int i = 0; i < fileCount; i++)
                {
                    long offset = ReadLong();
                    int headerLength = ReadInt();
                    int compressedLength = ReadInt();
                    int decompressedLength = ReadInt();
                    ulong hash = ReadULong();
                    Skip(6);

                    if (offset == 0)
                        continue;

                    hashes.Add(hash, new UopFileData((uint)(offset + headerLength), (uint)compressedLength, (uint)decompressedLength, _indexFile));
                }
            } while (nextblock != 0);
        }
    }

}
