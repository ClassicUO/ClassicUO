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
using System.Text;

namespace ClassicUO.Utility
{
    internal static class Crypter
    {
        public static string Encrypt(string source)
        {
            byte[] buff = Encoding.ASCII.GetBytes(source);
            int kidx = 0;
            string key = CalculateKey();

            if (key == string.Empty)
                return string.Empty;

            StringBuilder sb = new StringBuilder(source.Length * 2 + 2);
            sb.Append("1+");

            for (int i = 0; i < buff.Length; i++)
            {
                sb.AppendFormat("{0:X2}", (byte) (buff[i] ^ (byte) key[kidx++]));

                if (kidx >= key.Length)
                    kidx = 0;
            }

            return sb.ToString();
        }

        public static string Decrypt(string source)
        {
            byte[] buff = null;

            if (source.Length > 2 && source[0] == '1' && source[1] == '+')
            {
                buff = new byte[(source.Length - 2) >> 1];
                string key = CalculateKey();

                if (key == string.Empty)
                    return string.Empty;

                int kidx = 0;

                for (int i = 2; i < source.Length; i += 2)
                {
                    byte c;

                    try
                    {
                        c = Convert.ToByte(source.Substring(i, 2), 16);
                    }
                    catch
                    {
                        continue;
                    }

                    buff[(i - 2) >> 1] = (byte) (c ^ (byte) key[kidx++]);

                    if (kidx >= key.Length)
                        kidx = 0;
                }
            }
            else
            {
                byte key = (byte) (source.Length >> 1);
                buff = new byte[key];

                for (int i = 0; i < source.Length; i += 2)
                {
                    byte c;

                    try
                    {
                        c = Convert.ToByte(source.Substring(i, 2), 16);
                    }
                    catch
                    {
                        continue;
                    }

                    buff[i >> 1] = (byte) (c ^ key++);
                }
            }

            return Encoding.ASCII.GetString(buff);
        }

        private static string CalculateKey()
        {
            return Environment.MachineName;
        }
    }
}