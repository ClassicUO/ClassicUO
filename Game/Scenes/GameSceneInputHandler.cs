using System;
using System.Runtime.Remoting.Lifetime;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Gumps.UIGumps;
using ClassicUO.Game.Map;
using ClassicUO.Game.System;
using ClassicUO.Input;
using ClassicUO.Interfaces;
using ClassicUO.IO.Resources;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

using SDL2;

namespace ClassicUO.Game.Scenes
{
    partial class GameScene
    {
        private double _dequeueAt;
        private bool _inqueue;
        private InputMouseEvent _queuedEvent;
        private GameObject _queuedObject;
        private Point _queuedPosition;
        private bool _rightMousePressed;
        private Action _queuedAction;

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
            if (TargetSystem.IsTargeting && IsMouseOverWorld)
            {
                switch (TargetSystem.TargetingState)
                {
                    case TargetType.Position:
                    case TargetType.Object:

                        GameObject obj = null;

                        if (IsMouseOverUI)
                        {
                            GumpControl control = UIManager.MouseOverControl;

                            if (control is ItemGumpling gumpling)
                                obj = gumpling.Item;
                            //else if (control.RootParent is Mobil)

                        }
                        else if (IsMouseOverWorld)
                        {
                            obj = SelectedObject;
                        }

                        if (obj != null)
                        {
                            TargetSystem.MouseTargetingEventObject(obj);

                            Mouse.LastLeftButtonClickTime = 0;
                        }

                        break;
                    case TargetType.Nothing:

                        break;
                    default:
                        Log.Message(LogTypes.Warning, "Not implemented.");

                        break;
                }
            }
            else if (IsHoldingItem)
            {
                SelectedObject = null;

                if (IsMouseOverUI)
                {
                    GumpControl target = UIManager.MouseOverControl;

                    if (target is ItemGumpling gumpling && !(target is ItemGumplingPaperdoll))
                    {
                        Item item = gumpling.Item;
                        SelectedObject = item;

                        if (TileData.IsContainer((long)item.ItemData.Flags))
                        {
                            DropHeldItemToContainer(item);
                        }
                        else if (HeldItem.Graphic == item.Graphic && TileData.IsStackable((long)HeldItem.ItemData.Flags))
                        {
                            MergeHeldItem(item);
                        }
                        else
                        {
                            if (item.Container.IsItem)
                                DropHeldItemToContainer(World.Items.Get(item.Container), (ushort)(target.X + (Mouse.Position.X - target.ScreenCoordinateX) - _heldOffset.X), (ushort)(target.Y + (Mouse.Position.Y - target.ScreenCoordinateY) - _heldOffset.Y));
                        }
                    }
                    else if (target is GumpPicContainer container)
                    {
                        SelectedObject = container.Item;
                        int x = Mouse.Position.X - _heldOffset.X - (target.X + target.Parent.X);
                        int y = Mouse.Position.Y - _heldOffset.Y - (target.Y + target.Parent.Y);
                        DropHeldItemToContainer(container.Item, (ushort)x, (ushort)y);
                    }
                    else if (target is ItemGumplingPaperdoll || target is GumpPic pic && pic.IsPaperdoll || target is EquipmentSlot)
                    {
                        if (TileData.IsWearable((long)HeldItem.ItemData.Flags))
                            WearHeldItem();
                    }
                    else if (target is GumpPicBackpack backpack)
                    {
                        DropHeldItemToContainer(backpack.BackpackItem);
                    }
                }
                else if (IsMouseOverWorld)
                {
                    GameObject obj = _mousePicker.MouseOverObject;
                    if (obj != null && obj.Distance < 5)
                        switch (obj)
                        {
                            case Mobile mobile:
                                MergeHeldItem(mobile);

                                break;
                            case IDynamicItem dyn:

                                if (dyn is Item item)
                                {
                                    if (item.IsCorpse)
                                    {
                                        MergeHeldItem(item);
                                    }
                                    else
                                    {
                                        SelectedObject = item;

                                        if (item.Graphic == HeldItem.Graphic && HeldItem is IDynamicItem dyn1 && TileData.IsStackable((long)dyn1.ItemData.Flags))
                                            MergeHeldItem(item);
                                        else
                                            DropHeldItemToWorld(obj.Position.X, obj.Position.Y, (sbyte)(obj.Position.Z + dyn.ItemData.Height));
                                    }
                                }
                                else
                                {
                                    DropHeldItemToWorld(obj.Position.X, obj.Position.Y, (sbyte)(obj.Position.Z + dyn.ItemData.Height));
                                }

                                break;
                            case Tile _:
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

                                _queuedAction = () => GameActions.SingleClick(entity);
                            }

                            break;
                    }
                }
            }
        }

        private bool OnLeftMouseDoubleClick()
        {
            bool result = false;

            if (IsMouseOverWorld)
            {
                GameObject obj = _mousePicker.MouseOverObject;
                Point point = _mousePicker.MouseOverObjectPoint;

                switch (obj)
                {
                    case Item item:
                        result = true;
                        GameActions.DoubleClick(item);

                        break;
                    //TODO: attack request also
                    case Mobile mob when World.Player.InWarMode:
                        result = true;
                        break;
                    case Mobile mob:
                        result = true;
                        GameActions.DoubleClick(mob);

                        break;
                }

                ClearDequeued();
            }

            return result;
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

        private bool OnRightMouseDoubleClick()
        {
            if (IsMouseOverWorld)
            {
                if (_settings.EnablePathfind && !Pathfinder.AutoWalking)
                {
                    if (_mousePicker.MouseOverObject is Tile || _mousePicker.MouseOverObject is IDynamicItem dyn && TileData.IsSurface((long)dyn.ItemData.Flags))
                    {
                        GameObject obj = _mousePicker.MouseOverObject;

                        if (Pathfinder.WalkTo(obj.Position.X, obj.Position.Y, obj.Position.Z, 0))
                        {
                            World.Player.AddGameText(MessageType.Label, "Pathfinding!", 3, 0, false);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void OnMouseDragging(object sender, EventArgs e)
        {
            if (Mouse.LButtonPressed && !IsHoldingItem && IsMouseOverWorld)
            {
                GameObject obj = _mousePicker.MouseOverObject;

                switch (obj)
                {
                    case Mobile mobile:

                        // get the lifebar
                        break;
                    case Item item:
                        PickupItemBegin(item, _dragOffset.X, _dragOffset.Y);

                        break;
                }
            }
        }


        private void OnKeyDown(object sender, SDL.SDL_KeyboardEvent e)
        {
            if (TargetSystem.IsTargeting && e.keysym.sym == SDL.SDL_Keycode.SDLK_ESCAPE && e.keysym.mod == SDL.SDL_Keymod.KMOD_NONE)
            {
                TargetSystem.SetTargeting(TargetType.Nothing, 0 ,0);
            }
        }

        private void OnKeyUp(object sender, SDL.SDL_KeyboardEvent e)
        {

        }
    }
}