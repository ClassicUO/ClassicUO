using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;
using ClassicUO.Game.Gumps.UIGumps;
using ClassicUO.Game.Map;
using ClassicUO.Game.System;
using ClassicUO.Input;
using ClassicUO.Interfaces;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
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
        private GameObject _queuedObject;
        private bool _rightMousePressed;
        private PaperDollInteractable _lastFakeParedoll;

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
                        else if (IsMouseOverWorld) obj = SelectedObject;

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

                    if (_lastFakeParedoll != null)
                    {
                        _lastFakeParedoll.AddFakeDress(null);
                        _lastFakeParedoll.Update();
                        _lastFakeParedoll = null;
                    }

                    if (target is ItemGumpling gumpling && !(target is ItemGumplingPaperdoll))
                    {
                        Item item = gumpling.Item;
                        SelectedObject = item;

                        if (TileData.IsContainer((long)item.ItemData.Flags))
                            DropHeldItemToContainer(item);
                        else if (HeldItem.Graphic == item.Graphic && TileData.IsStackable((long)HeldItem.ItemData.Flags))
                            MergeHeldItem(item);
                        else
                        {
                            if (item.Container.IsItem)
                            {
                                SpriteTexture texture = Art.GetStaticTexture(item.Graphic);

                                DropHeldItemToContainer(World.Items.Get(item.Container), (ushort)(target.X + (Mouse.Position.X - target.ScreenCoordinateX) - texture.Width / 2), (ushort)(target.Y + (Mouse.Position.Y - target.ScreenCoordinateY) - texture.Height / 2));
                            }
                        }
                    }
                    else if (target is GumpPicContainer container)
                    {
                        SelectedObject = container.Item;

                        SpriteTexture texture = Art.GetStaticTexture(container.Item.Graphic);

                        int x = Mouse.Position.X - texture.Width / 2 - (target.X + target.Parent.X);
                        int y = Mouse.Position.Y - texture.Height / 2 - (target.Y + target.Parent.Y);
                        DropHeldItemToContainer(container.Item, (ushort)x, (ushort)y);
                    }
                    else if (target is GumpPicBackpack backpack)
                        DropHeldItemToContainer(backpack.BackpackItem);
                    else if (target is IMobilePaperdollOwner paperdollOwner)
                    {
                        if (TileData.IsWearable((long)HeldItem.ItemData.Flags))
                        {
                            WearHeldItem(paperdollOwner.Mobile);
                        }
                    }
                    else if (target.Parent is IMobilePaperdollOwner paperdollOwner1)
                    {
                        if (TileData.IsWearable((long)HeldItem.ItemData.Flags))
                        {
                            WearHeldItem(paperdollOwner1.Mobile);
                        }
                    }
                }
                else if (IsMouseOverWorld)
                {
                    GameObject obj = _mousePicker.MouseOverObject;

                    if (obj != null && obj.Distance < 5)
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

                                        if (item.Graphic == HeldItem.Graphic && HeldItem is IDynamicItem dyn1 && TileData.IsStackable((long)dyn1.ItemData.Flags))
                                            MergeHeldItem(item);
                                        else
                                            DropHeldItemToWorld(obj.Position.X, obj.Position.Y, (sbyte)(obj.Position.Z + dyn.ItemData.Height));
                                    }
                                }
                                else
                                    DropHeldItemToWorld(obj.Position.X, obj.Position.Y, (sbyte)(obj.Position.Z + dyn.ItemData.Height));

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
                    case Mobile mob:
                        result = true;

                        if (World.Player.InWarMode)
                        {
                            //TODO: attack request
                        }
                        else
                        {
                            GameActions.DoubleClick(mob);
                        }

                        break;
                    case GameEffect effect when effect.Source is Item item:
                        result = true;
                        GameActions.DoubleClick(item);
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
                    if (_mousePicker.MouseOverObject is Land || _mousePicker.MouseOverObject is IDynamicItem dyn && TileData.IsSurface((long)dyn.ItemData.Flags))
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


        public List<Mobile> MobileGumpStack = new List<Mobile>();
        public List<Mobile> PartyMemberGumpStack = new List<Mobile>();
        public List<Skill> SkillButtonGumpStack = new List<Skill>();


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
                            if (PartySystem.Members.Exists(x => x.Serial == member.Serial ))
                            {
                                //Checks if party member gump is already on sceen
                                if (PartyMemberGumpStack.Contains(mobile))
                                {
                                    UIManager.Remove<PartyMemberGump>(mobile);
                                }
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
                                {
                                    UIManager.Remove<MobileHealthGump>(mobile);
                                }
                                else if (mobile == World.Player)
                                {
                                    StatusGumpBase status = UIManager.GetByLocalSerial<StatusGumpBase>();
                                    status?.Dispose();
                                }

                                MobileHealthGump currentMobileHealthGump;
                                MobileGumpStack.Add(mobile);
                                UIManager.Add(currentMobileHealthGump = new MobileHealthGump(mobile, _mousePicker.Position.X, _mousePicker.Position.Y));

                                Rectangle rect = IO.Resources.Gumps.GetGumpTexture(0x0804).Bounds;

                                UIManager.AttemptDragControl(currentMobileHealthGump, new Point(_mousePicker.Position.X + rect.Width / 2, _mousePicker.Position.Y + rect.Height / 2), true);

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
            if (Mouse.LButtonPressed)
            {
                HandleMouseFakeItem();
            }
        }

        private void OnMouseMoving(object sender, EventArgs e)
        {
            HandleMouseFakeItem();
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


        private void HandleMouseFakeItem()
        {
            if (IsMouseOverUI)
            {
                if (IsHoldingItem)
                {
                    GumpControl target = UIManager.MouseOverControl;

                    if (target != null && TileData.IsWearable((long)HeldItem.ItemData.Flags))
                    {

                        PaperDollInteractable gumpling = null;

                        if (target is ItemGumplingPaperdoll)
                            gumpling = (PaperDollInteractable)target.Parent;
                        else if (target is GumpPic pic && pic.IsPaperdoll)
                        {
                            gumpling = (PaperDollInteractable)target.Parent;
                        }
                        else if (target is EquipmentSlot || target is PaperDollGump || target.Parent is PaperDollGump)
                        {
                            gumpling = target.Parent.FindControls<PaperDollInteractable>().FirstOrDefault();
                        }

                        if (gumpling != null)
                        {
                            if (_lastFakeParedoll != gumpling)
                            {
                                _lastFakeParedoll = gumpling;

                                gumpling.AddFakeDress(new Item(Serial.Invalid)
                                {
                                    Amount = 1,
                                    Graphic = HeldItem.Graphic,
                                    Hue = HeldItem.Hue
                                });
                                gumpling.Update();
                            }

                            return;
                        }


                    }
                }
            }

            if (_lastFakeParedoll != null)
            {
                _lastFakeParedoll.AddFakeDress(null);
                _lastFakeParedoll.Update();
                _lastFakeParedoll = null;
            }
        }
    }
}