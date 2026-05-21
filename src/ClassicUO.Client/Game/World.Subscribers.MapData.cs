// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.IO;

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
            if (!InGame)
            {
                return;
            }

            MapGump gump = UIManager.GetGump<MapGump>(e.Serial);

            if (gump == null)
            {
                return;
            }

            var reader = new StackDataReader(e.Data);
            reader.Seek(e.Offset);

            switch ((MapMessageType)reader.ReadUInt8())
            {
                case MapMessageType.Add:
                    reader.Skip(1);

                    ushort x = reader.ReadUInt16BE();
                    ushort y = reader.ReadUInt16BE();

                    gump.AddPin(x, y);

                    break;

                case MapMessageType.Insert:
                    break;
                case MapMessageType.Move:
                    break;
                case MapMessageType.Remove:
                    break;

                case MapMessageType.Clear:
                    gump.ClearContainer();

                    break;

                case MapMessageType.Edit:
                    break;

                case MapMessageType.EditResponse:
                    gump.SetPlotState(reader.ReadUInt8());

                    break;
            }
        }
    }
}
