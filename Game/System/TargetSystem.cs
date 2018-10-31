using ClassicUO.Game.GameObjects;
using ClassicUO.Input;
using ClassicUO.IO.Resources;

namespace ClassicUO.Game.System
{
    public enum TargetType
    {
        Nothing = -1,
        Object = 0,
        Position = 1,
        MultiPlacement = 2
    }

    internal static class TargetSystem
    {
        private static Serial _targetCursorId;
        private static byte _targetCursorType;
        private static int _multiModel;

        public static TargetType TargetingState { get; private set; } = TargetType.Nothing;

        public static GameObject LastGameObject { get; private set; }

        public static bool IsTargeting => TargetingState != TargetType.Nothing;

        public static void ClearTargetingWithoutTargetCancelPacket()
        {
            TargetingState = TargetType.Nothing;
        }

        public static void SetTargeting(TargetType targeting, Serial cursorID, byte cursorType)
        {
            if (TargetingState != targeting || cursorID != _targetCursorId || cursorType != _targetCursorType)
            {
                if (targeting == TargetType.Nothing) GameActions.TargetCancel(_targetCursorId, _targetCursorType);
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

        private static void MouseTargetingEventXYZ(GameObject selectedEntity)
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

        public static void MouseTargetingEventObject(GameObject selectedEntity)
        {
            if (selectedEntity == null)
                return;
            LastGameObject = selectedEntity;

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

                    if (TileData.IsSurface((long) st.ItemData.Flags))
                        z += st.ItemData.Height;
                }

                GameActions.TargetXYZ(selectedEntity.Position.X, selectedEntity.Position.Y, z, modelNumber, _targetCursorId, _targetCursorType);
                Mouse.CancelDoubleClick = true;
            }

            ClearTargetingWithoutTargetCancelPacket();
        }
    }
}