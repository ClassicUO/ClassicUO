// SPDX-License-Identifier: BSD-2-Clause

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