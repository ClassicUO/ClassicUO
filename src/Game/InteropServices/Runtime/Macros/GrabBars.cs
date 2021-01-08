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
using System.Linq;
using ClassicUO.Configuration;
using Microsoft.Xna.Framework;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.InteropServices.Runtime.Macros
{
    internal class GrabBars
    {
        public static void GrabFriendlyBars()
        {
            GameObject _fbarObject;
            _fbarObject = SelectedObject.Object as GameObject;

            foreach (Mobile mobile in World.Mobiles)
            {
                if (World.Mobiles.Get(mobile).Distance < 18)
                {
                    if (mobile.Name == null || mobile.Name.Length == 0)
                    {
                        return;
                    }
                    else
                        foreach (Mobile mobile1 in World.Mobiles)
                        {
                            if ((mobile.NotorietyFlag == NotorietyFlag.Innocent) && (mobile != World.Player) && (mobile.NotorietyFlag != NotorietyFlag.Invulnerable))
                            {
                                Entity entity = World.Get(mobile.Serial);
                                Point offset = ProfileManager.CurrentProfile.PullFriendlyBars;
                                var _dragginObject = SelectedObject.Object as GameObject;

                                if (Math.Abs(offset.X) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS || Math.Abs(offset.Y) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
                                {
                                    GameObject obj = ProfileManager.CurrentProfile.SallosEasyGrab && SelectedObject.LastObject is GameObject o ? o : _dragginObject;
                                    GameActions.RequestMobileStatus(entity.Serial);
                                    var customgump = UIManager.GetGump<HealthBarGumpCustom>(entity.Serial);
                                    if (customgump != null)
                                    {
                                        customgump.Dispose();
                                    }

                                    if (entity.Serial == World.Player)
                                        StatusGumpBase.GetStatusGump()?.Dispose();
                                    var BAR = UIManager.Gumps.OfType<HealthBarGumpCustom>().OrderBy(s => mobile.NotorietyFlag).Count();

                                    Rectangle rect = new Rectangle(0, 0, HealthBarGumpCustom.HPB_WIDTH, HealthBarGumpCustom.HPB_HEIGHT_SINGLELINE);
                                    HealthBarGumpCustom currentCustomHealthBarGump;
                                    UIManager.Add(currentCustomHealthBarGump = new HealthBarGumpCustom(entity) { X = ProfileManager.CurrentProfile.PullFriendlyBarsFinalLocation.X - (rect.Width >> 1), Y = ProfileManager.CurrentProfile.PullFriendlyBarsFinalLocation.Y + (36 * BAR) });

                                    break;
                                }
                            }
                        }
                    continue;
                }
            }
        }

        public static void GrabEnemyBars()
        {
            GameObject _ebarObject1;
            _ebarObject1 = SelectedObject.Object as GameObject;

            foreach (Mobile mobile in World.Mobiles)
            {
                if (World.Mobiles.Get(mobile).Distance < 18)
                {
                    if (mobile.Name == null || mobile.Name.Length == 0)
                    {
                        return;
                    }
                    else
                        foreach (Mobile mobile1 in World.Mobiles)
                        {
                            if ((mobile.NotorietyFlag == NotorietyFlag.Criminal || mobile.NotorietyFlag == NotorietyFlag.Enemy || mobile.NotorietyFlag == NotorietyFlag.Gray || mobile.NotorietyFlag == NotorietyFlag.Murderer) && (World.Party.Leader != mobile || World.Party.Members.Length > 0 && !World.Party.Contains(mobile)) && (mobile != World.Player) && (mobile.NotorietyFlag != NotorietyFlag.Invulnerable))
                            {
                                Entity entity = World.Get(mobile.Serial);
                                Point offset = ProfileManager.CurrentProfile.PullEnemyBars;
                                var _dragginObject = SelectedObject.Object as GameObject;

                                if (Math.Abs(offset.X) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS || Math.Abs(offset.Y) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
                                {
                                    GameObject obj = ProfileManager.CurrentProfile.SallosEasyGrab && SelectedObject.LastObject is GameObject o ? o : _dragginObject;
                                    GameActions.RequestMobileStatus(entity.Serial);
                                    var customgump = UIManager.GetGump<HealthBarGumpCustom>(entity.Serial);
                                    if (customgump != null)
                                    {
                                        customgump.Dispose();
                                    }

                                    if (entity.Serial == World.Player)
                                        StatusGumpBase.GetStatusGump()?.Dispose();
                                    var BAR = UIManager.Gumps.OfType<HealthBarGumpCustom>().OrderBy(s => mobile.NotorietyFlag).Count();

                                    Rectangle rect = new Rectangle(0, 0, HealthBarGumpCustom.HPB_WIDTH, HealthBarGumpCustom.HPB_HEIGHT_SINGLELINE);
                                    HealthBarGumpCustom currentCustomHealthBarGump;
                                    UIManager.Add(currentCustomHealthBarGump = new HealthBarGumpCustom(entity) { X = ProfileManager.CurrentProfile.PullEnemyBarsFinalLocation.X - (rect.Width >> 1), Y = ProfileManager.CurrentProfile.PullEnemyBarsFinalLocation.Y + (36 * BAR) });

                                    break;
                                }
                            }
                        }
                    continue;
                }
            }
        }

        public static void GrabPartyAllyBars()
        {
            GameObject _ebarObject2;
            _ebarObject2 = SelectedObject.Object as GameObject;

            foreach (Mobile mobile in World.Mobiles)
            {
                if (World.Mobiles.Get(mobile).Distance < 18)
                {
                    if (mobile.Name == null || mobile.Name.Length == 0)
                    {
                        return;
                    }
                    else
                        foreach (Mobile mobile1 in World.Mobiles)
                        {
                            if ((mobile.NotorietyFlag == NotorietyFlag.Ally || World.Party.Leader == mobile || World.Party.Members.Length > 0 && World.Party.Contains(mobile)) && (mobile != World.Player) && (mobile.NotorietyFlag != NotorietyFlag.Invulnerable))
                            {
                                Entity entity = World.Get(mobile.Serial);
                                Point offset = ProfileManager.CurrentProfile.PullPartyAllyBars;//new Point(1470, 214);
                                var _dragginObject = SelectedObject.Object as GameObject;

                                if (Math.Abs(offset.X) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS || Math.Abs(offset.Y) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
                                {
                                    GameObject obj = ProfileManager.CurrentProfile.SallosEasyGrab && SelectedObject.LastObject is GameObject o ? o : _dragginObject;
                                    GameActions.RequestMobileStatus(entity.Serial);
                                    var customgump = UIManager.GetGump<HealthBarGumpCustom>(entity.Serial);
                                    if (customgump != null)
                                    {
                                        customgump.Dispose();
                                    }

                                    if (entity.Serial == World.Player)
                                        StatusGumpBase.GetStatusGump()?.Dispose();
                                    var BAR = UIManager.Gumps.OfType<HealthBarGumpCustom>().OrderBy(s => mobile.NotorietyFlag).Count();

                                    Rectangle rect = new Rectangle(0, 0, HealthBarGumpCustom.HPB_WIDTH, HealthBarGumpCustom.HPB_HEIGHT_SINGLELINE);
                                    HealthBarGumpCustom currentCustomHealthBarGump;
                                    UIManager.Add(currentCustomHealthBarGump = new HealthBarGumpCustom(entity) { X = ProfileManager.CurrentProfile.PullPartyAllyBarsFinalLocation.X - (rect.Width >> 1), Y = ProfileManager.CurrentProfile.PullPartyAllyBarsFinalLocation.Y + (36 * BAR) });

                                    break;
                                }
                            }
                        }
                    continue;
                }
            }
        }
    }
}
