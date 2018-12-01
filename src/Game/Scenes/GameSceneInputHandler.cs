﻿#region license
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

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Gumps.UIGumps;
using ClassicUO.Game.System;
using ClassicUO.Input;
using ClassicUO.Interfaces;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

using SDL2;

namespace ClassicUO.Game.Scenes
{
    partial class GameScene
    {
        private double _dequeueAt;
        private bool _inqueue;
        private Action _queuedAction;
        private Entity _queuedObject;
        private bool _rightMousePressed;
        public List<Mobile> MobileGumpStack = new List<Mobile>();
        public List<Mobile> PartyMemberGumpStack = new List<Mobile>();
        public List<Skill> SkillButtonGumpStack = new List<Skill>();
        
        public bool IsMouseOverUI => UIManager.IsMouseOverUI && !(UIManager.MouseOverControl is WorldViewport);

        public bool IsMouseOverWorld => UIManager.IsMouseOverUI && UIManager.MouseOverControl is WorldViewport;

        private void MoveCharacterByInputs()
        {
            if (World.InGame)
            {
                Point center = new Point(_settings.GameWindowX + _settings.GameWindowWidth / 2, _settings.GameWindowY + _settings.GameWindowHeight / 2);
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
            if (TargetSystem.IsTargeting)
            {
                switch (TargetSystem.TargetingState)
                {
                    case TargetType.Position:
                    case TargetType.Object:
                        GameObject obj = null;

                        if (IsMouseOverUI)
                        {
                            GumpControl control = UIManager.MouseOverControl;

                            if (control is ItemGump gumpling)
                                obj = gumpling.Item;
                            else if (control.Parent is MobileHealthGump healthGump)
                                obj = healthGump.Mobile;
                        }
                        else if (IsMouseOverWorld) obj = SelectedObject;

                        if (obj != null)
                        {
                            TargetSystem.TargetGameObject(obj);
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
                            TargetSystem.TargetGameObject(obj);
                            Mouse.LastLeftButtonClickTime = 0;
                            UIManager.Add(new InfoGump(obj));

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

                    if (obj != null && obj.Distance < 3)
                    {
                        switch (obj)
                        {
                            case Mobile mobile:
                                MergeHeldItem(mobile);

                                break;
                            case IDynamicItem dyn:

                                if (dyn is Item item)
                                {
                                    if (item.IsCorpse)
                                        MergeHeldItem(item);
                                    else
                                    {
                                        SelectedObject = item;

                                        if (item.Graphic == HeldItem.Graphic && HeldItem is IDynamicItem dyn1 && TileData.IsStackable(dyn1.ItemData.Flags))
                                            MergeHeldItem(item);
                                        else
                                            DropHeldItemToWorld(obj.Position.X, obj.Position.Y, (sbyte) (obj.Position.Z + dyn.ItemData.Height));
                                    }
                                }
                                else
                                    DropHeldItemToWorld(obj.Position.X, obj.Position.Y, (sbyte) (obj.Position.Z + dyn.ItemData.Height));

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

                        {
                            if (string.IsNullOrEmpty(st.Name))
                                TileData.StaticData[st.Graphic].Name = Cliloc.GetString(1020000 + st.Graphic);
                            obj.AddGameText(MessageType.Label, st.Name, 3, 0, false);
                            _staticManager.Add(st);

                            break;
                        }
                        case Entity entity:

                            if (!_inqueue)
                            {
                                _inqueue = true;
                                _queuedObject = entity;
                                _dequeueAt = Mouse.MOUSE_DELAY_DOUBLE_CLICK;

                                _queuedAction = () =>
                                {
                                    if (!World.ClientFeatures.TooltipsEnabled)
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
                Point point = _mousePicker.MouseOverObjectPoint;

                switch (obj)
                {
                    case Item item:
                        e.Result = true;
                        GameActions.DoubleClick(item);

                        break;
                    case Mobile mob:
                        e.Result = true;

                        if (World.Player.InWarMode)
                        {
                            //TODO: attack request
                            
                        }
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
                if (_settings.EnablePathfind && !Pathfinder.AutoWalking)
                {
                    if (_mousePicker.MouseOverObject is Land || _mousePicker.MouseOverObject is IDynamicItem dyn && TileData.IsSurface( dyn.ItemData.Flags))
                    {
                        GameObject obj = _mousePicker.MouseOverObject;

                        if (Pathfinder.WalkTo(obj.Position.X, obj.Position.Y, obj.Position.Z, 0))
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
                            //Health Bar

                            // Check if dragged mobile is in party for doing the party part ;)
                            PartyMember member = new PartyMember(mobile);

                            if (PartySystem.Members.Exists(x => x.Serial == member.Serial))
                            {
                                //Checks if party member gump is already on sceen
                                if (PartyMemberGumpStack.Contains(mobile))
                                    UIManager.Remove<PartyMemberGump>(mobile);
                                else if (mobile == World.Player)
                                {
                                    StatusGumpBase status = UIManager.GetByLocalSerial<StatusGumpBase>();
                                    status?.Dispose();
                                }

                                PartyMemberGump partymemberGump = new PartyMemberGump(member, _mousePicker.Position.X, _mousePicker.Position.Y);
                                UIManager.Add(partymemberGump);
                                PartyMemberGumpStack.Add(mobile);
                                Rectangle rect = IO.Resources.Gumps.GetGumpTexture(0x0804).Bounds;
                                UIManager.AttemptDragControl(partymemberGump, new Point(_mousePicker.Position.X + rect.Width / 2, _mousePicker.Position.Y + rect.Height / 2), true);
                            }
                            else
                            {
                                if (MobileGumpStack.Contains(mobile))
                                    UIManager.Remove<MobileHealthGump>(mobile);
                                else if (mobile == World.Player)
                                {
                                    StatusGumpBase status = UIManager.GetByLocalSerial<StatusGumpBase>();
                                    status?.Dispose();
                                }

                                MobileGumpStack.Add(mobile);
                                Rectangle rect = IO.Resources.Gumps.GetGumpTexture(0x0804).Bounds;
                                MobileHealthGump currentMobileHealthGump;
                                UIManager.Add(currentMobileHealthGump = new MobileHealthGump(mobile, Mouse.Position.X - rect.Width / 2, Mouse.Position.Y - rect.Height / 2));
                                UIManager.AttemptDragControl(currentMobileHealthGump, Mouse.Position, true);
                            }

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
            if (TargetSystem.IsTargeting && e.keysym.sym == SDL.SDL_Keycode.SDLK_ESCAPE && e.keysym.mod == SDL.SDL_Keymod.KMOD_NONE)
                TargetSystem.SetTargeting(TargetType.Nothing, 0, 0);

          
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