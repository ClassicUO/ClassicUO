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
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

using System.Diagnostics;

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
        private bool _rightMousePressed, _continueRunning, _useObjectHandles;

        private readonly Dictionary<SDL.SDL_Keycode, Direction> _keycodeDirection = new Dictionary<SDL.SDL_Keycode, Direction>()
        {
           { SDL.SDL_Keycode.SDLK_LEFT, Direction.Left },
           { SDL.SDL_Keycode.SDLK_RIGHT, Direction.Right },
           { SDL.SDL_Keycode.SDLK_UP, Direction.Up },
           { SDL.SDL_Keycode.SDLK_DOWN, Direction.Down },
        };

        private readonly Dictionary<SDL.SDL_Keycode, Direction> _keycodeDirectionNum = new Dictionary<SDL.SDL_Keycode, Direction>()
        {
           { SDL.SDL_Keycode.SDLK_KP_4, Direction.Left },
           { SDL.SDL_Keycode.SDLK_KP_6, Direction.Right },
           { SDL.SDL_Keycode.SDLK_KP_8, Direction.Up },
           { SDL.SDL_Keycode.SDLK_KP_2, Direction.Down },
           { SDL.SDL_Keycode.SDLK_KP_9, Direction.North },
           { SDL.SDL_Keycode.SDLK_KP_3, Direction.East },
           { SDL.SDL_Keycode.SDLK_KP_7, Direction.West },
           { SDL.SDL_Keycode.SDLK_KP_1, Direction.South },
        };

        private bool _isShiftDown;

        public bool IsMouseOverUI => Engine.UI.IsMouseOverAControl && !(Engine.UI.MouseOverControl is WorldViewport);
	    
        private void MoveCharacterByInputs()
        {
            if (World.InGame && !Pathfinder.AutoWalking)
            {
                Point center = new Point(Engine.Profile.Current.GameWindowPosition.X + (Engine.Profile.Current.GameWindowSize.X >> 1), Engine.Profile.Current.GameWindowPosition.Y + (Engine.Profile.Current.GameWindowSize.Y>> 1));
                Direction direction = DirectionHelper.DirectionFromPoints(center, Mouse.Position);

                float distanceFromCenter = Utility.MathHelper.GetDistance(center, Mouse.Position);

                bool run = distanceFromCenter >= 150.0f;

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
                
                _dragginObject = _mousePicker.MouseOverObject;
                _dragOffset = _mousePicker.MouseOverObjectPoint;
                
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
                if (_dragginObject != null)
                    _dragginObject = null;

                if (Engine.UI.IsDragging /*&& Mouse.LDroppedOffset != Point.Zero*/)
                    return;

                if (TargetManager.IsTargeting)
                {
                    switch (TargetManager.TargetingState)
                    {
                        case CursorTarget.Position:
                        case CursorTarget.Object:
                        case CursorTarget.MultiPlacement:
                            GameObject obj = SelectedObject;
                            if (obj != null)
                            {
                                TargetManager.TargetGameObject(obj);
                                Mouse.LastLeftButtonClickTime = 0;
                            }

                            break;

                        case CursorTarget.SetTargetClientSide:
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
                                // DropHeldItemToContainer(mobile.Equipment[(int) Layer.Backpack]);
                                MergeHeldItem(mobile);
                                break;

                            case Item item:
                                if (item.IsCorpse)
                                    MergeHeldItem(item);
                                else
                                {
                                    SelectedObject = item;

                                    if (item.Graphic == HeldItem.Graphic && HeldItem.IsStackable)
                                        MergeHeldItem(item);
                                    else
                                        DropHeldItemToWorld(obj.Position.X, obj.Position.Y, (sbyte)(obj.Position.Z + item.ItemData.Height));
                                }
                                break;

                            case Multi multi:
                                DropHeldItemToWorld(obj.Position.X, obj.Position.Y, (sbyte)(obj.Position.Z + multi.ItemData.Height));
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
                    else
                        Engine.SceneManager.CurrentScene.Audio.PlaySound(0x0051);
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
                            if (!obj.HasOverheads || obj.Overheads.Count == 0)
                                obj.AddOverhead(MessageType.Label, name, 3, 0, false);
                            break;

                        case Multi multi:
                            name = multi.Name;
                            if (string.IsNullOrEmpty(name))
                                name = FileManager.Cliloc.GetString(1020000 + multi.Graphic);
                            if (!obj.HasOverheads || obj.Overheads.Count == 0)
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
                        e.Result = true;
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
                        e.Result = true;
                        GameActions.DoubleClick(item);
                        break;

                    case TextOverhead overhead when overhead.Parent is Entity entity:
                        e.Result = true;
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

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (Mouse.LButtonPressed && !IsHoldingItem)
            {
                Point offset = Mouse.LDroppedOffset;

                if (Math.Abs(offset.X) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS || Math.Abs(offset.Y) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
                {
                    GameObject obj = _dragginObject;

                    switch (obj)
                    {
                        case Mobile mobile:
                            GameActions.RequestMobileStatus(mobile);

                            Engine.UI.GetByLocalSerial<HealthBarGump>(mobile)?.Dispose();

                            if (mobile == World.Player)
                                StatusGumpBase.GetStatusGump()?.Dispose();

                            Rectangle rect = FileManager.Gumps.GetTexture(0x0804).Bounds;
                            HealthBarGump currentHealthBarGump;
                            Engine.UI.Add(currentHealthBarGump = new HealthBarGump(mobile) { X = Mouse.Position.X - (rect.Width >> 1), Y = Mouse.Position.Y - (rect.Height >> 1) });
                            Engine.UI.AttemptDragControl(currentHealthBarGump, Mouse.Position, true);

                            break;

                        case Item item when !item.IsCorpse:
                            PickupItemBegin(item, _dragOffset.X, _dragOffset.Y);
                            break;
                    }

                    _dragginObject = null;
                }
            }
        }

        private void OnMouseDragBegin(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButton.Left)
            {
                if (!IsHoldingItem)
                {
                    GameObject obj = _dragginObject;

                    switch (obj)
                    {
                        case Mobile mobile:
                            GameActions.RequestMobileStatus(mobile);

                            Engine.UI.GetByLocalSerial<HealthBarGump>(mobile)?.Dispose();

                            if (mobile == World.Player)
                                StatusGumpBase.GetStatusGump()?.Dispose();

                            Rectangle rect = FileManager.Gumps.GetTexture(0x0804).Bounds;
                            HealthBarGump currentHealthBarGump;
                            Engine.UI.Add(currentHealthBarGump = new HealthBarGump(mobile) { X = Mouse.Position.X - (rect.Width >> 1), Y = Mouse.Position.Y - (rect.Height >> 1) });
                            Engine.UI.AttemptDragControl(currentHealthBarGump, Mouse.Position, true);
                            break;

                        case Item item:
							PickupItemBegin(item, _dragOffset.X, _dragOffset.Y);
                            break;
                    }

                    _dragginObject = null;
                }
            }
        }

        private void OnKeyDown(object sender, SDL.SDL_KeyboardEvent e)
        {
            if (TargetManager.IsTargeting && e.keysym.sym == SDL.SDL_Keycode.SDLK_ESCAPE && Input.Keyboard.IsModPressed(e.keysym.mod, SDL.SDL_Keymod.KMOD_NONE))
                TargetManager.CancelTarget();

            _isShiftDown = Input.Keyboard.IsModPressed(e.keysym.mod, SDL.SDL_Keymod.KMOD_SHIFT);

            if (e.keysym.sym == SDL.SDL_Keycode.SDLK_TAB)
                if (!World.Player.InWarMode && Engine.Profile.Current.HoldDownKeyTab)
                    GameActions.SetWarMode(true);

            if (_keycodeDirection.TryGetValue(e.keysym.sym, out Direction dWalk))
            {
                WorldViewportGump viewport = Engine.UI.GetByLocalSerial<WorldViewportGump>();
                SystemChatControl chat = viewport?.FindControls<SystemChatControl>().SingleOrDefault();
                if (chat != null && chat.textBox.Text.Length == 0)
                    World.Player.Walk(dWalk, false);
            }

            if ((e.keysym.mod & SDL2.SDL.SDL_Keymod.KMOD_NUM) != SDL2.SDL.SDL_Keymod.KMOD_NUM)
                if (_keycodeDirectionNum.TryGetValue(e.keysym.sym, out Direction dWalkN))
                    World.Player.Walk(dWalkN, false);

            bool isshift = (e.keysym.mod & SDL.SDL_Keymod.KMOD_SHIFT) != SDL.SDL_Keymod.KMOD_NONE;
            bool isalt = (e.keysym.mod & SDL.SDL_Keymod.KMOD_ALT) != SDL.SDL_Keymod.KMOD_NONE;
            bool isctrl = (e.keysym.mod & SDL.SDL_Keymod.KMOD_CTRL) != SDL.SDL_Keymod.KMOD_NONE;

            _useObjectHandles = isshift && isctrl;

            Macro macro = _macroManager.FindMacro(e.keysym.sym, isalt, isctrl, isshift);

            if (macro != null)
            {
                _macroManager.SetMacroToExecute(macro.FirstNode);
                _macroManager.WaitForTargetTimer = 0;
                _macroManager.Update();
            }

            //if (_hotkeysManager.TryExecuteIfBinded(e.keysym.sym, e.keysym.mod, out Action action))
            //{
            //    action();
            //}
        }

        private void OnKeyUp(object sender, SDL.SDL_KeyboardEvent e)
        {
            bool isshift = (e.keysym.mod & SDL.SDL_Keymod.KMOD_SHIFT) != SDL.SDL_Keymod.KMOD_NONE;
            bool isalt = (e.keysym.mod & SDL.SDL_Keymod.KMOD_ALT) != SDL.SDL_Keymod.KMOD_NONE;
            bool isctrl = (e.keysym.mod & SDL.SDL_Keymod.KMOD_CTRL) != SDL.SDL_Keymod.KMOD_NONE;


            if (Engine.Profile.Current.EnableScaleZoom &&
                Engine.Profile.Current.RestoreScaleAfterUnpressCtrl 
                && !isctrl)
            {
                Engine.SceneManager.GetScene<GameScene>().Scale = Engine.Profile.Current.RestoreScaleValue;
            }

            _isShiftDown = isshift;

            _useObjectHandles = isctrl && isshift;

			if (e.keysym.sym == SDL.SDL_Keycode.SDLK_TAB)
			{
                if (Engine.Profile.Current.HoldDownKeyTab)
                {
                    if (World.Player.InWarMode)
                        GameActions.SetWarMode(false);
                }
                else
                    GameActions.ToggleWarMode();
			}
		}

    }
}