using System.IO;

using ClassicUO.IO.Audio.MP3Sharp.Support;

namespace ClassicUO.IO.Audio.MP3Sharp.IO
{
    /// <summary>
    ///     Class to manage RIFF files
    /// </summary>
    internal class RiffFile
    {
        protected const int DDC_SUCCESS = 0; // The operation succeded
        protected const int DDC_FAILURE = 1; // The operation failed for unspecified reasons
        protected const int DDC_OUT_OF_MEMORY = 2; // Operation failed due to running out of memory
        protected const int DDC_FILE_ERROR = 3; // Operation encountered file I/O error
        protected const int DDC_INVALID_CALL = 4; // Operation was called with invalid parameters
        protected const int DDC_USER_ABORT = 5; // Operation was aborted by the user
        protected const int DDC_INVALID_FILE = 6; // File format does not match
        protected const int RFM_UNKNOWN = 0; // undefined type (can use to mean "N/A" or "not open")
        protected const int RFM_WRITE = 1; // open for write
        protected const int RFM_READ = 2; // open for read
        private readonly RiffChunkHeader m_RiffHeader; // header for whole file
        protected int Fmode; // current file I/O mode
        private Stream m_File; // I/O stream to use

        /// <summary>
        ///     Dummy Constructor
        /// </summary>
        public RiffFile()
        {
            m_File = null;
            Fmode = RFM_UNKNOWN;
            m_RiffHeader = new RiffChunkHeader(this);

            m_RiffHeader.CkId = FourCC("RIFF");
            m_RiffHeader.CkSize = 0;
        }

        /// <summary>
        ///     Return File Mode.
        /// </summary>
        public virtual int CurrentFileMode()
        {
            return Fmode;
        }

        /// <summary>
        ///     Open a RIFF file.
        /// </summary>
        public virtual int Open(string filename, int newMode)
        {
            int retcode = DDC_SUCCESS;

            if (Fmode != RFM_UNKNOWN) retcode = Close();

            if (retcode == DDC_SUCCESS)
            {
                switch (newMode)
                {
                    case RFM_WRITE:

                        try
                        {
                            m_File = RandomAccessFileStream.CreateRandomAccessFile(filename, "rw");

                            try
                            {
                                // Write the RIFF header...
                                // We will have to come back later and patch it!
                                sbyte[] br = new sbyte[8];
                                br[0] = (sbyte) (SupportClass.URShift(m_RiffHeader.CkId, 24) & 0x000000FF);
                                br[1] = (sbyte) (SupportClass.URShift(m_RiffHeader.CkId, 16) & 0x000000FF);
                                br[2] = (sbyte) (SupportClass.URShift(m_RiffHeader.CkId, 8) & 0x000000FF);
                                br[3] = (sbyte) (m_RiffHeader.CkId & 0x000000FF);

                                sbyte br4 = (sbyte) (SupportClass.URShift(m_RiffHeader.CkSize, 24) & 0x000000FF);
                                sbyte br5 = (sbyte) (SupportClass.URShift(m_RiffHeader.CkSize, 16) & 0x000000FF);
                                sbyte br6 = (sbyte) (SupportClass.URShift(m_RiffHeader.CkSize, 8) & 0x000000FF);
                                sbyte br7 = (sbyte) (m_RiffHeader.CkSize & 0x000000FF);

                                br[4] = br7;
                                br[5] = br6;
                                br[6] = br5;
                                br[7] = br4;

                                m_File.Write(SupportClass.ToByteArray(br), 0, 8);
                                Fmode = RFM_WRITE;
                            }
                            catch
                            {
                                m_File.Close();
                                Fmode = RFM_UNKNOWN;
                            }
                        }
                        catch
                        {
                            Fmode = RFM_UNKNOWN;
                            retcode = DDC_FILE_ERROR;
                        }

                        break;

                    case RFM_READ:

                        try
                        {
                            m_File = RandomAccessFileStream.CreateRandomAccessFile(filename, "r");

                            try
                            {
                                // Try to read the RIFF header...
                                sbyte[] br = new sbyte[8];
                                SupportClass.ReadInput(m_File, ref br, 0, 8);
                                Fmode = RFM_READ;

                                m_RiffHeader.CkId = ((br[0] << 24) & (int) SupportClass.Identity(0xFF000000)) |
                                                    ((br[1] << 16) & 0x00FF0000) | ((br[2] << 8) & 0x0000FF00) |
                                                    (br[3] & 0x000000FF);

                                m_RiffHeader.CkSize = ((br[4] << 24) & (int) SupportClass.Identity(0xFF000000)) |
                                                      ((br[5] << 16) & 0x00FF0000) | ((br[6] << 8) & 0x0000FF00) |
                                                      (br[7] & 0x000000FF);
                            }
                            catch
                            {
                                m_File.Close();
                                Fmode = RFM_UNKNOWN;
                            }
                        }
                        catch
                        {
                            Fmode = RFM_UNKNOWN;
                            retcode = DDC_FILE_ERROR;
                        }

                        break;

                    default:
                        retcode = DDC_INVALID_CALL;

                        break;
                }
            }

            return retcode;
        }

        /// <summary>
        ///     Open a RIFF STREAM.
        /// </summary>
        public virtual int Open(Stream stream, int newMode)
        {
            int retcode = DDC_SUCCESS;

            if (Fmode != RFM_UNKNOWN) retcode = Close();

            if (retcode == DDC_SUCCESS)
            {
                switch (newMode)
                {
                    case RFM_WRITE:

                        try
                        {
                            //file = SupportClass.RandomAccessFileSupport.CreateRandomAccessFile(Filename, "rw");
                            m_File = stream;

                            try
                            {
                                // Write the RIFF header...
                                // We will have to come back later and patch it!
                                sbyte[] br = new sbyte[8];
                                br[0] = (sbyte) (SupportClass.URShift(m_RiffHeader.CkId, 24) & 0x000000FF);
                                br[1] = (sbyte) (SupportClass.URShift(m_RiffHeader.CkId, 16) & 0x000000FF);
                                br[2] = (sbyte) (SupportClass.URShift(m_RiffHeader.CkId, 8) & 0x000000FF);
                                br[3] = (sbyte) (m_RiffHeader.CkId & 0x000000FF);

                                sbyte br4 = (sbyte) (SupportClass.URShift(m_RiffHeader.CkSize, 24) & 0x000000FF);
                                sbyte br5 = (sbyte) (SupportClass.URShift(m_RiffHeader.CkSize, 16) & 0x000000FF);
                                sbyte br6 = (sbyte) (SupportClass.URShift(m_RiffHeader.CkSize, 8) & 0x000000FF);
                                sbyte br7 = (sbyte) (m_RiffHeader.CkSize & 0x000000FF);

                                br[4] = br7;
                                br[5] = br6;
                                br[6] = br5;
                                br[7] = br4;

                                m_File.Write(SupportClass.ToByteArray(br), 0, 8);
                                Fmode = RFM_WRITE;
                            }
                            catch
                            {
                                m_File.Close();
                                Fmode = RFM_UNKNOWN;
                            }
                        }
                        catch
                        {
                            Fmode = RFM_UNKNOWN;
                            retcode = DDC_FILE_ERROR;
                        }

                        break;

                    case RFM_READ:

                        try
                        {
                            m_File = stream;

                            //file = SupportClass.RandomAccessFileSupport.CreateRandomAccessFile(Filename, "r");
                            try
                            {
                                // Try to read the RIFF header... 
                                sbyte[] br = new sbyte[8];
                                SupportClass.ReadInput(m_File, ref br, 0, 8);
                                Fmode = RFM_READ;

                                m_RiffHeader.CkId = ((br[0] << 24) & (int) SupportClass.Identity(0xFF000000)) |
                                                    ((br[1] << 16) & 0x00FF0000) | ((br[2] << 8) & 0x0000FF00) |
                                                    (br[3] & 0x000000FF);

                                m_RiffHeader.CkSize = ((br[4] << 24) & (int) SupportClass.Identity(0xFF000000)) |
                                                      ((br[5] << 16) & 0x00FF0000) | ((br[6] << 8) & 0x0000FF00) |
                                                      (br[7] & 0x000000FF);
                            }
                            catch
                            {
                                m_File.Close();
                                Fmode = RFM_UNKNOWN;
                            }
                        }
                        catch
                        {
                            Fmode = RFM_UNKNOWN;
                            retcode = DDC_FILE_ERROR;
                        }

                        break;

                    default:
                        retcode = DDC_INVALID_CALL;

                        break;
                }
            }

            return retcode;
        }

        /// <summary>
        ///     Write NumBytes data.
        /// </summary>
        public virtual int Write(sbyte[] data, int numBytes)
        {
            if (Fmode != RFM_WRITE) return DDC_INVALID_CALL;

            try
            {
                m_File.Write(SupportClass.ToByteArray(data), 0, numBytes);
                Fmode = RFM_WRITE;
            }
            catch
            {
                return DDC_FILE_ERROR;
            }

            m_RiffHeader.CkSize += numBytes;

            return DDC_SUCCESS;
        }

        /// <summary>
        ///     Write NumBytes data.
        /// </summary>
        public virtual int Write(short[] data, int numBytes)
        {
            sbyte[] theData = new sbyte[numBytes];
            int yc = 0;

            for (int y = 0; y < numBytes; y = y + 2)
            {
                theData[y] = (sbyte) (data[yc] & 0x00FF);
                theData[y + 1] = (sbyte) (SupportClass.URShift(data[yc++], 8) & 0x00FF);
            }

            if (Fmode != RFM_WRITE) return DDC_INVALID_CALL;

            try
            {
                m_File.Write(SupportClass.ToByteArray(theData), 0, numBytes);
                Fmode = RFM_WRITE;
            }
            catch
            {
                return DDC_FILE_ERROR;
            }

            m_RiffHeader.CkSize += numBytes;

            return DDC_SUCCESS;
        }

        /// <summary>
        ///     Write NumBytes data.
        /// </summary>
        public virtual int Write(RiffChunkHeader riffHeader, int numBytes)
        {
            sbyte[] br = new sbyte[8];
            br[0] = (sbyte) (SupportClass.URShift(riffHeader.CkId, 24) & 0x000000FF);
            br[1] = (sbyte) (SupportClass.URShift(riffHeader.CkId, 16) & 0x000000FF);
            br[2] = (sbyte) (SupportClass.URShift(riffHeader.CkId, 8) & 0x000000FF);
            br[3] = (sbyte) (riffHeader.CkId & 0x000000FF);

            sbyte br4 = (sbyte) (SupportClass.URShift(riffHeader.CkSize, 24) & 0x000000FF);
            sbyte br5 = (sbyte) (SupportClass.URShift(riffHeader.CkSize, 16) & 0x000000FF);
            sbyte br6 = (sbyte) (SupportClass.URShift(riffHeader.CkSize, 8) & 0x000000FF);
            sbyte br7 = (sbyte) (riffHeader.CkSize & 0x000000FF);

            br[4] = br7;
            br[5] = br6;
            br[6] = br5;
            br[7] = br4;

            if (Fmode != RFM_WRITE) return DDC_INVALID_CALL;

            try
            {
                m_File.Write(SupportClass.ToByteArray(br), 0, numBytes);
                Fmode = RFM_WRITE;
            }
            catch
            {
                return DDC_FILE_ERROR;
            }

            m_RiffHeader.CkSize += numBytes;

            return DDC_SUCCESS;
        }

        /// <summary>
        ///     Write NumBytes data.
        /// </summary>
        public virtual int Write(short data, int numBytes)
        {
            short theData = data; //(short) (((SupportClass.URShift(data, 8)) & 0x00FF) | ((Data << 8) & 0xFF00));

            if (Fmode != RFM_WRITE) return DDC_INVALID_CALL;

            try
            {
                BinaryWriter tempBinaryWriter = new BinaryWriter(m_File);
                tempBinaryWriter.Write(theData);
                Fmode = RFM_WRITE;
            }
            catch
            {
                return DDC_FILE_ERROR;
            }

            m_RiffHeader.CkSize += numBytes;

            return DDC_SUCCESS;
        }

        /// <summary>
        ///     Write NumBytes data.
        /// </summary>
        public virtual int Write(int data, int numBytes)
        {
            int theData = data;

            if (Fmode != RFM_WRITE) return DDC_INVALID_CALL;

            try
            {
                BinaryWriter tempBinaryWriter = new BinaryWriter(m_File);
                tempBinaryWriter.Write(theData);
                Fmode = RFM_WRITE;
            }
            catch
            {
                return DDC_FILE_ERROR;
            }

            m_RiffHeader.CkSize += numBytes;

            return DDC_SUCCESS;
        }

        /// <summary>
        ///     Read NumBytes data.
        /// </summary>
        public virtual int Read(sbyte[] data, int numBytes)
        {
            int retcode = DDC_SUCCESS;

            try
            {
                SupportClass.ReadInput(m_File, ref data, 0, numBytes);
            }
            catch
            {
                retcode = DDC_FILE_ERROR;
            }

            return retcode;
        }

        /// <summary>
        ///     Expect NumBytes data.
        /// </summary>
        public virtual int Expect(string data, int numBytes)
        {
            int cnt = 0;

            try
            {
                while (numBytes-- != 0)
                {
                    sbyte target = (sbyte) m_File.ReadByte();

                    if (target != data[cnt++])
                        return DDC_FILE_ERROR;
                }
            }
            catch
            {
                return DDC_FILE_ERROR;
            }

            return DDC_SUCCESS;
        }

        /// <summary>
        ///     Close Riff File.
        ///     Length is written too.
        /// </summary>
        public virtual int Close()
        {
            int retcode = DDC_SUCCESS;

            switch (Fmode)
            {
                case RFM_WRITE:

                    try
                    {
                        m_File.Seek(0, SeekOrigin.Begin);

                        try
                        {
                            sbyte[] br = new sbyte[8];
                            br[0] = (sbyte) (SupportClass.URShift(m_RiffHeader.CkId, 24) & 0x000000FF);
                            br[1] = (sbyte) (SupportClass.URShift(m_RiffHeader.CkId, 16) & 0x000000FF);
                            br[2] = (sbyte) (SupportClass.URShift(m_RiffHeader.CkId, 8) & 0x000000FF);
                            br[3] = (sbyte) (m_RiffHeader.CkId & 0x000000FF);

                            br[7] = (sbyte) (SupportClass.URShift(m_RiffHeader.CkSize, 24) & 0x000000FF);
                            br[6] = (sbyte) (SupportClass.URShift(m_RiffHeader.CkSize, 16) & 0x000000FF);
                            br[5] = (sbyte) (SupportClass.URShift(m_RiffHeader.CkSize, 8) & 0x000000FF);
                            br[4] = (sbyte) (m_RiffHeader.CkSize & 0x000000FF);
                            m_File.Write(SupportClass.ToByteArray(br), 0, 8);
                            m_File.Close();
                        }
                        catch
                        {
                            retcode = DDC_FILE_ERROR;
                        }
                    }
                    catch
                    {
                        retcode = DDC_FILE_ERROR;
                    }

                    break;

                case RFM_READ:

                    try
                    {
                        m_File.Close();
                    }
                    catch
                    {
                        retcode = DDC_FILE_ERROR;
                    }

                    break;
            }

            m_File = null;
            Fmode = RFM_UNKNOWN;

            return retcode;
        }

        /// <summary>
        ///     Return File Position.
        /// </summary>
        public virtual long CurrentFilePosition()
        {
            long position;

            try
            {
                position = m_File.Position;
            }
            catch
            {
                position = -1;
            }

            return position;
        }

        /// <summary>
        ///     Write Data to specified offset.
        /// </summary>
        public virtual int Backpatch(long fileOffset, RiffChunkHeader data, int numBytes)
        {
            if (m_File == null) return DDC_INVALID_CALL;

            try
            {
                m_File.Seek(fileOffset, SeekOrigin.Begin);
            }
            catch
            {
                return DDC_FILE_ERROR;
            }

            return Write(data, numBytes);
        }

        public virtual int Backpatch(long fileOffset, sbyte[] data, int numBytes)
        {
            if (m_File == null) return DDC_INVALID_CALL;

            try
            {
                m_File.Seek(fileOffset, SeekOrigin.Begin);
            }
            catch
            {
                return DDC_FILE_ERROR;
            }

            return Write(data, numBytes);
        }

        /// <summary>
        ///     Seek in the File.
        /// </summary>
        protected internal virtual int Seek(long offset)
        {
            int rc;

            try
            {
                m_File.Seek(offset, SeekOrigin.Begin);
                rc = DDC_SUCCESS;
            }
            catch
            {
                rc = DDC_FILE_ERROR;
            }

            return rc;
        }

        /// <summary>
        ///     Error Messages.
        /// </summary>
        private string DDCRET_String(int retcode)
        {
            switch (retcode)
            {
                case DDC_SUCCESS:

                    return "DDC_SUCCESS";

                case DDC_FAILURE:

                    return "DDC_FAILURE";

                case DDC_OUT_OF_MEMORY:

                    return "DDC_OUT_OF_MEMORY";

                case DDC_FILE_ERROR:

                    return "DDC_FILE_ERROR";

                case DDC_INVALID_CALL:

                    return "DDC_INVALID_CALL";

                case DDC_USER_ABORT:

                    return "DDC_USER_ABORT";

                case DDC_INVALID_FILE:

                    return "DDC_INVALID_FILE";
            }

            return "Unknown Error";
        }

        /// <summary>
        ///     Fill the header.
        /// </summary>
        public static int FourCC(string chunkName)
        {
            sbyte[] p = {0x20, 0x20, 0x20, 0x20};
            SupportClass.GetSBytesFromString(chunkName, 0, 4, ref p, 0);

            int ret = ((p[0] << 24) & (int) SupportClass.Identity(0xFF000000)) | ((p[1] << 16) & 0x00FF0000) |
                      ((p[2] << 8) & 0x0000FF00) | (p[3] & 0x000000FF);

            return ret;
        }

        internal class RiffChunkHeader
        {
            public int CkId; // Four-character chunk ID
            public int CkSize;

            // Length of data in chunk
            public RiffChunkHeader(RiffFile enclosingInstance)
            {
                InitBlock(enclosingInstance);
            }

            public RiffFile EnclosingInstance { get; private set; }

            private void InitBlock(RiffFile enclosingInstance)
            {
                EnclosingInstance = enclosingInstance;
            }
        }
    }
}