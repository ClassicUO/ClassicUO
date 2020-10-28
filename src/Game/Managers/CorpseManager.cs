using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Utility.Collections;

namespace ClassicUO.Game.Managers
{
    internal class CorpseManager
    {
        private readonly Deque<CorpseInfo> _corpses = new Deque<CorpseInfo>();

        public void Add(uint corpse, uint obj, Direction dir, bool run)
        {
            for (int i = 0; i < _corpses.Count; i++)
            {
                ref CorpseInfo c = ref _corpses.GetAt(i);

                if (c.CorpseSerial == corpse)
                {
                    return;
                }
            }

            _corpses.AddToBack(new CorpseInfo(corpse, obj, dir, run));
        }

        public void Remove(uint corpse, uint obj)
        {
            for (int i = 0; i < _corpses.Count;)
            {
                ref CorpseInfo c = ref _corpses.GetAt(i);

                if (c.CorpseSerial == corpse || c.ObjectSerial == obj)
                {
                    if (corpse != 0)
                    {
                        Item item = World.Items.Get(corpse);

                        if (item != null)
                        {
                            item.Layer =
                                (Layer) ((c.Direction & Direction.Mask) | (c.IsRunning ? Direction.Running : 0));
                        }
                    }

                    _corpses.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        public bool Exists(uint corpse, uint obj)
        {
            for (int i = 0; i < _corpses.Count; i++)
            {
                ref CorpseInfo c = ref _corpses.GetAt(i);

                if (c.CorpseSerial == corpse || c.ObjectSerial == obj)
                {
                    return true;
                }
            }

            return false;
        }

        public Item GetCorpseObject(uint serial)
        {
            for (int i = 0; i < _corpses.Count; i++)
            {
                ref CorpseInfo c = ref _corpses.GetAt(i);

                if (c.ObjectSerial == serial)
                {
                    return World.Items.Get(c.CorpseSerial);
                }
            }

            return null;
        }

        public void Clear()
        {
            _corpses.Clear();
        }
    }

    internal struct CorpseInfo
    {
        public CorpseInfo(uint corpseSerial, uint objectSerial, Direction direction, bool isRunning)
        {
            CorpseSerial = corpseSerial;
            ObjectSerial = objectSerial;
            Direction = direction;
            IsRunning = isRunning;
        }

        public uint CorpseSerial, ObjectSerial;
        public Direction Direction;
        public bool IsRunning;
    }
}