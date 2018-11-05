#region license

//  Copyright (C) 2018 ClassicUO Development Community on Github
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

using ClassicUO.IO.Resources;

namespace ClassicUO.IO
{
    public class UOFileMul : UOFile
    {
        private readonly int _count, _patch;
        private readonly UOFileIdxMul _idxFile;

        public UOFileMul(string file, string idxfile, int count, int patch = -1, bool load = true) : base(file)
        {
            _idxFile = new UOFileIdxMul(idxfile);
            _count = count;
            _patch = patch;
            Load(load);
        }

        public UOFileMul(string file, bool load = true) : base(file)
        {
            Load(load);
        }

        public UOFile IdxFile => _idxFile;

        protected override void Load(bool loadentries = true)
        {
            base.Load(loadentries);

            if (loadentries && _idxFile != null)
            {
                int count = (int) _idxFile.Length / 12;
                Entries = new UOFileIndex3D[count];

                for (int i = 0; i < count; i++)
                    Entries[i] = new UOFileIndex3D(_idxFile.ReadInt(), _idxFile.ReadInt(), 0, _idxFile.ReadInt());
                UOFileIndex5D[] patches = Verdata.Patches;

                for (int i = 0; i < patches.Length; i++)
                {
                    UOFileIndex5D patch = patches[i];

                    if (patch.File == _patch && patch.Index >= 0 && patch.Index < Entries.Length)
                    {
                        ref UOFileIndex3D entry = ref Entries[patch.Index];
                        entry.Offset = patch.Offset;
                        entry.Length = patch.Length | (1 << 31);
                        entry.Extra = patch.Extra;
                    }
                }
            }
        }

        public override void Unload()
        {
            _idxFile?.Unload();
            base.Unload();
        }

        private class UOFileIdxMul : UOFile
        {
            public UOFileIdxMul(string idxpath) : base(idxpath)
            {
                Load();
            }
        }
    }
}