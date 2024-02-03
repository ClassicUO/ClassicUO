#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Text;

namespace ClassicUO.Utility
{
    public static class Crypter
    {
        public static string Encrypt(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }

            byte[] buff = Encoding.ASCII.GetBytes(source);
            int kidx = 0;
            string key = CalculateKey();

            if (key == string.Empty)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder(source.Length * 2 + 2);
            sb.Append("1-");

            for (int i = 0; i < buff.Length; i++)
            {
                sb.AppendFormat("{0:X2}", (byte) (buff[i] ^ (byte) key[kidx++]));

                if (kidx >= key.Length)
                {
                    kidx = 0;
                }
            }

            return sb.ToString();
        }

        public static string Decrypt(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }

            byte[] buff = null;

            if (source.Length > 2 && source[0] == '1' && (source[1] == '-' || source[1] == '+'))
            {
                buff = new byte[(source.Length - 2) >> 1];
                string key = CalculateKey();

                if (key == string.Empty)
                {
                    return string.Empty;
                }

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
                    {
                        kidx = 0;
                    }
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