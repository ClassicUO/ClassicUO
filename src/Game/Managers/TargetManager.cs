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

using ClassicUO.Game.GameObjects;
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
        SetTargetClientSide = 3
    }

    public enum TargetType
    {
        Neutral,
        Harmful,
        Helpful,
        Cancel
    }
    internal class MultiTargetInfo
    {
        public ushort XOff, YOff, ZOff, Model;
        public Position Offset => new Position(XOff,YOff,(sbyte)ZOff);

        public MultiTargetInfo(ushort model, ushort x, ushort y, ushort z)
        {
            Model = model;
            XOff = x;
            YOff = y;
            ZOff = z;
        }
    }
    internal static class TargetManager
    {
        private static Serial _targetCursorId;
        private static TargetType _targetCursorType;
        private static MultiTargetInfo _multiModel;

        public static MultiTargetInfo MultiTargetInfo => _multiModel;
        
        public static CursorTarget TargetingState { get; private set; } = CursorTarget.Invalid;

        public static Serial LastGameObject { get; set; }

        public static bool IsTargeting { get; private set; }

        public static TargetType TargeringType => _targetCursorType;

        public static void ClearTargetingWithoutTargetCancelPacket()
        {
            if (TargetingState == CursorTarget.MultiPlacement)
            {
                World.HouseManager.Remove(Serial.INVALID);
            }
            IsTargeting = false;
        }

        private static Action<Serial, Graphic, ushort, ushort, sbyte, bool> _enqueuedAction;

        public static void SetTargeting(CursorTarget targeting, Serial cursorID, TargetType cursorType)
        {
            if (targeting == CursorTarget.Invalid)
                throw new Exception("Invalid target type");

            TargetingState = targeting;
            _targetCursorId = cursorID;
            _targetCursorType = cursorType;
            IsTargeting = cursorType < TargetType.Cancel;

            if (IsTargeting)
            {
                Engine.UI.RemoveTargetLineGump(LastGameObject);
            }
        }

        public static void EnqueueAction(Action<Serial, Graphic, ushort, ushort, sbyte, bool> action)
        {
            _enqueuedAction = action;
        }

        public static void CancelTarget()
        {
            if (TargetingState == CursorTarget.MultiPlacement)
            {
                World.HouseManager.Remove(Serial.INVALID);
            }
            GameActions.TargetCancel(TargetingState, _targetCursorId, (byte)_targetCursorType);
            IsTargeting = false;
            
        }

        public static void SetTargetingMulti(Serial deedSerial, ushort model, ushort x, ushort y, ushort z)
        {
            SetTargeting(CursorTarget.MultiPlacement, deedSerial, TargetType.Neutral);
            _multiModel = new MultiTargetInfo(model,x,y,z);
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

            GameActions.TargetXYZ(selectedEntity.Position.X, selectedEntity.Position.Y, z, modelNumber, _targetCursorId, (byte)_targetCursorType);
            ClearTargetingWithoutTargetCancelPacket();
        }

        public static void TargetGameObject(GameObject selectedEntity)
        {
            if (selectedEntity == null || !IsTargeting)
                return;

            if (selectedEntity is GameEffect effect && effect.Source != null)
            {
                selectedEntity = effect.Source;
            }
            else if (selectedEntity is TextOverhead overhead && overhead.Parent != null)
                selectedEntity = overhead.Parent;

            if (selectedEntity is Entity entity)
            {
                if (selectedEntity != World.Player)
                {
                    Engine.UI.RemoveTargetLineGump(World.LastAttack);
                    Engine.UI.RemoveTargetLineGump(LastGameObject);
                    LastGameObject = entity.Serial;
                }

                if (_enqueuedAction != null)
                {
                    _enqueuedAction(entity.Serial, entity.Graphic, entity.X, entity.Y, entity.Z, entity is Item it && it.OnGround || entity.Serial.IsMobile);
                }
                else
                    GameActions.TargetObject(entity, _targetCursorId, (byte)_targetCursorType);
                Mouse.CancelDoubleClick = true;
            }
            else
            {
                Graphic modelNumber = 0;
                short z = selectedEntity.Position.Z;

                if (selectedEntity is Static st)
                {
                    modelNumber = st.OriginalGraphic;

                    if (st.ItemData.IsSurface)
                        z += st.ItemData.Height;
                }
                else if (selectedEntity is Multi m)
                {
                    modelNumber = m.Graphic;

                    if (m.ItemData.IsSurface)
                        z += m.ItemData.Height;
                }

                GameActions.TargetXYZ(selectedEntity.Position.X, selectedEntity.Position.Y, z, modelNumber, _targetCursorId, (byte)_targetCursorType);
                Mouse.CancelDoubleClick = true;
            }

            ClearTargetingWithoutTargetCancelPacket();
        }
    }
}