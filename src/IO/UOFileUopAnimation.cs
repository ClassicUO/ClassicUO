using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.IO.Resources;

namespace ClassicUO.IO
{
    public class UOFileUopAnimation : UOFile
    {
        private const uint UOP_MAGIC_NUMBER = 0x50594D;
        private readonly int _indexFile;

        public UOFileUopAnimation(string path, int index) : base(path)
        {
            _indexFile = index;
        }

        internal void LoadEx(ref Dictionary<ulong, UOPAnimationData> hashes)
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
                    UOPAnimationData data = new UOPAnimationData((uint)(offset + headerLength), (uint)compressedLength, (uint)decompressedLength, _indexFile);
                    hashes.Add(hash, data);
                }
            } while (nextblock != 0);
        }
    }

}
