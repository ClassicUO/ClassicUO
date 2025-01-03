// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Network;
using ClassicUO.Resources;
using ClassicUO.Utility;

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
        HueCommandTarget,
        IgnorePlayerTarget
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

    internal sealed class TargetManager
    {
        private uint _targetCursorId;
        private readonly World _world;
        private readonly byte[] _lastDataBuffer = new byte[19];


        public TargetManager(World world) { _world = world; }

        public uint LastAttack, SelectedTarget, NewTargetSystemSerial;

        public readonly LastTargetInfo LastTargetInfo = new LastTargetInfo();


        public MultiTargetInfo MultiTargetInfo { get; private set; }

        public CursorTarget TargetingState { get; private set; } = CursorTarget.Invalid;

        public bool IsTargeting { get; private set; }

        public TargetType TargetingType { get; private set; }

        private void ClearTargetingWithoutTargetCancelPacket()
        {
            if (TargetingState == CursorTarget.MultiPlacement)
            {
                MultiTargetInfo = null;
                TargetingState = 0;
                _world.HouseManager.Remove(0);
            }

            IsTargeting = false;
        }

        public void Reset()
        {
            ClearTargetingWithoutTargetCancelPacket();

            TargetingState = 0;
            _targetCursorId = 0;
            MultiTargetInfo = null;
            TargetingType = 0;
        }

        public void SetTargeting(CursorTarget targeting, uint cursorID, TargetType cursorType)
        {
            if (targeting == CursorTarget.Invalid)
            {
                return;
            }

            bool lastTargetting = IsTargeting;
            IsTargeting = cursorType < TargetType.Cancel;
            TargetingState = targeting;
            TargetingType = cursorType;

            if (IsTargeting)
            {
                //UIManager.RemoveTargetLineGump(LastTarget);
            }
            else if (lastTargetting)
            {
                CancelTarget();
            }

            // https://github.com/andreakarasho/ClassicUO/issues/1373
            // when receiving a cancellation target from the server we need
            // to send the last active cursorID, so update cursor data later

            _targetCursorId = cursorID;
        }


        public void CancelTarget()
        {
            if (TargetingState == CursorTarget.MultiPlacement)
            {
                _world.HouseManager.Remove(0);

                if (_world.CustomHouseManager != null)
                {
                    _world.CustomHouseManager.Erasing = false;
                    _world.CustomHouseManager.SeekTile = false;
                    _world.CustomHouseManager.SelectedGraphic = 0;
                    _world.CustomHouseManager.CombinedStair = false;

                    UIManager.GetGump<HouseCustomizationGump>()?.Update();
                }
            }

            if (IsTargeting || TargetingType == TargetType.Cancel)
            {
                NetClient.Socket.Send_TargetCancel(TargetingState, _targetCursorId, (byte)TargetingType);
                IsTargeting = false;
            }

            Reset();
        }

        public void SetTargetingMulti
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
            MultiTargetInfo = new MultiTargetInfo
            (
                model,
                x,
                y,
                z,
                hue
            );
        }

        public void Target(uint serial)
        {
            if (!IsTargeting)
            {
                return;
            }

            Entity entity = _world.InGame ? _world.Get(serial) : null;

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

                        if (entity != _world.Player)
                        {
                            LastTargetInfo.SetEntity(serial);
                        }

                        if (SerialHelper.IsMobile(serial) && serial != _world.Player && (_world.Player.NotorietyFlag == NotorietyFlag.Innocent || _world.Player.NotorietyFlag == NotorietyFlag.Ally))
                        {
                            Mobile mobile = entity as Mobile;

                            if (mobile != null)
                            {
                                bool showCriminalQuery = false;

                                if (TargetingType == TargetType.Harmful && ProfileManager.CurrentProfile.EnabledCriminalActionQuery && mobile.NotorietyFlag == NotorietyFlag.Innocent)
                                {
                                    showCriminalQuery = true;
                                }
                                else if (TargetingType == TargetType.Beneficial && ProfileManager.CurrentProfile.EnabledBeneficialCriminalActionQuery && (mobile.NotorietyFlag == NotorietyFlag.Criminal || mobile.NotorietyFlag == NotorietyFlag.Murderer || mobile.NotorietyFlag == NotorietyFlag.Gray))
                                {
                                    showCriminalQuery = true;
                                }

                                if (showCriminalQuery && UIManager.GetGump<QuestionGump>() == null)
                                {
                                    QuestionGump messageBox = new QuestionGump
                                    (
                                        _world,
                                        "This may flag\nyou criminal!",
                                        s =>
                                        {
                                            if (s)
                                            {
                                                NetClient.Socket.Send_TargetObject(entity,
                                                                                   entity.Graphic,
                                                                                   entity.X,
                                                                                   entity.Y,
                                                                                   entity.Z,
                                                                                   _targetCursorId,
                                                                                   (byte)TargetingType);

                                                ClearTargetingWithoutTargetCancelPacket();

                                                if (LastTargetInfo.Serial != serial)
                                                {
                                                    GameActions.RequestMobileStatus(_world, serial);
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
                            _lastDataBuffer[0] = 0x6C;

                            _lastDataBuffer[1] = 0x00;

                            _lastDataBuffer[2] = (byte)(_targetCursorId >> 24);
                            _lastDataBuffer[3] = (byte)(_targetCursorId >> 16);
                            _lastDataBuffer[4] = (byte)(_targetCursorId >> 8);
                            _lastDataBuffer[5] = (byte)_targetCursorId;

                            _lastDataBuffer[6] = (byte) TargetingType;

                            _lastDataBuffer[7] = (byte)(entity.Serial >> 24);
                            _lastDataBuffer[8] = (byte)(entity.Serial >> 16);
                            _lastDataBuffer[9] = (byte)(entity.Serial >> 8);
                            _lastDataBuffer[10] = (byte)entity.Serial;

                            _lastDataBuffer[11] = (byte)(entity.X >> 8);
                            _lastDataBuffer[12] = (byte)entity.X;

                            _lastDataBuffer[13] = (byte)(entity.Y >> 8);
                            _lastDataBuffer[14] = (byte)entity.Y;

                            _lastDataBuffer[15] = (byte)(entity.Z >> 8);
                            _lastDataBuffer[16] = (byte)entity.Z;

                            _lastDataBuffer[17] = (byte)(entity.Graphic >> 8);
                            _lastDataBuffer[18] = (byte)entity.Graphic;


                            NetClient.Socket.Send_TargetObject(entity,
                                                               entity.Graphic,
                                                               entity.X,
                                                               entity.Y,
                                                               entity.Z,
                                                               _targetCursorId,
                                                               (byte)TargetingType);

                            if (SerialHelper.IsMobile(serial) && LastTargetInfo.Serial != serial)
                            {
                                GameActions.RequestMobileStatus(_world,serial);
                            }
                        }

                        ClearTargetingWithoutTargetCancelPacket();

                        Mouse.CancelDoubleClick = true;

                        break;

                    case CursorTarget.Grab:

                        if (SerialHelper.IsItem(serial))
                        {
                            GameActions.GrabItem(_world, serial, ((Item) entity).Amount);
                        }

                        ClearTargetingWithoutTargetCancelPacket();

                        return;

                    case CursorTarget.SetGrabBag:

                        if (SerialHelper.IsItem(serial))
                        {
                            ProfileManager.CurrentProfile.GrabBagSerial = serial;
                            GameActions.Print(_world, string.Format(ResGeneral.GrabBagSet0, serial));
                        }

                        ClearTargetingWithoutTargetCancelPacket();

                        return;
                    case CursorTarget.IgnorePlayerTarget:
                        if (SelectedObject.Object is Entity pmEntity)
                        {
                            _world.IgnoreManager.AddIgnoredTarget(pmEntity);
                        }
                        CancelTarget();
                        return;
                }
            }
        }

        public void Target(ushort graphic, ushort x, ushort y, short z, bool wet = false)
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
                if (graphic >= Client.Game.UO.FileManager.TileData.StaticData.Length)
                {
                    return;
                }

                ref StaticTiles itemData = ref Client.Game.UO.FileManager.TileData.StaticData[graphic];

                if (Client.Game.UO.Version >= ClientVersion.CV_7090 && itemData.IsSurface)
                {
                    z += itemData.Height;
                }
            }

            LastTargetInfo.SetStatic(graphic, x, y, (sbyte) z);

            TargetPacket(graphic, x, y, (sbyte) z);
        }

        public void SendMultiTarget(ushort x, ushort y, sbyte z)
        {
            TargetPacket(0, x, y, z);
            MultiTargetInfo = null;
        }

        public void TargetLast()
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

            NetClient.Socket.Send(_lastDataBuffer);
            Mouse.CancelDoubleClick = true;
            ClearTargetingWithoutTargetCancelPacket();
        }

        private void TargetPacket(ushort graphic, ushort x, ushort y, sbyte z)
        {
            if (!IsTargeting)
            {
                return;
            }

            _lastDataBuffer[0] = 0x6C;

            _lastDataBuffer[1] = 0x01;

            _lastDataBuffer[2] = (byte)(_targetCursorId >> 24);
            _lastDataBuffer[3] = (byte)(_targetCursorId >> 16);
            _lastDataBuffer[4] = (byte)(_targetCursorId >> 8);
            _lastDataBuffer[5] = (byte)_targetCursorId;

            _lastDataBuffer[6] = (byte)TargetingType;

            _lastDataBuffer[7] = (byte)(0 >> 24);
            _lastDataBuffer[8] = (byte)(0 >> 16);
            _lastDataBuffer[9] = (byte)(0 >> 8);
            _lastDataBuffer[10] = (byte)0;

            _lastDataBuffer[11] = (byte)(x >> 8);
            _lastDataBuffer[12] = (byte)x;

            _lastDataBuffer[13] = (byte)(y >> 8);
            _lastDataBuffer[14] = (byte)y;

            _lastDataBuffer[15] = (byte)(z >> 8);
            _lastDataBuffer[16] = (byte)z;

            _lastDataBuffer[17] = (byte)(graphic >> 8);
            _lastDataBuffer[18] = (byte)graphic;



            NetClient.Socket.Send_TargetXYZ(graphic,
                                            x,
                                            y,
                                            z,
                                            _targetCursorId,
                                            (byte)TargetingType);


            Mouse.CancelDoubleClick = true;
            ClearTargetingWithoutTargetCancelPacket();
        }
    }
}