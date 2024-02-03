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
using System.IO;
using System.IO.Compression;

namespace ZLibNative
{
    public sealed class ZLIBStream : Stream
    {
        private readonly Adler32 _Adler32 = new Adler32();
        private bool _Closed;
        private readonly CompressionLevel _CompressionLevel = CompressionLevel.NoCompression;
        private readonly CompressionMode _CompressionMode = CompressionMode.Compress;
        private byte[] _CRC;
        private DeflateStream _DeflateStream;
        private readonly bool _LeaveOpen;
        private readonly Stream _RawStream;

        /// <summary>
        ///     Initializes new instance of ZLIBStream using specified stream and compression level, closing the stream at the end.
        /// </summary>
        /// <param name="stream">Stream to compress</param>
        /// <param name="compressionLevel">Compression level</param>
        public ZLIBStream(Stream stream, CompressionLevel compressionLevel) : this(stream, compressionLevel, false)
        {
        }

        /// <summary>
        ///     Initializes new instance of ZLIBStream using specified stream with compress or decompress mode, closing the stream
        ///     at the end.
        /// </summary>
        /// <param name="stream">Stream to compress or decompress</param>
        /// <param name="compressionMode">Compression Mode</param>
        public ZLIBStream(Stream stream, CompressionMode compressionMode) : this(stream, compressionMode, false)
        {
        }

        /// <summary>
        ///     Initializes new instance of ZLIBStream using specified stream and compression level, optionally leaves the stream
        ///     open.
        /// </summary>
        /// <param name="stream">Stream to compress</param>
        /// <param name="compressionLevel">Compression Level</param>
        /// <param name="leaveOpen">Leave the stream open</param>
        public ZLIBStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen)
        {
            _CompressionMode = CompressionMode.Compress;
            _CompressionLevel = compressionLevel;
            _LeaveOpen = leaveOpen;
            _RawStream = stream;
            InitStream();
        }

        /// <summary>
        ///     Initializes new instance of ZLIBStream using the specified stream with compress or decompress mode, optionally
        ///     leaves the stream open.
        /// </summary>
        /// <param name="stream">Stream to compress or decompress</param>
        /// <param name="compressionMode">Compression Mode</param>
        /// <param name="leaveOpen">Leave the stream open</param>
        public ZLIBStream(Stream stream, CompressionMode compressionMode, bool leaveOpen)
        {
            _CompressionMode = compressionMode;
            _CompressionLevel = CompressionLevel.Fastest;
            _LeaveOpen = leaveOpen;
            _RawStream = stream;
            InitStream();
        }

        public override bool CanRead => _CompressionMode == CompressionMode.Decompress && !_Closed;

        public override bool CanWrite => _CompressionMode == CompressionMode.Compress && !_Closed;

        public override bool CanSeek => false;

        public override long Length => throw new NotImplementedException();

        public override long Position
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public override int ReadByte()
        {
            int result;

            if (CanRead)
            {
                result = _DeflateStream.ReadByte();

                if (result == -1)
                {
                    ReadCRC();
                }
                else
                {
                    _Adler32.Update(Convert.ToByte(result));
                }
            }
            else
            {
                throw new InvalidOperationException();
            }

            return result;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int result;

            if (CanRead)
            {
                result = _DeflateStream.Read(buffer, offset, count);

                //check for end of stream
                if (result < 1 && count > 0)
                {
                    ReadCRC();
                }
                else
                {
                    _Adler32.Update(buffer, offset, result);
                }
            }
            else
            {
                throw new InvalidOperationException();
            }

            return result;
        }

        public override void WriteByte(byte value)
        {
            if (CanWrite)
            {
                _DeflateStream.WriteByte(value);
                _Adler32.Update(value);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (CanWrite)
            {
                _DeflateStream.Write(buffer, offset, count);
                _Adler32.Update(buffer, offset, count);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public override void Close()
        {
            if (!_Closed)
            {
                _Closed = true;

                if (_CompressionMode == CompressionMode.Compress)
                {
                    Flush();
                    _DeflateStream.Close();

                    _CRC = BitConverter.GetBytes(_Adler32.GetValue());

                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(_CRC);
                    }

                    _RawStream.Write(_CRC, 0, _CRC.Length);
                }
                else
                {
                    _DeflateStream.Close();

                    if (_CRC == null)
                    {
                        ReadCRC();
                    }
                }

                if (!_LeaveOpen)
                {
                    _RawStream.Close();
                }
            }
        }

        public override void Flush()
        {
            if (_DeflateStream != null)
            {
                _DeflateStream.Flush();
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Check if the stream is in ZLib format
        /// </summary>
        /// <param name="stream">Stream to check</param>
        /// <returns>Returns true if stream is zlib format</returns>
        public static bool IsZLibStream(Stream stream)
        {
            bool bResult = false;
            ZLibHeader header;

            //check if sequence is at position 0, if not, we throw an exception
            if (stream.Position != 0)
            {
                throw new ArgumentOutOfRangeException("Sequence must be at position 0");
            }

            //check if we can read two bytes that make the header
            if (stream.CanRead)
            {
                int CMF = stream.ReadByte();
                int Flag = stream.ReadByte();
                header = ZLibHeader.DecodeHeader(CMF, Flag);
                bResult = header.IsSupportedZLibStream;
            }

            return bResult;
        }

        /// <summary>
        ///     Read last 4 bytes of stream for CRC
        /// </summary>
        private void ReadCRC()
        {
            _CRC = new byte[4];
            _RawStream.Seek(-4, SeekOrigin.End);

            if (_RawStream.Read(_CRC, 0, 4) < 4)
            {
                throw new EndOfStreamException();
            }

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_CRC);
            }

            uint crcAdler = _Adler32.GetValue();
            uint crcStream = BitConverter.ToUInt32(_CRC, 0);

            if (crcStream != crcAdler)
            {
                throw new Exception("CRC mismatch");
            }
        }

        /// <summary>
        ///     Initialize the stream
        /// </summary>
        private void InitStream()
        {
            switch (_CompressionMode)
            {
                case CompressionMode.Compress:
                {
                    InitZLibHeader();
                    _DeflateStream = new DeflateStream(_RawStream, _CompressionLevel, true);

                    break;
                }

                case CompressionMode.Decompress:
                {
                    if (!IsZLibStream(_RawStream))
                    {
                        throw new InvalidDataException();
                    }

                    _DeflateStream = new DeflateStream(_RawStream, CompressionMode.Decompress, true);

                    break;
                }
            }
        }

        /// <summary>
        ///     Initialize stream header in ZLib format
        /// </summary>
        private void InitZLibHeader()
        {
            byte[] bytesHeader;

            //set header settings
            ZLibHeader header = new ZLibHeader
            {
                CompressionMethod = 8, //Deflate
                CompressionInfo = 7,
                FDict = false //No dictionary
            };

            switch (_CompressionLevel)
            {
                case CompressionLevel.NoCompression:
                {
                    header.FLevel = FLevel.Faster;

                    break;
                }

                case CompressionLevel.Fastest:
                {
                    header.FLevel = FLevel.Default;

                    break;
                }

                case CompressionLevel.Optimal:
                {
                    header.FLevel = FLevel.Optimal;

                    break;
                }
            }

            bytesHeader = header.EncodeZlibHeader();

            _RawStream.WriteByte(bytesHeader[0]);
            _RawStream.WriteByte(bytesHeader[1]);
        }
    }
}