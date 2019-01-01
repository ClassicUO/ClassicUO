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
using System.IO;
using System.IO.Compression;

namespace ClassicUO.Utility
{
    internal static class ZLib
    {
        public static void Decompress(byte[] source, int offset, byte[] dest, int length)
        {
            using (MemoryStream ms = new MemoryStream(source, offset, source.Length - offset))
            {
                ms.Seek(2, SeekOrigin.Begin);
                using (DeflateStream stream = new DeflateStream(ms, CompressionMode.Decompress)) stream.Read(dest, 0, length);
            }
        }
    }
}