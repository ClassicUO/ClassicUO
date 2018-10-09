using ClassicUO.Game.GameObjects;
using ClassicUO.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ClassicUO.Game.System
{
    class TargetSystem
    {
        
        public enum TargetType
        {
            Nothing = -1,
            Object = 0,
            Position = 1,
            MultiPlacement = 2
        }

        private static TargetType _targeting = TargetType.Nothing;
        private static int _targetCursorId;
        private static byte _targetCursorType;

        public static TargetType TargetingState
        {
            get { return _targeting; }
        }

        public static GameObject LastGameObject { get; set; }

        public static bool IsTargeting
        {
            get { return _targeting != TargetType.Nothing; }
        }

        public static void ClearTargetingWithoutTargetCancelPacket()
        {
            _targeting = TargetType.Nothing;
        }

        public static void SetTargeting(TargetType targeting, int cursorID, byte cursorType)
        {
            if (_targeting != targeting || cursorID != _targetCursorId || cursorType != _targetCursorType)
            {
                if (targeting == TargetType.Nothing)
                {
                    GameActions.RequestTargetCancel(_targetCursorId, _targetCursorType);
                }
                //else
                //{
                //    // if we start targeting, we cancel movement.
                //        m_World.Input.ContinuousMouseMovementCheck = false;
                //}
                _targeting = targeting;
                _targetCursorId = cursorID;
                _targetCursorType = cursorType;
            }
        }

        int _multiModel;
        public void SetTargetingMulti(int deedSerial, int model, byte targetType)
        {
            SetTargeting(TargetType.MultiPlacement, deedSerial, targetType);
            _multiModel = model;
        }

        void mouseTargetingEventXYZ(Entity selectedEntity)
        {
            int modelNumber = 0;
            if (selectedEntity is IDynamicItem)
            {
                modelNumber = selectedEntity.Graphic;
            }

            GameActions.RequestTargetObjectPosition((ushort)selectedEntity.Position.X, (ushort)selectedEntity.Position.Y, (ushort)selectedEntity.Position.Z, (ushort)modelNumber, _targetCursorId, _targetCursorType);
            ClearTargetingWithoutTargetCancelPacket();
        }

        public static void mouseTargetingEventObject(GameObject selectedEntity)
        {
            if (selectedEntity == null)
                return;
            if (selectedEntity is Mobile mobile && mobile.Serial.IsValid)
            {
                GameActions.RequestTargetObject(mobile, _targetCursorId, _targetCursorType);
            }
            else
            {
                if (selectedEntity is Item item)
                {
                    int modelNumber = selectedEntity.Graphic;
                    GameActions.RequestTargetObjectPosition((ushort)selectedEntity.Position.X, (ushort)selectedEntity.Position.Y, (ushort)selectedEntity.Position.Z, (ushort)modelNumber, _targetCursorId, _targetCursorType);
                }
            }
            ClearTargetingWithoutTargetCancelPacket();
        }
    }
}
