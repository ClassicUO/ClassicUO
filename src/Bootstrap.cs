#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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
using System.Reflection;
using System.Runtime.InteropServices;

using ClassicUO.Utility.Logging;

namespace ClassicUO
{
    internal static class Bootstrap
    {
        public static string ExeDirectory { get; private set; }

        public static Assembly Assembly { get; private set; }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetDllDirectory(string lpPathName);

        private static void Main(string[] args)
        {
            Log.Start(LogTypes.All);
            Assembly = Assembly.GetExecutingAssembly();
            ExeDirectory = Path.GetDirectoryName(Assembly.Location);

            // We can use the mono's dllmap feature, but 99% of people use VS to compile.
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                string libsPath = Path.Combine(ExeDirectory, "libs", Environment.Is64BitProcess ? "x64" : "x86");
                SetDllDirectory(libsPath);
            }

            Environment.SetEnvironmentVariable("FNA_GRAPHICS_ENABLE_HIGHDPI", "1");
            Environment.SetEnvironmentVariable("FNA_OPENGL_BACKBUFFER_SCALE_NEAREST", "1");

            using (GameLoop game = new GameLoop())
            {
                Log.Message(LogTypes.Trace, $"Exe directory: {ExeDirectory}");
                game.Run();
            }
        }
    }
}