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

using System.Text;

namespace ClassicUO.Utility
{
    public static class StringHelper
    {
        public static string CapitalizeFirstCharacter(string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            if (str.Length == 1)
                return char.ToUpper(str[0]).ToString();

            return char.ToUpper(str[0]) + str.Substring(1);
        }

        public static string CapitalizeAllWords(string str)
        {
            if (string.IsNullOrEmpty(str))
                return string.Empty;

            if (str.Length == 1)
                return char.ToUpper(str[0]).ToString();
            StringBuilder sb = new StringBuilder();
            bool capitalizeNext = true;

            for (int i = 0; i < str.Length; i++)
            {
                if (capitalizeNext)
                    sb.Append(char.ToUpper(str[i]));
                else
                    sb.Append(str[i]);
                capitalizeNext = " .,;!".Contains(str[i].ToString());
            }

            return sb.ToString();
        }

        public static unsafe string ReadUTF8(byte* data)
        {
            byte* endPtr = data;

            if (*endPtr != 0)
            {
                int bytes = 0;

                while (*endPtr != 0)
                {
                    endPtr++;
                    bytes++;
                }

                char* buffer = stackalloc char[bytes];
                int chars = Encoding.UTF8.GetChars(data, bytes, buffer, bytes);

                return new string(buffer, 0, chars);
            }

            return string.Empty;
        }
    }
}