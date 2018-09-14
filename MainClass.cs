#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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
using ClassicUO.Input;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ClassicUO
{
    class MainClass
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetDllDirectory(string lpPathName);

        private static void Main(string[] args)
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                SetDllDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "/Graphic/FNA/", Environment.Is64BitProcess ? "x64" : "x86"));

            Environment.SetEnvironmentVariable("FNA_GRAPHICS_ENABLE_HIGHDPI", "1");
            Environment.SetEnvironmentVariable("FNA_OPENGL_BACKBUFFER_SCALE_NEAREST", "1");



            using (GameLoop game = new GameLoop())
            {
                //========================================================
                //SERVICE STACK
                Service.Register(new Log());
                Service.Register(new SpriteBatch3D(game));
                Service.Register(new SpriteBatchUI(game));
                Service.Register(new InputManager());
                //========================================================

                bool isHighDPI = Environment.GetEnvironmentVariable("FNA_GRAPHICS_ENABLE_HIGHDPI") == "1";
                if (isHighDPI)
                    Debug.WriteLine("HiDPI Enabled");

                game.Run();
            }
        }        
    }
}
