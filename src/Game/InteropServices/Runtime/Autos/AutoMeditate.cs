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
using System.Threading;
using ClassicUO.Game.InteropServices.Runtime.Shared;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;

namespace ClassicUO.Game.InteropServices.Runtime.Autos
{
    internal class AutoMeditate
    {
        public static bool IsEnabled { get; set; }
        public static Thread a_MeditateThread;

		//##AutoMeditate Toggle##//
        public static void Toggle()
        {
            GameActions.Print(String.Format("Auto Meditate:{0}abled", (IsEnabled = !IsEnabled) == true ? "En" : "Dis"), 70);
        }
		
		//##Register Command and Perform Checks##//
        public static void Initialize()
        {
            CommandManager.Register("automed", args => Toggle());

            a_MeditateThread = new Thread(new ThreadStart(DoAutoMeditate))
            {
                IsBackground = true
            };
        }
		
		//##Default AutoMeditate Status on GameLoad##//
        static AutoMeditate()
        {
            IsEnabled = false;
        }
		
		//##Perform AutoMeditate Update on Toggle/Player Death##//
        public static void Update()
        {
            if (!IsEnabled || World.Player.IsDead)            
                DisableAutoMeditate();

            if (IsEnabled)
                EnableAutoMeditate();            
        }

		//##Enable AutoMeditate##//
        private static void EnableAutoMeditate()
        {
            if (!a_MeditateThread.IsAlive)
            {
                a_MeditateThread = new Thread(new ThreadStart(DoAutoMeditate))
                {
                    IsBackground = true
                };
                a_MeditateThread.Start();
            }
        }

		//##Disable AutoMeditate##//
        private static void DisableAutoMeditate()
        {
            if (a_MeditateThread.IsAlive)
            {
                a_MeditateThread.Abort();
            }
        }
		
		//##Perform Meditate Checks##//
        private static void DoAutoMeditate()
        {
            DateTime dateTime = DateTime.Now;
            while (true)
            {

                if (World.Player == null || World.Player.IsDead)
                    DisableAutoMeditate();

                if (!World.Player.IsRunning && !World.Player.IsDead && World.Player != null && World.Player.Mana < World.Player.ManaMax && TargetManager.IsTargeting == false && ((DateTime.Now - dateTime) > TimeSpan.FromSeconds(2.5)))
                { 
                    Meditate();
                    dateTime = DateTime.Now;
                    Thread.Sleep(250);
                }
                Thread.Sleep(250);
            }
        }
		
		//##Perform UseSkill + Pause##//
        private static void Meditate()
        {
            GameActions.UseSkill(46);
            Thread.Sleep(15000);//7500
        }
		
		//##Perform Message##//
        public static void Print(string message, MessageColor color = MessageColor.Default)
        {
            GameActions.Print(message, (ushort) color, MessageType.System, 1);
        }
    }
}