// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;

namespace ClassicUO.Game.Managers
{
    internal interface IBoatMovingManager
    {
        void MoveRequest(Direction direciton, byte speed);

        void AddStep(uint serial, byte speed, Direction movingDir, Direction facingDir, ushort x, ushort y, sbyte z);

        void ClearSteps(uint serial);

        void ClearEntities(uint serial);

        void PushItemToList(uint serial, uint objSerial, int x, int y, int z);

        void Update();
    }
}
