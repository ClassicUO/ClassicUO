// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Events;

namespace ClassicUO.Game.Movement
{
    /// <summary>
    /// Handles server walk responses: deny + confirm.
    /// </summary>
    internal sealed class WalkHandler : IEventListener
    {
        private readonly World _world;

        public WalkHandler(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.WalkDenied += OnWalkDenied;
            EventSink.WalkConfirmed += OnWalkConfirmed;
        }

        public void Unsubscribe()
        {
            EventSink.WalkDenied -= OnWalkDenied;
            EventSink.WalkConfirmed -= OnWalkConfirmed;
        }

        private void OnWalkDenied(WalkDeniedArgs e)
        {
            if (_world.Player == null) return;

            _world.Player.Walker.DenyWalk(e.Sequence, e.X, e.Y, e.Z);
            _world.Player.Direction = e.Direction;
        }

        private void OnWalkConfirmed(WalkConfirmedArgs e)
        {
            if (_world.Player == null) return;

            byte noto = e.Notoriety;
            if (noto == 0 || noto >= 8)
            {
                noto = 0x01;
            }

            _world.Player.NotorietyFlag = (NotorietyFlag)noto;
            _world.Player.Walker.ConfirmWalk(e.Sequence);
            _world.Player.AddToTile();
        }
    }
}
