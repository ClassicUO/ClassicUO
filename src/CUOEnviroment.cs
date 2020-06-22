#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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
using System.Threading;

namespace ClassicUO
{
    static class CUOEnviroment
    {
        public static Thread GameThread;
        public static float DPIScaleFactor = 1.0f;
        public static bool NoSound;
        public static string[] Args;
        public static string[] Plugins;
        public static bool Debug;
        public static bool IsHighDPI;
        public static uint CurrentRefreshRate;
        public static bool SkipLoginScreen;
        public static bool IsOutlands;

        public static readonly bool IsUnix = Environment.OSVersion.Platform != PlatformID.Win32NT &&
                                             Environment.OSVersion.Platform != PlatformID.Win32Windows &&
                                             Environment.OSVersion.Platform != PlatformID.Win32S &&
                                             Environment.OSVersion.Platform != PlatformID.WinCE;

        public static readonly Version Version = Assembly.GetExecutingAssembly().GetName().Version;
        public static readonly string ExecutablePath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);

        public static bool DisableUpdateWindowCaption;
    }
}
