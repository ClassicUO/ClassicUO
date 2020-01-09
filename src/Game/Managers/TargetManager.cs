#region license

//  Copyright (C) 2020 ClassicUO Development Community on Github
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
using ClassicUO.Data;
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
        public static readonly uint Target = 6983686;
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
        private static uint _targetCursorId;

        public static MultiTargetInfo MultiTargetInfo { get; private set; }

        public static CursorTarget TargetingState { get; private set; } = CursorTarget.Invalid;

        public static uint LastTarget, LastAttack, SelectedTarget;

        public static bool IsTargeting { get; private set; }

        public static TargetType TargeringType { get; private set; }

        public static void ClearTargetingWithoutTargetCancelPacket()
        {
            if (TargetingState == CursorTarget.MultiPlacement) World.HouseManager.Remove(0);
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

        public static void SetTargeting(CursorTarget targeting, uint cursorID, TargetType cursorType)
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


        public static void CancelTarget()
        {
            if (TargetingState == CursorTarget.MultiPlacement)
            {
                World.HouseManager.Remove(0);

                if (World.CustomHouseManager != null)
                {
                    World.CustomHouseManager.Erasing = false;
                    World.CustomHouseManager.SeekTile = false;
                    World.CustomHouseManager.SelectedGraphic = 0;

                    UIManager.GetGump<HouseCustomizationGump>()?.Update();
                }
            }
            NetClient.Socket.Send(new PTargetCancel(TargetingState, _targetCursorId, (byte) TargeringType));
            IsTargeting = false;
        }

        public static void SetTargetingMulti(uint deedSerial, ushort model, ushort x, ushort y, ushort z, ushort hue)
        {
            SetTargeting(CursorTarget.MultiPlacement, deedSerial, TargetType.Neutral);

            if (model != 0)
                MultiTargetInfo = new MultiTargetInfo(model, x, y, z, hue);
        }


        public static void Target(uint serial)
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

                        if (TargeringType == TargetType.Harmful && SerialHelper.IsMobile(serial) &&
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

                        Mouse.CancelDoubleClick = true;
                        break;
                    case CursorTarget.Grab:

                        if (SerialHelper.IsItem(serial))
                        {
                            GameActions.GrabItem(serial, ((Item) entity).Amount);
                        }

                        ClearTargetingWithoutTargetCancelPacket();

                        return;
                    case CursorTarget.SetGrabBag:

                        if (SerialHelper.IsItem(serial))
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

            if (Client.Version >= ClientVersion.CV_7090 && itemData.IsSurface)
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

        public static void SendMultiTarget(ushort x, ushort y, sbyte z)
        {
            NetClient.Socket.Send(new PTargetXYZ(x, y, z, 0, _targetCursorId, (byte)TargeringType));
            Mouse.CancelDoubleClick = true;
            MultiTargetInfo = null;
            ClearTargetingWithoutTargetCancelPacket();
        }
    }
}
