#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System.IO;

namespace ClassicUO.IO.Resources
{
    internal static class Verdata
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