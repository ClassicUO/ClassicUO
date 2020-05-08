using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Game.Data
{
    enum WaypointsType : ushort
    {
        Corpse = 0x01,
        PartyMember = 0x02,
        RallyPoint = 0x03,
        QuestGiver = 0x04,
        QuestDestination = 0x05,
        Resurrection = 0x06,
        PointOfInterest = 0x07,
        Landmark = 0x08,
        Town = 0x09,
        Dungeon = 0x0A,
        Moongate = 0x0B,
        Shop = 0x0C,
        Player = 0x0D,
    }
}
