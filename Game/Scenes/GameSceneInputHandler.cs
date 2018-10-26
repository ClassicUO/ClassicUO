using System.Runtime.Remoting.Lifetime;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Gumps.UIGumps;
using ClassicUO.Game.Map;
using ClassicUO.Input;
using ClassicUO.Interfaces;
using ClassicUO.IO.Resources;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

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

        public bool IsMouseOverUI => UIManager.IsMouseOverUI && !(UIManager.MouseOverControl is WorldViewport);

        public bool IsMouseOverWorld => UIManager.IsMouseOverUI && UIManager.MouseOverControl is WorldViewport;

        private void MoveCharacterByInputs()
        {
            if (World.InGame)
            {
                Point center = new Point(_settings.GameWindowX + _settings.GameWindowWidth / 2, _settings.GameWindowY + _settings.GameWindowHeight / 2);
                Direction direction = DirectionHelper.DirectionFromPoints(center, InputManager.MousePosition);
                World.Player.Walk(direction, true);
            }
        }

        private void EnqueueSingleClick(InputMouseEvent e, GameObject obj, Point point)
        {
            _inqueue = true;
            _queuedObject = obj;
            _queuedPosition = point;
            _dequeueAt = 200f;
            _queuedEvent = e;
        }

        private void CheckForQueuedClicks(double frameMS)
        {
            if (_inqueue)
            {
                _dequeueAt -= frameMS;

                if (_dequeueAt <= 0d)
                {
                    DoMouseButton(_queuedEvent, _queuedObject, _queuedPosition);
                    ClearQueuedClicks();
                }
            }
        }

        private void ClearQueuedClicks()
        {
            _inqueue = false;
            _queuedEvent = null;
            _queuedObject = null;
        }

        private void HandleMouseActions()
        {
            SelectedObject = null;

            if (IsHoldingItem)
            {
                if (IsMouseOverUI && InputManager.HandleMouseEvent(MouseEvent.Up, MouseButton.Left))
                {
                    GumpControl target = UIManager.MouseOverControl;

                    if (target is ItemGumpling gumpling && !(target is ItemGumplingPaperdoll))
                    {
                        Item item = gumpling.Item;
                        SelectedObject = item;

                        if (TileData.IsContainer((long) item.ItemData.Flags))
                        {
                            DropHeldItemToContainer(item);
                        }
                        else if (HeldItem.Graphic == item.Graphic && TileData.IsStackable((long) HeldItem.ItemData.Flags))
                        {
                            MergeHeldItem(item);
                        }
                        else
                        {
                            if (item.Container.IsItem)
                                DropHeldItemToContainer(World.Items.Get(item.Container), (ushort) (target.X + (InputManager.MousePosition.X - target.ScreenCoordinateX) - _heldOffset.X), (ushort) (target.Y + (InputManager.MousePosition.Y - target.ScreenCoordinateY) - _heldOffset.Y));
                        }
                    }
                    else if (target is GumpPicContainer container)
                    {
                        SelectedObject = container.Item;
                        int x = InputManager.MousePosition.X - _heldOffset.X - (target.X + target.Parent.X);
                        int y = InputManager.MousePosition.Y - _heldOffset.Y - (target.Y + target.Parent.Y);
                        DropHeldItemToContainer(container.Item, (ushort) x, (ushort) y);
                    }
                    else if (target is ItemGumplingPaperdoll || target is GumpPic pic && pic.IsPaperdoll || target is EquipmentSlot)
                    {
                        if (TileData.IsWearable((long) HeldItem.ItemData.Flags))
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

                    if (obj != null && obj.Distance < 5 && InputManager.HandleMouseEvent(MouseEvent.Up, MouseButton.Left))
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

                                        if (item.Graphic == HeldItem.Graphic && HeldItem is IDynamicItem dyn1 && TileData.IsStackable((long) dyn1.ItemData.Flags))
                                            MergeHeldItem(item);
                                        else
                                            DropHeldItemToWorld(obj.Position.X, obj.Position.Y, (sbyte) (obj.Position.Z + dyn.ItemData.Height));
                                    }
                                }
                                else
                                {
                                    DropHeldItemToWorld(obj.Position.X, obj.Position.Y, (sbyte) (obj.Position.Z + dyn.ItemData.Height));
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

            if (SelectedObject == null) SelectedObject = _mousePicker.MouseOverObject;
        }

        private void MouseHandler(double frameMS)
        {
            if (!IsMouseOverWorld)
            {
                if (_rightMousePressed)
                    _rightMousePressed = false;

                return;
            }

            foreach (InputMouseEvent e in InputManager.GetMouseEvents())
                switch (e.Button)
                {
                    case MouseButton.Right:
                        _rightMousePressed = e.EventType == MouseEvent.Down;
                        e.IsHandled = true;

                        if (e.EventType == MouseEvent.DoubleClick)
                        {
                            GameObject obj = null;

                            if (_mousePicker.MouseOverTile is Tile tile)
                            {
                                obj = tile;
                            }
                            else if (_mousePicker.MouseOverObject is IDynamicItem dyn && TileData.IsSurface((long)dyn.ItemData.Flags))
                            {
                                obj = _mousePicker.MouseOverObject;
                            }

                            if (obj != null)
                            {
                                if (Pathfinder.WalkTo(obj.Position.X, obj.Position.Y, obj.Position.Z, 0))
                                    World.Player.AddGameText(MessageType.Label, "Pathfinding!", 3, 0, false);
                            }

                        }

                        break;
                    case MouseButton.Left:

                        switch (e.EventType)
                        {
                            case MouseEvent.Click:
                                EnqueueSingleClick(e, _mousePicker.MouseOverObject, _mousePicker.MouseOverObjectPoint);

                                continue;
                            case MouseEvent.DoubleClick:
                                ClearQueuedClicks();

                                break;
                        }

                        DoMouseButton(e, _mousePicker.MouseOverObject, _mousePicker.MouseOverObjectPoint);

                        break;
                }
            CheckForQueuedClicks(frameMS);
        }

        private void DoMouseButton(InputMouseEvent e, GameObject obj, Point point)
        {
            switch (e.EventType)
            {
                case MouseEvent.Down:

                {
                    _dragginObject = obj;
                    _dragOffset = point;
                }

                    break;
                case MouseEvent.Click:

                {
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
                            GameActions.SingleClick(entity);

                            break;
                    }
                }

                    break;
                case MouseEvent.DoubleClick:

                {
                    switch (obj)
                    {
                        case Item item:
                            GameActions.DoubleClick(item);

                            break;
                        //TODO: attack request also
                        case Mobile mob when World.Player.InWarMode:

                            break;
                        case Mobile mob:
                            GameActions.DoubleClick(mob);

                            break;
                    }
                }

                    break;
                case MouseEvent.DragBegin:

                {
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

                    break;
            }

            e.IsHandled = true;
        }
    }
}