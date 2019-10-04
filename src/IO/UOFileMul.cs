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
            UOFile file = _idxFile ?? (UOFile)this;

            int count = (int)file.Length / 12;
            entries = new UOFileIndex[count];

            for (int i = 0; i < count; i++)
                entries[i] = new UOFileIndex(file.ReadInt(), file.ReadInt(), 0, file.ReadInt());
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