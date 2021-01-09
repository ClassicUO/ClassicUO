#region license

// Copyright (c) 2021, andreakarasho
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
        private int m_NumForwardBytesInBuffer;
        private readonly Stream m_Stream;
        private readonly byte[] m_TemporaryBuffer;

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

            if (m_NumForwardBytesInBuffer > m_BackBufferSize)
            {
                throw new Exception("The backstream cannot unread the requested number of bytes.");
            }
        }

        public void Close()
        {
            m_Stream.Close();
        }
    }
}