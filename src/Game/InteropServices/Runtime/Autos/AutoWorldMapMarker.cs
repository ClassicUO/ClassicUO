#region license

// Copyright (C) 2020 project dust765
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
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Game.InteropServices.Runtime.Shared;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.InteropServices.Runtime.Autos
{
    internal class AutoWorldMapMarker
    {
        //##Register Command and Perform Checks##//
        public static void LoadCommands()
        {
            CommandManager.Register("marker", new Action<string[]>(OnCommand_Client));
        }

        private static void OnCommand_Client(string[] s)
        {
            if (s.Length == 0)
            {
                Print("Usage: -marker X Y (or -marker to remove em)");
                return;
            }

            string strcmd = s[0];
            if (strcmd.Length == 0 | strcmd == "marker")
            {
                Print("Usage: -marker X Y (or -marker to remove em)");

                DoRemoveMarkers();

                return;
            }

            string[] posXY = strcmd.Split(' ');

            if (posXY.Length < 2 || posXY.Length > 2)
            {
                Print("Usage: -marker X Y (or -marker to remove em)");
            }
            else
            {
                DoAddMarker(posXY[0], posXY[1]);
            }
        }
        //##Perform AutoMarker Stuff##//
        private static void DoRemoveMarkers()
        {
            WorldMapGump worldMap = UIManager.GetGump<WorldMapGump>();

            if (worldMap == null || worldMap.IsDisposed)
            {
                Print("No world map open");
            }
            else
            {
                worldMap._tempX = 0;
                worldMap._tempY = 0;
                worldMap._tempTmapStartX = 0;
                worldMap._tempTmapStartY = 0;
                worldMap._tempTmapWidth = 0;
                worldMap._tempTmapHeight = 0;
                worldMap._tempTmapX = 0;
                worldMap._tempTmapY = 0;
            }
        }

        private static void DoAddMarker(string X, string Y)
        {
            WorldMapGump worldMap = UIManager.GetGump<WorldMapGump>();

            if (worldMap == null || worldMap.IsDisposed)
            {
                Print("No world map open");
            }
            else
            {
                worldMap._tempX = int.Parse(X);
                worldMap._tempY = int.Parse(Y);
            }
        }

        public static void TmapPinXY(ushort pinX, ushort pinY)
        {
            if (!ProfileManager.CurrentProfile.AutoWorldmapMarker)
                return;

            WorldMapGump worldMap = UIManager.GetGump<WorldMapGump>();

            if (worldMap == null || worldMap.IsDisposed)
            {
                Print("No world map open");
            }
            else
            {
                if (worldMap._tempTmapStartX != 0 || worldMap._tempTmapStartY != 0 || worldMap._tempTmapWidth != 0 || worldMap._tempTmapHeight != 0)
                {
                    int X = worldMap._tempTmapStartX + pinX;
                    int Y = worldMap._tempTmapStartY + pinY;

                    if (worldMap._tempTmapWidth >= pinX || worldMap._tempTmapHeight >= pinY)
                    {
                        worldMap._tempTmapX = X;
                        worldMap._tempTmapY = Y;
                    }
                }
            }
        }
        public static void TmapMarker(ushort startX, ushort startY, ushort endX, ushort endY, ushort width, ushort height)
        {
            if (!ProfileManager.CurrentProfile.AutoWorldmapMarker)
                return;

            WorldMapGump worldMap = UIManager.GetGump<WorldMapGump>();

            if (worldMap == null || worldMap.IsDisposed)
            {
                Print("No world map open");
            }
            else
            {
                worldMap._tempTmapStartX = startX;
                worldMap._tempTmapStartY = startY;
                worldMap._tempTmapEndX = endX;
                worldMap._tempTmapEndY = endY;
                worldMap._tempTmapWidth = width;
                worldMap._tempTmapHeight = height;
            }
        }

        public static void Print(string message, MessageColor color = MessageColor.Default)
        {
            GameActions.Print(message, (ushort) color, MessageType.System, 1);
        }
    }
}