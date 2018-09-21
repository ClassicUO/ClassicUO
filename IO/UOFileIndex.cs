#region license

//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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
    public struct UOFileIndex3D
    {
        public UOFileIndex3D(long offset, int length, int extra = 0)
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
        public UOFileIndex5D(int file, int index, int offset, int length, int extra = 0)
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