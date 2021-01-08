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
using ClassicUO.Game.InteropServices.Runtime.Shared;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using System;
using System.Threading;

namespace ClassicUO.Game.InteropServices.Runtime.Autos
{
    internal class AutoEngage
    {
        public static bool IsEnabled { get; set; }
        public static Thread a_EngageThread;

        public static Entity _followingtarget;

        //##Auto Engage Toggle##//
        public static void Toggle()
        {
            GameActions.Print(String.Format("Auto Engage Is:{0}abled", (IsEnabled = !IsEnabled) == true ? "En" : "Dis"), 70);
        }

        //##Register Command and Perform Checks##//
        public static void Initialize()
        {
            CommandManager.Register("engage", args => Toggle());

            a_EngageThread = new Thread(new ThreadStart(DoAutoEngage))
            {
                IsBackground = true
            };
        }

        //##Default AutoEngage Status on GameLoad##//
        static AutoEngage()
        {
            IsEnabled = false;
        }

        //##Perform AutoEngage Update on Toggle/Player Death##//
        public static void Update()
        {
            if (!IsEnabled || World.Player.IsDead)
                DisableAutoEngage();

            if (IsEnabled)
                EnableAutoEngage();
        }

        //##Enable AutoEngage##//
        private static void EnableAutoEngage()
        {
            if (!a_EngageThread.IsAlive)
            {
                a_EngageThread = new Thread(new ThreadStart(DoAutoEngage))
                {
                    IsBackground = true
                };
                a_EngageThread.Start();
                GameActions.Print("Commence Enage!", 65, MessageType.System); //#Test
            }
        }

        //##Disable AutoEngage##//
        private static void DisableAutoEngage()
        {
            if (a_EngageThread.IsAlive)
            {
                a_EngageThread.Abort();
                GameActions.Print("Aborting Engage!", 33, MessageType.System); //#Test
            }
        }

        //##Perform AutoEngage Checks##//
        private static void DoAutoEngage()
        {
            while (true)
            {

                if (World.Player == null || World.Player.IsDead)
                    DisableAutoEngage();

                if (!World.Player.IsDead && World.Player != null)
                {
                    Engage();
                    Thread.Sleep(125);
                }
                Thread.Sleep(125);
            }
        }

        //##Perform UseSkill + Pause##//
        private static void Engage()
        {
            var target = World.Get(TargetManager.LastTargetInfo.Serial);
            if (target != null)
            {
                _followingtarget = target;

                //MORE CHECKS
                if (_followingtarget.IsDestroyed)
                    return;
                if (_followingtarget.Hits == 0)
                    return;


                //CHECK IF WE HAVE ATTACKED IT YET
                if (TargetManager.LastAttack != _followingtarget.Serial)
                {
                    GameActions.Print("Attacking Target!", 101, MessageType.System);
                    GameActions.Attack(_followingtarget.Serial);
                    Thread.Sleep(250);
                }

                if (target.Hits <= target.HitsMax && target.Distance < 12 && target.Distance > 1)
                {
                    //IF BETWEEN 2 AND 11 TILES, PATHFIND TO IT, WE DONT NEED TO IF NEXT TO IT

                    //walk to it
                    GameActions.Print("Pathfinding to Target for 2 secs!", 101, MessageType.System);
                    Pathfinder.WalkTo(_followingtarget.X, _followingtarget.Y, _followingtarget.Z, 0);

                    Thread.Sleep(2000);
                    return;
                }
                else if (target.Hits <= target.HitsMax && target.Distance >= 12 && target.Distance <= 30)
                {
                    Print("Distance to LastTarget too far: " + target.Distance + " tiles, get closer!");
                    Thread.Sleep(2000);
                }
                return;
            }
        }

        //##Perform Message##//
        public static void Print(string message, MessageColor color = MessageColor.Default)
        {
            GameActions.Print(message, (ushort) color, MessageType.System, 1);
        }
    }
}
