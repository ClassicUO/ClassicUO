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
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Interfaces;
using ClassicUO.IO;
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
        private bool _rightMousePressed, _continueRunning;


        public bool IsMouseOverUI => Engine.UI.IsMouseOverAControl && !(Engine.UI.MouseOverControl is WorldViewport);


        private void MoveCharacterByInputs()
        {
            if (World.InGame && !Pathfinder.AutoWalking)
            {
                Point center = new Point(Engine.Profile.Current.GameWindowPosition.X + (Engine.Profile.Current.GameWindowSize.X >> 1), Engine.Profile.Current.GameWindowPosition.Y + (Engine.Profile.Current.GameWindowSize.Y>> 1));
                Direction direction = DirectionHelper.DirectionFromPoints(center, Mouse.Position);

                float distanceFromCenter = Utility.MathHelper.GetDistance(center, Mouse.Position);

                bool run = distanceFromCenter >= 190.0f;

                World.Player.Walk(direction, run);
            }
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButton.Left)
            {
                if (_rightMousePressed)
                {
                    _continueRunning = true;
                }
                else
                {

                    GameObject obj = _mousePicker.MouseOverObject;
                    Point point = _mousePicker.MouseOverObjectPoint;
                    _dragginObject = obj;
                    _dragOffset = point;
                }

            }
            else if (e.Button == MouseButton.Right)
            {
                if (!_rightMousePressed)
                {
                    _rightMousePressed = true;
                    _continueRunning = false;
                }
            }
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButton.Left)
            {
                if (Engine.UI.IsDragging /*&& Mouse.LDroppedOffset != Point.Zero*/)
                    return;

                if (TargetManager.IsTargeting)
                {
                    switch (TargetManager.TargetingState)
                    {
                        case TargetType.Position:
                        case TargetType.Object:
                            GameObject obj = SelectedObject;

                            if (obj != null)
                            {
                                TargetManager.TargetGameObject(obj);
                                Mouse.LastLeftButtonClickTime = 0;
                            }

                            break;
                        case TargetType.Nothing:

                            break;
                        case TargetType.SetTargetClientSide:

                            obj = SelectedObject;
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

                                    if (item.Graphic == HeldItem.Graphic && HeldItem is Item dyn1 && dyn1.ItemData.IsStackable)
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
                else
                {                 
                    GameObject obj = _mousePicker.MouseOverObject;

                    switch (obj)
                    {
                        case Static st:

                            string name = st.Name;
                            if (string.IsNullOrEmpty(name))
                                name = FileManager.Cliloc.GetString(1020000 + st.Graphic);

                            if (obj.Overheads.Count == 0)
                                obj.AddOverhead(MessageType.Label, name, 3, 0, false);

                            break;
                        case Multi multi:
                            name = multi.Name;

                            if (string.IsNullOrEmpty(name))
                                name = FileManager.Cliloc.GetString(1020000 + multi.Graphic);

                            if (obj.Overheads.Count == 0)
                                obj.AddOverhead(MessageType.Label, name, 3, 0, false);
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
            else if (e.Button == MouseButton.Right)
            {
                if (_rightMousePressed)
                    _rightMousePressed = false;
            }
        }

        private void OnMouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
        {
            if (e.Button == MouseButton.Left)
            {
                GameObject obj = _mousePicker.MouseOverObject;

                switch (obj)
                {
                    case Item item:
                        e.Result = false;
                        GameActions.DoubleClick(item);

                        break;
                    case Mobile mob:
                        e.Result = true;

                        if (World.Player.InWarMode && World.Player != mob)
                            GameActions.Attack(mob);
                        else
                            GameActions.DoubleClick(mob);

                        break;
                    case GameEffect effect when effect.Source is Item item:
                        e.Result = false;
                        GameActions.DoubleClick(item);

                        break;
                    case TextOverhead overhead when overhead.Parent is Entity entity:
                        e.Result = false;
                        GameActions.DoubleClick(entity);
                        break;
                }

                ClearDequeued();
            }
            else if (e.Button == MouseButton.Right)
            {
                if (Engine.Profile.Current.EnablePathfind && !Pathfinder.AutoWalking)
                {
                    if (_mousePicker.MouseOverObject is Land || (GameObjectHelper.TryGetStaticData(_mousePicker.MouseOverObject, out var itemdata) && itemdata.IsSurface))
                    {
                        GameObject obj = _mousePicker.MouseOverObject;

                        if (Pathfinder.WalkTo(obj.X, obj.Y, obj.Z, 0))
                        {
                            World.Player.AddOverhead(MessageType.Label, "Pathfinding!", 3, 0, false);

                            e.Result = true;
                        }
                    }
                }
            }
        }

        private void OnMouseDragBegin(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButton.Left)
            {
                if (!IsHoldingItem)
                {
                    GameObject obj = _mousePicker.MouseOverObject;

                    switch (obj)
                    {
                        case Mobile mobile:
                            GameActions.RequestMobileStatus(mobile);

                            Engine.UI.GetByLocalSerial<HealthBarGump>(mobile)?.Dispose();

                            if (mobile == World.Player)
                                Engine.UI.GetByLocalSerial<StatusGump>()?.Dispose();

                            Rectangle rect = FileManager.Gumps.GetTexture(0x0804).Bounds;
                            HealthBarGump currentHealthBarGump;
                            Engine.UI.Add(currentHealthBarGump = new HealthBarGump(mobile) { X = Mouse.Position.X - (rect.Width >> 1), Y = Mouse.Position.Y - (rect.Height >> 1) });
                            Engine.UI.AttemptDragControl(currentHealthBarGump, Mouse.Position, true);


                            break;
                        case Item item:
                            PickupItemBegin(item, _dragOffset.X, _dragOffset.Y);

                            break;
                    }
                }
            }
        }


        private void OnKeyDown(object sender, SDL.SDL_KeyboardEvent e)
        {
            if (TargetManager.IsTargeting && e.keysym.sym == SDL.SDL_Keycode.SDLK_ESCAPE && Input.Keyboard.IsModPressed(e.keysym.mod, SDL.SDL_Keymod.KMOD_NONE))
                TargetManager.SetTargeting(TargetType.Nothing, 0, 0);

            if (e.keysym.sym == SDL.SDL_Keycode.SDLK_0)
            {

                foreach (Mobile mobile in World.Mobiles)
                {
                    mobile.AddOverhead(MessageType.Regular, "AAAAAAAAAAAAAAAAAAAAA", 1, 0x45, true);
                }

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