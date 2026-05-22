// SPDX-License-Identifier: BSD-2-Clause

namespace ClassicUO.Game.Houses
{
    /// <summary>Queue of house serials pending a CustomHouseDataRequest send.</summary>
    internal interface IHouseRevisionTracker
    {
        void Enqueue(uint serial);
        void Drain();
    }
}
