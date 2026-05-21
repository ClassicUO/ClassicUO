// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events;

namespace ClassicUO.Game
{
    internal sealed partial class World
    {
        private void SubscribeExtendedWalk()
        {
            EventSink.FastWalkStackInit += OnFastWalkStackInit;
            EventSink.FastWalkStackAdd += OnFastWalkStackAdd;
            EventSink.MapIndexChanged += OnMapIndexChanged;
        }

        private void UnsubscribeExtendedWalk()
        {
            EventSink.FastWalkStackInit -= OnFastWalkStackInit;
            EventSink.FastWalkStackAdd -= OnFastWalkStackAdd;
            EventSink.MapIndexChanged -= OnMapIndexChanged;
        }

        private void OnFastWalkStackInit(FastWalkStackInitArgs e)
        {
            if (Player == null) return;

            var values = e.Values;
            int count = values?.Count ?? 0;

            for (int i = 0; i < 6; i++)
            {
                uint v = i < count ? values[i] : 0;
                Player.Walker.FastWalkStack.SetValue(i, v);
            }
        }

        private void OnFastWalkStackAdd(FastWalkStackAddArgs e)
        {
            if (Player == null) return;

            Player.Walker.FastWalkStack.AddValue(e.Value);
        }

        private void OnMapIndexChanged(MapIndexChangedArgs e)
        {
            MapIndex = e.MapIndex;
        }
    }
}
