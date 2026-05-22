// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;

namespace ClassicUO.Game.Movement
{
    internal sealed class PathfindingHandler : IEventListener
    {
        private readonly World _world;

        public PathfindingHandler(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.PathfindingReceived += OnPathfindingReceived;
        }

        public void Unsubscribe()
        {
            EventSink.PathfindingReceived -= OnPathfindingReceived;
        }

        private void OnPathfindingReceived(PathfindingReceivedArgs e)
        {
            if (_world.Player == null) return;
            _world.Player.Pathfinder.WalkTo(e.X, e.Y, e.Z, 0);
        }
    }
}
