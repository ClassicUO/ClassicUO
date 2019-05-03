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