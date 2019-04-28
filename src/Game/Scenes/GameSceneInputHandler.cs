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

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

using SDL2;

using MathHelper = ClassicUO.Utility.MathHelper;

namespace ClassicUO.Game.Scenes
{
    internal partial class GameScene
    {
        private readonly Dictionary<SDL.SDL_Keycode, Direction> _keycodeDirection = new Dictionary<SDL.SDL_Keycode, Direction>
        {
            {SDL.SDL_Keycode.SDLK_LEFT, Direction.Left},
            {SDL.SDL_Keycode.SDLK_RIGHT, Direction.Right},
            {SDL.SDL_Keycode.SDLK_UP, Direction.Up},
            {SDL.SDL_Keycode.SDLK_DOWN, Direction.Down}
        };

        private readonly Dictionary<SDL.SDL_Keycode, Direction> _keycodeDirectionNum = new Dictionary<SDL.SDL_Keycode, Direction>
        {
            {SDL.SDL_Keycode.SDLK_KP_4, Direction.Left},
            {SDL.SDL_Keycode.SDLK_KP_6, Direction.Right},
            {SDL.SDL_Keycode.SDLK_KP_8, Direction.Up},
            {SDL.SDL_Keycode.SDLK_KP_2, Direction.Down},
            {SDL.SDL_Keycode.SDLK_KP_9, Direction.North},
            {SDL.SDL_Keycode.SDLK_KP_3, Direction.East},
            {SDL.SDL_Keycode.SDLK_KP_7, Direction.West},
            {SDL.SDL_Keycode.SDLK_KP_1, Direction.South}
        };
        private double _dequeueAt;
        private bool _inqueue;
        private bool _isCtrlDown;

        private bool _isShiftDown;
        private Action _queuedAction;
        private Entity _queuedObject;
        private bool _rightMousePressed, _continueRunning, _useObjectHandles;

        public bool IsMouseOverUI => Engine.UI.IsMouseOverAControl && !(Engine.UI.MouseOverControl is WorldViewport);
        public bool IsMouseOverViewport => Engine.UI.MouseOverControl is WorldViewport;

        private void MoveCharacterByInputs()
        {
            if (World.InGame && !Pathfinder.AutoWalking)
            {
                Point center = new Point(Engine.Profile.Current.GameWindowPosition.X + (Engine.Profile.Current.GameWindowSize.X >> 1), Engine.Profile.Current.GameWindowPosition.Y + (Engine.Profile.Current.GameWindowSize.Y >> 1));
                Direction direction = DirectionHelper.DirectionFromPoints(center, Mouse.Position);

                float distanceFromCenter = MathHelper.GetDistance(center, Mouse.Position);

                bool run = distanceFromCenter >= 150.0f;

                World.Player.Walk(direction, run);
            }
        }

        // LEFT
        private void OnLeftMouseDown(object sender, EventArgs e)
        {
            if (!IsMouseOverViewport)
                return;

            if (_rightMousePressed) _continueRunning = true;

            _dragginObject = Game.SelectedObject.Object as GameObject;
            _dragOffset = Mouse.LDropPosition;
        }

        private void OnLeftMouseUp(object sender, EventArgs e)
        {
            if (!IsMouseOverViewport)
                return;

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
                        var obj = SelectedObject;

                        if (obj != null)
                        {
                            TargetManager.TargetGameObject(obj);
                            Mouse.LastLeftButtonClickTime = 0;
                        }

                        break;

                    case CursorTarget.SetTargetClientSide:

                        if (SelectedObject is GameObject obj2)
                        {
                            TargetManager.TargetGameObject(obj2);
                            Mouse.LastLeftButtonClickTime = 0;
                            Engine.UI.Add(new InfoGump(obj2));
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

                if (Game.SelectedObject.Object is GameObject obj && obj.Distance < Constants.DRAG_ITEMS_DISTANCE)
                {
                    if (obj is AnimatedItemEffect eff && eff.Source is Item it)
                        obj = it;

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
                                    DropHeldItemToWorld(obj.Position.X, obj.Position.Y, (sbyte) (obj.Position.Z + item.ItemData.Height));
                            }

                            break;

                        case Multi multi:
                            DropHeldItemToWorld(obj.Position.X, obj.Position.Y, (sbyte) (obj.Position.Z + multi.ItemData.Height));

                            break;

                        case Static st:
                            DropHeldItemToWorld(obj.Position.X, obj.Position.Y, (sbyte) (obj.Position.Z + st.ItemData.Height));

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
                GameObject obj = Game.SelectedObject.Object as GameObject;

                switch (obj)
                {
                    case Static st:
                        string name = st.Name;

                        if (string.IsNullOrEmpty(name))
                            name = FileManager.Cliloc.GetString(1020000 + st.Graphic);
                        obj.AddOverhead(MessageType.Label, name, 3, 0, false);

                        break;

                    case Multi multi:
                        name = multi.Name;

                        if (string.IsNullOrEmpty(name))
                            name = FileManager.Cliloc.GetString(1020000 + multi.Graphic);
                        obj.AddOverhead(MessageType.Label, name, 3, 0, false);

                        break;

                    case AnimatedItemEffect effect when effect.Source is Entity:
                    case Entity _:

                        if (!_inqueue)
                        {
                            _inqueue = true;
                            _queuedObject = obj is AnimatedItemEffect ef ? (Entity) ef.Source : (Entity) obj;
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

        private void OnLeftMouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
        {
            if (!IsMouseOverViewport)
                return;

            IGameEntity obj = Game.SelectedObject.Object;

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
                case MessageInfo msg when msg.Parent.Parent is Entity entity:
                    e.Result = true;
                    GameActions.DoubleClick(entity);

                    break;
            }

            ClearDequeued();
        }


        // RIGHT
        private void OnRightMouseDown(object sender, EventArgs e)
        {
            if (!IsMouseOverViewport)
                return;

            if (!_rightMousePressed)
            {
                _rightMousePressed = true;
                _continueRunning = false;
            }
        }

        private void OnRightMouseUp(object sender, EventArgs e)
        {
            if (_rightMousePressed)
                _rightMousePressed = false;
        }

        private void OnRightMouseDoubleClick(object sender, MouseDoubleClickEventArgs e)
        {
            if (!IsMouseOverViewport)
                return;

            if (Engine.Profile.Current.EnablePathfind && !Pathfinder.AutoWalking)
            {
                if (Game.SelectedObject.Object is Land || GameObjectHelper.TryGetStaticData(Game.SelectedObject.Object as GameObject, out var itemdata) && itemdata.IsSurface)
                {
                    if (Game.SelectedObject.Object is GameObject obj && Pathfinder.WalkTo(obj.X, obj.Y, obj.Z, 0))
                    {
                        World.Player.AddOverhead(MessageType.Label, "Pathfinding!", 3, 0, false);
                        e.Result = true;
                    }
                }
            }
        }


        // MOUSE MOVING
        private void OnMouseMoving(object sender, EventArgs e)
        {
            if (!IsMouseOverViewport)
                return;

            if (Mouse.LButtonPressed && !IsHoldingItem)
            {
                Point offset = Mouse.LDroppedOffset;

                if (Math.Abs(offset.X) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS || Math.Abs(offset.Y) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
                {
                    GameObject obj = _dragginObject;

                    if (obj is AnimatedItemEffect eff && eff.Source is Item it)
                    {
                        if (PickupItemBegin(it, _dragOffset.X, _dragOffset.Y))
                        {
                            obj.Destroy();

                            return;
                        }
                    }

                    switch (obj)
                    {
                        case Mobile mobile:
                            GameActions.RequestMobileStatus(mobile);

                            Engine.UI.GetByLocalSerial<HealthBarGump>(mobile)?.Dispose();

                            if (mobile == World.Player)
                                StatusGumpBase.GetStatusGump()?.Dispose();

                            Rectangle rect = FileManager.Gumps.GetTexture(0x0804).Bounds;
                            HealthBarGump currentHealthBarGump;
                            Engine.UI.Add(currentHealthBarGump = new HealthBarGump(mobile) {X = Mouse.Position.X - (rect.Width >> 1), Y = Mouse.Position.Y - (rect.Height >> 1)});
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


        // MOUSE WHEEL
        private void OnMouseWheel(object sender, bool e)
        {
            if (!IsMouseOverViewport)
                return;

            if (!Engine.Profile.Current.EnableScaleZoom || !Keyboard.Ctrl)
                return;

            if (!e)
                ScalePos++;
            else
                ScalePos--;

            if (Engine.Profile.Current.SaveScaleAfterClose)
                Engine.Profile.Current.ScaleZoom = Scale;
        }


        // MOUSE DRAG
        private void OnMouseDragBegin(object sender, EventArgs e)
        {
            //if (!IsMouseOverViewport)
            //    return;

            //if (!IsHoldingItem)
            //{
            //    GameObject obj = _dragginObject;

            //    if (obj is AnimatedItemEffect eff && eff.Source is Item it)
            //        obj = it;

            //    switch (obj)
            //    {
            //        case Mobile mobile:
            //            GameActions.RequestMobileStatus(mobile);

            //            Engine.UI.GetByLocalSerial<HealthBarGump>(mobile)?.Dispose();

            //            if (mobile == World.Player)
            //                StatusGumpBase.GetStatusGump()?.Dispose();

            //            Rectangle rect = FileManager.Gumps.GetTexture(0x0804).Bounds;
            //            HealthBarGump currentHealthBarGump;
            //            Engine.UI.Add(currentHealthBarGump = new HealthBarGump(mobile) { X = Mouse.Position.X - (rect.Width >> 1), Y = Mouse.Position.Y - (rect.Height >> 1) });
            //            Engine.UI.AttemptDragControl(currentHealthBarGump, Mouse.Position, true);
            //            break;

            //        case Item item:
            //            PickupItemBegin(item, _dragOffset.X, _dragOffset.Y);
            //            break;
            //    }

            //    _dragginObject = null;
            //}
        }



        private void OnKeyDown(object sender, SDL.SDL_KeyboardEvent e)
        {
            _isShiftDown = Keyboard.IsModPressed(e.keysym.mod, SDL.SDL_Keymod.KMOD_SHIFT);
            _isCtrlDown = Keyboard.IsModPressed(e.keysym.mod, SDL.SDL_Keymod.KMOD_CTRL);

            if (TargetManager.IsTargeting && e.keysym.sym == SDL.SDL_Keycode.SDLK_ESCAPE && Keyboard.IsModPressed(e.keysym.mod, SDL.SDL_Keymod.KMOD_NONE))
                TargetManager.CancelTarget();

            if (Engine.Profile.Current.ActivateChatAfterEnter)
            {
                // Activate chat after `Enter` pressing, 
                // If chat active - ignores hotkeys from cuo
                if (Engine.Profile.Current.ActivateChatIgnoreHotkeys && Engine.Profile.Current.ActivateChatStatus)
                    return;
            }

            if (e.keysym.sym == SDL.SDL_Keycode.SDLK_TAB)
            {
                if (!World.Player.InWarMode && Engine.Profile.Current.HoldDownKeyTab)
                    GameActions.SetWarMode(true);
            }

            if (_keycodeDirection.TryGetValue(e.keysym.sym, out Direction dWalk))
            {
                if (!Engine.Profile.Current.ActivateChatStatus)
                    World.Player.Walk(dWalk, false);
                else
                {
                    if (Engine.UI.SystemChat?.textBox.Text.Length == 0)
                        World.Player.Walk(dWalk, false);
                }
            }

            if ((e.keysym.mod & SDL.SDL_Keymod.KMOD_NUM) != SDL.SDL_Keymod.KMOD_NUM)
            {
                if (_keycodeDirectionNum.TryGetValue(e.keysym.sym, out Direction dWalkN))
                    World.Player.Walk(dWalkN, false);
            }

            bool isshift = (e.keysym.mod & SDL.SDL_Keymod.KMOD_SHIFT) != SDL.SDL_Keymod.KMOD_NONE;
            bool isalt = (e.keysym.mod & SDL.SDL_Keymod.KMOD_ALT) != SDL.SDL_Keymod.KMOD_NONE;
            bool isctrl = (e.keysym.mod & SDL.SDL_Keymod.KMOD_CTRL) != SDL.SDL_Keymod.KMOD_NONE;

            _useObjectHandles = isshift && isctrl;

            Macro macro = Macros.FindMacro(e.keysym.sym, isalt, isctrl, isshift);

            if (macro != null)
            {
                Macros.SetMacroToExecute(macro.FirstNode);
                Macros.WaitForTargetTimer = 0;
                Macros.Update();
            }
        }

        private void OnKeyUp(object sender, SDL.SDL_KeyboardEvent e)
        {
            bool isshift = (e.keysym.mod & SDL.SDL_Keymod.KMOD_SHIFT) != SDL.SDL_Keymod.KMOD_NONE;
            bool isalt = (e.keysym.mod & SDL.SDL_Keymod.KMOD_ALT) != SDL.SDL_Keymod.KMOD_NONE;
            bool isctrl = (e.keysym.mod & SDL.SDL_Keymod.KMOD_CTRL) != SDL.SDL_Keymod.KMOD_NONE;

            if (Engine.Profile.Current.EnableScaleZoom && Engine.Profile.Current.RestoreScaleAfterUnpressCtrl && _isCtrlDown && !isctrl)
                Engine.SceneManager.GetScene<GameScene>().Scale = Engine.Profile.Current.RestoreScaleValue;

            _isShiftDown = isshift;
            _isCtrlDown = isctrl;

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