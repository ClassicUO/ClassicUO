using System;
using System.IO;

using ClassicUO.IO.Audio.MP3Sharp.Support;
using ClassicUO.Utility.Logging;

namespace ClassicUO.IO.Audio.MP3Sharp.Decoding
{
    /// <summary>
    ///     The Bistream class is responsible for parsing an MPEG audio bitstream.
    ///     REVIEW: much of the parsing currently occurs in the various decoders.
    ///     This should be moved into this class and associated inner classes.
    /// </summary>
    internal sealed class Bitstream
    {
        /// <summary>
        ///     Maximum size of the frame buffer:
        ///     1730 bytes per frame: 144 * 384kbit/s / 32000 Hz + 2 Bytes CRC
        /// </summary>
        private const int BUFFER_INT_SIZE = 433;

        /// <summary>
        ///     Synchronization control constant for the initial
        ///     synchronization to the start of a frame.
        /// </summary>
        internal static sbyte INITIAL_SYNC;

        /// <summary>
        ///     Synchronization control constant for non-inital frame
        ///     synchronizations.
        /// </summary>
        internal static sbyte STRICT_SYNC = 1;

        private readonly int[] bitmask =
        {
            0x00000000, 0x00000001, 0x00000003, 0x00000007, 0x0000000F, 0x0000001F, 0x0000003F, 0x0000007F,
            0x000000FF, 0x000001FF, 0x000003FF, 0x000007FF, 0x00000FFF, 0x00001FFF, 0x00003FFF, 0x00007FFF,
            0x0000FFFF, 0x0001FFFF
        };

        private readonly PushbackStream m_SourceStream;

        /// <summary>
        ///     Number (0-31, from MSB to LSB) of next bit for get_bits()
        /// </summary>
        private int m_BitIndex;

        private Crc16[] m_CRC;

        /// <summary>
        ///     The frame buffer that holds the data for the current frame.
        /// </summary>
        private int[] m_FrameBuffer;

        /// <summary>
        ///     The bytes read from the stream.
        /// </summary>
        private sbyte[] m_FrameBytes;

        /// <summary>
        ///     Number of valid bytes in the frame buffer.
        /// </summary>
        private int m_FrameSize;

        private Header m_Header;

        private sbyte[] m_SyncBuffer;

        /// <summary>
        ///     The current specified syncword
        /// </summary>
        private int m_SyncWord;

        /// <summary>
        ///     Index into framebuffer where the next bits are retrieved.
        /// </summary>
        private int m_WordPointer;

        private bool single_ch_mode;

        /// <summary>
        ///     Construct a IBitstream that reads data from a given InputStream.
        /// </summary>
        internal Bitstream(PushbackStream stream)
        {
            InitBlock();

            if (stream == null)
                throw new NullReferenceException("in stream is null");

            m_SourceStream = stream;

            CloseFrame();
        }

        private void InitBlock()
        {
            m_CRC = new Crc16[1];
            m_SyncBuffer = new sbyte[4];
            m_FrameBytes = new sbyte[BUFFER_INT_SIZE * 4];
            m_FrameBuffer = new int[BUFFER_INT_SIZE];
            m_Header = new Header();
        }

        public void close()
        {
            try
            {
                m_SourceStream.Close();
            }
            catch (IOException ex)
            {
                throw newBitstreamException(BitstreamErrors.STREAM_ERROR, ex);
            }
        }

        /// <summary>
        ///     Reads and parses the next frame from the input source.
        /// </summary>
        /// <returns>
        ///     The Header describing details of the frame read,
        ///     or null if the end of the stream has been reached.
        /// </returns>
        internal Header readFrame()
        {
            Header result = null;

            try
            {
                result = readNextFrame();
            }
            catch (BitstreamException ex)
            {
                if (ex.ErrorCode != BitstreamErrors.STREAM_EOF)
                {
                    // wrap original exception so stack trace is maintained.
                    throw newBitstreamException(ex.ErrorCode, ex);
                }
            }

            return result;
        }

        private Header readNextFrame()
        {
            if (m_FrameSize == -1)
            {
                // entire frame is read by the header class.
                m_Header.read_header(this, m_CRC);
            }

            return m_Header;
        }

        /// <summary>
        ///     Unreads the bytes read from the frame.
        ///     Throws BitstreamException.
        ///     REVIEW: add new error codes for this.
        /// </summary>
        public void unreadFrame()
        {
            if (m_WordPointer == -1 && m_BitIndex == -1 && m_FrameSize > 0)
            {
                try
                {
                    m_SourceStream.UnRead(m_FrameSize);
                }
                catch
                {
                    throw newBitstreamException(BitstreamErrors.STREAM_ERROR);
                }
            }
        }

        public void CloseFrame()
        {
            m_FrameSize = -1;
            m_WordPointer = -1;
            m_BitIndex = -1;
        }

        /// <summary>
        ///     Determines if the next 4 bytes of the stream represent a frame header.
        /// </summary>
        public bool IsSyncCurrentPosition(int syncmode)
        {
            int read = readBytes(m_SyncBuffer, 0, 4);

            int headerstring = ((m_SyncBuffer[0] << 24) & (int) SupportClass.Identity(0xFF000000)) |
                               ((m_SyncBuffer[1] << 16) & 0x00FF0000) | ((m_SyncBuffer[2] << 8) & 0x0000FF00) |
                               ((m_SyncBuffer[3] << 0) & 0x000000FF);

            try
            {
                m_SourceStream.UnRead(read);
            }
            catch
            {
            }

            bool sync = false;

            switch (read)
            {
                case 0:

                    Log.Message(LogTypes.Trace, "Bitstream: 0 bytes read == sync?");
                    sync = true;

                    break;

                case 4:
                    sync = isSyncMark(headerstring, syncmode, m_SyncWord);

                    break;
            }

            return sync;
        }

        // REVIEW: this class should provide inner classes to
        // parse the frame contents. Eventually, readBits will
        // be removed.
        public int readBits(int n)
        {
            return GetBitsFromBuffer(n);
        }

        public int readCheckedBits(int n)
        {
            // REVIEW: implement CRC check.
            return GetBitsFromBuffer(n);
        }

        internal BitstreamException newBitstreamException(int errorcode)
        {
            return new BitstreamException(errorcode, null);
        }

        internal BitstreamException newBitstreamException(int errorcode, Exception throwable)
        {
            return new BitstreamException(errorcode, throwable);
        }

        /// <summary>
        ///     Get next 32 bits from bitstream.
        ///     They are stored in the headerstring.
        ///     syncmod allows Synchro flag ID
        ///     The returned value is False at the end of stream.
        /// </summary>
        internal int syncHeader(sbyte syncmode)
        {
            bool sync;

            // read additinal 2 bytes
            int bytesRead = readBytes(m_SyncBuffer, 0, 3);

            if (bytesRead != 3)
                throw newBitstreamException(BitstreamErrors.STREAM_EOF, null);

            //_baos.write(syncbuf, 0, 3); // E.B

            int headerstring = ((m_SyncBuffer[0] << 16) & 0x00FF0000) | ((m_SyncBuffer[1] << 8) & 0x0000FF00) |
                               ((m_SyncBuffer[2] << 0) & 0x000000FF);

            do
            {
                headerstring <<= 8;

                if (readBytes(m_SyncBuffer, 3, 1) != 1)
                    throw newBitstreamException(BitstreamErrors.STREAM_EOF, null);

                //_baos.write(syncbuf, 3, 1); // E.B

                headerstring |= m_SyncBuffer[3] & 0x000000FF;

                sync = isSyncMark(headerstring, syncmode, m_SyncWord);
            } while (!sync);

            //current_frame_number++;
            //if (last_frame_number < current_frame_number) last_frame_number = current_frame_number;

            return headerstring;
        }

        public bool isSyncMark(int headerstring, int syncmode, int word)
        {
            bool sync = false;

            if (syncmode == INITIAL_SYNC)
            {
                //sync =  ((headerstring & 0xFFF00000) == 0xFFF00000);
                sync = (headerstring & 0xFFE00000) == 0xFFE00000; // SZD: MPEG 2.5
            }
            else
            {
                //sync = ((headerstring & 0xFFF80C00) == word) 
                sync = (headerstring & 0xFFE00000) == 0xFFE00000 // ROB -- THIS IS PROBABLY WRONG. A WEAKER CHECK.
                       && (headerstring & 0x000000C0) == 0x000000C0 == single_ch_mode;
            }

            // filter out invalid sample rate
            if (sync)
            {
                sync = (SupportClass.URShift(headerstring, 10) & 3) != 3;
                if (!sync) Log.Message(LogTypes.Trace, "Bitstream: INVALID SAMPLE RATE DETECTED");
            }

            // filter out invalid layer
            if (sync)
            {
                sync = (SupportClass.URShift(headerstring, 17) & 3) != 0;
                if (!sync) Log.Message(LogTypes.Trace, "Bitstream: INVALID LAYER DETECTED");
            }

            // filter out invalid version
            if (sync)
            {
                sync = (SupportClass.URShift(headerstring, 19) & 3) != 1;
                if (!sync) Log.Message(LogTypes.Trace, "Bitstream: INVALID VERSION DETECTED");
            }

            return sync;
        }

        /// <summary>
        ///     Reads the data for the next frame. The frame is not parsed
        ///     until parse frame is called.
        /// </summary>
        internal void read_frame_data(int bytesize)
        {
            readFully(m_FrameBytes, 0, bytesize);
            m_FrameSize = bytesize;
            m_WordPointer = -1;
            m_BitIndex = -1;
        }

        /// <summary>
        ///     Parses the data previously read with read_frame_data().
        /// </summary>
        internal void ParseFrame()
        {
            // Convert Bytes read to int
            int b = 0;
            sbyte[] byteread = m_FrameBytes;
            int bytesize = m_FrameSize;

            for (int k = 0; k < bytesize; k = k + 4)
            {
                sbyte b0 = 0;
                sbyte b1 = 0;
                sbyte b2 = 0;
                sbyte b3 = 0;
                b0 = byteread[k];

                if (k + 1 < bytesize)
                    b1 = byteread[k + 1];

                if (k + 2 < bytesize)
                    b2 = byteread[k + 2];

                if (k + 3 < bytesize)
                    b3 = byteread[k + 3];

                m_FrameBuffer[b++] = ((b0 << 24) & (int) SupportClass.Identity(0xFF000000)) | ((b1 << 16) & 0x00FF0000) |
                                     ((b2 << 8) & 0x0000FF00) | (b3 & 0x000000FF);
            }

            m_WordPointer = 0;
            m_BitIndex = 0;
        }

        /// <summary>
        ///     Read bits from buffer into the lower bits of an unsigned int.
        ///     The LSB contains the latest read bit of the stream.
        ///     (between 1 and 16, inclusive).
        /// </summary>
        public int GetBitsFromBuffer(int countBits)
        {
            int returnvalue = 0;
            int sum = m_BitIndex + countBits;

            // E.B
            // There is a problem here, wordpointer could be -1 ?!
            if (m_WordPointer < 0)
                m_WordPointer = 0;
            // E.B : End.

            if (sum <= 32)
            {
                // all bits contained in *wordpointer
                returnvalue = SupportClass.URShift(m_FrameBuffer[m_WordPointer], 32 - sum) & bitmask[countBits];

                // returnvalue = (wordpointer[0] >> (32 - sum)) & bitmask[number_of_bits];
                if ((m_BitIndex += countBits) == 32)
                {
                    m_BitIndex = 0;
                    m_WordPointer++; // added by me!
                }

                return returnvalue;
            }

            // Magouille a Voir
            //((short[])&returnvalue)[0] = ((short[])wordpointer + 1)[0];
            //wordpointer++; // Added by me!
            //((short[])&returnvalue + 1)[0] = ((short[])wordpointer)[0];
            int Right = m_FrameBuffer[m_WordPointer] & 0x0000FFFF;
            m_WordPointer++;
            int Left = m_FrameBuffer[m_WordPointer] & (int) SupportClass.Identity(0xFFFF0000);

            returnvalue = ((Right << 16) & (int) SupportClass.Identity(0xFFFF0000)) |
                          (SupportClass.URShift(Left, 16) & 0x0000FFFF);

            returnvalue = SupportClass.URShift(returnvalue, 48 - sum);
            // returnvalue >>= 16 - (number_of_bits - (32 - bitindex))
            returnvalue &= bitmask[countBits];
            m_BitIndex = sum - 32;

            return returnvalue;
        }

        /// <summary>
        ///     Set the word we want to sync the header to.
        ///     In Big-Endian byte order
        /// </summary>
        internal void SetSyncWord(int syncword0)
        {
            m_SyncWord = syncword0 & unchecked((int) 0xFFFFFF3F);
            single_ch_mode = (syncword0 & 0x000000C0) == 0x000000C0;
        }

        /// <summary>
        ///     Reads the exact number of bytes from the source input stream into a byte array.
        /// </summary>
        private void readFully(sbyte[] b, int offs, int len)
        {
            try
            {
                while (len > 0)
                {
                    int bytesread = m_SourceStream.Read(b, offs, len);

                    if (bytesread == -1 || bytesread == 0) // t/DD -- .NET returns 0 at end-of-stream!
                    {
                        // t/DD: this really SHOULD throw an exception here...
                        Log.Message(LogTypes.Trace, "Bitstream: readFully -- returning success at EOF? (" + bytesread + ")"
                                   );
                        while (len-- > 0) b[offs++] = 0;

                        break;

                        //throw newBitstreamException(UNEXPECTED_EOF, new EOFException());
                    }

                    offs += bytesread;
                    len -= bytesread;
                }
            }
            catch (IOException ex)
            {
                throw newBitstreamException(BitstreamErrors.STREAM_ERROR, ex);
            }
        }

        /// <summary>
        ///     Simlar to readFully, but doesn't throw exception when EOF is reached.
        /// </summary>
        private int readBytes(sbyte[] b, int offs, int len)
        {
            int totalBytesRead = 0;

            try
            {
                while (len > 0)
                {
                    int bytesread = m_SourceStream.Read(b, offs, len);

                    // for (int i = 0; i < len; i++) b[i] = (sbyte)Temp[i];
                    if (bytesread == -1 || bytesread == 0) break;

                    totalBytesRead += bytesread;
                    offs += bytesread;
                    len -= bytesread;
                }
            }
            catch (IOException ex)
            {
                throw newBitstreamException(BitstreamErrors.STREAM_ERROR, ex);
            }

            return totalBytesRead;
        }
    }
}