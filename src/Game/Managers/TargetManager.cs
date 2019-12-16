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

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.Network;

namespace ClassicUO.Game.Managers
{
    public enum CursorTarget
    {
        Invalid = -1,
        Object = 0,
        Position = 1,
        MultiPlacement = 2,
        SetTargetClientSide = 3,
        Grab,
        SetGrabBag,
        HueCommandTarget
    }

    internal class CursorType
    {
        public static readonly Serial Target = new Serial(6983686);
    }

    public enum TargetType
    {
        Neutral,
        Harmful,
        Beneficial,
        Cancel
    }

    internal class MultiTargetInfo
    {
        public readonly ushort XOff, YOff, ZOff, Model, Hue;

        public MultiTargetInfo(ushort model, ushort x, ushort y, ushort z, ushort hue)
        {
            Model = model;
            XOff = x;
            YOff = y;
            ZOff = z;
            Hue = hue;
        }
    }

    internal static class TargetManager
    {
        private static Serial _targetCursorId;

        private static Action<Serial, ushort, ushort, ushort, sbyte, bool> _enqueuedAction;

        public static MultiTargetInfo MultiTargetInfo { get; private set; }

        public static CursorTarget TargetingState { get; private set; } = CursorTarget.Invalid;

        public static Serial LastTarget, LastAttack, SelectedTarget;

        public static bool IsTargeting { get; private set; }

        public static TargetType TargeringType { get; private set; }

        public static void ClearTargetingWithoutTargetCancelPacket()
        {
            if (TargetingState == CursorTarget.MultiPlacement) World.HouseManager.Remove(Serial.INVALID);
            IsTargeting = false;
        }

        public static void Reset()
        {
            ClearTargetingWithoutTargetCancelPacket();

            TargetingState = 0;
            _targetCursorId = 0;
            MultiTargetInfo = null;
            TargeringType = 0;
        }

        public static void SetTargeting(CursorTarget targeting, Serial cursorID, TargetType cursorType)
        {
            if (targeting == CursorTarget.Invalid)
                return;

            TargetingState = targeting;
            _targetCursorId = cursorID;
            TargeringType = cursorType;

            bool lastTargetting = IsTargeting;
            IsTargeting = cursorType < TargetType.Cancel;

            if (IsTargeting)
                UIManager.RemoveTargetLineGump(LastTarget);
            else if (lastTargetting)
            {
                CancelTarget();
            }
        }

        public static void EnqueueAction(Action<Serial, ushort, ushort, ushort, sbyte, bool> action)
        {
            _enqueuedAction = action;
        }

        public static void CancelTarget()
        {
            if (TargetingState == CursorTarget.MultiPlacement) World.HouseManager.Remove(Serial.INVALID);
            NetClient.Socket.Send(new PTargetCancel(TargetingState, _targetCursorId, (byte) TargeringType));
            IsTargeting = false;
        }

        public static void SetTargetingMulti(Serial deedSerial, ushort model, ushort x, ushort y, ushort z, ushort hue)
        {
            SetTargeting(CursorTarget.MultiPlacement, deedSerial, TargetType.Neutral);

            if (model != 0)
                MultiTargetInfo = new MultiTargetInfo(model, x, y, z, hue);
        }


        public static void Target(Serial serial)
        {
            if (!IsTargeting)
                return;

            Entity entity = World.InGame ? World.Get(serial) : null;

            if (entity != null)
            {
                switch (TargetingState)
                {
                    case CursorTarget.Invalid:                     
                        return;
                    case CursorTarget.MultiPlacement:
                    case CursorTarget.Position:
                    case CursorTarget.Object:
                    case CursorTarget.HueCommandTarget:
                    case CursorTarget.SetTargetClientSide:

                        if (entity != World.Player)
                        {
                            UIManager.RemoveTargetLineGump(LastAttack);
                            UIManager.RemoveTargetLineGump(LastTarget);
                            LastTarget = entity.Serial;
                        }

                        if (_enqueuedAction != null)
                            _enqueuedAction(entity.Serial, entity.Graphic, entity.X, entity.Y, entity.Z, entity is Item it && it.OnGround || entity.Serial.IsMobile);
                        else
                        {
                            if (TargeringType == TargetType.Harmful && serial.IsMobile &&                   
                                ProfileManager.Current.EnabledCriminalActionQuery)
                            {
                                Mobile mobile = entity as Mobile;

                                if (((World.Player.NotorietyFlag == NotorietyFlag.Innocent ||
                                    World.Player.NotorietyFlag == NotorietyFlag.Ally) && mobile.NotorietyFlag == NotorietyFlag.Innocent && serial != World.Player))
                                {
                                    QuestionGump messageBox = new QuestionGump("This may flag\nyou criminal!",
                                                                       s =>
                                                                       {
                                                                           if (s)
                                                                           {
                                                                               NetClient.Socket.Send(new PTargetObject(entity, entity.Graphic, entity.X, entity.Y, entity.Z, _targetCursorId, (byte) TargeringType));
                                                                               ClearTargetingWithoutTargetCancelPacket();
                                                                           }
                                                                       });

                                    UIManager.Add(messageBox);

                                    return;
                                }
                            }

                            NetClient.Socket.Send(new PTargetObject(entity, entity.Graphic, entity.X, entity.Y, entity.Z, _targetCursorId, (byte) TargeringType));
                            ClearTargetingWithoutTargetCancelPacket();
                        }
                        Mouse.CancelDoubleClick = true;
                        break;
                    case CursorTarget.Grab:

                        if (serial.IsItem)
                        {
                            GameActions.GrabItem(serial, ((Item) entity).Amount);
                        }

                        ClearTargetingWithoutTargetCancelPacket();

                        return;
                    case CursorTarget.SetGrabBag:

                        if (serial.IsItem)
                        {
                            ProfileManager.Current.GrabBagSerial = serial;
                            GameActions.Print($"Grab Bag set: {serial}");
                        }

                        ClearTargetingWithoutTargetCancelPacket();

                        return;
                }
            }
        }

        public static void Target(ushort graphic, ushort x, ushort y, short z)
        {
            if (!IsTargeting || TargeringType != TargetType.Neutral || graphic >= UOFileManager.TileData.StaticData.Length)
                return;

            ref readonly var itemData = ref UOFileManager.TileData.StaticData[graphic];

            if (UOFileManager.ClientVersion >= ClientVersions.CV_7090 && itemData.IsSurface)
            {
                z += itemData.Height;
            }

            NetClient.Socket.Send(new PTargetXYZ(x, y, z, graphic, _targetCursorId, (byte) TargeringType));
            Mouse.CancelDoubleClick = true;
            ClearTargetingWithoutTargetCancelPacket();
        }

        public static void Target(ushort x, ushort y, short z)
        {
            if (!IsTargeting || TargeringType != TargetType.Neutral)
                return;

            NetClient.Socket.Send(new PTargetXYZ(x, y, z, 0, _targetCursorId, (byte) TargeringType));
            Mouse.CancelDoubleClick = true;
            ClearTargetingWithoutTargetCancelPacket();
        }


        //public static void TargetGameObject(BaseGameObject selectedEntity)
        //{
        //    if (selectedEntity == null || !IsTargeting)
        //        return;

        //    if (selectedEntity is GameEffect effect && effect.Source != null)
        //        selectedEntity = effect.Source;
        //    else if (selectedEntity is TextOverhead overhead && overhead.Owner != null)
        //        selectedEntity = overhead.Owner;

        //    if (TargetingState == CursorTarget.SetGrabBag)
        //    {
        //        if (selectedEntity is Item item)
        //        {
        //            ProfileManager.Current.GrabBagSerial = item.Serial;
        //            GameActions.Print($"Grab Bag set: {item.Serial}");
        //        }

        //        ClearTargetingWithoutTargetCancelPacket();

        //        return;
        //    }

        //    if (TargetingState == CursorTarget.Grab)
        //    {
        //        if (selectedEntity is Item item)
        //        {
        //            GameActions.GrabItem(item,item.Amount);
        //        }

        //        ClearTargetingWithoutTargetCancelPacket();

        //        return;
        //    }

        //    if (selectedEntity is Entity entity)
        //    {
        //        if (selectedEntity != World.Player)
        //        {
        //            UIManager.RemoveTargetLineGump(LastAttack);
        //            UIManager.RemoveTargetLineGump(LastTarget);
        //            LastTarget = entity.Serial;
        //        }

        //        if (_enqueuedAction != null)
        //            _enqueuedAction(entity.Serial, entity.Graphic, entity.X, entity.Y, entity.Z, entity is Item it && it.OnGround || entity.Serial.IsMobile);
        //        else
        //        {
        //            if (ProfileManager.Current.EnabledCriminalActionQuery && TargeringType == TargetType.Harmful)
        //            {
        //                Mobile m = World.Mobiles.Get(entity);

        //                if (m != null && (World.Player.NotorietyFlag == NotorietyFlag.Innocent || World.Player.NotorietyFlag == NotorietyFlag.Ally) && m.NotorietyFlag == NotorietyFlag.Innocent && m != World.Player)
        //                {
        //                    QuestionGump messageBox = new QuestionGump("This may flag\nyou criminal!",
        //                                                               s =>
        //                                                               {
        //                                                                   if (s)
        //                                                                   {
        //                                                                       NetClient.Socket.Send(new PTargetObject(entity, entity.Graphic, entity.X, entity.Y, entity.Z, _targetCursorId, (byte) TargeringType));
        //                                                                       ClearTargetingWithoutTargetCancelPacket();
        //                                                                   }
        //                                                               });

        //                    UIManager.Add(messageBox);

        //                    return;
        //                }
        //            }

        //            NetClient.Socket.Send(new PTargetObject(entity, entity.Graphic, entity.X, entity.Y, entity.Z, _targetCursorId, (byte) TargeringType));
        //            ClearTargetingWithoutTargetCancelPacket();
        //        }

        //        Mouse.CancelDoubleClick = true;
        //    }
        //    else if (TargeringType == TargetType.Neutral && selectedEntity is GameObject gobj)
        //    {
        //        Graphic modelNumber = 0;
        //        short z = gobj.Z;

        //        if (gobj is Static st)
        //        {
        //            modelNumber = st.OriginalGraphic;

        //            if (FileManager.ClientVersion >= ClientVersions.CV_7090 && st.ItemData.IsSurface)
        //            {
        //                z += st.ItemData.Height;
        //            }
        //        }
        //        else if (gobj is Multi m)
        //        {
        //            modelNumber = m.Graphic;

        //            if (FileManager.ClientVersion >= ClientVersions.CV_7090 && m.ItemData.IsSurface)
        //            {
        //                z += m.ItemData.Height;
        //            }
        //        }

        //        NetClient.Socket.Send(new PTargetXYZ(gobj.X, gobj.Y, z, modelNumber, _targetCursorId, (byte) TargeringType));
        //        Mouse.CancelDoubleClick = true;
        //        ClearTargetingWithoutTargetCancelPacket();
        //    }
        //}

        public static void SendMultiTarget(ushort x, ushort y, sbyte z)
        {
            NetClient.Socket.Send(new PTargetXYZ(x, y, z, 0, _targetCursorId, (byte)TargeringType));
            Mouse.CancelDoubleClick = true;
            MultiTargetInfo = null;
            ClearTargetingWithoutTargetCancelPacket();
        }

        enum SCAN_TYPE_OBJECT
        {
            STO_HOSTILE = 0,
            STO_PARTY,
            STO_FOLLOWERS,
            STO_OBJECTS,
            STO_MOBILES
        }
        enum SCAN_MODE_OBJECT
        {
            SMO_NEXT = 0,
            SMO_PREV,
            SMO_NEAREST
        }

        public static bool IsMobileSelectableAsTarget(Serial serial, int type)
        {
            Mobile mobile = World.Mobiles.Get(serial);

            if (mobile == null)
                return false;

            if (mobile == World.Player)
                return false;

            if (Math.Abs(mobile.Z - World.Player.Z) >= 20)
                return false;

            if (mobile.Distance > 12)
                return false;

            if (type >= 0 && type != 4)
            {
                // 0 - Hostile (only hostile mobiles: gray, criminal, enemy, murderer)
                if (
                    type == 0 &&
                    mobile.NotorietyFlag != NotorietyFlag.Gray &&
                    mobile.NotorietyFlag != NotorietyFlag.Criminal &&
                    mobile.NotorietyFlag != NotorietyFlag.Enemy &&
                    mobile.NotorietyFlag != NotorietyFlag.Murderer
                )
                    return false;

                // 1 - Party (only party members)
                if (type == 1 && !World.Party.Contains(mobile))
                    return false;

                // 2 - Follower (only your followers)
                // TODO: Find a better way to determine follower instead of checking if it is "renamable"
                if (type == 2 && (mobile.NotorietyFlag != NotorietyFlag.Ally || !mobile.IsRenamable))
                    return false;

                // 3 - Object (no mobiles, only objects (items)?!)
                if (type == 3)
                    return false;

                // 4 - Mobile (any mobiles)
                // No need to check anything here
            }

            return true;
        }

        private static bool CanBeSelectedAsTarget(Serial serial, SCAN_TYPE_OBJECT scanType)
        {
            if (scanType == SCAN_TYPE_OBJECT.STO_OBJECTS)
            {
                if (serial.IsItem)
                {
                    return true;
                }
            }
            else
            {
                switch (scanType)
                {
                    case SCAN_TYPE_OBJECT.STO_HOSTILE: 
                        return true;
                    case SCAN_TYPE_OBJECT.STO_PARTY:
                        return World.Party.Contains(serial);
                    case SCAN_TYPE_OBJECT.STO_FOLLOWERS: 
                        break;
                    case SCAN_TYPE_OBJECT.STO_MOBILES: 
                        break;
                }
            }

            return false;
        }
    }
}
