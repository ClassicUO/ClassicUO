#region license

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
// ## BEGIN - END ## //
using ClassicUO.Game.InteropServices.Runtime.UOClassicCombat;
// ## BEGIN - END ## //
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Network;
using ClassicUO.Resources;

namespace ClassicUO.Game.Managers
{
    internal enum CursorTarget
    {
        Invalid = -1,
        Object = 0,
        Position = 1,
        MultiPlacement = 2,
        SetTargetClientSide = 3,
        Grab,
        SetGrabBag,
        // ## BEGIN - END ## // 
        SetCustomSerial,
        // ## BEGIN - END ## //
        HueCommandTarget
    }

    internal class CursorType
    {
        public static readonly uint Target = 6983686;
    }

    internal enum TargetType
    {
        Neutral,
        Harmful,
        Beneficial,
        Cancel
    }

    internal class MultiTargetInfo
    {
        public MultiTargetInfo(ushort model, ushort x, ushort y, ushort z, ushort hue)
        {
            Model = model;
            XOff = x;
            YOff = y;
            ZOff = z;
            Hue = hue;
        }

        public readonly ushort XOff, YOff, ZOff, Model, Hue;
    }

    internal class LastTargetInfo
    {
        public bool IsEntity => SerialHelper.IsValid(Serial);
        public bool IsStatic => !IsEntity && Graphic != 0 && Graphic != 0xFFFF;
        public bool IsLand => !IsStatic;
        public ushort Graphic;
        public uint Serial;
        public ushort X, Y;
        public sbyte Z;


        public void SetEntity(uint serial)
        {
            Serial = serial;
            Graphic = 0xFFFF;
            X = Y = 0xFFFF;
            Z = sbyte.MinValue;
        }

        public void SetStatic(ushort graphic, ushort x, ushort y, sbyte z)
        {
            Serial = 0;
            Graphic = graphic;
            X = x;
            Y = y;
            Z = z;
        }

        public void SetLand(ushort x, ushort y, sbyte z)
        {
            Serial = 0;
            Graphic = 0xFFFF;
            X = x;
            Y = y;
            Z = z;
        }

        public void Clear()
        {
            Serial = 0;
            Graphic = 0xFFFF;
            X = Y = 0xFFFF;
            Z = sbyte.MinValue;
        }
    }

    internal static class TargetManager
    {
        private static uint _targetCursorId;
        private static readonly byte[] _lastDataBuffer = new byte[19];

        public static uint LastAttack, SelectedTarget;

        public static readonly LastTargetInfo LastTargetInfo = new LastTargetInfo();


        public static MultiTargetInfo MultiTargetInfo { get; private set; }

        public static CursorTarget TargetingState { get; private set; } = CursorTarget.Invalid;

        public static bool IsTargeting { get; private set; }

        public static TargetType TargetingType { get; private set; }

        private static void ClearTargetingWithoutTargetCancelPacket()
        {
            if (TargetingState == CursorTarget.MultiPlacement)
            {
                MultiTargetInfo = null;
                TargetingState = 0;
                World.HouseManager.Remove(0);
            }

            IsTargeting = false;

            // ## BEGIN - END ## //
            GameActions.LastSpellIndexCursor = 0;
            GameCursor._spellTime = 0;
            // ## BEGIN - END ## //
        }

        public static void Reset()
        {
            ClearTargetingWithoutTargetCancelPacket();

            TargetingState = 0;
            _targetCursorId = 0;
            MultiTargetInfo = null;
            TargetingType = 0;
        }

        public static void SetTargeting(CursorTarget targeting, uint cursorID, TargetType cursorType)
        {
            if (targeting == CursorTarget.Invalid)
            {
                return;
            }

            TargetingState = targeting;
            _targetCursorId = cursorID;
            TargetingType = cursorType;

            // ## BEGIN - END ## //
            UOClassicCombatCollection.StartSpelltime();
            // ## BEGIN - END ## //

            bool lastTargetting = IsTargeting;
            IsTargeting = cursorType < TargetType.Cancel;

            if (IsTargeting)
            {
                //UIManager.RemoveTargetLineGump(LastTarget);
            }
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

            NetClient.Socket.Send(new PTargetCancel(TargetingState, _targetCursorId, (byte) TargetingType));
            IsTargeting = false;

            // ## BEGIN - END ## //
            GameActions.LastSpellIndexCursor = 0;
            GameCursor._spellTime = 0;
            // ## BEGIN - END ## //

            Reset();
        }

        public static void SetTargetingMulti
        (
            uint deedSerial,
            ushort model,
            ushort x,
            ushort y,
            ushort z,
            ushort hue
        )
        {
            SetTargeting(CursorTarget.MultiPlacement, deedSerial, TargetType.Neutral);

            //if (model != 0)
            MultiTargetInfo = new MultiTargetInfo(model, x, y, z, hue);
        }


        public static void Target(uint serial)
        {
            if (!IsTargeting)
            {
                return;
            }

            Entity entity = World.InGame ? World.Get(serial) : null;

            if (entity != null)
            {
                switch (TargetingState)
                {
                    case CursorTarget.Invalid: return;

                    case CursorTarget.MultiPlacement:
                    case CursorTarget.Position:
                    case CursorTarget.Object:
                    case CursorTarget.HueCommandTarget:
                    case CursorTarget.SetTargetClientSide:

                        if (entity != World.Player)
                        {
                            LastTargetInfo.SetEntity(serial);
                        }

                        if (SerialHelper.IsMobile
                                (serial) && serial != World.Player &&
                            (World.Player.NotorietyFlag == NotorietyFlag.Innocent ||
                             World.Player.NotorietyFlag == NotorietyFlag.Ally))
                        {
                            Mobile mobile = entity as Mobile;

                            if (mobile != null)
                            {
                                bool showCriminalQuery = false;

                                if (TargetingType == TargetType.Harmful &&
                                    ProfileManager.CurrentProfile.EnabledCriminalActionQuery &&
                                    mobile.NotorietyFlag == NotorietyFlag.Innocent)
                                {
                                    showCriminalQuery = true;
                                }
                                else if (TargetingType == TargetType.Beneficial &&
                                         ProfileManager.CurrentProfile.EnabledBeneficialCriminalActionQuery &&
                                         (mobile.NotorietyFlag == NotorietyFlag.Criminal ||
                                          mobile.NotorietyFlag == NotorietyFlag.Murderer ||
                                          mobile.NotorietyFlag == NotorietyFlag.Gray))
                                {
                                    showCriminalQuery = true;
                                }

                                if (showCriminalQuery && UIManager.GetGump<QuestionGump>() == null)
                                {
                                    QuestionGump messageBox = new QuestionGump
                                    (
                                        "This may flag\nyou criminal!", s =>
                                        {
                                            if (s)
                                            {
                                                NetClient.Socket.Send
                                                (
                                                    new PTargetObject
                                                    (
                                                        entity, entity.Graphic, entity.X, entity.Y, entity.Z,
                                                        _targetCursorId, (byte) TargetingType
                                                    )
                                                );

                                                ClearTargetingWithoutTargetCancelPacket();

                                                if (LastTargetInfo.Serial != serial)
                                                {
                                                    GameActions.RequestMobileStatus(serial);
                                                }
                                            }
                                        }
                                    );

                                    UIManager.Add(messageBox);

                                    return;
                                }
                            }
                        }

                        if (TargetingState != CursorTarget.SetTargetClientSide)
                        {
                            PTargetObject packet = new PTargetObject
                            (
                                entity, entity.Graphic, entity.X, entity.Y, entity.Z, _targetCursorId,
                                (byte) TargetingType
                            );

                            for (int i = 0; i < _lastDataBuffer.Length; i++)
                            {
                                _lastDataBuffer[i] = packet[i];
                            }

                            NetClient.Socket.Send(packet);

                            if (SerialHelper.IsMobile(serial) && LastTargetInfo.Serial != serial)
                            {
                                GameActions.RequestMobileStatus(serial);
                            }
                        }

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
                            ProfileManager.CurrentProfile.GrabBagSerial = serial;
                            GameActions.Print(string.Format(ResGeneral.GrabBagSet0, serial));
                        }

                        ClearTargetingWithoutTargetCancelPacket();

                        return;

                    // ## BEGIN - END ## //
                    case CursorTarget.SetCustomSerial:

                        if (SerialHelper.IsItem(serial))
                        {
                            ProfileManager.CurrentProfile.CustomSerial = serial;
                            GameActions.Print($"Custom UOClassicEquipment Item set: {serial}", 88);
                        }
                        else if ((TargetingType == TargetType.Neutral && SerialHelper.IsMobile(serial)))
                        {
                            Mobile mobile = entity as Mobile;

                            if ((!World.Player.IsDead && !mobile.IsDead) && serial != World.Player)
                            {
                                ProfileManager.CurrentProfile.Mimic_PlayerSerial = entity;//entity.serial
                                GameActions.Print($"Mimic Player Serial Set: {entity.Name} : {entity.Serial}", 88);//entity.serial
                            }
                        }

                        ClearTargetingWithoutTargetCancelPacket();

                        return;
                        // ## BEGIN - END ## //
                }
            }
        }

        public static void Target(ushort graphic, ushort x, ushort y, short z, bool wet = false)
        {
            if (!IsTargeting)
            {
                return;
            }

            if (graphic == 0)
            {
                if (TargetingState == CursorTarget.Object)
                {
                    return;
                }
            }
            else
            {
                if (graphic >= TileDataLoader.Instance.StaticData.Length)
                {
                    return;
                }

                ref StaticTiles itemData = ref TileDataLoader.Instance.StaticData[graphic];

                if (Client.Version >= ClientVersion.CV_7090 && itemData.IsSurface)
                {
                    z += itemData.Height;
                }
            }

            LastTargetInfo.SetStatic(graphic, x, y, (sbyte) z);

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
            {
                return;
            }

            _lastDataBuffer[0] = 0x6C;
            _lastDataBuffer[1] = (byte) TargetingState;
            _lastDataBuffer[2] = (byte) (_targetCursorId >> 24);
            _lastDataBuffer[3] = (byte) (_targetCursorId >> 16);
            _lastDataBuffer[4] = (byte) (_targetCursorId >> 8);
            _lastDataBuffer[5] = (byte) _targetCursorId;
            _lastDataBuffer[6] = (byte) TargetingType;

            NetClient.Socket.Send(_lastDataBuffer, _lastDataBuffer.Length);
            Mouse.CancelDoubleClick = true;
            ClearTargetingWithoutTargetCancelPacket();
        }

        private static void TargetPacket(ushort graphic, ushort x, ushort y, sbyte z)
        {
            if (!IsTargeting)
            {
                return;
            }

            PTargetXYZ packet = new PTargetXYZ(x, y, z, graphic, _targetCursorId, (byte) TargetingType);
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