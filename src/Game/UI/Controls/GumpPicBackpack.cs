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

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class GumpPicBackpack : ItemGumpPaperdoll
    {
        public GumpPicBackpack(int x, int y, Item backpack) : base(x, y, backpack, World.Mobiles.Get(backpack.RootContainer), false)
        {
            Backpack = backpack;
            AcceptMouseInput = false;
            CanPickUp = false;
        }

        public Item Backpack { get; protected set; }

        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            if (button == MouseButton.Left)
            {
                GameScene gs = Engine.SceneManager.GetScene<GameScene>();

                if (TargetManager.IsTargeting)
                {
                    Point offset = Mouse.LDroppedOffset;

                    if (Mouse.IsDragging && !(Math.Abs(offset.X) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS || Math.Abs(offset.Y) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS))
                        return;

                    switch (TargetManager.TargetingState)
                    {
                        case CursorTarget.Position:
                        case CursorTarget.Object:
                            gs.SelectedObject = Backpack;


                            if (Backpack != null)
                            {
                                TargetManager.TargetGameObject(Backpack);
                                Mouse.LastLeftButtonClickTime = 0;
                            }

                            break;

                        case CursorTarget.SetTargetClientSide:
                            gs.SelectedObject = Backpack;

                            if (Backpack != null)
                            {
                                TargetManager.TargetGameObject(Backpack);
                                Mouse.LastLeftButtonClickTime = 0;
                                Engine.UI.Add(new InfoGump(Backpack));
                            }
                            break;
                    }
                }
                else
                {

                    if (gs.IsHoldingItem && gs.IsMouseOverUI)
                        gs.DropHeldItemToContainer(Backpack);
                    else
                    {
                        base.OnMouseUp(x, y, button);
                        //if (!World.ClientFlags.TooltipsEnabled)
                        //    GameActions.SingleClick(Backpack);
                        //GameActions.OpenPopupMenu(Backpack);
                    }
                }
            }
        }


    }
}