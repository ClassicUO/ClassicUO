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
using ClassicUO.Dust765.Autos;
using ClassicUO.Game.Cheats.AIBot;
// ## BEGIN - END ## // LOBBY
using ClassicUO.Dust765.Lobby;
// ## BEGIN - END ## // LOBBY

namespace ClassicUO.Game.Managers // only exception to not being in the 'InteropServices.Runtime' namespace
{
    internal class ModulesManager
    {
        public static void Load()
        {
            AutoRangeDisplay.Initialize(); //AutoRangeDisplay//
            AutoMeditate.Initialize();  //AUTOMEDITATE##//
            AutoEngage.Initialize();  //AUTOENGAGE##//
            AutoMimic.Initialize();  //##AUTO MIMIC##//
            Automation.Initialize();
            //AutoWorldMapMarker.LoadCommands();
            Defender.Initialize();
            // ## BEGIN - END ## // LOBBY
            Lobby.LoadCommands();
            AutoLobbyStealthPosition.Initialize();
            // ## BEGIN - END ## // LOBBY

            GameActions.Print("Project dust765 Enabled.", 95);
        }

        public static void Unload()
        {
            // ## BEGIN - END ## // LOBBY
            Lobby.Disconnect();
            // ## BEGIN - END ## // LOBBY
        }

        /// <summary>
        ///     Placed above _effectManager.Update(totalMS, frameMS);
        /// </summary>
        /// <param name="totalMS"></param>
        public static void OnWorldUpdate()
        {
            AutoRangeDisplay.Update(); //AutoRangeDisplay//
            AutoMeditate.Update();  //AUTOMEDITATE##//
            AutoEngage.Update();  //AUTOENGAGE##//
            Defender.Update();
            Automation.Update();
            // ## BEGIN - END ## // LOBBY
            Lobby.Update();
            AutoLobbyStealthPosition.Update();
            // ## BEGIN - END ## // LOBBY
        }
    }
}
