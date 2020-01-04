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
using System.Diagnostics;
using System.IO;

namespace ClassicUO.Data
{
    enum ClientVersion
    {
        CV_OLD = (1 << 24) | (0 << 16) | (0 << 8) | 0, // Original game
        CV_200 = (2 << 24) | (0 << 16) | (0 << 8) | 0, // T2A Introduction. Adds screen dimensions packet
        CV_204C = (2 << 24) | (0 << 16) | (4 << 8) | 2, // Adds *.def files
        CV_300 = (3 << 24) | (0 << 16) | (0 << 8) | 0,
        CV_305D = (3 << 24) | (0 << 16) | (5 << 8) | 3, // Renaissance. Expanded character slots.
        CV_306E = (3 << 24) | (0 << 16) | (0 << 8) | 0, // Adds a packet with the client type, switches to mp3 from midi for sound files
        CV_308 = (3 << 24) | (0 << 16) | (8 << 8) | 0,
        CV_308D = (3 << 24) | (0 << 16) | (8 << 8) | 3, // Adds maximum stats to the status bar
        CV_308J = (3 << 24) | (0 << 16) | (8 << 8) | 9, // Adds followers to the status bar
        CV_308Z = (3 << 24) | (0 << 16) | (8 << 8) | 25, // Age of Shadows. Adds paladin, necromancer, custom housing, resists, profession selection window, removes save password checkbox
        CV_400B = (4 << 24) | (0 << 16) | (0 << 8) | 1, // Deletes tooltips
        CV_405A = (4 << 24) | (0 << 16) | (5 << 8) | 0, // Adds ninja, samurai
        CV_4011D = (4 << 24) | (0 << 16) | (11 << 8) | 3, // Adds elven race
        CV_500A = (5 << 24) | (0 << 16) | (0 << 8) | 0, // Paperdoll buttons journal becomes quests, chat becomes guild. Use mega FileManager.Cliloc. Removes verdata.mul.
        CV_5020 = (5 << 24) | (0 << 16) | (2 << 8) | 0, // Adds buff bar
        CV_5090 = (5 << 24) | (0 << 16) | (9 << 8) | 0, //
        CV_6000 = (6 << 24) | (0 << 16) | (0 << 8) | 0, // Adds colored guild/all chat and ignore system. New targeting systems, object properties and handles.
        CV_6013 = (6 << 24) | (0 << 16) | (1 << 8) | 3, //
        CV_6017 = (6 << 24) | (0 << 16) | (1 << 8) | 8, //
        CV_6040 = (6 << 24) | (0 << 16) | (4 << 8) | 0, // Increased number of player slots
        CV_6060 = (6 << 24) | (0 << 16) | (6 << 8) | 0, //
        CV_60142 = (6 << 24) | (0 << 16) | (14 << 8) | 2, //
        CV_60144 = (6 << 24) | (0 << 16) | (14 << 8) | 4, // Adds gargoyle race.
        CV_7000 = (7 << 24) | (0 << 16) | (0 << 8) | 0, //
        CV_7090 = (7 << 24) | (0 << 16) | (9 << 8) | 0, //
        CV_70130 = (7 << 24) | (0 << 16) | (13 << 8) | 0, //
        CV_70160 = (7 << 24) | (0 << 16) | (16 << 8) | 0, //
        CV_70180 = (7 << 24) | (0 << 16) | (18 << 8) | 0, //
        CV_70240 = (7 << 24) | (0 << 16) | (24 << 8) | 0, // *.mul -> *.uop
        CV_70331 = (7 << 24) | (0 << 16) | (33 << 8) | 1, //
        CV_704565 = (7 << 24) | (0 << 16) | (45 << 8) | 65, //
        CV_706400 = (7 << 24) | (0 << 16) | (64 << 8) | 0, // Endless Journey background
        CV_70796 = (7 << 24) | (0 << 16) | (79 << 8) | 6 // Display houses content option
    }

    static class ClientVersionHelper
    {
        public static bool TryParse(string versionText, out ClientVersion version)
        {
            if (!string.IsNullOrEmpty(versionText))
            {
                versionText = versionText.ToLower();

                string[] buff = versionText.ToLower().Split('.');

                if (buff.Length >= 3)
                {
                    int major = int.Parse(buff[0]);
                    int minor = int.Parse(buff[1]);
                    int extra = 0;

                    if (!int.TryParse(buff[2], out int build))
                    {
                        int index = buff[2].IndexOf('.');

                        if (index != -1)
                        {
                            build = int.Parse(buff[2].Substring(0, index));
                        }
                        else
                        {
                            int i = 0;

                            for (; i < buff[2].Length; i++)
                            {
                                if (!char.IsNumber(buff[2][i]))
                                {
                                    build = int.Parse(buff[2].Substring(0, i));
                                    break;
                                }
                            }

                            if (i < buff[2].Length)
                            {
                                extra = (sbyte) buff[2].Substring(i, buff[2].Length - i)[0];

                                char start = 'a';
                                index = 0;
                                while (start != extra && start <= 'z')
                                {
                                    start++;
                                    index++;
                                }

                                extra = index;
                            }
                        }
                    }

                    if (buff.Length > 3)
                        extra = int.Parse(buff[3]);

                    version = (ClientVersion) (((major & 0xFF) << 24) | ((minor & 0xFF) << 16) | ((build & 0xFF) << 8) | (extra & 0xFF));
                    return true;
                }               
            }

            version = 0;
            return false;
        }

        public static bool TryParseFromFile(string clientpath, out string version)
        {
            if (File.Exists(clientpath))
            {
                FileInfo fileInfo = new FileInfo(clientpath);

                DirectoryInfo dirInfo = new DirectoryInfo(fileInfo.DirectoryName);
                if (dirInfo.Exists)
                {
                    foreach (var clientInfo in dirInfo.GetFiles("client.exe", SearchOption.TopDirectoryOnly))
                    {
                        FileVersionInfo versInfo = FileVersionInfo.GetVersionInfo(clientInfo.FullName);
                        if (versInfo != null && !string.IsNullOrEmpty(versInfo.FileVersion))
                        {
                            version = versInfo.FileVersion.Replace(",", ".").Replace(" ", "").ToLower();
                            return true;
                        }
                    }
                }
               
            }

            version = null;
            return false;
        }
    }
}