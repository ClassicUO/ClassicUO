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

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Network;
using ClassicUO.Renderer;
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

        private bool _followingMode;
        private Serial _followingTarget;
        private bool _inqueue;
        private bool _isCtrlDown;
        private bool _isSelectionActive;

        private bool _isShiftDown;
        private bool _isUpDown, _isDownDown, _isLeftDown, _isRightDown, _isMacroMoveDown, _isAuraActive;
        public Direction _numPadDirection;
        private Action _queuedAction;
        private Entity _queuedObject;
        private bool _wasShiftDown;

        private bool _requestedWarMode;
        private bool _rightMousePressed, _continueRunning, _ctrlAndShiftPressed, _arrowKeyPressed, _numPadKeyPressed;
        private (int, int) _selectionStart, _selectionEnd;
        private uint _holdMouse2secOverItemTime;
        private bool _isMouseLeftDown;

        public bool IsMouseOverUI => UIManager.IsMouseOverAControl && !(UIManager.MouseOverControl is WorldViewport);
        public bool IsMouseOverViewport => UIManager.MouseOverControl is WorldViewport;

        private Direction _lastBoatDirection;
        private bool _boatRun, _boatIsMoving;

        private void MoveCharacterByMouseInput()
        {
            if (World.InGame && !Pathfinder.AutoWalking)
            {
                int x = ProfileManager.Current.GameWindowPosition.X + (ProfileManager.Current.GameWindowSize.X >> 1);
                int y = ProfileManager.Current.GameWindowPosition.Y + (ProfileManager.Current.GameWindowSize.Y >> 1);

                Direction direction = (Direction)GameCursor.GetMouseDirection(x, y, Mouse.Position.X, Mouse.Position.Y, 1);
                double mouseRange = MathHelper.Hypotenuse(x - Mouse.Position.X, y - Mouse.Position.Y);

                Direction facing = direction;

                if (facing == Direction.North)
                    facing = (Direction)8;

                bool run = mouseRange >= 190;

                if (World.Player.IsDrivingBoat)
                {
                    if (!_boatIsMoving || _boatRun != run || _lastBoatDirection != facing - 1)
                    {
                        _boatRun = run;
                        _lastBoatDirection = facing - 1;
                        _boatIsMoving = true;

                        NetClient.Socket.Send(new PMultiBoatMoveRequest(World.Player, facing - 1, (byte)(run ? 2 : 1)));
                    }
                }
                else
                    World.Player.Walk(facing - 1, run);
            }
        }

        private void MoveCharacterByKeyboardInput(bool numPadMovement)
        {
            if (World.InGame && !Pathfinder.AutoWalking)
            {
                Direction direction = DirectionHelper.DirectionFromKeyboardArrows(_isUpDown, _isDownDown, _isLeftDown, _isRightDown);

                if (numPadMovement) direction = _numPadDirection;

                World.Player.Walk(direction, false);
            }
        }

        private bool CanDragSelectOnObject(GameObject obj)
        {
            return obj is null || obj is Static || obj is Land || obj is Multi || obj is Item tmpitem && tmpitem.IsLocked;
        }

        private void SetDragSelectionStartEnd(ref (int, int) start, ref (int, int) end)
        {
            if (start.Item1 > Mouse.Position.X)
            {
                end.Item1 = start.Item1;
                start.Item1 = Mouse.Position.X;
            }
            else
                end.Item1 = Mouse.Position.X;

            if (start.Item2 > Mouse.Position.Y)
            {
                _selectionEnd.Item2 = start.Item2;
                start.Item2 = Mouse.Position.Y;
            }
            else
                end.Item2 = Mouse.Position.Y;
        }

        private bool DragSelectModifierActive()
        {
            if (ProfileManager.Current.DragSelectModifierKey == 0)
                return true;

            if (ProfileManager.Current.DragSelectModifierKey == 1 && _isCtrlDown)
                return true;

            if (ProfileManager.Current.DragSelectModifierKey == 2 && _isShiftDown)
                return true;

            return false;
        }


        private void DoDragSelect()
        {
            SetDragSelectionStartEnd(ref _selectionStart, ref _selectionEnd);

            _rectangleObj.X = _selectionStart.Item1;
            _rectangleObj.Y = _selectionStart.Item2;
            _rectangleObj.Width = _selectionEnd.Item1 - _selectionStart.Item1;
            _rectangleObj.Height = _selectionEnd.Item2 - _selectionStart.Item2;

            int finalX = 100;
            int finalY = 100;

            bool useCHB = ProfileManager.Current.CustomBarsToggled;

            Rectangle rect = useCHB ? new Rectangle(0,0,  HealthBarGumpCustom.HPB_BAR_WIDTH, HealthBarGumpCustom.HPB_HEIGHT_MULTILINE) : FileManager.Gumps.GetTexture(0x0804).Bounds;

            foreach (Mobile mobile in World.Mobiles)
            {
                if (ProfileManager.Current.DragSelectHumanoidsOnly && !mobile.IsHuman)
                    continue;

                int x = ProfileManager.Current.GameWindowPosition.X + mobile.RealScreenPosition.X + (int)mobile.Offset.X + 22 + 5;
                int y = ProfileManager.Current.GameWindowPosition.Y + (mobile.RealScreenPosition.Y - (int)mobile.Offset.Z) + 22 + 5;

                x -= mobile.FrameInfo.X;
                y -= mobile.FrameInfo.Y;
                int w = mobile.FrameInfo.Width;
                int h = mobile.FrameInfo.Height;

                x = (int)(x * (1 / Scale));
                y = (int)(y * (1 / Scale));

                _rectanglePlayer.X = x;
                _rectanglePlayer.Y = y;
                _rectanglePlayer.Width = w;
                _rectanglePlayer.Height = h;

                if (_rectangleObj.Intersects(_rectanglePlayer))
                {
                    if (mobile != World.Player)
                    {
                        if (UIManager.GetGump<BaseHealthBarGump>(mobile)?.IsInitialized ?? false)
                        {
                            continue;
                        }

                        //Instead of destroying existing HP bar, continue if already opened.
                        GameActions.RequestMobileStatus(mobile);

                        BaseHealthBarGump hbgc;

                        if (useCHB)
                        {
                            hbgc = new HealthBarGumpCustom(mobile);
                        }
                        else
                        {
                            hbgc = new HealthBarGump(mobile);
                        }

                        // Need to initialize before setting X Y otherwise AnchorableGump.OnMove() is not called
                        // if OnMove() is not called, _prevX _prevY are not set, anchoring is unpredictable
                        // maybe should be fixed elsewhere
                        hbgc.Initialize();


                        if (finalY >= ProfileManager.Current.GameWindowPosition.Y + ProfileManager.Current.GameWindowSize.Y - 100)
                        {
                            finalY = 100;
                            finalX += rect.Width + 2;
                        }

                        if (finalX >= ProfileManager.Current.GameWindowPosition.X + ProfileManager.Current.GameWindowSize.X - 100)
                        {
                            finalX = 100;
                        }

                        hbgc.X = finalX;
                        hbgc.Y = finalY;


                        foreach (var bar in UIManager.Gumps
                                                .OfType<BaseHealthBarGump>()
                                                  //.OrderBy(s => mobile.NotorietyFlag)
                                                  //.OrderBy(s => s.ScreenCoordinateX) ///testing placement SYRUPZ SYRUPZ SYRUPZ
                                                  .OrderBy(s => s.ScreenCoordinateX)
                                                  .ThenBy(s => s.ScreenCoordinateY))
                        {
                            if (bar.Bounds.Intersects(hbgc.Bounds))
                            {
                                finalY = bar.Bounds.Bottom + 2;

                                if (finalY >= ProfileManager.Current.GameWindowPosition.Y + ProfileManager.Current.GameWindowSize.Y - 100)
                                {
                                    finalY = 100;
                                    finalX = bar.Bounds.Right + 2;
                                }

                                if (finalX >= ProfileManager.Current.GameWindowPosition.X + ProfileManager.Current.GameWindowSize.X - 100)
                                {
                                    finalX = 100;
                                }

                                hbgc.X = finalX;
                                hbgc.Y = finalY;
                            }
                        }


                        finalY += rect.Height + 2;


                        UIManager.Add(hbgc);

                        hbgc.SetInScreen();
                    }
                }
            }

            _isSelectionActive = false;
        }

        internal override void OnLeftMouseDown()
        {
            if (!IsMouseOverViewport)
                return;

            _dragginObject = SelectedObject.Object as GameObject;
            _dragOffset = Mouse.LDropPosition;

            if (ProfileManager.Current.EnableDragSelect && DragSelectModifierActive())
            {
                if (CanDragSelectOnObject(SelectedObject.Object as GameObject))
                {
                    _selectionStart = (Mouse.Position.X, Mouse.Position.Y);
                    _isSelectionActive = true;
                }
            }
            else
            {
                _isMouseLeftDown = true;
                _holdMouse2secOverItemTime = Time.Ticks;
            }
        }

        internal override void OnLeftMouseUp()
        {
            if (_isMouseLeftDown)
            {
                _isMouseLeftDown = false;
                _holdMouse2secOverItemTime = 0;
            }

            //  drag-select code comes first to allow selection finish on mouseup outside of viewport
            if (_selectionStart.Item1 == Mouse.Position.X && _selectionStart.Item2 == Mouse.Position.Y)
                _isSelectionActive = false;

            if (_isSelectionActive)
            {
                DoDragSelect();

                return;
            }

            if (!IsMouseOverViewport)
            {
                if (IsHoldingItem)
                {
                    UIManager.MouseOverControl?.InvokeMouseUp(Mouse.Position, MouseButton.Left);
                }
                return;
            }

            if (_rightMousePressed) _continueRunning = true;

            if (_dragginObject != null)
                _dragginObject = null;

            if (UIManager.IsDragging)
                return;

            if (IsHoldingItem)
            {
                if (SelectedObject.Object is GameObject obj && obj.Distance < Constants.DRAG_ITEMS_DISTANCE)
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
                                SelectedObject.Object = item;

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
                            Log.Warn( "Unhandled mouse inputs for GameObject type " + obj.GetType());

                            return;
                    }
                }
                else
                    CUOEnviroment.Client.Scene.Audio.PlaySound(0x0051);
            }
            else if (TargetManager.IsTargeting)
            {
                switch (TargetManager.TargetingState)
                {
                    case CursorTarget.Grab:
                    case CursorTarget.SetGrabBag:
                    case CursorTarget.Position:
                    case CursorTarget.Object:
                    case CursorTarget.MultiPlacement:

                        var obj = SelectedObject.Object;

                        if (obj != null)
                        {
                            TargetManager.TargetGameObject(obj);
                            Mouse.LastLeftButtonClickTime = 0;
                        }

                        break;

                    case CursorTarget.SetTargetClientSide:

                        if (SelectedObject.Object is GameObject obj2)
                        {
                            TargetManager.TargetGameObject(obj2);
                            Mouse.LastLeftButtonClickTime = 0;
                            UIManager.Add(new InfoGump(obj2));
                        }

                        break;

                    case CursorTarget.HueCommandTarget:

                        if (SelectedObject.Object is Entity selectedEntity)
                        {
                            CommandManager.OnHueTarget(selectedEntity);
                        }

                        break;

                    default:
                        Log.Warn( "Not implemented.");

                        break;
                }
            }
            else
            {
                GameObject obj = SelectedObject.Object as GameObject;

                switch (obj)
                {
                    case Static st:
                        string name = st.Name;
                        if (string.IsNullOrEmpty(name))
                            name = FileManager.Cliloc.GetString(1020000 + st.Graphic);
                        obj.AddMessage(MessageType.Label, name, 3, 1001, false);


                        if (obj.TextContainer != null && obj.TextContainer.MaxSize == 5)
                            obj.TextContainer.MaxSize = 1;
                        break;

                    case Multi multi:
                        name = multi.Name;

                        if (string.IsNullOrEmpty(name))
                            name = FileManager.Cliloc.GetString(1020000 + multi.Graphic);
                        obj.AddMessage(MessageType.Label, name, 3, 1001, false);

                        if (obj.TextContainer != null && obj.TextContainer.MaxSize == 5)
                            obj.TextContainer.MaxSize = 1;
                        break;

                    case Entity ent:

                        if (Keyboard.Alt && ent is Mobile)
                        {
                            World.Player.AddMessage(MessageType.Regular, "Now following.", 3, 1001, false);
                            _followingMode = true;
                            _followingTarget = ent;
                        }
                        else if (!_inqueue)
                        {
                            _inqueue = true;
                            _queuedObject = ent;
                            _dequeueAt = Mouse.MOUSE_DELAY_DOUBLE_CLICK;
                            _wasShiftDown = _isShiftDown;

                            _queuedAction = () =>
                            {
                                if (!World.ClientFeatures.TooltipsEnabled)
                                    GameActions.SingleClick(_queuedObject);
                                GameActions.OpenPopupMenu(_queuedObject, _wasShiftDown);
                            };
                        }

                        break;
                }
            }
        }


        internal override bool OnLeftMouseDoubleClick()
        {
            bool result = false;

            if (!IsMouseOverViewport)
                return result;

            BaseGameObject obj = SelectedObject.Object;

            switch (obj)
            {
                case Item item:
                    result = true;
                    GameActions.DoubleClick(item);

                    break;

                case Mobile mob:
                    result = true;

                    if (World.Player.InWarMode && World.Player != mob)
                        GameActions.Attack(mob);
                    else
                        GameActions.DoubleClick(mob);

                    break;

                case TextOverhead msg when msg.Owner is Entity entity:
                    result = true;
                    GameActions.DoubleClick(entity);

                    break;
            }

            ClearDequeued();

            return result;
        }


        internal override void OnRightMouseDown()
        {
            if (!IsMouseOverViewport)
                return;

            _rightMousePressed = true;
            _continueRunning = false;
            StopFollowing();
        }


        private void StopFollowing()
        {
            if (_followingMode)
            {
                _followingMode = false;
                _followingTarget = Serial.INVALID;
                Pathfinder.StopAutoWalk();
                World.Player.AddMessage(MessageType.Regular, "Stopped following.", 3, 1001, false);
            }
        }


        internal override void OnRightMouseUp()
        {
            _rightMousePressed = false;


            if (_boatIsMoving)
            {
                _boatIsMoving = false;
                NetClient.Socket.Send(new PMultiBoatMoveRequest(World.Player, World.Player.Direction, 0x00));
            }
        }


        internal override bool OnRightMouseDoubleClick()
        {
            if (!IsMouseOverViewport)
                return false;

            if (ProfileManager.Current.EnablePathfind && !Pathfinder.AutoWalking)
            {
                if (ProfileManager.Current.UseShiftToPathfind && !_isShiftDown)
                    return false;

                if (SelectedObject.Object is GameObject obj)
                {
                    if (obj is Static || obj is Multi || obj is Item)
                    {
                        ref readonly var itemdata = ref FileManager.TileData.StaticData[obj.Graphic];

                        if (itemdata.IsSurface && Pathfinder.WalkTo(obj.X, obj.Y, obj.Z, 0))
                        {
                            World.Player.AddMessage(MessageType.Label, "Pathfinding!", 3, 1001, false);
                            return true;
                        }
                    }
                    else if (obj is Land && Pathfinder.WalkTo(obj.X, obj.Y, obj.Z, 0))
                    {
                        World.Player.AddMessage(MessageType.Label, "Pathfinding!", 3, 1001, false);
                        return true;
                    }
                }

                //if (SelectedObject.Object is Land || GameObjectHelper.TryGetStaticData(SelectedObject.Object as GameObject, out var itemdata) && itemdata.IsSurface)
                //{
                //    if (SelectedObject.Object is GameObject obj && Pathfinder.WalkTo(obj.X, obj.Y, obj.Z, 0))
                //    {

                //    }
                //}
            }

            return false;
        }



        internal override void OnMouseWheel(bool up)
        {
            if (!IsMouseOverViewport)
                return;

            if (!ProfileManager.Current.EnableScaleZoom || !Keyboard.Ctrl)
                return;

            if (!up)
                ScalePos++;
            else
                ScalePos--;

            if (ProfileManager.Current.SaveScaleAfterClose)
                ProfileManager.Current.ScaleZoom = Scale;
        }


        internal override void OnMouseDragging()
        {
            if (!IsMouseOverViewport)
                return;

            if (Mouse.LButtonPressed && !IsHoldingItem)
            {
                Point offset = Mouse.LDroppedOffset;

                if (Math.Abs(offset.X) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS || Math.Abs(offset.Y) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS)
                {
                    GameObject obj = ProfileManager.Current.SallosEasyGrab && SelectedObject.LastObject is GameObject o ? o : _dragginObject;


                    switch (obj)
                    {
                        case Mobile _:
                            mobile:
                            Entity entity = obj as Entity;
                            if (entity == null)
                                break;

                            GameActions.RequestMobileStatus(entity);
                            var customgump = UIManager.GetGump<BaseHealthBarGump>(entity);
                            if (customgump != null)
                            {
                                if (!customgump.IsInitialized)
                                    break;
                                customgump.Dispose();
                            }

                            if (entity == World.Player)
                                StatusGumpBase.GetStatusGump()?.Dispose();

                            if (ProfileManager.Current.CustomBarsToggled)
                            {
                                Rectangle rect = new Rectangle(0, 0, HealthBarGumpCustom.HPB_WIDTH, HealthBarGumpCustom.HPB_HEIGHT_SINGLELINE);
                                UIManager.Add(customgump = new HealthBarGumpCustom(entity) { X = Mouse.Position.X - (rect.Width >> 1), Y = Mouse.Position.Y - (rect.Height >> 1) });
                            }
                            else
                            {
                                Rectangle rect = FileManager.Gumps.GetTexture(0x0804).Bounds;
                                UIManager.Add(customgump = new HealthBarGump(entity) { X = Mouse.Position.X - (rect.Width >> 1), Y = Mouse.Position.Y - (rect.Height >> 1) });
                            }
                            UIManager.AttemptDragControl(customgump, Mouse.Position, true);

                            break;

                        case Item item /*when !item.IsCorpse*/:

                            if (item.IsDamageable)
                                goto mobile;

                            PickupItemBegin(item, item.Bounds.Width >> 1, item.Bounds.Height >> 1);

                            break;
                    }

                    _dragginObject = null;
                }
            }
        }



        internal override void OnKeyDown(SDL.SDL_KeyboardEvent e)
        {
            bool isshift = (e.keysym.mod & SDL.SDL_Keymod.KMOD_SHIFT) != SDL.SDL_Keymod.KMOD_NONE;
            bool isalt = (e.keysym.mod & SDL.SDL_Keymod.KMOD_ALT) != SDL.SDL_Keymod.KMOD_NONE;
            bool isctrl = (e.keysym.mod & SDL.SDL_Keymod.KMOD_CTRL) != SDL.SDL_Keymod.KMOD_NONE;

            Macro macro = Macros.FindMacro(e.keysym.sym, isalt, isctrl, isshift);

            _isShiftDown = Keyboard.IsModPressed(e.keysym.mod, SDL.SDL_Keymod.KMOD_SHIFT);
            _isCtrlDown = Keyboard.IsModPressed(e.keysym.mod, SDL.SDL_Keymod.KMOD_CTRL);

            _isMacroMoveDown = _isMacroMoveDown || macro != null && macro.FirstNode.Code == MacroType.MovePlayer;
            _isAuraActive = _isAuraActive || macro != null && macro.FirstNode.Code == MacroType.Aura;
            _isUpDown = _isUpDown || e.keysym.sym == SDL.SDL_Keycode.SDLK_UP || macro != null && macro.FirstNode.SubCode == MacroSubType.Top;
            _isDownDown = _isDownDown || e.keysym.sym == SDL.SDL_Keycode.SDLK_DOWN || macro != null && macro.FirstNode.SubCode == MacroSubType.Down;
            _isLeftDown = _isLeftDown || e.keysym.sym == SDL.SDL_Keycode.SDLK_LEFT || macro != null && macro.FirstNode.SubCode == MacroSubType.Left;
            _isRightDown = _isRightDown || e.keysym.sym == SDL.SDL_Keycode.SDLK_RIGHT || macro != null && macro.FirstNode.SubCode == MacroSubType.Right;

            if (_isUpDown || _isDownDown || _isLeftDown || _isRightDown)
            {
                if (UIManager.SystemChat?.IsActive == false || UIManager.SystemChat?.textBox.Text.Length == 0)
                    _arrowKeyPressed = true;
            }

            if (_isAuraActive && !AuraManager.IsEnabled)
                AuraManager.ToggleVisibility();

            if (TargetManager.IsTargeting && e.keysym.sym == SDL.SDL_Keycode.SDLK_ESCAPE && Keyboard.IsModPressed(e.keysym.mod, SDL.SDL_Keymod.KMOD_NONE))
                TargetManager.CancelTarget();

            if (!UIManager.IsKeyboardFocusAllowHotkeys)
                return;

            if (e.keysym.sym == SDL.SDL_Keycode.SDLK_TAB && !ProfileManager.Current.DisableTabBtn)
            {
                if (ProfileManager.Current.HoldDownKeyTab)
                {
                    if (!_requestedWarMode)
                    {
                        _requestedWarMode = true;
                        if (!World.Player.InWarMode)
                            NetClient.Socket.Send(new PChangeWarMode(true));
                    }
                }
            }

            if ((e.keysym.mod & SDL.SDL_Keymod.KMOD_NUM) != SDL.SDL_Keymod.KMOD_NUM)
            {
                if (_keycodeDirectionNum.TryGetValue(e.keysym.sym, out Direction dWalkN))
                {
                    _numPadKeyPressed = true;
                    _numPadDirection = dWalkN;
                }
            }

            _ctrlAndShiftPressed = isshift && isctrl;

            if (macro != null && e.keysym.sym != SDL.SDL_Keycode.SDLK_UNKNOWN)
            {
                Macros.SetMacroToExecute(macro.FirstNode);
                Macros.WaitForTargetTimer = 0;
                Macros.Update();
            }
        }




        internal override void OnKeyUp(SDL.SDL_KeyboardEvent e)
        {
            bool isshift = (e.keysym.mod & SDL.SDL_Keymod.KMOD_SHIFT) != SDL.SDL_Keymod.KMOD_NONE;
            bool isalt = (e.keysym.mod & SDL.SDL_Keymod.KMOD_ALT) != SDL.SDL_Keymod.KMOD_NONE;
            bool isctrl = (e.keysym.mod & SDL.SDL_Keymod.KMOD_CTRL) != SDL.SDL_Keymod.KMOD_NONE;

            if (ProfileManager.Current.EnableScaleZoom && ProfileManager.Current.RestoreScaleAfterUnpressCtrl && _isCtrlDown && !isctrl)
                Scale = ProfileManager.Current.RestoreScaleValue;

            _isShiftDown = isshift;
            _isCtrlDown = isctrl;

            switch (e.keysym.sym)
            {
                case SDL.SDL_Keycode.SDLK_UP:
                    _isUpDown = false;

                    break;

                case SDL.SDL_Keycode.SDLK_DOWN:
                    _isDownDown = false;

                    break;

                case SDL.SDL_Keycode.SDLK_LEFT:
                    _isLeftDown = false;

                    break;

                case SDL.SDL_Keycode.SDLK_RIGHT:
                    _isRightDown = false;

                    break;
            }

            if (_isAuraActive)
            {
                _isAuraActive = false;
                AuraManager.ToggleVisibility();
            }

            if (_isMacroMoveDown)
            {
                Macro macro = Macros.FindMacro(e.keysym.sym, isalt, isctrl, isshift);

                if (macro != null)
                {
                    switch (macro.FirstNode.SubCode)
                    {
                        case MacroSubType.Top:
                            _isUpDown = false;

                            break;

                        case MacroSubType.Down:
                            _isDownDown = false;

                            break;

                        case MacroSubType.Left:
                            _isLeftDown = false;

                            break;

                        case MacroSubType.Right:
                            _isRightDown = false;

                            break;
                    }
                }
            }

            if (!(_isUpDown || _isDownDown || _isLeftDown || _isRightDown)) _isMacroMoveDown = _arrowKeyPressed = false;

            if ((e.keysym.mod & SDL.SDL_Keymod.KMOD_NUM) != SDL.SDL_Keymod.KMOD_NUM) _numPadKeyPressed = false;

            _ctrlAndShiftPressed = isctrl && isshift;

            if (e.keysym.sym == SDL.SDL_Keycode.SDLK_TAB && !ProfileManager.Current.DisableTabBtn)
            {
                if (ProfileManager.Current.HoldDownKeyTab)
                {
                    if (_requestedWarMode)
                    {
                        NetClient.Socket.Send(new PChangeWarMode(false));
                        _requestedWarMode = false;
                    }
                }
                else
                    GameActions.ChangeWarMode();
            }
            else if (e.keysym.sym == SDL.SDL_Keycode.SDLK_ESCAPE && Pathfinder.AutoWalking) Pathfinder.StopAutoWalk();
        }
    }
}
