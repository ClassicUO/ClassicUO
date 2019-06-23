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

using System;
using System.Collections.Generic;

using ClassicUO.IO.Resources;

namespace ClassicUO.IO
{
    internal class UOFileUopNoFormat : UOFile
    {
        private const uint UOP_MAGIC_NUMBER = 0x50594D;
        private readonly int _indexFile;


        public UOFileUopNoFormat(string path, int index) : base(path)
        {
            _indexFile = index;
        }

        public UOFileUopNoFormat(string path) : base(path)
        {
            Load();
        }


        protected override void Load(bool loadentries = true)
        {
            base.Load(loadentries);
            Seek(0);

            if (ReadInt() != UOP_MAGIC_NUMBER)
                throw new ArgumentException("Bad uop file");

            Skip(8);
            long nextblock = ReadLong();
            Skip(4);

            Entries = new UOFileIndex3D[ReadInt()];

            int idx = 0;

            do
            {
                Seek(nextblock);
                int fileCount = ReadInt();
                nextblock = ReadLong();

                for (int i = 0; i < fileCount; i++)
                {
                    long offset = ReadLong();
                    int headerLength = ReadInt();
                    int compressedLength = ReadInt();
                    int decompressedLength = ReadInt();
                    ulong hash = ReadULong();
                    Skip(6);

                    if (offset == 0)
                        continue;

                    Entries[idx++] = new UOFileIndex3D((uint) (offset + headerLength), compressedLength, decompressedLength);
                }
            } while (nextblock != 0);
        }

        internal void LoadEx(ref Dictionary<ulong, UopFileData> hashes)
        {
            Load();
            Seek(0);

            if (ReadInt() != UOP_MAGIC_NUMBER)
                throw new ArgumentException("Bad uop file");

            Skip(8);
            long nextblock = ReadLong();
            Skip(4);

            do
            {
                Seek(nextblock);
                int fileCount = ReadInt();
                nextblock = ReadLong();

                for (int i = 0; i < fileCount; i++)
                {
                    long offset = ReadLong();
                    int headerLength = ReadInt();
                    int compressedLength = ReadInt();
                    int decompressedLength = ReadInt();
                    ulong hash = ReadULong();
                    Skip(6);

                    if (offset == 0)
                        continue;

                    hashes.Add(hash, new UopFileData((uint) (offset + headerLength), (uint) compressedLength, (uint) decompressedLength, _indexFile));
                }
            } while (nextblock != 0);
        }
    }
}