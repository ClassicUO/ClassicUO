// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game
{
    internal sealed partial class World
    {
        private void SubscribeMapData()
        {
            EventSink.MapDataReceived += OnMapDataReceived;
        }

        private void UnsubscribeMapData()
        {
            EventSink.MapDataReceived -= OnMapDataReceived;
        }

        private void OnMapDataReceived(MapDataReceivedArgs e)
        {
            if (!InGame) return;

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
