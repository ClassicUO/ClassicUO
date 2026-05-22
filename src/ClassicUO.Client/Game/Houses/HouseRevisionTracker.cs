// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Network;

namespace ClassicUO.Game.Houses
{
    /// <summary>
    /// Queue of house serials that need a fresh CustomHouseDataRequest sent
    /// on the next game-loop drain. Hooked into the per-frame drain from
    /// <see cref="Scenes.GameScene.Update"/> via
    /// <see cref="Managers.HouseManager.DrainRevisionRequests"/>.
    /// </summary>
    internal sealed class HouseRevisionTracker : IHouseRevisionTracker
    {
        private readonly List<uint> _pending = new();

        public void Enqueue(uint serial) => _pending.Add(serial);

        public void Drain()
        {
            if (_pending.Count == 0) return;

            for (int i = 0; i < _pending.Count; i++)
            {
                NetClient.Socket.Send_CustomHouseDataRequest(_pending[i]);
            }

            _pending.Clear();
        }
    }
}
