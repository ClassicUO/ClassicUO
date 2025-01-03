// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.IO;
using ClassicUO.Utility.Logging;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ClassicUO.Assets
{
    public sealed class VerdataLoader : UOFileLoader
    {
        public VerdataLoader(UOFileManager fileManager) : base(fileManager) { }

        public unsafe override void Load()
        {
            string path = FileManager.GetUOFilePath("verdata.mul");

            if (!System.IO.File.Exists(path))
            {
                File = null;
            }
            else
            {
                File = new UOFileMul(path);

                // the scope of this try/catch is to avoid unexpected crashes if servers redestribuite wrong verdata
                try
                {
                    var len = File.ReadInt32();
                    Patches = new UOFileIndex5D[len];

                    for (var i = 0; i < len; i++)
                    {
                        Patches[i] = File.Read<UOFileIndex5D>();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"error while reading verdata.mul\n{ex}");
                }
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
        //12 - gumps.mul
        //13 - multi.idx
        //14 - multi.mul
        //15 - skills.idx
        //16 - skills.mul
        //30 - tiledata.mul
        //31 - animdata.mul

        public UOFileIndex5D[] Patches { get; private set; } = Array.Empty<UOFileIndex5D>();

        public UOFileMul File { get; private set; }
    }
}