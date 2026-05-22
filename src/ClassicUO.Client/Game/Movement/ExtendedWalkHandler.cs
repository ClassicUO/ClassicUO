// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;

namespace ClassicUO.Game.Movement
{
    internal sealed class ExtendedWalkHandler : IEventListener
    {
        private readonly World _world;

        public ExtendedWalkHandler(World world)
        {
            _world = world;
        }

        public void Subscribe()
        {
            EventSink.FastWalkStackInit += OnFastWalkStackInit;
            EventSink.FastWalkStackAdd += OnFastWalkStackAdd;
            EventSink.MapIndexChanged += OnMapIndexChanged;
        }

        public void Unsubscribe()
        {
            EventSink.FastWalkStackInit -= OnFastWalkStackInit;
            EventSink.FastWalkStackAdd -= OnFastWalkStackAdd;
            EventSink.MapIndexChanged -= OnMapIndexChanged;
        }

        private void OnFastWalkStackInit(FastWalkStackInitArgs e)
        {
            if (_world.Player == null) return;

            var values = e.Values;
            int count = values?.Count ?? 0;

            for (int i = 0; i < 6; i++)
            {
                uint v = i < count ? values[i] : 0;
                _world.Player.Walker.FastWalkStack.SetValue(i, v);
            }
        }

        private void OnFastWalkStackAdd(FastWalkStackAddArgs e)
        {
            if (_world.Player == null) return;

            _world.Player.Walker.FastWalkStack.AddValue(e.Value);
        }

        private void OnMapIndexChanged(MapIndexChangedArgs e)
        {
            _world.MapIndex = e.MapIndex;
        }
    }
}
