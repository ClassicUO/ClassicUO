using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Managers
{
    internal class CorpseManager
    {
        private readonly Dictionary<Serial, CorpseInfo?> _corpses = new Dictionary<Serial, CorpseInfo?>();

        public void Add(Serial corpse, Serial obj, Direction dir, bool run)
        {
            if (!_corpses.ContainsKey(corpse))
            {
                _corpses[corpse] = new CorpseInfo(corpse, obj, dir, run);
            }
        }

        public void Remove(Serial corpse, Serial obj)
        {
            CorpseInfo? c = _corpses.Values.FirstOrDefault(s => s.HasValue && (s.Value.CorpseSerial == corpse || s.Value.ObjectSerial == obj));

            if (c != null)
            {
                Item item = World.Items.Get(corpse);

                if (item != null)
                {
                    item.Layer = (Layer) ((c.Value.Direction & Direction.Mask) | (c.Value.IsRunning ? Direction.Running : 0));
                }
                _corpses.Remove(c.Value.CorpseSerial);
            }
        }

        public bool Exists(Serial corpse, Serial obj)
        {
            return _corpses.Values.Any(s => s.HasValue && (s.Value.CorpseSerial == corpse || s.Value.ObjectSerial == obj));
        }

        public Item GetCorpseObject(Serial serial)
        {
            CorpseInfo? c = _corpses.Values.FirstOrDefault(s => s.HasValue && s.Value.ObjectSerial == serial);

            return c.HasValue ? World.Items.Get(c.Value.CorpseSerial) : null;
        }

        public void Clear() => _corpses.Clear();
    }

    internal readonly struct CorpseInfo
    {
        public CorpseInfo(Serial corpseSerial, Serial objectSerial, Direction direction, bool isRunning)
        {
            CorpseSerial = corpseSerial;
            ObjectSerial = objectSerial;
            Direction = direction;
            IsRunning = isRunning;
        }

        public readonly Serial CorpseSerial, ObjectSerial;
        public readonly Direction Direction;
        public readonly bool IsRunning;  
    }
}
