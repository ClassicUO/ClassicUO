namespace ClassicUO.AssetsLoader
{
    public class UOFileMul : UOFile
    {
        private readonly int _count, _patch;
        private readonly UOFileIdxMul _idxFile;

        public UOFileMul(in string file, in string idxfile, in int count, in int patch = -1) : base(file)
        {
            _idxFile = new UOFileIdxMul(idxfile);
            _count = count;
            _patch = patch;
            Load();
        }

        public UOFileMul(in string file) : base(file)
        {
            Load();
        }

        public UOFile IdxFile => _idxFile;

        protected override void Load()
        {
            base.Load();

            if (_idxFile != null)
            {
                var count = (int) _idxFile.Length / 12;

                Entries = new UOFileIndex3D[count];

                for (var i = 0; i < count; i++)
                    Entries[i] = new UOFileIndex3D(_idxFile.ReadInt(), _idxFile.ReadInt(), _idxFile.ReadInt());

                var patches = Verdata.Patches;

                for (var i = 0; i < patches.Length; i++)
                {
                    var patch = patches[i];

                    if (patch.File == _patch && patch.Index >= 0 &&
                        patch.Index < Entries.Length)
                    {
                        var entry = Entries[patch.Index];
                        entry.Offset = patch.Offset;
                        entry.Length = patch.Length | (1 << 31);
                        entry.Extra = patch.Extra;
                    }
                }
            }
        }

        private class UOFileIdxMul : UOFile
        {
            public UOFileIdxMul(in string idxpath) : base(idxpath)
            {
                Load();
            }
        }
    }
}