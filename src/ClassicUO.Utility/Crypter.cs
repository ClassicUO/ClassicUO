// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.Versioning;

namespace ClassicUO.Utility
{
    public static class Crypter
    {
        private const string LEGACY_VERSION_PREFIX = "1-";
        private const string DPAPI_PREFIX = "dpapi:";
        private const string VOLATILE_PREFIX = "volatile:";
        private static readonly byte[] _entropy = Encoding.UTF8.GetBytes("ClassicUO.Password.v2");

        public static bool SupportsPersistentSecrets => OperatingSystem.IsWindows();

        public static string Encrypt(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }

            if (!OperatingSystem.IsWindows())
            {
                return VOLATILE_PREFIX + Convert.ToBase64String(Encoding.UTF8.GetBytes(source));
            }

            try
            {
                return EncryptWindows(source);
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string SanitizeForStorage(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }

            if (!OperatingSystem.IsWindows())
            {
                return string.Empty;
            }

            if (source.StartsWith(DPAPI_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                return source;
            }

            string plaintext = Decrypt(source);

            return string.IsNullOrEmpty(plaintext) ? string.Empty : Encrypt(plaintext);
        }

        public static string Decrypt(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }

            if (source.StartsWith(DPAPI_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                if (!OperatingSystem.IsWindows())
                {
                    return string.Empty;
                }

                try
                {
                    return DecryptWindows(source[DPAPI_PREFIX.Length..]);
                }
                catch
                {
                    return string.Empty;
                }
            }

            if (source.StartsWith(VOLATILE_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    return Encoding.UTF8.GetString(Convert.FromBase64String(source[VOLATILE_PREFIX.Length..]));
                }
                catch
                {
                    return string.Empty;
                }
            }

            if (!LooksLikeLegacySecret(source))
            {
                return source;
            }

            return DecryptLegacy(source);
        }

        private static string EncryptLegacy(string source)
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
            sb.Append(LEGACY_VERSION_PREFIX);

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

        private static string DecryptLegacy(string source)
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

        private static bool LooksLikeLegacySecret(string source)
        {
            if (source.Length > 2 && source[0] == '1' && (source[1] == '-' || source[1] == '+'))
            {
                if (((source.Length - 2) & 1) != 0)
                {
                    return false;
                }

                for (int i = 2; i < source.Length; i++)
                {
                    if (!Uri.IsHexDigit(source[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            if ((source.Length & 1) != 0)
            {
                return false;
            }

            for (int i = 0; i < source.Length; i++)
            {
                if (!Uri.IsHexDigit(source[i]))
                {
                    return false;
                }
            }

            return source.Length > 0;
        }

        private static string CalculateKey()
        {
            return Environment.MachineName;
        }

        [SupportedOSPlatform("windows")]
        private static string EncryptWindows(string source)
        {
            byte[] data = Encoding.UTF8.GetBytes(source);
            byte[] encrypted = ProtectedData.Protect(data, _entropy, DataProtectionScope.CurrentUser);

            return DPAPI_PREFIX + Convert.ToBase64String(encrypted);
        }

        [SupportedOSPlatform("windows")]
        private static string DecryptWindows(string encodedSource)
        {
            byte[] encrypted = Convert.FromBase64String(encodedSource);
            byte[] decrypted = ProtectedData.Unprotect(encrypted, _entropy, DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(decrypted);
        }
    }
}
