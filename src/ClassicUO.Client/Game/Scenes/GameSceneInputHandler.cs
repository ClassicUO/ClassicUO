// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Linq;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Sdk.Assets;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;
using SDL2;
using MathHelper = ClassicUO.Renderer.MathHelper;
using ClassicUO.Services;

namespace ClassicUO.Game.Scenes
{
    internal partial class GameScene
    {
        private bool _boatRun,
            _boatIsMoving;
        private readonly bool[] _flags = new bool[5];
        private bool _followingMode;
        private uint _followingTarget;
        private uint _holdMouse2secOverItemTime;
        private bool _isMouseLeftDown;
        private bool _isSelectionActive;
        private Direction _lastBoatDirection;
        private bool _requestedWarMode;
        private bool _rightMousePressed,
            _continueRunning;
        private Point _selectionStart,
            _selectionEnd;
        private int AnchorOffset => ProfileManager.CurrentProfile.DragSelectAsAnchor ? 0 : 2;

        private bool MoveCharacterByMouseInput()
        {
            if ((_rightMousePressed || _continueRunning) && _world.InGame) // && !Pathfinder.AutoWalking)
            {
                if (_world.Player.Pathfinder.AutoWalking)
                {
                    _world.Player.Pathfinder.StopAutoWalk();
                }

                int x = Camera.Bounds.X + (Camera.Bounds.Width >> 1);
                int y = Camera.Bounds.Y + (Camera.Bounds.Height >> 1);

                Direction direction = (Direction)
                    GameCursor.GetMouseDirection(x, y, Mouse.Position.X, Mouse.Position.Y, 1);

                double mouseRange = MathHelper.Hypotenuse(
                    x - Mouse.Position.X,
                    y - Mouse.Position.Y
                );

                Direction facing = direction;

                if (facing == Direction.North)
                {
                    facing = (Direction)8;
                }

                bool run = mouseRange >= 190;

                if (_world.Player.IsDrivingBoat)
                {
                    if (!_boatIsMoving || _boatRun != run || _lastBoatDirection != facing - 1)
                    {
                        _boatRun = run;
                        _lastBoatDirection = facing - 1;
                        _boatIsMoving = true;

                        _world.BoatMovingManager.MoveRequest(facing - 1, (byte)(run ? 2 : 1));
                    }
                }
                else
                {
                    _world.Player.Walk(facing - 1, run);
                }

                return true;
            }

            return false;
        }

        private bool CanDragSelectOnObject(GameObject? obj)
        {
            return obj is null
                || obj is Static
                || obj is Land
                || obj is Multi
                || obj is Item tmpitem && tmpitem.IsLocked;
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

            int finalX = ProfileManager.CurrentProfile.DragSelectStartX;
            int finalY = ProfileManager.CurrentProfile.DragSelectStartY;

            bool useCHB = ProfileManager.CurrentProfile.CustomBarsToggled;

            Rectangle rect;

            if (useCHB)
            {
                rect = new Rectangle(
                    0,
                    0,
                    HealthBarGumpCustom.HPB_BAR_WIDTH,
                    HealthBarGumpCustom.HPB_HEIGHT_MULTILINE
                );
            }
            else
            {
                rect = _uoService.Gumps.GetGump(0x0804).UV;
            }

            foreach (Mobile mobile in _world.Mobiles.Values)
            {
                if (ProfileManager.CurrentProfile.DragSelectHumanoidsOnly && !mobile.IsHuman)
                    continue;

                // Skip if is Renamable (follower), or non-hostile notoriety
                if (ProfileManager.CurrentProfile.DragSelectHostileOnly && (mobile.IsRenamable || mobile.NotorietyFlag is NotorietyFlag.Ally or NotorietyFlag.Innocent or NotorietyFlag.Invulnerable))
                    continue;

                Point p = mobile.RealScreenPosition;

                p.X += (int)mobile.Offset.X + 22 + 5;
                p.Y += (int)(mobile.Offset.Y - mobile.Offset.Z) + 12 * AnchorOffset;
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
                    if (mobile != _world.Player)
                    {
                        if (ServiceProvider.Get<GuiService>().GetGump<BaseHealthBarGump>(mobile) != null)
                        {
                            continue;
                        }

                        BaseHealthBarGump hbgc;

                        if (useCHB)
                        {
                            hbgc = new HealthBarGumpCustom(_world, mobile);
                        }
                        else
                        {
                            hbgc = new HealthBarGump(_world, mobile);
                        }

                        if (finalY >= Camera.Bounds.Bottom - 20)
                        {
                            finalY = ProfileManager.CurrentProfile.DragSelectStartY;
                            finalX += rect.Width + 2;
                        }

                        if (finalX >= Camera.Bounds.Right - 20)
                        {
                            finalX = ProfileManager.CurrentProfile.DragSelectStartX;
                        }

                        hbgc.X = finalX;
                        hbgc.Y = finalY;

                        foreach (
                            BaseHealthBarGump bar in ServiceProvider.Get<GuiService>().Gumps
                                .OfType<BaseHealthBarGump>()
                                //.OrderBy(s => mobile.NotorietyFlag)
                                //.OrderBy(s => s.ScreenCoordinateX) ///testing placement SYRUPZ SYRUPZ SYRUPZ
                                .OrderBy(s => s.ScreenCoordinateX)
                                .ThenBy(s => s.ScreenCoordinateY)
                        )
                        {
                            if (bar.Bounds.Intersects(hbgc.Bounds))
                            {
                                finalY = bar.Bounds.Bottom + AnchorOffset;

                                if (finalY >= Camera.Bounds.Bottom - 100)
                                {
                                    finalY = ProfileManager.CurrentProfile.DragSelectStartY;
                                    finalX = bar.Bounds.Right + AnchorOffset;
                                }

                                if (finalX >= Camera.Bounds.Right - 100)
                                {
                                    finalX = ProfileManager.CurrentProfile.DragSelectStartX;
                                }

                                hbgc.X = finalX;
                                hbgc.Y = finalY;
                                if (ProfileManager.CurrentProfile.DragSelectAsAnchor)
                                    hbgc.TryAttacheToExist();
                            }
                        }

                        if (!ProfileManager.CurrentProfile.DragSelectAsAnchor)
                            finalY += rect.Height + 2;

                        ServiceProvider.Get<GuiService>().Add(hbgc);

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
                case MouseButtonType.Left:
                    return OnLeftMouseDown();
                case MouseButtonType.Right:
                    return OnRightMouseDown();
                case MouseButtonType.Middle:
                case MouseButtonType.XButton1:
                case MouseButtonType.XButton2:
                    return OnExtraMouseDown(button);
            }

            return false;
        }

        internal override bool OnMouseUp(MouseButtonType button)
        {
            switch (button)
            {
                case MouseButtonType.Left:
                    return OnLeftMouseUp();
                case MouseButtonType.Right:
                    return OnRightMouseUp();
                case MouseButtonType.Middle:
                case MouseButtonType.XButton1:
                case MouseButtonType.XButton2:
                    return OnExtraMouseUp(button);
            }

            return false;
        }

        internal override bool OnMouseDoubleClick(MouseButtonType button)
        {
            switch (button)
            {
                case MouseButtonType.Left:
                    return OnLeftMouseDoubleClick();
                case MouseButtonType.Right:
                    return OnRightMouseDoubleClick();
            }

            return false;
        }

        private bool OnLeftMouseDown()
        {
            if (
                ServiceProvider.Get<GuiService>().PopupMenu != null
                && !ServiceProvider.Get<GuiService>().PopupMenu.Bounds.Contains(Mouse.Position.X, Mouse.Position.Y)
            )
            {
                ServiceProvider.Get<GuiService>().ShowGamePopup(null);
            }

            if (!ServiceProvider.Get<GuiService>().IsMouseOverWorld)
            {
                return false;
            }

            if (_world.CustomHouseManager != null)
            {
                _isMouseLeftDown = true;

                if (
                    _world.TargetManager.IsTargeting
                    && _world.TargetManager.TargetingState == CursorTarget.MultiPlacement
                    && (
                        _world.CustomHouseManager.SelectedGraphic != 0
                        || _world.CustomHouseManager.Erasing
                        || _world.CustomHouseManager.SeekTile
                    )
                    && SelectedObject.Object is GameObject obj
                )
                {
                    _world.CustomHouseManager.OnTargetWorld(obj);
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
            if (
                ServiceProvider.Get<GuiService>().PopupMenu != null
                && !ServiceProvider.Get<GuiService>().PopupMenu.Bounds.Contains(Mouse.Position.X, Mouse.Position.Y)
            )
            {
                ServiceProvider.Get<GuiService>().ShowGamePopup(null);
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

            if (!ServiceProvider.Get<GuiService>().IsMouseOverWorld)
            {
                return false;
            }

            if (ServiceProvider.Get<GuiService>().SystemChat != null && !ServiceProvider.Get<GuiService>().SystemChat.IsFocused)
            {
                ServiceProvider.Get<GuiService>().KeyboardFocusControl = null;
                ServiceProvider.Get<GuiService>().SystemChat.SetFocus();
            }

            if (!ProfileManager.CurrentProfile.DisableAutoMove && _rightMousePressed)
            {
                _continueRunning = true;
            }

            var lastObj = SelectedObject.Object;
            SelectedObject.LastLeftDownObject = null;

            if (ServiceProvider.Get<GuiService>().IsDragging)
            {
                return false;
            }

            if (
                ServiceProvider.Get<GameCursorService>().GameCursor.ItemHold.Enabled
                && !ServiceProvider.Get<GameCursorService>().GameCursor.ItemHold.IsFixedPosition
            )
            {
                uint drop_container = 0xFFFF_FFFF;
                bool can_drop = false;
                ushort dropX = 0;
                ushort dropY = 0;
                sbyte dropZ = 0;

                var gobj = SelectedObject.Object as GameObject;

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
                        else if (
                            obj is Item it2
                            && (
                                it2.ItemData.IsSurface
                                || it2.ItemData.IsStackable
                                    && it2.Graphic == ServiceProvider.Get<GameCursorService>().GameCursor.ItemHold.Graphic
                            )
                        )
                        {
                            dropX = obj.X;
                            dropY = obj.Y;
                            dropZ = obj.Z;

                            if (it2.ItemData.IsSurface)
                            {
                                dropZ += (sbyte)(
                                    it2.ItemData.Height == 0xFF ? 0 : it2.ItemData.Height
                                );
                            }
                            else
                            {
                                drop_container = obj.Serial;
                            }
                        }
                    }
                    else
                    {
                        _audioService.PlaySound(0x0051);
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

                        if (gobj is Land land) { }
                        else
                        {
                            ref var itemData = ref ServiceProvider.Get<AssetsService>().TileData.StaticData[
                                gobj.Graphic
                            ];

                            if (itemData.IsSurface)
                            {
                                dropZ += (sbyte)(itemData.Height == 0xFF ? 0 : itemData.Height);
                            }
                        }
                    }
                    else
                    {
                        _audioService.PlaySound(0x0051);
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
                        GameActions.DropItem(
                            ServiceProvider.Get<GameCursorService>().GameCursor.ItemHold.Serial,
                            dropX,
                            dropY,
                            dropZ,
                            drop_container
                        );
                    }
                }
            }
            else if (_world.TargetManager.IsTargeting)
            {
                switch (_world.TargetManager.TargetingState)
                {
                    case CursorTarget.Grab:
                    case CursorTarget.SetGrabBag:
                    case CursorTarget.Position:
                    case CursorTarget.Object:
                    case CursorTarget.MultiPlacement when _world.CustomHouseManager == null:

                        {
                            var obj = lastObj;

                            if (obj is TextObject ov)
                            {
                                obj = ov.Owner;
                            }

                            switch (obj)
                            {
                                case Entity ent:
                                    _world.TargetManager.Target(ent.Serial);

                                    break;

                                case Land land:
                                    _world.TargetManager.Target(
                                        0,
                                        land.X,
                                        land.Y,
                                        land.Z,
                                        land.TileData.IsWet
                                    );

                                    break;

                                case GameObject o:
                                    _world.TargetManager.Target(o.Graphic, o.X, o.Y, o.Z);

                                    break;
                            }
                        }

                        Mouse.LastLeftButtonClickTime = 0;

                        break;

                    case CursorTarget.SetTargetClientSide:

                        {
                            var obj = lastObj;

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
                                    _world.TargetManager.Target(ent.Serial);
                                    ServiceProvider.Get<GuiService>().Add(new InspectorGump(_world, ent));

                                    break;

                                case Land land:
                                    _world.TargetManager.Target(0, land.X, land.Y, land.Z);
                                    ServiceProvider.Get<GuiService>().Add(new InspectorGump(_world, land));

                                    break;

                                case GameObject o:
                                    _world.TargetManager.Target(o.Graphic, o.X, o.Y, o.Z);
                                    ServiceProvider.Get<GuiService>().Add(new InspectorGump(_world, o));

                                    break;
                            }

                            Mouse.LastLeftButtonClickTime = 0;
                        }

                        break;

                    case CursorTarget.HueCommandTarget:

                        if (SelectedObject.Object is Entity selectedEntity)
                        {
                            _world.CommandManager.OnHueTarget(selectedEntity);
                        }

                        break;
                    case CursorTarget.IgnorePlayerTarget:
                        if (SelectedObject.Object is Entity pmEntity)
                        {
                            _world.IgnoreManager.AddIgnoredTarget(pmEntity);
                        }
                        _world.TargetManager.CancelTarget();
                        break;
                }
            }
            else
            {
                var obj = lastObj as GameObject;

                switch (obj)
                {
                    case Static st:
                        string name = StringHelper.GetPluralAdjustedString(
                            st.Name,
                            st.ItemData.Count > 1
                        );

                        if (string.IsNullOrEmpty(name))
                        {
                            name = ServiceProvider.Get<AssetsService>().Clilocs.GetString(
                                1020000 + st.Graphic,
                                st.ItemData.Name
                            );
                        }

                        _world.MessageManager.HandleMessage(
                            null,
                            name,
                            string.Empty,
                            0x03b2,
                            MessageType.Label,
                            3,
                            TextType.CLIENT
                        );

                        obj.AddMessage(MessageType.Label, name, 3, 0x03b2, false, TextType.CLIENT);

                        if (obj.TextContainer != null && obj.TextContainer.MaxSize != 1)
                        {
                            obj.TextContainer.MaxSize = 1;
                        }

                        break;

                    case Multi multi:
                        name = multi.Name;

                        if (string.IsNullOrEmpty(name))
                        {
                            name = ServiceProvider.Get<AssetsService>().Clilocs.GetString(
                                1020000 + multi.Graphic,
                                multi.ItemData.Name
                            );
                        }

                        _world.MessageManager.HandleMessage(
                            null,
                            name,
                            string.Empty,
                            0x03b2,
                            MessageType.Label,
                            3,
                            TextType.CLIENT
                        );

                        obj.AddMessage(MessageType.Label, name, 3, 0x03b2, false, TextType.CLIENT);

                        if (obj.TextContainer != null && obj.TextContainer.MaxSize == 5)
                        {
                            obj.TextContainer.MaxSize = 1;
                        }

                        break;

                    case Entity ent:

                        if (Keyboard.Alt && ent is Mobile)
                        {
                            _world.MessageManager.HandleMessage(
                                _world.Player,
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
                        else if (!_world.DelayedObjectClickManager.IsEnabled)
                        {
                            _world.DelayedObjectClickManager.Set(
                                ent.Serial,
                                Mouse.Position.X,
                                Mouse.Position.Y,
                                Time.Ticks + Mouse.MOUSE_DELAY_DOUBLE_CLICK
                            );
                        }

                        break;
                }
            }

            return true;
        }

        private bool OnLeftMouseDoubleClick()
        {
            bool result = false;

            if (!ServiceProvider.Get<GuiService>().IsMouseOverWorld)
            {
                result = _world.DelayedObjectClickManager.IsEnabled;

                if (result)
                {
                    _world.DelayedObjectClickManager.Clear();

                    return false;
                }

                return false;
            }

            var obj = SelectedObject.Object;

            switch (obj)
            {
                case Item item:
                    result = true;

                    if (!GameActions.OpenCorpse(_world, item))
                    {
                        GameActions.DoubleClick(_world, item);
                    }

                    break;

                case Mobile mob:
                    result = true;

                    if (_world.Player.InWarMode && _world.Player != mob)
                    {
                        GameActions.Attack(_world, mob);
                    }
                    else
                    {
                        GameActions.DoubleClick(_world, mob);
                    }

                    break;

                case TextObject msg when msg.Owner is Entity entity:
                    result = true;
                    GameActions.DoubleClick(_world, entity);

                    break;

                default:
                    _world.LastObject = 0;

                    break;
            }

            if (result)
            {
                _world.DelayedObjectClickManager.Clear();
            }

            return result;
        }

        private bool OnRightMouseDown()
        {
            if (
                ServiceProvider.Get<GuiService>().PopupMenu != null
                && !ServiceProvider.Get<GuiService>().PopupMenu.Bounds.Contains(Mouse.Position.X, Mouse.Position.Y)
            )
            {
                ServiceProvider.Get<GuiService>().ShowGamePopup(null);
            }

            if (!ServiceProvider.Get<GuiService>().IsMouseOverWorld)
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
            if (
                ServiceProvider.Get<GuiService>().PopupMenu != null
                && !ServiceProvider.Get<GuiService>().PopupMenu.Bounds.Contains(Mouse.Position.X, Mouse.Position.Y)
            )
            {
                ServiceProvider.Get<GuiService>().ShowGamePopup(null);
            }

            _rightMousePressed = false;

            if (_boatIsMoving)
            {
                _boatIsMoving = false;
                _world.BoatMovingManager.MoveRequest(_world.Player.Direction, 0);
            }

            return ServiceProvider.Get<GuiService>().IsMouseOverWorld;
        }

        private bool OnRightMouseDoubleClick()
        {
            if (!ServiceProvider.Get<GuiService>().IsMouseOverWorld)
            {
                return false;
            }

            if (ProfileManager.CurrentProfile.EnablePathfind && !_world.Player.Pathfinder.AutoWalking)
            {
                if (ProfileManager.CurrentProfile.UseShiftToPathfind && !Keyboard.Shift)
                {
                    return false;
                }

                if (SelectedObject.Object is GameObject obj)
                {
                    if (obj is Static || obj is Multi || obj is Item)
                    {
                        ref var itemdata = ref ServiceProvider.Get<AssetsService>().TileData.StaticData[
                            obj.Graphic
                        ];

                        if (itemdata.IsSurface && _world.Player.Pathfinder.WalkTo(obj.X, obj.Y, obj.Z, 0))
                        {
                            _world.Player.AddMessage(
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
                    else if (obj is Land && _world.Player.Pathfinder.WalkTo(obj.X, obj.Y, obj.Z, 0))
                    {
                        _world.Player.AddMessage(
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
            }

            return false;
        }

        private bool OnExtraMouseDown(MouseButtonType button)
        {
            if (CanExecuteMacro())
            {
                var macro = _world.Macros.FindMacro(button, Keyboard.Alt, Keyboard.Ctrl, Keyboard.Shift);

                if (macro != null && button != MouseButtonType.None)
                {
                    if (macro.Items is MacroObject mac)
                    {
                        if (mac.Code == MacroType.LookAtMouse)
                        {
                            var camera = _sceneService.Scene?.Camera;
                            if (camera == null)
                                return false;

                            camera.PeekingToMouse = true;

                            if (mac.SubCode == MacroSubType.LookBackwards)
                            {
                                camera.PeekBackwards = true;
                            }

                            return true;
                        }

                        ExecuteMacro(mac);

                        return true;
                    }
                }
            }

            return false;
        }

        private bool OnExtraMouseUp(MouseButtonType button)
        {
            if (_sceneService.Scene?.Camera.PeekingToMouse ?? false)
            {
                var macro = _world.Macros.FindMacro(button, Keyboard.Alt, Keyboard.Ctrl, Keyboard.Shift);

                if (
                    macro != null
                    && macro.Items is MacroObject mac
                    && mac.Code == MacroType.LookAtMouse
                )
                {
                    _sceneService.Scene.Camera.PeekingToMouse = false;
                }

                return true;
            }

            return false;
        }

        internal override bool OnMouseWheel(bool up)
        {
            if (Keyboard.Ctrl && ServiceProvider.Get<GameCursorService>().GameCursor.ItemHold.Enabled)
            {
                if (!up && !ServiceProvider.Get<GameCursorService>().GameCursor.ItemHold.IsFixedPosition)
                {
                    ServiceProvider.Get<GameCursorService>().GameCursor.ItemHold.IsFixedPosition = true;
                    ServiceProvider.Get<GameCursorService>().GameCursor.ItemHold.IgnoreFixedPosition = true;
                    ServiceProvider.Get<GameCursorService>().GameCursor.ItemHold.FixedX = Mouse.Position.X;
                    ServiceProvider.Get<GameCursorService>().GameCursor.ItemHold.FixedY = Mouse.Position.Y;
                }

                if (ServiceProvider.Get<GameCursorService>().GameCursor.ItemHold.IgnoreFixedPosition)
                {
                    return true;
                }
            }

            if (CanExecuteMacro())
            {
                var macro = _world.Macros.FindMacro(up, Keyboard.Alt, Keyboard.Ctrl, Keyboard.Shift);

                if (macro != null)
                {
                    if (macro.Items is MacroObject mac)
                    {
                        ExecuteMacro(mac);

                        return true;
                    }
                }
            }

            if (!ServiceProvider.Get<GuiService>().IsMouseOverWorld)
            {
                return false;
            }

            if (Keyboard.Ctrl && ProfileManager.CurrentProfile.EnableMousewheelScaleZoom)
            {
                if (up)
                {
                    Camera.ZoomIn();
                }
                else
                {
                    Camera.ZoomOut();
                }

                return true;
            }

            return false;
        }

        internal override bool OnMouseDragging()
        {
            if (!ServiceProvider.Get<GuiService>().IsMouseOverWorld)
            {
                return false;
            }

            bool ok = true;

            if (Mouse.LButtonPressed && !ServiceProvider.Get<GameCursorService>().GameCursor.ItemHold.Enabled)
            {
                Point offset = Mouse.LDragOffset;

                if (
                    !ServiceProvider.Get<GameCursorService>().GameCursor.IsDraggingCursorForced
                    && // don't trigger "sallos ez grab" when dragging wmap or skill
                    !_isSelectionActive
                    && // and ofc when selection is enabled
                    (
                        Math.Abs(offset.X) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS
                        || Math.Abs(offset.Y) > Constants.MIN_PICKUP_DRAG_DISTANCE_PIXELS
                    )
                )
                {
                    Entity? obj;

                    if (
                        ProfileManager.CurrentProfile.SallosEasyGrab
                        && SelectedObject.Object is Entity ent
                        && SelectedObject.LastLeftDownObject == null
                    )
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
                            var customgump = ServiceProvider.Get<GuiService>().GetGump<BaseHealthBarGump>(
                                obj
                            );
                            customgump?.Dispose();

                            if (obj == _world.Player && ProfileManager.CurrentProfile.StatusGumpBarMutuallyExclusive)
                            {
                                StatusGumpBase.GetStatusGump()?.Dispose();
                            }

                            if (ProfileManager.CurrentProfile.CustomBarsToggled)
                            {
                                Rectangle rect = new Rectangle(
                                    0,
                                    0,
                                    HealthBarGumpCustom.HPB_WIDTH,
                                    HealthBarGumpCustom.HPB_HEIGHT_SINGLELINE
                                );

                                ServiceProvider.Get<GuiService>().Add(
                                    customgump = new HealthBarGumpCustom(_world, obj)
                                    {
                                        X = Mouse.LClickPosition.X - (rect.Width >> 1),
                                        Y = Mouse.LClickPosition.Y - (rect.Height >> 1)
                                    }
                                );
                            }
                            else
                            {
                                var bounds = _uoService.Gumps.GetGump(0x0804).UV;

                                ServiceProvider.Get<GuiService>().Add(
                                    customgump = new HealthBarGump(_world, obj)
                                    {
                                        X = Mouse.LClickPosition.X - (bounds.Width >> 1),
                                        Y = Mouse.LClickPosition.Y - (bounds.Height >> 1)
                                    }
                                );
                            }

                            ServiceProvider.Get<GuiService>().AttemptDragControl(customgump, true);
                            ok = false;
                        }
                        else if (obj is Item item)
                        {
                            GameActions.PickUp(_world, item, Mouse.Position.X, Mouse.Position.Y);
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

            if (e.keysym.sym == SDL.SDL_Keycode.SDLK_ESCAPE && _world.TargetManager.IsTargeting)
            {
                _world.TargetManager.CancelTarget();
            }

            if (ServiceProvider.Get<GuiService>().SystemChat == null)
                return;

            if (ServiceProvider.Get<GuiService>().KeyboardFocusControl != ServiceProvider.Get<GuiService>().SystemChat.TextBoxControl)
            {
                return;
            }

            switch (e.keysym.sym)
            {
                case SDL.SDL_Keycode.SDLK_ESCAPE:

                    if (_world.Player.Pathfinder.AutoWalking && _world.Player.Pathfinder.PathindingCanBeCancelled)
                    {
                        _world.Player.Pathfinder.StopAutoWalk();
                    }

                    break;

                case SDL.SDL_Keycode.SDLK_TAB when !ProfileManager.CurrentProfile.DisableTabBtn:

                    if (ProfileManager.CurrentProfile.HoldDownKeyTab)
                    {
                        if (!_requestedWarMode)
                        {
                            _requestedWarMode = true;

                            if (!_world.Player.InWarMode)
                            {
                                ServiceProvider.Get<PacketHandlerService>().Out.Send_ChangeWarMode(true);
                            }
                        }
                    }

                    break;

                // chat system activation

                case SDL.SDL_Keycode.SDLK_1 when Keyboard.Shift: // !
                case SDL.SDL_Keycode.SDLK_BACKSLASH when Keyboard.Shift: // \

                    if (
                        ProfileManager.CurrentProfile.ActivateChatAfterEnter
                        && ProfileManager.CurrentProfile.ActivateChatAdditionalButtons
                        && !ServiceProvider.Get<GuiService>().SystemChat.IsActive
                    )
                    {
                        ServiceProvider.Get<GuiService>().SystemChat.IsActive = true;
                    }

                    break;

                case SDL.SDL_Keycode.SDLK_EXCLAIM: // !
                case SDL.SDL_Keycode.SDLK_SEMICOLON: // ;
                case SDL.SDL_Keycode.SDLK_COLON: // :
                case SDL.SDL_Keycode.SDLK_SLASH: // /
                case SDL.SDL_Keycode.SDLK_BACKSLASH: // \
                case SDL.SDL_Keycode.SDLK_PERIOD: // .
                case SDL.SDL_Keycode.SDLK_KP_PERIOD: // .
                case SDL.SDL_Keycode.SDLK_COMMA: // ,
                case SDL.SDL_Keycode.SDLK_LEFTBRACKET: // [
                case SDL.SDL_Keycode.SDLK_MINUS: // -
                case SDL.SDL_Keycode.SDLK_KP_MINUS: // -
                    if (
                        ProfileManager.CurrentProfile.ActivateChatAfterEnter
                        && ProfileManager.CurrentProfile.ActivateChatAdditionalButtons
                        && !ServiceProvider.Get<GuiService>().SystemChat.IsActive
                    )
                    {
                        if (!Keyboard.Shift && !Keyboard.Alt && !Keyboard.Ctrl)
                        {
                            ServiceProvider.Get<GuiService>().SystemChat.IsActive = true;
                        }
                        else if (Keyboard.Shift && e.keysym.sym == SDL.SDL_Keycode.SDLK_SEMICOLON)
                        {
                            ServiceProvider.Get<GuiService>().SystemChat.IsActive = true;
                        }
                    }

                    break;

                case SDL.SDL_Keycode.SDLK_RETURN:
                case SDL.SDL_Keycode.SDLK_KP_ENTER:

                    if (ServiceProvider.Get<GuiService>().KeyboardFocusControl == ServiceProvider.Get<GuiService>().SystemChat.TextBoxControl)
                    {
                        if (ProfileManager.CurrentProfile.ActivateChatAfterEnter)
                        {
                            ServiceProvider.Get<GuiService>().SystemChat.Mode = ChatMode.Default;

                            if (
                                !(
                                    Keyboard.Shift
                                    && ProfileManager.CurrentProfile.ActivateChatShiftEnterSupport
                                )
                            )
                            {
                                ServiceProvider.Get<GuiService>().SystemChat.ToggleChatVisibility();
                            }
                        }

                        return;
                    }

                    break;
            }

            if (
                ServiceProvider.Get<GuiService>().KeyboardFocusControl == ServiceProvider.Get<GuiService>().SystemChat.TextBoxControl
                && ServiceProvider.Get<GuiService>().SystemChat.IsActive
                && ProfileManager.CurrentProfile.ActivateChatAfterEnter
            )
            {
                return;
            }

            if (CanExecuteMacro())
            {
                var macro = _world.Macros.FindMacro(
                    e.keysym.sym,
                    Keyboard.Alt,
                    Keyboard.Ctrl,
                    Keyboard.Shift
                );

                if (macro != null && e.keysym.sym != SDL.SDL_Keycode.SDLK_UNKNOWN)
                {
                    if (macro.Items is MacroObject mac)
                    {
                        if (mac.Code == MacroType.LookAtMouse)
                        {
                            var camera = _sceneService.Scene?.Camera;
                            if (camera != null)
                            {
                                camera.PeekingToMouse = true;

                                if (mac.SubCode == MacroSubType.LookBackwards)
                                {
                                    camera.PeekBackwards = true;
                                }
                            }
                        }
                        else if (mac.Code == MacroType.Walk)
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
                            ExecuteMacro(mac);
                        }
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(ServiceProvider.Get<GuiService>().SystemChat.TextBoxControl.Text))
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
            if (!_world.InGame)
            {
                return;
            }

            if (
                ProfileManager.CurrentProfile.EnableMousewheelScaleZoom
                && ProfileManager.CurrentProfile.RestoreScaleAfterUnpressCtrl
                && !Keyboard.Ctrl
            )
            {
                Camera.Zoom = ProfileManager.CurrentProfile.DefaultScale;
            }

            if (_flags[4] || _sceneService.Scene.Camera.PeekingToMouse)
            {
                var macro = _world.Macros.FindMacro(
                    e.keysym.sym,
                    Keyboard.Alt,
                    Keyboard.Ctrl,
                    Keyboard.Shift
                );

                if (macro != null && e.keysym.sym != SDL.SDL_Keycode.SDLK_UNKNOWN)
                {
                    if (macro.Items is MacroObject mac)
                    {
                        if (mac.Code == MacroType.LookAtMouse)
                        {
                            _sceneService.Scene.Camera.PeekingToMouse = false;
                        }
                        else if (mac.Code == MacroType.Walk)
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

                            _world.Macros.SetMacroToExecute(mac);
                            _world.Macros.WaitForTargetTimer = 0;
                            _world.Macros.Update();

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

            if (
                e.keysym.sym == SDL.SDL_Keycode.SDLK_TAB
                && !ProfileManager.CurrentProfile.DisableTabBtn
            )
            {
                if (ProfileManager.CurrentProfile.HoldDownKeyTab)
                {
                    if (_requestedWarMode)
                    {
                        ServiceProvider.Get<PacketHandlerService>().Out.Send_ChangeWarMode(false);
                        _requestedWarMode = false;
                    }
                }
                else
                {
                    GameActions.ToggleWarMode(_world.Player);
                }
            }
        }

        private bool CanExecuteMacro()
        {
            return ServiceProvider.Get<GuiService>().KeyboardFocusControl == ServiceProvider.Get<GuiService>().SystemChat.TextBoxControl
                && ServiceProvider.Get<GuiService>().SystemChat.Mode >= ChatMode.Default;
        }

        private void ExecuteMacro(MacroObject macro)
        {
            _world.Macros.SetMacroToExecute(macro);
            _world.Macros.WaitingBandageTarget = false;
            _world.Macros.WaitForTargetTimer = 0;
            _world.Macros.Update();
        }

        private void HandleMouseInput()
        {
            // Ottimizzazione: utilizzo di variabili locali per i servizi
            var uoService = ServiceProvider.Get<UOService>();
            var audioService = ServiceProvider.Get<AudioService>();
            var sceneService = ServiceProvider.Get<SceneService>();
            var assetsService = ServiceProvider.Get<AssetsService>();

            if ((_rightMousePressed || _continueRunning) && _world.InGame) // && !Pathfinder.AutoWalking)
            {
                if (_world.Player.Pathfinder.AutoWalking)
                {
                    _world.Player.Pathfinder.StopAutoWalk();
                }

                int x = Camera.Bounds.X + (Camera.Bounds.Width >> 1);
                int y = Camera.Bounds.Y + (Camera.Bounds.Height >> 1);

                Direction direction = (Direction)
                    GameCursor.GetMouseDirection(x, y, Mouse.Position.X, Mouse.Position.Y, 1);

                double mouseRange = MathHelper.Hypotenuse(
                    x - Mouse.Position.X,
                    y - Mouse.Position.Y
                );

                Direction facing = direction;

                if (facing == Direction.North)
                {
                    facing = (Direction)8;
                }

                bool run = mouseRange >= 190;

                if (_world.Player.IsDrivingBoat)
                {
                    if (!_boatIsMoving || _boatRun != run || _lastBoatDirection != facing - 1)
                    {
                        _boatRun = run;
                        _lastBoatDirection = facing - 1;
                        _boatIsMoving = true;

                        _world.BoatMovingManager.MoveRequest(facing - 1, (byte)(run ? 2 : 1));
                    }
                }
                else
                {
                    _world.Player.Walk(facing - 1, run);
                }
            }

            if (!ServiceProvider.Get<GuiService>().IsMouseOverWorld)
            {
                return;
            }

            if (_world.CustomHouseManager != null)
            {
                _isMouseLeftDown = true;

                if (
                    _world.TargetManager.IsTargeting
                    && _world.TargetManager.TargetingState == CursorTarget.MultiPlacement
                    && (
                        _world.CustomHouseManager.SelectedGraphic != 0
                        || _world.CustomHouseManager.Erasing
                        || _world.CustomHouseManager.SeekTile
                    )
                    && SelectedObject.Object is GameObject obj
                )
                {
                    _world.CustomHouseManager.OnTargetWorld(obj);
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

            if (ServiceProvider.Get<GuiService>().SystemChat != null && !ServiceProvider.Get<GuiService>().SystemChat.IsFocused)
            {
                ServiceProvider.Get<GuiService>().KeyboardFocusControl = null;
                ServiceProvider.Get<GuiService>().SystemChat.SetFocus();
            }

            if (!ProfileManager.CurrentProfile.DisableAutoMove && _rightMousePressed)
            {
                _continueRunning = true;
            }

            var lastObj = SelectedObject.Object;
            SelectedObject.LastLeftDownObject = null;

            if (ServiceProvider.Get<GuiService>().IsDragging)
            {
                return;
            }

            if (
                ServiceProvider.Get<GameCursorService>().GameCursor.ItemHold.Enabled
                && !ServiceProvider.Get<GameCursorService>().GameCursor.ItemHold.IsFixedPosition
            )
            {
                uint drop_container = 0xFFFF_FFFF;
                bool can_drop = false;
                ushort dropX = 0;
                ushort dropY = 0;
                sbyte dropZ = 0;

                var gobj = SelectedObject.Object as GameObject;

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
                        else if (
                            obj is Item it2
                            && (
                                it2.ItemData.IsSurface
                                || it2.ItemData.IsStackable
                                    && it2.Graphic == ServiceProvider.Get<GameCursorService>().GameCursor.ItemHold.Graphic
                            )
                        )
                        {
                            dropX = obj.X;
                            dropY = obj.Y;
                            dropZ = obj.Z;

                            if (it2.ItemData.IsSurface)
                            {
                                dropZ += (sbyte)(
                                    it2.ItemData.Height == 0xFF ? 0 : it2.ItemData.Height
                                );
                            }
                            else
                            {
                                drop_container = obj.Serial;
                            }
                        }
                    }
                    else
                    {
                        audioService.PlaySound(0x0051);
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

                        if (gobj is Land land) { }
                        else
                        {
                            ref var itemData = ref assetsService.TileData.StaticData[
                                gobj.Graphic
                            ];

                            if (itemData.IsSurface)
                            {
                                dropZ += (sbyte)(itemData.Height == 0xFF ? 0 : itemData.Height);
                            }
                        }
                    }
                    else
                    {
                        audioService.PlaySound(0x0051);
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
                        GameActions.DropItem(
                            ServiceProvider.Get<GameCursorService>().GameCursor.ItemHold.Serial,
                            dropX,
                            dropY,
                            dropZ,
                            drop_container
                        );
                    }
                }
            }
            else if (_world.TargetManager.IsTargeting)
            {
                switch (_world.TargetManager.TargetingState)
                {
                    case CursorTarget.Grab:
                    case CursorTarget.SetGrabBag:
                    case CursorTarget.Position:
                    case CursorTarget.Object:
                    case CursorTarget.MultiPlacement when _world.CustomHouseManager == null:

                        {
                            var obj = lastObj;

                            if (obj is TextObject ov)
                            {
                                obj = ov.Owner;
                            }

                            switch (obj)
                            {
                                case Entity ent:
                                    _world.TargetManager.Target(ent.Serial);

                                    break;

                                case Land land:
                                    _world.TargetManager.Target(
                                        0,
                                        land.X,
                                        land.Y,
                                        land.Z,
                                        land.TileData.IsWet
                                    );

                                    break;

                                case GameObject o:
                                    _world.TargetManager.Target(o.Graphic, o.X, o.Y, o.Z);

                                    break;
                            }
                        }

                        Mouse.LastLeftButtonClickTime = 0;

                        break;

                    case CursorTarget.SetTargetClientSide:

                        {
                            var obj = lastObj;

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
                                    _world.TargetManager.Target(ent.Serial);
                                    ServiceProvider.Get<GuiService>().Add(new InspectorGump(_world, ent));

                                    break;

                                case Land land:
                                    _world.TargetManager.Target(0, land.X, land.Y, land.Z);
                                    ServiceProvider.Get<GuiService>().Add(new InspectorGump(_world, land));

                                    break;

                                case GameObject o:
                                    _world.TargetManager.Target(o.Graphic, o.X, o.Y, o.Z);
                                    ServiceProvider.Get<GuiService>().Add(new InspectorGump(_world, o));

                                    break;
                            }

                            Mouse.LastLeftButtonClickTime = 0;
                        }

                        break;

                    case CursorTarget.HueCommandTarget:

                        if (SelectedObject.Object is Entity selectedEntity)
                        {
                            _world.CommandManager.OnHueTarget(selectedEntity);
                        }

                        break;
                    case CursorTarget.IgnorePlayerTarget:
                        if (SelectedObject.Object is Entity pmEntity)
                        {
                            _world.IgnoreManager.AddIgnoredTarget(pmEntity);
                        }
                        _world.TargetManager.CancelTarget();
                        break;
                }
            }
            else
            {
                var obj = lastObj as GameObject;

                switch (obj)
                {
                    case Static st:
                        string name = StringHelper.GetPluralAdjustedString(
                            st.Name,
                            st.ItemData.Count > 1
                        );

                        if (string.IsNullOrEmpty(name))
                        {
                            name = assetsService.Clilocs.GetString(
                                1020000 + st.Graphic,
                                st.ItemData.Name
                            );
                        }

                        _world.MessageManager.HandleMessage(
                            null,
                            name,
                            string.Empty,
                            0x03b2,
                            MessageType.Label,
                            3,
                            TextType.CLIENT
                        );

                        obj.AddMessage(MessageType.Label, name, 3, 0x03b2, false, TextType.CLIENT);

                        if (obj.TextContainer != null && obj.TextContainer.MaxSize != 1)
                        {
                            obj.TextContainer.MaxSize = 1;
                        }

                        break;

                    case Multi multi:
                        name = multi.Name;

                        if (string.IsNullOrEmpty(name))
                        {
                            name = assetsService.Clilocs.GetString(
                                1020000 + multi.Graphic,
                                multi.ItemData.Name
                            );
                        }

                        _world.MessageManager.HandleMessage(
                            null,
                            name,
                            string.Empty,
                            0x03b2,
                            MessageType.Label,
                            3,
                            TextType.CLIENT
                        );

                        obj.AddMessage(MessageType.Label, name, 3, 0x03b2, false, TextType.CLIENT);

                        if (obj.TextContainer != null && obj.TextContainer.MaxSize == 5)
                        {
                            obj.TextContainer.MaxSize = 1;
                        }

                        break;

                    case Entity ent:

                        if (Keyboard.Alt && ent is Mobile)
                        {
                            _world.MessageManager.HandleMessage(
                                _world.Player,
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
                        else if (!_world.DelayedObjectClickManager.IsEnabled)
                        {
                            _world.DelayedObjectClickManager.Set(
                                ent.Serial,
                                Mouse.Position.X,
                                Mouse.Position.Y,
                                Time.Ticks + Mouse.MOUSE_DELAY_DOUBLE_CLICK
                            );
                        }

                        break;
                }
            }
        }
    }
}
