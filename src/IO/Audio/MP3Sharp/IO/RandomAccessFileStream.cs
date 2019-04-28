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