#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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
using System.IO;

namespace ClassicUO.IO.Audio.MP3Sharp.IO
{
    internal class RandomAccessFileStream
    {
        public static FileStream CreateRandomAccessFile(string fileName, string mode)
        {
            FileStream newFile = null;

            if (mode.CompareTo("rw") == 0)
            {
                newFile = new FileStream(fileName, FileMode.OpenOrCreate,
                                         FileAccess.ReadWrite);
            }
            else if (mode.CompareTo("r") == 0)
                newFile = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            else
                throw new ArgumentException();

            return newFile;
        }
    }
}