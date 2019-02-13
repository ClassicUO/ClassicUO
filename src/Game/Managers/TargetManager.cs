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
    public enum TargetType
    {
        Invalid = -1,
        Object = 0,
        Position = 1,
        MultiPlacement = 2,
        SetTargetClientSide = 3

    }

    internal static class TargetManager
    {
        private static Serial _targetCursorId;
        private static byte _targetCursorType;
        private static int _multiModel;
        
        public static TargetType TargetingState { get; private set; } = TargetType.Invalid;

        public static GameObject LastGameObject { get; set; }

        public static bool IsTargeting { get; private set; }

        public static void ClearTargetingWithoutTargetCancelPacket()
        {
            IsTargeting = false;
        }

        private static Action<Serial, Graphic, ushort, ushort, sbyte, bool> _enqueuedAction;

        public static void SetTargeting(TargetType targeting, Serial cursorID, byte cursorType)
        {
            if (targeting == TargetType.Invalid)
                throw new Exception("Invalid target type");

            TargetingState = targeting;
            _targetCursorId = cursorID;
            _targetCursorType = cursorType;
            IsTargeting = cursorType < 3;            
        }

        public static void EnqueueAction(Action<Serial, Graphic, ushort, ushort, sbyte, bool> action)
        {
            _enqueuedAction = action;
        }

        public static void CancelTarget()
        {
            GameActions.TargetCancel(TargetingState, _targetCursorId, _targetCursorType);
            IsTargeting = false;
        }

        public static void SetTargetingMulti(Serial deedSerial, int model, byte targetType)
        {
            SetTargeting(TargetType.MultiPlacement, deedSerial, targetType);
            _multiModel = model;
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

            GameActions.TargetXYZ(selectedEntity.Position.X, selectedEntity.Position.Y, z, modelNumber, _targetCursorId, _targetCursorType);
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
                    LastGameObject = selectedEntity;

                if (_enqueuedAction != null)
                {
                    _enqueuedAction(entity.Serial, entity.Graphic, entity.X, entity.Y, entity.Z, entity is Item it && it.OnGround || entity.Serial.IsMobile);
                }
                else
                    GameActions.TargetObject(entity, _targetCursorId, _targetCursorType);
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

                GameActions.TargetXYZ(selectedEntity.Position.X, selectedEntity.Position.Y, z, modelNumber, _targetCursorId, _targetCursorType);
                Mouse.CancelDoubleClick = true;
            }

            ClearTargetingWithoutTargetCancelPacket();
        }
    }
}