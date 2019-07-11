#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Utility.Logging
{
    internal sealed class LogFile : IDisposable
    {
        private readonly FileStream logStream;

        public LogFile(string directory, string file)
        {
            logStream = new FileStream($"{directory}/{Engine.CurrDateTime:yyyy-MM-dd_hh-mm-ss}_{file}", FileMode.Append, FileAccess.Write, FileShare.ReadWrite, 4096, true);
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

        public void Write(string message)
        {
            byte[] logBytes = Encoding.UTF8.GetBytes($"{message}\n");
            logStream.Write(logBytes, 0, logBytes.Length);
            logStream.Flush();
        }

        public override string ToString()
        {
            return logStream.Name;
        }
    }
}