// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.IO
{
    public class UOFileMul : UOFile
    {
        private readonly UOFile _idxFile;

        public UOFileMul(string file, string idxfile) : this(file)
        {
            _idxFile = new UOFile(idxfile);
        }

        public UOFileMul(string file) : base(file)
        {

        }

        public UOFile IdxFile => _idxFile;


        public override void FillEntries()
        {
            UOFile f = _idxFile ?? this;
            int count = (int)f.Length / 12;
            Entries = new UOFileIndex[count];

            for (int i = 0; i < Entries.Length; i++)
            {
                ref var e = ref Entries[i];
                e.File = this;   // .mul mmf address
                e.Offset = f.ReadUInt32(); // .idx offset
                e.Length = f.ReadInt32();  // .idx length
                e.DecompressedLength = 0;   // UNUSED HERE --> .UOP

                int size = f.ReadInt32();

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
    }
}