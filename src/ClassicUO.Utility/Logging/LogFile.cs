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
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Utility.Logging
{
    public sealed class LogFile : IDisposable
    {
        private readonly FileStream logStream;

        public LogFile(string directory, string file)
        {
            logStream = new FileStream
            (
                $"{directory}/{DateTime.Now:yyyy-MM-dd_hh-mm-ss}_{file}",
                FileMode.Append,
                FileAccess.Write,
                FileShare.ReadWrite,
                4096,
                true
            );
        }

        public void Dispose()
        {
            logStream.Close();
        }


        public void Write(string message)
        {
            byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(message.Length);

            try
            {
                Encoding.UTF8.GetBytes
                (
                    message,
                    0,
                    message.Length,
                    buffer,
                    0
                );

                logStream.Write(buffer, 0, message.Length);
                logStream.WriteByte((byte) '\n');
                logStream.Flush();
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public async Task WriteAsync(string message)
        {
            byte[] buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(message.Length);

            try
            {
                Encoding.UTF8.GetBytes
                (
                    message,
                    0,
                    message.Length,
                    buffer,
                    0
                );

                await logStream.WriteAsync(buffer, 0, message.Length);
                logStream.WriteByte((byte) '\n');
                await logStream.FlushAsync();
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            }
        }


        public override string ToString()
        {
            return logStream.Name;
        }
    }
}