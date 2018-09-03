using ClassicUO.IO;
using System.IO;

namespace ClassicUO.IO.Resources
{
    public static class Verdata
    {
        static Verdata()
        {
            string path = Path.Combine(FileManager.UoFolderPath, "verdata.mul");

            if (!System.IO.File.Exists(path))
            {
                Patches = new UOFileIndex5D[0];
                File = null;
            }
            else
            {
                File = new UOFileMul("verdata.mul");
                Patches = new UOFileIndex5D[File.ReadInt()];

                Patches = File.ReadArray<UOFileIndex5D>(File.ReadInt());

                /* for (int i = 0; i < Patches.Length; i++)
                 {
                     Patches[i].File = File.ReadInt();
                     Patches[i].Index = File.ReadInt();
                     Patches[i].Offset = File.ReadInt();
                     Patches[i].Length = File.ReadInt();
                     Patches[i].Extra = File.ReadInt();
                 }*/
            }
        }

        // FileIDs
        //0 - map0.mul
        //1 - staidx0.mul
        //2 - statics0.mul
        //3 - artidx.mul
        //4 - art.mul
        //5 - anim.idx
        //6 - anim.mul
        //7 - soundidx.mul
        //8 - sound.mul
        //9 - texidx.mul
        //10 - texmaps.mul
        //11 - gumpidx.mul
        //12 - gumpart.mul
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