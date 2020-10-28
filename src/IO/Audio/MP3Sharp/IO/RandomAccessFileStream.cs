using System;
using System.IO;

namespace ClassicUO.IO.Audio.MP3Sharp.IO
{
    internal class RandomAccessFileStream
    {
        public static FileStream CreateRandomAccessFile(string fileName, string mode)
        {
            FileStream newFile = null;

            if (string.Compare(mode, "rw", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                newFile = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            }
            else if (string.Compare(mode, "r", StringComparison.CurrentCultureIgnoreCase) == 0)
            {
                newFile = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            }
            else
            {
                throw new ArgumentException();
            }

            return newFile;
        }
    }
}