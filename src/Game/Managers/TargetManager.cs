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

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
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
        SetGrabBag
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
        public ushort XOff, YOff, ZOff, Model;

        public MultiTargetInfo(ushort model, ushort x, ushort y, ushort z)
        {
            Model = model;
            XOff = x;
            YOff = y;
            ZOff = z;
        }

        public Position Offset => new Position(XOff, YOff, (sbyte) ZOff);
    }

    internal static class TargetManager
    {
        private static Serial _targetCursorId;

        private static Action<Serial, Graphic, ushort, ushort, sbyte, bool> _enqueuedAction;

        public static MultiTargetInfo MultiTargetInfo { get; private set; }

        public static CursorTarget TargetingState { get; private set; } = CursorTarget.Invalid;

        public static Serial LastTarget { get; set; }

        public static Serial LastAttack { get; set; }


        public static bool IsTargeting { get; private set; }

        public static TargetType TargeringType { get; private set; }

        public static void ClearTargetingWithoutTargetCancelPacket()
        {
            if (TargetingState == CursorTarget.MultiPlacement) World.HouseManager.Remove(Serial.INVALID);
            IsTargeting = false;
        }

        public static void SetTargeting(CursorTarget targeting, Serial cursorID, TargetType cursorType)
        {
            if (targeting == CursorTarget.Invalid)
                return;

            TargetingState = targeting;
            _targetCursorId = cursorID;
            TargeringType = cursorType;
            IsTargeting = cursorType < TargetType.Cancel;

            if (IsTargeting)
                Engine.UI.RemoveTargetLineGump(LastTarget);
        }

        public static void EnqueueAction(Action<Serial, Graphic, ushort, ushort, sbyte, bool> action)
        {
            _enqueuedAction = action;
        }

        public static void CancelTarget()
        {
            if (TargetingState == CursorTarget.MultiPlacement) World.HouseManager.Remove(Serial.INVALID);
            NetClient.Socket.Send(new PTargetCancel(TargetingState, _targetCursorId, (byte) TargeringType));
            IsTargeting = false;
        }

        public static void SetTargetingMulti(Serial deedSerial, ushort model, ushort x, ushort y, ushort z)
        {
            SetTargeting(CursorTarget.MultiPlacement, deedSerial, TargetType.Neutral);
            MultiTargetInfo = new MultiTargetInfo(model, x, y, z);
        }

        private static void TargetXYZ(GameObject selectedEntity)
        {
            Graphic modelNumber = 0;
            short z = selectedEntity.Position.Z;

            if (selectedEntity is Static st)
            {
                modelNumber = selectedEntity.Graphic;
                z += st.ItemData.Height;
            }

            NetClient.Socket.Send(new PTargetXYZ(selectedEntity.X, selectedEntity.Y, z, modelNumber, _targetCursorId, (byte) TargeringType));
            ClearTargetingWithoutTargetCancelPacket();
        }

        public static void TargetGameObject(IGameEntity selectedEntity)
        {
            if (selectedEntity == null || !IsTargeting)
                return;

            if (selectedEntity is GameEffect effect && effect.Source != null)
                selectedEntity = effect.Source;
            else if (selectedEntity is MessageInfo overhead && overhead.Parent.Parent != null)
                selectedEntity = overhead.Parent.Parent;

            if (TargetingState == CursorTarget.SetGrabBag)
            {
                if (selectedEntity is Item item)
                {
                    Engine.Profile.Current.GrabBagSerial = item.Serial;
                    GameActions.Print($"Grab Bag set: {item.Serial}");
                }
                ClearTargetingWithoutTargetCancelPacket();
                return;
            }
            
            if (TargetingState == CursorTarget.Grab)
            {
                if (selectedEntity is Item item)
                {
                    GameActions.PickUp(item,item.Amount);
                    var bag = Engine.Profile.Current.GrabBagSerial == 0
                        ? World.Player.Equipment[(int) Layer.Backpack].Serial
                        : (Serial) Engine.Profile.Current.GrabBagSerial;
                    if (!World.Items.Contains(bag))
                    {
                        GameActions.Print($"Grab Bag not found, setting to Backpack.");
                        Engine.Profile.Current.GrabBagSerial = 0;
                        bag = World.Player.Equipment[(int) Layer.Backpack].Serial;
                    }
                    GameActions.DropItem(item.Serial,0,0,0,bag);
                }
                ClearTargetingWithoutTargetCancelPacket();
                return;
            }
            
            if (selectedEntity is Entity entity)
            {
                if (selectedEntity != World.Player)
                {
                    Engine.UI.RemoveTargetLineGump(TargetManager.LastAttack);
                    Engine.UI.RemoveTargetLineGump(LastTarget);
                    LastTarget = entity.Serial;
                }

                if (_enqueuedAction != null)
                    _enqueuedAction(entity.Serial, entity.Graphic, entity.X, entity.Y, entity.Z, entity is Item it && it.OnGround || entity.Serial.IsMobile);
                else
                {
                    if (Engine.Profile.Current.EnabledCriminalActionQuery && TargeringType == TargetType.Harmful)
                    {
                        Mobile m = World.Mobiles.Get(entity);

                        if (m != null && (World.Player.NotorietyFlag == NotorietyFlag.Innocent || World.Player.NotorietyFlag == NotorietyFlag.Ally) && m.NotorietyFlag == NotorietyFlag.Innocent && m != World.Player)
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

                            Engine.UI.Add(messageBox);

                            return;
                        }
                    }

                    NetClient.Socket.Send(new PTargetObject(entity, entity.Graphic, entity.X, entity.Y, entity.Z, _targetCursorId, (byte) TargeringType));
                    ClearTargetingWithoutTargetCancelPacket();
                }

                Mouse.CancelDoubleClick = true;
            }
            else if (selectedEntity is GameObject gobj)
            {
                Graphic modelNumber = 0;
                short z = gobj.Position.Z;

                if (gobj is Static st)
                {
                    modelNumber = st.OriginalGraphic;

                    if (st.ItemData.IsSurface)
                        z += st.ItemData.Height;
                }
                else if (gobj is Multi m)
                {
                    modelNumber = m.Graphic;

                    if (m.ItemData.IsSurface)
                        z += m.ItemData.Height;
                }

                NetClient.Socket.Send(new PTargetXYZ(gobj.X, gobj.Y, z, modelNumber, _targetCursorId, (byte) TargeringType));
                Mouse.CancelDoubleClick = true;
                ClearTargetingWithoutTargetCancelPacket();
            }
        }
    }
}