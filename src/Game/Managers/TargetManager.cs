﻿#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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

using ClassicUO.Configuration;
using ClassicUO.Data;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Network;

namespace ClassicUO.Game.Managers
{
    enum CursorTarget
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

    enum TargetType
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

        private static byte[] _lastDataBuffer = new byte[19];


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
                    World.CustomHouseManager.CombinedStair = false;

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

                        var packet = new PTargetObject(entity, entity.Graphic, entity.X, entity.Y, entity.Z, _targetCursorId, (byte) TargeringType);
                       
                        for (int i = 0; i < _lastDataBuffer.Length; i++)
                        {
                            _lastDataBuffer[i] = packet[i];
                        }

                        NetClient.Socket.Send(packet);
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
            if (!IsTargeting)
                return;

            if (graphic == 0)
            {
                if (TargeringType != TargetType.Neutral)
                    return;
            }
            else
            {
                if (graphic >= TileDataLoader.Instance.StaticData.Length)
                    return;

                ref readonly var itemData = ref TileDataLoader.Instance.StaticData[graphic];

                if (Client.Version >= ClientVersion.CV_7090 && itemData.IsSurface)
                {
                    z += itemData.Height;
                }
            }

            TargetPacket(graphic, x, y, (sbyte) z);
        }

        public static void SendMultiTarget(ushort x, ushort y, sbyte z)
        {
            TargetPacket(0, x, y, z);
            MultiTargetInfo = null;
        }

        public static void TargetLast()
        {
            if (!IsTargeting)
                return;

            _lastDataBuffer[0] = 0x6C;
            _lastDataBuffer[1] = (byte) TargetingState;
            _lastDataBuffer[2] = (byte) (_targetCursorId >> 24);
            _lastDataBuffer[3] = (byte) (_targetCursorId >> 16);
            _lastDataBuffer[4] = (byte) (_targetCursorId >> 8);
            _lastDataBuffer[5] = (byte) _targetCursorId;
            _lastDataBuffer[6] = (byte) TargeringType;

            NetClient.Socket.Send(_lastDataBuffer);
            Mouse.CancelDoubleClick = true;
            ClearTargetingWithoutTargetCancelPacket();
        }

        private static void TargetPacket(ushort graphic, ushort x, ushort y, sbyte z)
        {
            if (!IsTargeting)
                return;

            var packet = new PTargetXYZ(x, y, z, graphic, _targetCursorId, (byte) TargeringType);       
            NetClient.Socket.Send(packet);
            for (int i = 0; i < _lastDataBuffer.Length; i++)
            {
                _lastDataBuffer[i] = packet[i];
            }

            Mouse.CancelDoubleClick = true;
            ClearTargetingWithoutTargetCancelPacket();
        }
    }
}
