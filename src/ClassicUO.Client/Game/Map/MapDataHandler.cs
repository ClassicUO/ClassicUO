// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.Map
{
    internal sealed class MapDataHandler : IEventListener
    {
        private readonly World _world;

        public MapDataHandler(World world)
        {
            _world = world;
        }

        public void Subscribe()
        {
            EventSink.MapDataReceived += OnMapDataReceived;
        }

        public void Unsubscribe()
        {
            EventSink.MapDataReceived -= OnMapDataReceived;
        }

        private void OnMapDataReceived(MapDataReceivedArgs e)
        {
            if (!_world.InGame) return;

            MapGump gump = UIManager.GetGump<MapGump>(e.Serial);
            if (gump == null) return;

            switch ((MapMessageType)e.Action)
            {
                case MapMessageType.Add:
                    gump.AddPin(e.PinX, e.PinY);
                    break;

                case MapMessageType.Clear:
                    gump.ClearContainer();
                    break;

                case MapMessageType.EditResponse:
                    gump.SetPlotState(e.PlotState);
                    break;
            }
        }
    }
}
