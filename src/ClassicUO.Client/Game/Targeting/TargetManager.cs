// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Targeting;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Network;
using ClassicUO.Resources;
using ClassicUO.Utility;
using System;

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
        IgnorePlayerTarget,
        CallbackTarget
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

    /// <summary>
    /// Facade over the three Targeting collaborators: keeps the existing public
    /// surface (<c>_world.TargetManager.X</c>) and owns the EventSink
    /// subscriptions. Cursor state, attack tracking and multi-placement
    /// translation live in dedicated cohesive classes under
    /// <see cref="Game.Targeting"/>.
    /// </summary>
    internal sealed class TargetManager : IEventListener
    {
        private readonly World _world;
        private readonly ITargetCursorState _cursor;
        private readonly IAttackTargetTracker _attack;
        private readonly IMultiPlacementCoordinator _multi;
        private readonly byte[] _lastDataBuffer = new byte[19];


        /// <summary>Production composition root. Defaults to concrete collaborators.</summary>
        public TargetManager(World world)
            : this(world, new TargetCursorState(world), new AttackTargetTracker(world))
        {
        }

        /// <summary>Test / extension seam — inject cursor + attack collaborators.</summary>
        internal TargetManager(World world, ITargetCursorState cursor, IAttackTargetTracker attack)
            : this(world, cursor, attack, new MultiPlacementCoordinator(cursor))
        {
        }

        /// <summary>Full DI seam — inject all three collaborators.</summary>
        internal TargetManager(World world, ITargetCursorState cursor, IAttackTargetTracker attack, IMultiPlacementCoordinator multi)
        {
            _world = world;
            _cursor = cursor;
            _attack = attack;
            _multi = multi;
        }

        public void Subscribe()
        {
            EventSink.TargetCursorReceived += OnTargetCursorReceived;
            EventSink.AttackTargetChanged += OnAttackTargetChanged;
            EventSink.MultiPlacementReceived += OnMultiPlacementReceived;
        }

        public void Unsubscribe()
        {
            EventSink.TargetCursorReceived -= OnTargetCursorReceived;
            EventSink.AttackTargetChanged -= OnAttackTargetChanged;
            EventSink.MultiPlacementReceived -= OnMultiPlacementReceived;
        }

        // ---- Event handlers (delegate to collaborators) ----

        private void OnTargetCursorReceived(TargetCursorReceivedArgs e) => _cursor.OnTargetCursorReceived(e);
        private void OnAttackTargetChanged(AttackTargetChangedArgs e) => _attack.OnAttackTargetChanged(e);
        private void OnMultiPlacementReceived(MultiPlacementReceivedArgs e) => _multi.OnMultiPlacementReceived(e);

        // ---- Attack tracker facade ----
        public uint LastAttack
        {
            get => _attack.LastAttack;
            set => _attack.LastAttack = value;
        }

        // ---- Cursor state facade ----
        public CursorTarget TargetingState => _cursor.TargetingState;
        public TargetType TargetingType => _cursor.TargetingType;
        public bool IsTargeting => _cursor.IsTargeting;
        public MultiTargetInfo MultiTargetInfo => _cursor.MultiTargetInfo;

        public uint SelectedTarget, NewTargetSystemSerial;

        public readonly LastTargetInfo LastTargetInfo = new LastTargetInfo();


        public void Reset() => _cursor.ResetAll();

        public void SetTargeting(Action<GameObject> callback, uint cursorID, TargetType cursorType)
            => _cursor.SetTargeting(callback, cursorID, cursorType);

        public void SetTargeting(CursorTarget targeting, uint cursorID, TargetType cursorType)
            => _cursor.SetTargeting(targeting, cursorID, cursorType);

        public void SetTargetingMulti(uint deedSerial, ushort model, ushort x, ushort y, ushort z, ushort hue)
            => _cursor.SetTargetingMulti(deedSerial, model, x, y, z, hue);


        public void CancelTarget()
        {
            if (_cursor.TargetingState == CursorTarget.MultiPlacement)
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

            if (_cursor.TargetingState == CursorTarget.CallbackTarget)
            {
                _cursor.TargetCallback?.Invoke(null);
            }

            if (_cursor.IsTargeting || _cursor.TargetingType == TargetType.Cancel)
            {
                NetClient.Socket.Send_TargetCancel(_cursor.TargetingState, _cursor.TargetCursorId, (byte)_cursor.TargetingType);
                _cursor.IsTargeting = false;
            }

            Reset();
        }

        public void Target(uint serial)
        {
            if (!_cursor.IsTargeting)
            {
                return;
            }

            Entity entity = _world.InGame ? _world.Get(serial) : null;

            if (entity != null)
            {
                switch (_cursor.TargetingState)
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

                                if (_cursor.TargetingType == TargetType.Harmful && ProfileManager.CurrentProfile.EnabledCriminalActionQuery && mobile.NotorietyFlag == NotorietyFlag.Innocent)
                                {
                                    showCriminalQuery = true;
                                }
                                else if (_cursor.TargetingType == TargetType.Beneficial && ProfileManager.CurrentProfile.EnabledBeneficialCriminalActionQuery && (mobile.NotorietyFlag == NotorietyFlag.Criminal || mobile.NotorietyFlag == NotorietyFlag.Murderer || mobile.NotorietyFlag == NotorietyFlag.Gray))
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
                                                                                   _cursor.TargetCursorId,
                                                                                   (byte)_cursor.TargetingType);

                                                _cursor.ClearTargetingWithoutTargetCancelPacket();

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

                        if (_cursor.TargetingState != CursorTarget.SetTargetClientSide)
                        {
                            _lastDataBuffer[0] = 0x6C;

                            _lastDataBuffer[1] = 0x00;

                            _lastDataBuffer[2] = (byte)(_cursor.TargetCursorId >> 24);
                            _lastDataBuffer[3] = (byte)(_cursor.TargetCursorId >> 16);
                            _lastDataBuffer[4] = (byte)(_cursor.TargetCursorId >> 8);
                            _lastDataBuffer[5] = (byte)_cursor.TargetCursorId;

                            _lastDataBuffer[6] = (byte) _cursor.TargetingType;

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
                                                               _cursor.TargetCursorId,
                                                               (byte)_cursor.TargetingType);

                            if (SerialHelper.IsMobile(serial) && LastTargetInfo.Serial != serial)
                            {
                                GameActions.RequestMobileStatus(_world,serial);
                            }
                        }

                        _cursor.ClearTargetingWithoutTargetCancelPacket();

                        Mouse.CancelDoubleClick = true;

                        break;

                    case CursorTarget.Grab:

                        if (SerialHelper.IsItem(serial))
                        {
                            GameActions.GrabItem(_world, serial, ((Item) entity).Amount);
                        }

                        _cursor.ClearTargetingWithoutTargetCancelPacket();

                        return;

                    case CursorTarget.SetGrabBag:

                        if (SerialHelper.IsItem(serial))
                        {
                            ProfileManager.CurrentProfile.GrabBagSerial = serial;
                            GameActions.Print(_world, string.Format(ResGeneral.GrabBagSet0, serial));
                        }

                        _cursor.ClearTargetingWithoutTargetCancelPacket();

                        return;
                    case CursorTarget.IgnorePlayerTarget:
                        if (SelectedObject.Object is Entity pmEntity)
                        {
                            _world.IgnoreManager.AddIgnoredTarget(pmEntity);
                        }
                        CancelTarget();
                        return;
                    case CursorTarget.CallbackTarget:
                        _cursor.TargetCallback?.Invoke(entity);

                        _cursor.ClearTargetingWithoutTargetCancelPacket();
                        return;
                }
            }
        }

        public void Target(ushort graphic, ushort x, ushort y, short z, bool wet = false)
        {
            if (!_cursor.IsTargeting)
            {
                return;
            }

            switch (_cursor.TargetingState)
            {
                case CursorTarget.CallbackTarget:
                    GameObject candidate = _world.Map.GetTile(x, y);

                    while (candidate != null)
                    {
                        if (candidate.Graphic == graphic && candidate.Z == z)
                        {
                            _cursor.TargetCallback?.Invoke(candidate);
                            break;
                        }
                        candidate = candidate.TNext;
                    }

                    _cursor.ClearTargetingWithoutTargetCancelPacket();
                    return;
            }

            if (graphic == 0)
            {
                if (_cursor.TargetingState == CursorTarget.Object)
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
            _cursor.ClearMultiTargetInfo();
        }

        public void TargetLast()
        {
            if (!_cursor.IsTargeting)
            {
                return;
            }

            _lastDataBuffer[0] = 0x6C;
            _lastDataBuffer[1] = (byte) _cursor.TargetingState;
            _lastDataBuffer[2] = (byte) (_cursor.TargetCursorId >> 24);
            _lastDataBuffer[3] = (byte) (_cursor.TargetCursorId >> 16);
            _lastDataBuffer[4] = (byte) (_cursor.TargetCursorId >> 8);
            _lastDataBuffer[5] = (byte) _cursor.TargetCursorId;
            _lastDataBuffer[6] = (byte) _cursor.TargetingType;

            NetClient.Socket.Send(_lastDataBuffer);
            Mouse.CancelDoubleClick = true;
            _cursor.ClearTargetingWithoutTargetCancelPacket();
        }

        private void TargetPacket(ushort graphic, ushort x, ushort y, sbyte z)
        {
            if (!_cursor.IsTargeting)
            {
                return;
            }

            _lastDataBuffer[0] = 0x6C;

            _lastDataBuffer[1] = 0x01;

            _lastDataBuffer[2] = (byte)(_cursor.TargetCursorId >> 24);
            _lastDataBuffer[3] = (byte)(_cursor.TargetCursorId >> 16);
            _lastDataBuffer[4] = (byte)(_cursor.TargetCursorId >> 8);
            _lastDataBuffer[5] = (byte)_cursor.TargetCursorId;

            _lastDataBuffer[6] = (byte)_cursor.TargetingType;

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
                                            _cursor.TargetCursorId,
                                            (byte)_cursor.TargetingType);


            Mouse.CancelDoubleClick = true;
            _cursor.ClearTargetingWithoutTargetCancelPacket();
        }
    }
}
