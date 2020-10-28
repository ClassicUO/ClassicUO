namespace ClassicUO.IO
{
    internal class UOFileMul : UOFile
    {
        private readonly int _count, _patch;
        private readonly UOFileIdxMul _idxFile;

        public UOFileMul(string file, string idxfile, int count, int patch = -1) : this(file)
        {
            _idxFile = new UOFileIdxMul(idxfile);
            _count = count;
            _patch = patch;
        }

        public UOFileMul(string file) : base(file)
        {
            Load();
        }

        public UOFile IdxFile => _idxFile;


        public override void FillEntries(ref UOFileIndex[] entries)
        {
            UOFile file = _idxFile ?? (UOFile) this;

            int count = (int) file.Length / 12;
            entries = new UOFileIndex[count];

            for (int i = 0; i < count; i++)
            {
                ref UOFileIndex e = ref entries[i];
                e.Address = StartAddress;   // .mul mmf address
                e.FileSize = (uint) Length; // .mul mmf length
                e.Offset = file.ReadUInt(); // .idx offset
                e.Length = file.ReadInt();  // .idx length
                e.DecompressedLength = 0;   // UNUSED HERE --> .UOP

                int size = file.ReadInt();

                if (size > 0)
                {
                    e.Width = (short) (size >> 16);
                    e.Height = (short) (size & 0xFFFF);
                }
            }
        }

        public override void Dispose()
        {
            _idxFile?.Dispose();
            base.Dispose();
        }

        private class UOFileIdxMul : UOFile
        {
            public UOFileIdxMul(string idxpath) : base(idxpath)
            {
                Load();
            }

            public override void FillEntries(ref UOFileIndex[] entries)
            {
            }
        }
    }
}