using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Utility
{
    internal static class Crypter
    {
        public static string Encrypt(string plain, string key)
        {
            RijndaelManaged aesEncryption = new RijndaelManaged
            {
                KeySize = 256, BlockSize = 128, Mode = CipherMode.ECB, Padding = PaddingMode.ISO10126
            };
            byte[] KeyInBytes = Encoding.UTF8.GetBytes(key);
            aesEncryption.Key = KeyInBytes;
            byte[] plainText = Encoding.UTF8.GetBytes(plain);
            ICryptoTransform crypto = aesEncryption.CreateEncryptor();
            byte[] cipherText = crypto.TransformFinalBlock(plainText, 0, plainText.Length);
            return Convert.ToBase64String(cipherText);
        }

        private static string Decrypt(string encryptedText, string keyString)
        {
            RijndaelManaged aesEncryption = new RijndaelManaged
            {
                KeySize = 256, BlockSize = 128, Mode = CipherMode.ECB, Padding = PaddingMode.ISO10126
            };
            byte[] keyInBytes = Encoding.UTF8.GetBytes(keyString);
            aesEncryption.Key = keyInBytes;
            ICryptoTransform decrypto = aesEncryption.CreateDecryptor();
            byte[] encryptedBytes = Convert.FromBase64CharArray(encryptedText.ToCharArray(), 0, encryptedText.Length);
            return Encoding.UTF8.GetString(decrypto.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length));
        }
    }
}
