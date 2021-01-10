#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;
using SDL2;
using MathHelper = ClassicUO.Utility.MathHelper;

namespace ClassicUO.Game.Scenes
{
    internal partial class GameScene
    {
        private bool _boatRun, _boatIsMoving;
        private readonly bool[] _flags = new bool[5];
        private bool _followingMode;
        private uint _followingTarget;
        private uint _holdMouse2secOverItemTime;
        private bool _isMouseLeftDown;
        private bool _isSelectionActive;
        private Direction _lastBoatDirection;
        private bool _requestedWarMode;
        private bool _rightMousePressed, _continueRunning;
        private Point _selectionStart, _selectionEnd;


        private bool MoveCharacterByMouseInput()
        {
            if ((_rightMousePressed || _continueRunning) && World.InGame) // && !Pathfinder.AutoWalking)
            {
                if (Pathfinder.AutoWalking)
                {
                    Pathfinder.StopAutoWalk();
                }

                int x = ProfileManager.CurrentProfile.GameWindowPosition.X + (ProfileManager.CurrentProfile.GameWindowSize.X >> 1);
                int y = ProfileManager.CurrentProfile.GameWindowPosition.Y + (ProfileManager.CurrentProfile.GameWindowSize.Y >> 1);

                Direction direction = (Direction) GameCursor.GetMouseDirection
                (
                    x,
                    y,
                    Mouse.Position.X,
                    Mouse.Position.Y,
                    1
                );

                double mouseRange = MathHelper.Hypotenuse(x - Mouse.Position.X, y - Mouse.Position.Y);

                Direction facing = direction;

                if (facing == Direction.North)
                {
                    facing = (Direction) 8;
                }

                bool run = mouseRange >= 190;

                if (World.Player.IsDrivingBoat)
                {
                    if (!_boatIsMoving || _boatRun != run || _lastBoatDirection != facing - 1)
                    {
                        _boatRun = run;
                        _lastBoatDirection = facing - 1;
                        _boatIsMoving = true;

                        BoatMovingManager.MoveRequest(facing - 1, (byte) (run ? 2 : 1));
                    }
                }
                else
                {
                    World.Player.Walk(facing - 1, run);
                }

                return true;
            }

            return false;
        }

        private bool CanDragSelectOnObject(GameObject obj)
        {
            return obj is null || obj is Static || obj is Land || obj is Multi || obj is Item tmpitem && tmpitem.IsLocked;
        }


        private bool DragSelectModifierActive()
        {
            // src: https://github.com/andreakarasho/ClassicUO/issues/621
            // drag-select should be disabled when using nameplates
            if (Keyboard.Ctrl && Keyboard.Shift)
            {
                return false;
            }

            if (ProfileManager.CurrentProfile.DragSelectModifierKey == 0)
            {
                return true;
            }

            if (ProfileManager.CurrentProfile.DragSelectModifierKey == 1 && Keyboard.Ctrl)
            {
                return true;
            }

            if (ProfileManager.CurrentProfile.DragSelectModifierKey == 2 && Keyboard.Shift)
            {
                return true;
            }

            return false;
        }


        private void DoDragSelect()
        {
            if (_selectionStart.X > Mouse.Position.X)
            {
                _selectionEnd.X = _selectionStart.X;
                _selectionStart.X = Mouse.Position.X;
            }
            else
            {
                _selectionEnd.X = Mouse.Position.X;
            }

            if (_selectionStart.Y > Mouse.Position.Y)
            {
                _selectionEnd.Y = _selectionStart.Y;
                _selectionStart.Y = Mouse.Position.Y;
            }
            else
            {
                _selectionEnd.Y = Mouse.Position.Y;
            }


            _rectangleObj.X = _selectionStart.X - Camera.Bounds.X;
            _rectangleObj.Y = _selectionStart.Y - Camera.Bounds.Y;
            _rectangleObj.Width = _selectionEnd.X - Camera.Bounds.X - _rectangleObj.X;
            _rectangleObj.Height = _selectionEnd.Y - Camera.Bounds.Y - _rectangleObj.Y;

            int finalX = 100;
            int finalY = 100;

            bool useCHB = ProfileManager.CurrentProfile.CustomBarsToggled;

            Rectangle rect = useCHB ? new Rectangle(0, 0, HealthBarGumpCustom.HPB_BAR_WIDTH, HealthBarGumpCustom.HPB_HEIGHT_MULTILINE) : GumpsLoader.Instance.GetTexture(0x0804).Bounds;

            foreach (Mobile mobile in World.Mobiles)
            {
                if (ProfileManager.CurrentProfile.DragSelectHumanoidsOnly && !mobile.IsHuman)
                {
                    continue;
                }

                Point p = mobile.RealScreenPosition;

                p.X += (int) mobile.Offset.X + 22 + 5;
                p.Y += (int) (mobile.Offset.Y - mobile.Offset.Z) + 22 + 5;
                p.X -= mobile.FrameInfo.X;
                p.Y -= mobile.FrameInfo.Y;

                Point size = new Point(p.X + mobile.FrameInfo.Width, p.Y + mobile.FrameInfo.Height);

                p = Camera.WorldToScreen(p);
                _rectanglePlayer.X = p.X;
                _rectanglePlayer.Y = p.Y;


                size = Camera.WorldToScreen(size);
                _rectanglePlayer.Width = size.X - p.X;
                _rectanglePlayer.Height = size.Y - p.Y;

                if (_rectangleObj.Intersects(_rectanglePlayer))
                {
                    if (mobile != World.Player)
                    {
                        if (UIManager.GetGump<BaseHealthBarGump>(mobile) != null)
                        {
                            continue;
                        }

                        BaseHealthBarGump hbgc;

                        if (useCHB)
                        {
                            hbgc = new HealthBarGumpCustom(mobile);
                        }
                        else
                        {
                            hbgc = new HealthBarGump(mobile);
                        }

                        if (finalY >= ProfileManager.CurrentProfile.GameWindowPosition.Y + ProfileManager.CurrentProfile.GameWindowSize.Y - 100)
                        {
                            finalY = 100;
                            finalX += rect.Width + 2;
                        }

                        if (finalX >= ProfileManager.CurrentProfile.GameWindowPosition.X + ProfileManager.CurrentProfile.GameWindowSize.X - 100)
                        {
                            finalX = 100;
                        }

                        hbgc.X = finalX;
                        hbgc.Y = finalY;


                        foreach (BaseHealthBarGump bar in UIManager.Gumps.OfType<BaseHealthBarGump>()
                                                                   //.OrderBy(s => mobile.NotorietyFlag)
                                                                   //.OrderBy(s => s.ScreenCoordinateX) ///testing placement SYRUPZ SYRUPZ SYRUPZ
                                                                   .OrderBy(s => s.ScreenCoordinateX)
                                                                   .ThenBy(s => s.ScreenCoordinateY))
                        {
                            if (bar.Bounds.Intersects(hbgc.Bounds))
                            {
                                finalY = bar.Bounds.Bottom + 2;

                                if (finalY >= ProfileManager.CurrentProfile.GameWindowPosition.Y + ProfileManager.CurrentProfile.GameWindowSize.Y - 100)
                                {
                                    finalY = 100;
                                    finalX = bar.Bounds.Right + 2;
                                }

                                if (finalX >= ProfileManager.CurrentProfile.GameWindowPosition.X + ProfileManager.CurrentProfile.GameWindowSize.X - 100)
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

        internal override bool OnMouseDown(MouseButtonType button)
        {
            switch (button)
            {
                case MouseButtonType.Left: return OnLeftMouseDown();
                case MouseButtonType.Right: return OnRightMouseDown();
            }

            return false;
        }

        internal override bool OnMouseUp(MouseButtonType button)
        {
            switch (button)
            {
                case MouseButtonType.Left: return OnLeftMouseUp();
                case MouseButtonType.Right: return OnRightMouseUp();
            }

            return false;
        }

        internal override bool OnMouseDoubleClick(MouseButtonType button)
        {
            switch (button)
            {
                case MouseButtonType.Left: return OnLeftMouseDoubleClick();
                case MouseButtonType.Right: return OnRightMouseDoubleClick();
            }

            return false;
        }

        private bool OnLeftMouseDown()
        {
            if (UIManager.PopupMenu != null && !UIManager.PopupMenu.Bounds.Contains(Mouse.Position.X, Mouse.Position.Y))
            {
                UIManager.ShowGamePopup(null);
            }

            if (!UIManager.IsMouseOverWorld)
            {
                return false;
            }

            if (World.CustomHouseManager != null)
            {
                _isMouseLeftDown = true;

                if (TargetManager.IsTargeting && TargetManager.TargetingState == CursorTarget.MultiPlacement && (World.CustomHouseManager.SelectedGraphic != 0 || World.CustomHouseManager.Erasing || World.CustomHouseManager.SeekTile) && SelectedObject.LastObject is GameObject obj)
                {
                    World.CustomHouseManager.OnTargetWorld(obj);
                    _lastSelectedMultiPositionInHouseCustomization.X = obj.X;
                    _lastSelectedMultiPositionInHouseCustomization.Y = obj.Y;
                }
            }
            else
            {
                SelectedObject.LastLeftDownObject = SelectedObject.Object;

                if (ProfileManager.CurrentProfile.EnableDragSelect && DragSelectModifierActive())
                {
                    if (CanDragSelectOnObject(SelectedObject.Object as GameObject))
                    {
                        _selectionStart = Mouse.Position;
                        _isSelectionActive = true;
                    }
                }
                else
                {
                    _isMouseLeftDown = true;
                    _holdMouse2secOverItemTime = Time.Ticks;
                }
            }

            return true;
        }

        private bool OnLeftMouseUp()
        {
            if (UIManager.PopupMenu != null && !UIManager.PopupMenu.Bounds.Contains(Mouse.Position.X, Mouse.Position.Y))
            {
                UIManager.ShowGamePopup(null);
            }

            if (_isMouseLeftDown)
            {
                _isMouseLeftDown = false;
                _holdMouse2secOverItemTime = 0;
            }

            //  drag-select code comes first to allow selection finish on mouseup outside of viewport
            if (_selectionStart.X == Mouse.Position.X && _selectionStart.Y == Mouse.Position.Y)
            {
                _isSelectionActive = false;
            }

            if (_isSelectionActive)
            {
                DoDragSelect();

                return true;
            }

            if (!UIManager.IsMouseOverWorld)
            {
                return false;
            }

            if (UIManager.SystemChat != null && !UIManager.SystemChat.IsFocused)
            {
                UIManager.KeyboardFocusControl = null;
                UIManager.SystemChat.SetFocus();
            }

            if (!ProfileManager.CurrentProfile.DisableAutoMove && _rightMousePressed)
            {
                _continueRunning = true;
            }

            BaseGameObject lastObj = SelectedObject.LastLeftDownObject;
            SelectedObject.LastLeftDownObject = null;

            if (UIManager.IsDragging)
            {
                return false;
            }

            if (ItemHold.Enabled && !ItemHold.IsFixedPosition)
            {
                uint drop_container = 0xFFFF_FFFF;
                bool can_drop = false;
                ushort dropX = 0;
                ushort dropY = 0;
                sbyte dropZ = 0;

                GameObject gobj = SelectedObject.LastObject as GameObject;

                if (gobj is Entity obj)
                {
                    can_drop = obj.Distance <= Constants.DRAG_ITEMS_DISTANCE;

                    if (can_drop)
                    {
                        if (obj is Item it && it.ItemData.IsContainer || obj is Mobile)
                        {
                            dropX = 0xFFFF;
                            dropY = 0xFFFF;
                            dropZ = 0;
                            drop_container = obj.Serial;
                        }
                        else if (obj is Item it2 && (it2.ItemData.IsSurface || it2.ItemData.IsStackable && it2.Graphic == ItemHold.Graphic))
                        {
                            dropX = obj.X;
                            dropY = obj.Y;
                            dropZ = obj.Z;

                            if (it2.ItemData.IsSurface)
                            {
                                dropZ += (sbyte) (it2.ItemData.Height == 0xFF ? 0 : it2.ItemData.Height);
                            }
                            else
                            {
                                drop_container = obj.Serial;
                            }
                        }
                    }
                    else
                    {
                        Client.Game.Scene.Audio.PlaySound(0x0051);
                    }
                }
                else if (gobj is Land || gobj is Static || gobj is Multi)
                {
                    can_drop = gobj.Distance <= Constants.DRAG_ITEMS_DISTANCE;

                    if (can_drop)
                    {
                        dropX = gobj.X;
                        dropY = gobj.Y;
                        dropZ = gobj.Z;
                    }
                    else
                    {
                        Client.Game.Scene.Audio.PlaySound(0x0051);
                    }
                }


                if (can_drop)
                {
                    if (drop_container == 0xFFFF_FFFF && dropX == 0 && dropY == 0)
                    {
                        can_drop = false;
                    }

                    if (can_drop)
                    {
                        GameActions.DropItem
                        (
                            ItemHold.Serial,
                            dropX,
                            dropY,
                            dropZ,
                            drop_container
                        );
                    }
                }
            }
            else if (TargetManager.IsTargeting)
            {
                switch (TargetManager.TargetingState)
                {
                    case CursorTarget.Grab:
                    case CursorTarget.SetGrabBag:
                    case CursorTarget.Position:
                    case CursorTarget.Object:
                    case CursorTarget.MultiPlacement when World.CustomHouseManager == null:
                    {
                        BaseGameObject obj = lastObj;

                        if (obj is TextObject ov)
                        {
                            obj = ov.Owner;
                        }

                        switch (obj)
                        {
                            case Entity ent:
                                TargetManager.Target(ent.Serial);

                                break;

                            case Land land:
                                TargetManager.Target
                                (
                                    0,
                                    land.X,
                                    land.Y,
                                    land.Z,
                                    land.TileData.IsWet
                                );

                                break;

                            case GameObject o:
                                TargetManager.Target(o.Graphic, o.X, o.Y, o.Z);

                                break;
                        }
                    }

                        Mouse.LastLeftButtonClickTime = 0;

                        break;

                    case CursorTarget.SetTargetClientSide:
                    {
                        BaseGameObject obj = lastObj;

                        if (obj is TextObject ov)
                        {
                            obj = ov.Owner;
                        }
                        else if (obj is GameEffect eff && eff.Source != null)
                        {
                            obj = eff.Source;
                        }

                        switch (obj)
                        {
                            case Entity ent:
                                TargetManager.Target(ent.Serial);
                                UIManager.Add(new InspectorGump(ent));

                                break;

                            case Land land:
                                TargetManager.Target(0, land.X, land.Y, land.Z);
                                UIManager.Add(new InspectorGump(land));

                                break;

                            case GameObject o:
                                TargetManager.Target(o.Graphic, o.X, o.Y, o.Z);
                                UIManager.Add(new InspectorGump(o));

                                break;
                        }

                        Mouse.LastLeftButtonClickTime = 0;
                    }

                        break;

                    case CursorTarget.HueCommandTarget:

                        if (SelectedObject.Object is Entity selectedEntity)
                        {
                            CommandManager.OnHueTarget(selectedEntity);
                        }

                        break;
                }
            }
            else
            {
                GameObject obj = lastObj as GameObject;

                switch (obj)
                {
                    case Static st:
                        string name = st.Name;

                        if (string.IsNullOrEmpty(name))
                        {
                            name = ClilocLoader.Instance.GetString(1020000 + st.Graphic, st.ItemData.Name);
                        }

                        MessageManager.HandleMessage
                        (
                            null,
                            name,
                            string.Empty,
                            0x03b2,
                            MessageType.Label,
                            3,
                            TextType.CLIENT
                        );

                        obj.AddMessage
                        (
                            MessageType.Label,
                            name,
                            3,
                            0x03b2,
                            false,
                            TextType.CLIENT
                        );


                        if (obj.TextContainer != null && obj.TextContainer.MaxSize != 1)
                        {
                            obj.TextContainer.MaxSize = 1;
                        }

                        break;

                    case Multi multi:
                        name = multi.Name;

                        if (string.IsNullOrEmpty(name))
                        {
                            name = ClilocLoader.Instance.GetString(1020000 + multi.Graphic, multi.ItemData.Name);
                        }

                        MessageManager.HandleMessage
                        (
                            null,
                            name,
                            string.Empty,
                            0x03b2,
                            MessageType.Label,
                            3,
                            TextType.CLIENT
                        );

                        obj.AddMessage
                        (
                            MessageType.Label,
                            name,
                            3,
                            0x03b2,
                            false,
                            TextType.CLIENT
                        );

                        if (obj.TextContainer != null && obj.TextContainer.MaxSize == 5)
                        {
                            obj.TextContainer.MaxSize = 1;
                        }

                        break;

                    case Entity ent:

                        if (Keyboard.Alt && ent is Mobile)
                        {
                            MessageManager.HandleMessage
                            (
                                World.Player,
                                ResGeneral.NowFollowing,
                                string.Empty,
                                0,
                                MessageType.Regular,
                                3,
                                TextType.CLIENT
                            );

                            _followingMode = true;
                            _followingTarget = ent;
                        }
                        else if (!DelayedObjectClickManager.IsEnabled)
                        {
                            DelayedObjectClickManager.Set(ent.Serial, Mouse.Position.X, Mouse.Position.Y, Time.Ticks + Mouse.MOUSE_DELAY_DOUBLE_CLICK);
                        }

                        break;
                }
            }

            return true;
        }

        private bool OnLeftMouseDoubleClick()
        {
            bool result = false;

            if (!UIManager.IsMouseOverWorld)
            {
                result = DelayedObjectClickManager.IsEnabled;

                if (result)
                {
                    DelayedObjectClickManager.Clear();

                    return false;
                }

                return false;
            }

            BaseGameObject obj = SelectedObject.LastObject;

            switch (obj)
            {
                case Item item:
                    result = true;

                    if (!GameActions.OpenCorpse(item))
                    {
                        GameActions.DoubleClick(item);
                    }

                    break;

                case Mobile mob:
                    result = true;

                    if (World.Player.InWarMode && World.Player != mob)
                    {
                        GameActions.Attack(mob);
                    }
                    else
                    {
                        GameActions.DoubleClick(mob);
                    }

                    break;

                case TextObject msg when msg.Owner is Entity entity:
                    result = true;
                    GameActions.DoubleClick(entity);

                    break;

                default:
                    World.LastObject = 0;

                    break;
            }

            if (result)
            {
                DelayedObjectClickManager.Clear();
            }

            return result;
        }


        private bool OnRightMouseDown()
        {
            if (UIManager.PopupMenu != null && !UIManager.PopupMenu.Bounds.Contains(Mouse.Position.X, Mouse.Position.Y))
            {
                UIManager.ShowGamePopup(null);
            }

            if (!UIManager.IsMouseOverWorld)
            {
                return false;
            }

            _rightMousePressed = true;
            _continueRunning = false;
            StopFollowing();

            return true;
        }


        private bool OnRightMouseUp()
        {
            if (UIManager.PopupMenu != null && !UIManager.PopupMenu.Bounds.Contains(Mouse.Position.X, Mouse.Position.Y))
            {
                UIManager.ShowGamePopup(null);
            }

            _rightMousePressed = false;

            if (_boatIsMoving)
            {
                _boatIsMoving = false;
                BoatMovingManager.MoveRequest(World.Player.Direction, 0);
            }

            return UIManager.IsMouseOverWorld;
        }


        private bool OnRightMouseDoubleClick()
        {
            if (!UIManager.IsMouseOverWorld)
            {
                return false;
            }

            if (ProfileManager.CurrentProfile.EnablePathfind && !Pathfinder.AutoWalking)
            {
                if (ProfileManager.CurrentProfile.UseShiftToPathfind && !Keyboard.Shift)
                {
                    return false;
                }

                if (SelectedObject.Object is GameObject obj)
                {
                    if (obj is Static || obj is Multi || obj is Item)
                    {
                        ref StaticTiles itemdata = ref TileDataLoader.Instance.StaticData[obj.Graphic];

                        if (itemdata.IsSurface && Pathfinder.WalkTo(obj.X, obj.Y, obj.Z, 0))
                        {
                            World.Player.AddMessage
                            (
                                MessageType.Label,
                                ResGeneral.Pathfinding,
                                3,
                                0,
                                false,
                                TextType.CLIENT
                            );

                            return true;
                        }
                    }
                    else if (obj is Land && Pathfinder.WalkTo(obj.X, obj.Y, obj.Z, 0))
                    {
                        World.Player.AddMessage
                        (
                            MessageType.Label,
                            ResGeneral.Pathfinding,
                            3,
                            0,
                            false,
                            TextType.CLIENT
                        );

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


        internal override bool OnMouseWheel(bool up)
        {
            if (Keyboard.Ctrl && ItemHold.Enabled)
            {
                if (!up && !ItemHold.IsFixedPosition)
                {
                    ItemHold.IsFixedPosition = true;
                    ItemHold.IgnoreFixedPosition = true;
                    ItemHold.FixedX = Mouse.Position.X;
                    ItemHold.FixedY = Mouse.Position.Y;
                }

                if (ItemHold.IgnoreFixedPosition)
                {
                    return true;
                }
            }

            if (!UIManager.IsMouseOverWorld)
            {
                return false;
            }

            if (Keyboard.Ctrl && ProfileManager.CurrentProfile.EnableMousewheelScaleZoom)
            {
                Camera.ZoomIndex += up ? -1 : 1;

                return true;
            }

            return false;
        }


        internal override bool OnMouseDragging()
        {
            if (!UIManager.IsMouseOverWorld)
            {
                return false;
            }

            bool ok = true;

            if (Mouse.LButtonPressed && !ItemHold.Enabled)
            {
                Point offset = Mouse.LDragOffset;

                if (!UIManager.GameCursor.IsDraggingCursorForced && // don't trigger "sallos ez grab" when dragging wmap or skill
                    !_isSelectionActive &&                          // and ofc when selection is enabled
                    (Math.Abs(offset.X) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS || Math.Abs(offset.Y) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS))
                {
                    Entity obj;

                    if (ProfileManager.CurrentProfile.SallosEasyGrab && SelectedObject.LastObject is Entity ent && SelectedObject.LastLeftDownObject == null)
                    {
                        obj = ent;
                    }
                    else
                    {
                        obj = SelectedObject.LastLeftDownObject as Entity;
                    }

                    if (obj != null)
                    {
                        if (SerialHelper.IsMobile(obj.Serial) || obj is Item it && it.IsDamageable)
                        {
                            BaseHealthBarGump customgump = UIManager.GetGump<BaseHealthBarGump>(obj);
                            customgump?.Dispose();

                            if (obj == World.Player)
                            {
                                StatusGumpBase.GetStatusGump()?.Dispose();
                            }

                            if (ProfileManager.CurrentProfile.CustomBarsToggled)
                            {
                                Rectangle rect = new Rectangle(0, 0, HealthBarGumpCustom.HPB_WIDTH, HealthBarGumpCustom.HPB_HEIGHT_SINGLELINE);

                                UIManager.Add
                                (
                                    customgump = new HealthBarGumpCustom(obj)
                                    {
                                        X = Mouse.Position.X - (rect.Width >> 1),
                                        Y = Mouse.Position.Y - (rect.Height >> 1)
                                    }
                                );
                            }
                            else
                            {
                                Rectangle rect = GumpsLoader.Instance.GetTexture(0x0804).Bounds;

                                UIManager.Add
                                (
                                    customgump = new HealthBarGump(obj)
                                    {
                                        X = Mouse.Position.X - (rect.Width >> 1),
                                        Y = Mouse.Position.Y - (rect.Height >> 1)
                                    }
                                );
                            }

                            UIManager.AttemptDragControl(customgump, true);
                            ok = false;
                        }
                        else if (obj is Item item)
                        {
                            GameActions.PickUp(item, Mouse.Position.X, Mouse.Position.Y);
                        }
                    }

                    SelectedObject.LastLeftDownObject = null;
                }
            }

            return ok;
        }

        internal override void OnKeyDown(SDL.SDL_KeyboardEvent e)
        {
            if (e.keysym.sym == SDL.SDL_Keycode.SDLK_TAB && e.repeat != 0)
            {
                return;
            }

            if (e.keysym.sym == SDL.SDL_Keycode.SDLK_ESCAPE && TargetManager.IsTargeting)
            {
                TargetManager.CancelTarget();
            }

            if (UIManager.KeyboardFocusControl != UIManager.SystemChat.TextBoxControl)
            {
                return;
            }

            switch (e.keysym.sym)
            {
                case SDL.SDL_Keycode.SDLK_ESCAPE:

                    if (Pathfinder.AutoWalking && Pathfinder.PathindingCanBeCancelled)
                    {
                        Pathfinder.StopAutoWalk();
                    }

                    break;

                case SDL.SDL_Keycode.SDLK_TAB when !ProfileManager.CurrentProfile.DisableTabBtn:

                    if (ProfileManager.CurrentProfile.HoldDownKeyTab)
                    {
                        if (!_requestedWarMode)
                        {
                            _requestedWarMode = true;

                            if (!World.Player.InWarMode)
                            {
                                NetClient.Socket.Send(new PChangeWarMode(true));
                            }
                        }
                    }

                    break;


                // chat system activation

                case SDL.SDL_Keycode.SDLK_1 when Keyboard.Shift:         // !
                case SDL.SDL_Keycode.SDLK_BACKSLASH when Keyboard.Shift: // \

                    if (ProfileManager.CurrentProfile.ActivateChatAfterEnter && ProfileManager.CurrentProfile.ActivateChatAdditionalButtons && !UIManager.SystemChat.IsActive)
                    {
                        UIManager.SystemChat.IsActive = true;
                    }

                    break;

                case SDL.SDL_Keycode.SDLK_EXCLAIM:     // !
                case SDL.SDL_Keycode.SDLK_SEMICOLON:   // ;
                case SDL.SDL_Keycode.SDLK_COLON:       // :
                case SDL.SDL_Keycode.SDLK_SLASH:       // /
                case SDL.SDL_Keycode.SDLK_BACKSLASH:   // \
                case SDL.SDL_Keycode.SDLK_PERIOD:      // .
                case SDL.SDL_Keycode.SDLK_KP_PERIOD:   // .
                case SDL.SDL_Keycode.SDLK_COMMA:       // ,
                case SDL.SDL_Keycode.SDLK_LEFTBRACKET: // [
                case SDL.SDL_Keycode.SDLK_MINUS:       // -
                case SDL.SDL_Keycode.SDLK_KP_MINUS:    // -
                    if (ProfileManager.CurrentProfile.ActivateChatAfterEnter && ProfileManager.CurrentProfile.ActivateChatAdditionalButtons && !UIManager.SystemChat.IsActive)
                    {
                        if (!Keyboard.Shift && !Keyboard.Alt && !Keyboard.Ctrl)
                        {
                            UIManager.SystemChat.IsActive = true;
                        }
                        else if (Keyboard.Shift && e.keysym.sym == SDL.SDL_Keycode.SDLK_SEMICOLON)
                        {
                            UIManager.SystemChat.IsActive = true;
                        }
                    }

                    break;

                case SDL.SDL_Keycode.SDLK_RETURN:
                case SDL.SDL_Keycode.SDLK_KP_ENTER:

                    if (UIManager.KeyboardFocusControl == UIManager.SystemChat.TextBoxControl)
                    {
                        if (ProfileManager.CurrentProfile.ActivateChatAfterEnter)
                        {
                            UIManager.SystemChat.Mode = ChatMode.Default;

                            if (!(Keyboard.Shift && ProfileManager.CurrentProfile.ActivateChatShiftEnterSupport))
                            {
                                UIManager.SystemChat.ToggleChatVisibility();
                            }
                        }

                        return;
                    }

                    break;
            }

            if (UIManager.KeyboardFocusControl == UIManager.SystemChat.TextBoxControl && UIManager.SystemChat.IsActive && ProfileManager.CurrentProfile.ActivateChatAfterEnter)
            {
                return;
            }

            bool canExecuteMacro = UIManager.KeyboardFocusControl == UIManager.SystemChat.TextBoxControl && UIManager.SystemChat.Mode >= ChatMode.Default;

            if (canExecuteMacro)
            {
                Macro macro = Macros.FindMacro(e.keysym.sym, Keyboard.Alt, Keyboard.Ctrl, Keyboard.Shift);

                if (macro != null && e.keysym.sym != SDL.SDL_Keycode.SDLK_UNKNOWN)
                {
                    if (macro.Items != null && macro.Items is MacroObject mac)
                    {
                        if (mac.Code == MacroType.Walk)
                        {
                            _flags[4] = true;

                            switch (mac.SubCode)
                            {
                                case MacroSubType.NW:
                                    _flags[0] = true;

                                    break;

                                case MacroSubType.SW:
                                    _flags[1] = true;

                                    break;

                                case MacroSubType.SE:
                                    _flags[2] = true;

                                    break;

                                case MacroSubType.NE:
                                    _flags[3] = true;

                                    break;

                                case MacroSubType.N:
                                    _flags[0] = true;
                                    _flags[3] = true;

                                    break;

                                case MacroSubType.S:
                                    _flags[1] = true;
                                    _flags[2] = true;

                                    break;

                                case MacroSubType.E:
                                    _flags[3] = true;
                                    _flags[2] = true;

                                    break;

                                case MacroSubType.W:
                                    _flags[0] = true;
                                    _flags[1] = true;

                                    break;
                            }
                        }
                        else
                        {
                            Macros.SetMacroToExecute(mac);
                            Macros.WaitingBandageTarget = false;
                            Macros.WaitForTargetTimer = 0;
                            Macros.Update();
                        }
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(UIManager.SystemChat.TextBoxControl.Text))
                    {
                        switch (e.keysym.sym)
                        {
                            case SDL.SDL_Keycode.SDLK_UP:
                                _flags[0] = true;

                                break;

                            case SDL.SDL_Keycode.SDLK_LEFT:
                                _flags[1] = true;

                                break;

                            case SDL.SDL_Keycode.SDLK_DOWN:
                                _flags[2] = true;

                                break;

                            case SDL.SDL_Keycode.SDLK_RIGHT:
                                _flags[3] = true;

                                break;
                        }
                    }
                }
            }
        }


        internal override void OnKeyUp(SDL.SDL_KeyboardEvent e)
        {
            if (ProfileManager.CurrentProfile.EnableMousewheelScaleZoom && ProfileManager.CurrentProfile.RestoreScaleAfterUnpressCtrl && !Keyboard.Ctrl)
            {
                Camera.Zoom = ProfileManager.CurrentProfile.DefaultScale;
            }

            if (_flags[4])
            {
                Macro macro = Macros.FindMacro(e.keysym.sym, Keyboard.Alt, Keyboard.Ctrl, Keyboard.Shift);

                if (macro != null && e.keysym.sym != SDL.SDL_Keycode.SDLK_UNKNOWN)
                {
                    if (macro.Items != null && macro.Items is MacroObject mac && mac.Code == MacroType.Walk)
                    {
                        _flags[4] = false;

                        switch (mac.SubCode)
                        {
                            case MacroSubType.NW:
                                _flags[0] = false;

                                break;

                            case MacroSubType.SW:
                                _flags[1] = false;

                                break;

                            case MacroSubType.SE:
                                _flags[2] = false;

                                break;

                            case MacroSubType.NE:
                                _flags[3] = false;

                                break;

                            case MacroSubType.N:
                                _flags[0] = false;
                                _flags[3] = false;

                                break;

                            case MacroSubType.S:
                                _flags[1] = false;
                                _flags[2] = false;

                                break;

                            case MacroSubType.E:
                                _flags[3] = false;
                                _flags[2] = false;

                                break;

                            case MacroSubType.W:
                                _flags[0] = false;
                                _flags[1] = false;

                                break;
                        }

                        Macros.SetMacroToExecute(mac);
                        Macros.WaitForTargetTimer = 0;
                        Macros.Update();

                        for (int i = 0; i < 4; i++)
                        {
                            if (_flags[i])
                            {
                                _flags[4] = true;

                                break;
                            }
                        }
                    }
                }
            }

            switch (e.keysym.sym)
            {
                case SDL.SDL_Keycode.SDLK_UP:
                    _flags[0] = false;

                    break;

                case SDL.SDL_Keycode.SDLK_LEFT:
                    _flags[1] = false;

                    break;

                case SDL.SDL_Keycode.SDLK_DOWN:
                    _flags[2] = false;

                    break;

                case SDL.SDL_Keycode.SDLK_RIGHT:
                    _flags[3] = false;

                    break;
            }

            if (e.keysym.sym == SDL.SDL_Keycode.SDLK_TAB && !ProfileManager.CurrentProfile.DisableTabBtn)
            {
                if (ProfileManager.CurrentProfile.HoldDownKeyTab)
                {
                    if (_requestedWarMode)
                    {
                        NetClient.Socket.Send(new PChangeWarMode(false));
                        _requestedWarMode = false;
                    }
                }
                else
                {
                    GameActions.ToggleWarMode();
                }
            }
        }
    }
}