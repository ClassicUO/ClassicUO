#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
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
using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ClassicUO.Game.System
{
    public enum TargetType
    {
        Nothing = -1,
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

        
        public static TargetType TargetingState { get; private set; } = TargetType.Nothing;

        public static GameObject LastGameObject { get; private set; }

        public static bool IsTargeting => TargetingState != TargetType.Nothing && _targetCursorType < 3;

        public static void ClearTargetingWithoutTargetCancelPacket()
        {
            TargetingState = TargetType.Nothing;
        }

        public static void SetTargeting(TargetType targeting, Serial cursorID, byte cursorType)
        {
            if (TargetingState != targeting || cursorID != _targetCursorId || cursorType != _targetCursorType)
            {
                if (targeting == TargetType.Nothing)
                    GameActions.TargetCancel(_targetCursorId, _targetCursorType);
                TargetingState = targeting;
                _targetCursorId = cursorID;
                _targetCursorType = cursorType;
            }
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
            if (selectedEntity == null)
                return;
            LastGameObject = selectedEntity;

            if (selectedEntity is GameEffect effect && effect.Source != null)
            {
                selectedEntity = effect.Source;
            }

            if (selectedEntity is Entity entity)
            {
                GameActions.TargetObject(entity, _targetCursorId, _targetCursorType);
                Mouse.CancelDoubleClick = true;
            }
            else
            {
                Graphic modelNumber = 0;
                short z = selectedEntity.Position.Z;

                if (selectedEntity is Static st)
                {
                    modelNumber = selectedEntity.Graphic;

                    if (st.ItemData.IsSurface)
                        z += st.ItemData.Height;
                }

                GameActions.TargetXYZ(selectedEntity.Position.X, selectedEntity.Position.Y, z, modelNumber, _targetCursorId, _targetCursorType);
                Mouse.CancelDoubleClick = true;
            }

            ClearTargetingWithoutTargetCancelPacket();
        }
    }
}