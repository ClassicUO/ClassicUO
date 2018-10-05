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
using System.Diagnostics;
using System.IO;
using ClassicUO.IO.Resources;

namespace ClassicUO.IO
{
    public static class FileManager
    {
        private static string _uofolderpath;

        public static string UoFolderPath
        {
            get => _uofolderpath;
            set
            {
                _uofolderpath = value;
                FileInfo client = new FileInfo(Path.Combine(value, "client.exe"));
                if (!client.Exists)
                    throw new FileNotFoundException();

                FileVersionInfo versInfo = FileVersionInfo.GetVersionInfo(client.FullName);

                ClientVersion = (ClientVersions) ((versInfo.ProductMajorPart << 24) |
                                                  (versInfo.ProductMinorPart << 16) | (versInfo.ProductBuildPart << 8) |
                                                  versInfo.ProductPrivatePart);
            }
        }

        public static ClientVersions ClientVersion { get; private set; }
        public static bool IsUOPInstallation => ClientVersion >= ClientVersions.CV_70240;
        public static ushort GraphicMask => IsUOPInstallation ? (ushort) 0xFFFF : (ushort) 0x3FFF;


        public static void LoadFiles()
        {
            Map.Load();
            Art.Load();
            Cliloc.Load();
            Animations.Load();
            Gumps.Load();
            Fonts.Load();
            Hues.Load();
            TileData.Load();
            Multi.Load();
            Skills.Load();
            TextmapTextures.Load();
            SpecialKeywords.Load();
            AnimData.Load();
        }
    }
}