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
            logStream = new FileStream($"{directory}/{DateTime.Now:yyyy-MM-dd_hh-mm-ss}_{file}", FileMode.Append, FileAccess.Write, FileShare.ReadWrite, 4096, true);
        }

        public void Dispose()
        {
            logStream.Close();
        }

        public async Task WriteAsync(string logMessage)
        {
            byte[] logBytes = Encoding.UTF8.GetBytes($"{logMessage}\n");
            await logStream.WriteAsync(logBytes, 0, logBytes.Length);
            await logStream.FlushAsync();
        }
    }
}