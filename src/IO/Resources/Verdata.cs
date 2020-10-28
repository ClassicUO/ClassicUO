namespace ClassicUO.IO.Resources
{
    internal static class Verdata
    {
        static Verdata()
        {
            string path = UOFileManager.GetUOFilePath("verdata.mul");

            if (!System.IO.File.Exists(path))
            {
                Patches = new UOFileIndex5D[0];
                File = null;
            }
            else
            {
                File = new UOFileMul(path);

                // the scope of this try/catch is to avoid unexpected crashes if servers redestribuite wrong verdata
                try
                {
                    Patches = File.ReadArray<UOFileIndex5D>(File.ReadInt());
                }
                catch
                {
                    Patches = new UOFileIndex5D[0];
                }
            }
        }

        // FileIDs
        //0 - map0.mul
        //1 - staidx0.mul
        //2 - statics0.mul
        //3 - artidx.mul
        //4 - FileManager.Art.mul
        //5 - anim.idx
        //6 - anim.mul
        //7 - soundidx.mul
        //8 - sound.mul
        //9 - texidx.mul
        //10 - texmaps.mul
        //11 - gumpidx.mul
        //12 - gumpFileManager.Art.mul
        //13 - multi.idx
        //14 - multi.mul
        //15 - skills.idx
        //16 - skills.mul
        //30 - tiledata.mul
        //31 - animdata.mul 

        public static UOFileIndex5D[] Patches { get; }

        public static UOFileMul File { get; }
    }
}