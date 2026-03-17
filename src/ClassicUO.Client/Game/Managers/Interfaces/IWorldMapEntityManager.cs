// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;

namespace ClassicUO.Game.Managers
{
    internal interface IWorldMapEntityManager
    {
        bool Enabled { get; }

        Dictionary<uint, WMapEntity> Entities { get; }

        void SetACKReceived();

        void SetEnable(bool v);

        void AddOrUpdate(uint serial, int x, int y, int hp, int map, bool isguild, string name = null, bool from_packet = false);
    }
}
