using System;
using System.IO;

namespace ClassicUO.IO.Audio.MP3Sharp.Decoding
{
    /// <summary>
    ///     A PushbackStream is a stream that can "push back" or "unread" data. This is useful in situations where it is
    ///     convenient for a
    ///     fragment of code to read an indefinite number of data bytes that are delimited by a particular byte value; after
    ///     reading the
    ///     terminating byte, the code fragment can "unread" it, so that the next read operation on the input stream will
    ///     reread the byte
    ///     that was pushed back.
    /// </summary>
    internal class PushbackStream
    {
        private readonly int m_BackBufferSize;
        private readonly CircularByteBuffer m_CircularByteBuffer;
        private readonly Stream m_Stream;
        private readonly byte[] m_TemporaryBuffer;
        private int m_NumForwardBytesInBuffer;

        public PushbackStream(Stream s, int backBufferSize)
        {
            m_Stream = s;
            m_BackBufferSize = backBufferSize;
            m_TemporaryBuffer = new byte[m_BackBufferSize];
            m_CircularByteBuffer = new CircularByteBuffer(m_BackBufferSize);
        }

        public int Read(sbyte[] toRead, int offset, int length)
        {
            // Read 
            int currentByte = 0;
            bool canReadStream = true;

            while (currentByte < length && canReadStream)
            {
                if (m_NumForwardBytesInBuffer > 0)
                {
                    // from mem
                    m_NumForwardBytesInBuffer--;
                    toRead[offset + currentByte] = (sbyte) m_CircularByteBuffer[m_NumForwardBytesInBuffer];
                    currentByte++;
                }
                else
                {
                    // from stream
                    int newBytes = length - currentByte;
                    int numRead = m_Stream.Read(m_TemporaryBuffer, 0, newBytes);
                    canReadStream = numRead >= newBytes;

                    for (int i = 0; i < numRead; i++)
                    {
                        m_CircularByteBuffer.Push(m_TemporaryBuffer[i]);
                        toRead[offset + currentByte + i] = (sbyte) m_TemporaryBuffer[i];
                    }

                    currentByte += numRead;
                }
            }

            return currentByte;
        }

        public void UnRead(int length)
        {
            m_NumForwardBytesInBuffer += length;

            if (m_NumForwardBytesInBuffer > m_BackBufferSize) throw new Exception("The backstream cannot unread the requested number of bytes.");
        }

        public void Close()
        {
            m_Stream.Close();
        }
    }
}