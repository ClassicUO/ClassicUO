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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Gumps.UIGumps;
using ClassicUO.Game.System;
using ClassicUO.Input;
using ClassicUO.Interfaces;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

using SDL2;

using Multi = ClassicUO.Game.GameObjects.Multi;

namespace ClassicUO.Game.Scenes
{
    partial class GameScene
    {
        private double _dequeueAt;
        private bool _inqueue;
        private Action _queuedAction;
        private Entity _queuedObject;
        private bool _rightMousePressed;

 
        public bool IsMouseOverUI => Engine.UI.IsMouseOverUI && !(Engine.UI.MouseOverControl is WorldViewport);

        public bool IsMouseOverWorld => Engine.UI.IsMouseOverUI && Engine.UI.MouseOverControl is WorldViewport;

        private void MoveCharacterByInputs()
        {
            if (World.InGame && !Pathfinder.AutoWalking)
            {
                Point center = new Point(Engine.Profile.Current.GameWindowPosition.X + (Engine.Profile.Current.GameWindowSize.X >> 1), Engine.Profile.Current.GameWindowPosition.Y + (Engine.Profile.Current.GameWindowSize.Y>> 1));
                Direction direction = DirectionHelper.DirectionFromPoints(center, Mouse.Position);
                World.Player.Walk(direction, true);
            }
        }

        private void OnLeftMouseButtonDown(object sender, EventArgs e)
        {
            if (IsMouseOverWorld)
            {
                GameObject obj = _mousePicker.MouseOverObject;
                Point point = _mousePicker.MouseOverObjectPoint;
                _dragginObject = obj;
                _dragOffset = point;
            }
        }

        private void OnLeftMouseButtonUp(object sender, EventArgs e)
        {
            if (TargetManager.IsTargeting)
            {
                switch (TargetManager.TargetingState)
                {
                    case TargetType.Position:
                    case TargetType.Object:
                        GameObject obj = null;

                        if (IsMouseOverUI)
                        {
                            Control control = Engine.UI.MouseOverControl;

                            if (control is ItemGump gumpling)
                                obj = gumpling.Item;
                            else if (control.Parent is HealthBarGump healthGump)
                                obj = healthGump.Mobile;
                        }
                        else if (IsMouseOverWorld) obj = SelectedObject;

                        if (obj != null)
                        {
                            TargetManager.TargetGameObject(obj);
                            Mouse.LastLeftButtonClickTime = 0;
                        }

                        break;
                    case TargetType.Nothing:

                        break;
                    case TargetType.SetTargetClientSide:
                        obj = null;
                        if (IsMouseOverWorld) obj = SelectedObject;
                        if (obj != null)
                        {
                            TargetManager.TargetGameObject(obj);
                            Mouse.LastLeftButtonClickTime = 0;
                            Engine.UI.Add(new InfoGump(obj));

                        }
                        break;
                    default:
                        Log.Message(LogTypes.Warning, "Not implemented.");

                        break;
                }
            }
            else if (IsHoldingItem)
            {
                SelectedObject = null;

                if (IsMouseOverWorld)
                {
                    GameObject obj = _mousePicker.MouseOverObject;

                    if (obj != null && obj.Distance < Constants.DRAG_ITEMS_DISTANCE)
                    {
                        switch (obj)
                        {
                            case Mobile mobile:
                                MergeHeldItem(mobile);

                                break;
                            case Item item:
                                if (item.IsCorpse)
                                    MergeHeldItem(item);
                                else
                                {
                                    SelectedObject = item;

                                    if (item.Graphic == HeldItem.Graphic && HeldItem is Item dyn1 && TileData.IsStackable(dyn1.ItemData.Flags))
                                        MergeHeldItem(item);
                                    else
                                        DropHeldItemToWorld(obj.Position.X, obj.Position.Y, (sbyte)(obj.Position.Z + item.ItemData.Height));
                                }
                                break;
                            case Static st:
                                DropHeldItemToWorld(obj.Position.X, obj.Position.Y, (sbyte)(obj.Position.Z + st.ItemData.Height));
                                break;
                            case Land _:
                                DropHeldItemToWorld(obj.Position);

                                break;
                            default:
                                Log.Message(LogTypes.Warning, "Unhandled mouse inputs for GameObject type " + obj.GetType());

                                return;
                        }
                    }
                }
            }
            else
            {
                if (IsMouseOverWorld)
                {
                    GameObject obj = _mousePicker.MouseOverObject;

                    switch (obj)
                    {
                        case Static st:

                            string name = st.Name;
                            if (string.IsNullOrEmpty(name))
                                name = Cliloc.GetString(1020000 + st.Graphic);

                            if (obj.Overheads.Count == 0)
                                obj.AddGameText(MessageType.Label, name, 3, 0, false);

                            break;
                        case Multi multi:
                            name = multi.Name;

                            if (string.IsNullOrEmpty(name))
                                name = Cliloc.GetString(1020000 + multi.Graphic);

                            if (obj.Overheads.Count == 0)
                                obj.AddGameText(MessageType.Label, name, 3, 0, false);
                            break;
                        case Entity entity:

                            if (!_inqueue)
                            {
                                _inqueue = true;
                                _queuedObject = entity;
                                _dequeueAt = Mouse.MOUSE_DELAY_DOUBLE_CLICK;

                                _queuedAction = () =>
                                {
                                    if (!World.ClientFlags.TooltipsEnabled)
                                        GameActions.SingleClick(_queuedObject);
                                    GameActions.OpenPopupMenu(_queuedObject);
                                };
                            }

                            break;
                    }
                }
            }
        }

        private void OnLeftMouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
        {
            if (IsMouseOverWorld)
            {
                GameObject obj = _mousePicker.MouseOverObject;

                switch (obj)
                {
                    case Item item:
                        e.Result = true;
                        GameActions.DoubleClick(item);

                        break;
                    case Mobile mob:
                        e.Result = true;

                        if (World.Player.InWarMode)
                            GameActions.Attack(mob);                            
                        else
                            GameActions.DoubleClick(mob);

                        break;
                    case GameEffect effect when effect.Source is Item item:
                        e.Result = true;
                        GameActions.DoubleClick(item);

                        break;
                }

                ClearDequeued();
            }
        }

        private void OnRightMouseButtonDown(object sender, EventArgs e)
        {
            if (IsMouseOverWorld && !_rightMousePressed)
                _rightMousePressed = true;
        }

        private void OnRightMouseButtonUp(object sender, EventArgs e)
        {
            if (_rightMousePressed)
                _rightMousePressed = false;
        }

        private void OnRightMouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
        {
            if (IsMouseOverWorld)
            {
                if (Engine.Profile.Current.EnablePathfind && !Pathfinder.AutoWalking)
                {
                    if (_mousePicker.MouseOverObject is Land || (GameObjectHelper.TryGetStaticData(_mousePicker.MouseOverObject, out var itemdata) && TileData.IsSurface(itemdata.Flags)))
                    {
                        GameObject obj = _mousePicker.MouseOverObject;

                        if (Pathfinder.WalkTo(obj.X, obj.Y, obj.Z, 0))
                        {
                            World.Player.AddGameText(MessageType.Label, "Pathfinding!", 3, 0, false);

                            e.Result = true;
                        }
                    }
                }
            }
        }

        private void OnMouseDragBegin(object sender, EventArgs e)
        {
            if (Mouse.LButtonPressed)
            {
                if (!IsHoldingItem && IsMouseOverWorld)
                {
                    GameObject obj = _mousePicker.MouseOverObject;

                    switch (obj)
                    {
                        case Mobile mobile:
                            GameActions.RequestMobileStatus(mobile);
           
                            Engine.UI.GetByLocalSerial<HealthBarGump>(mobile)?.Dispose();

                            if (mobile == World.Player)
                                Engine.UI.GetByLocalSerial<StatusGump>()?.Dispose();

                            Rectangle rect = IO.Resources.Gumps.GetGumpTexture(0x0804).Bounds;
                            HealthBarGump currentHealthBarGump;
                            Engine.UI.Add(currentHealthBarGump = new HealthBarGump(mobile) { X= Mouse.Position.X - (rect.Width >> 1), Y = Mouse.Position.Y - (rect.Height >> 1)});
                            Engine.UI.AttemptDragControl(currentHealthBarGump, Mouse.Position, true);
                            

                            break;
                        case Item item:
                            PickupItemBegin(item, _dragOffset.X, _dragOffset.Y);

                            break;
                    }
                }
            }
        }

        private void OnMouseDragging(object sender, EventArgs e)
        {
        }

        private void OnMouseMoving(object sender, EventArgs e)
        {
        }

        private void OnKeyDown(object sender, SDL.SDL_KeyboardEvent e)
        {
            if (TargetManager.IsTargeting && e.keysym.sym == SDL.SDL_Keycode.SDLK_ESCAPE && e.keysym.mod == SDL.SDL_Keymod.KMOD_NONE)
                TargetManager.SetTargeting(TargetType.Nothing, 0, 0);

            if (e.keysym.sym == SDL.SDL_Keycode.SDLK_0)
            {
               
                //Task.Run(async () =>
                //{
                //    while (true)
                //    {
                //        await Task.Delay(1);
                //        GameActions.CastSpell(205);
                //    }

                //});
            }
            // TEST PURPOSE
            /*if (e.keysym.sym == SDL.SDL_Keycode.SDLK_0)
            {

                bool first = false;

                string tobrit = "[go britain";
                string toluna = "[go luna";

                Task.Run(async () =>
               {

                   while (true)
                   {
                       await Task.Delay(500);

                       NetClient.Socket.Send(new PUnicodeSpeechRequest(first ? tobrit : toluna, MessageType.Regular, MessageFont.Normal, 33, "ENU"));

                       first = !first;


                   }
               });
            }*/
        }

        private void OnKeyUp(object sender, SDL.SDL_KeyboardEvent e)
        {
        }
    }
}