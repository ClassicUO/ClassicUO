namespace ClassicUO.IO
{
    public struct UOFileIndex3D
    {
        public UOFileIndex3D(long offset,  int length,  int extra = 0)
        {
            Offset = offset;
            Length = length;
            Extra = extra;
        }

        public long Offset;
        public int Length;
        public int Extra;
    }

    public struct UOFileIndex5D
    {
        public UOFileIndex5D(int file,  int index,  int offset,  int length,  int extra = 0)
        {
            File = file;
            Index = index;
            Offset = offset;
            Length = length;
            Extra = extra;
        }

        public int File;
        public int Index;
        public int Offset;
        public int Length;
        public int Extra;
    }
}