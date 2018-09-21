using System;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;

namespace ClassicUO.Game.Gumps
{
    internal class PaperDollInteractable : Gump
    {
        private bool _isFemale;
        private bool _isElf;
        private GumpPicBackpack m_Backpack;


        public PaperDollInteractable(int x, int y, Mobile sourceEntity)
            : base(0, 0)
        {
            X = x;
            Y = y;
            _isFemale = (sourceEntity.Flags & Flags.Female) != 0;
            SourceEntity = sourceEntity;
            AcceptMouseInput = false;
        }

        public override void Dispose()
        {
            _sourceEntity.ClearCallBacks(OnEntityUpdated, OnEntityDisposed);
            if (m_Backpack != null) m_Backpack.MouseDoubleClick -= On_Doubleclick_Backpack;
            base.Dispose();
        }

        public override void Update(double totalMS, double frameMS)
        {
            if (_sourceEntity != null)
            {
                _isFemale = (((Mobile) _sourceEntity).Flags & Flags.Female) != 0;
                _isElf = false;
            }

            base.Update(totalMS, frameMS);
        }

        private void OnEntityUpdated(Entity entity)
        {
            Clear();
            // Add the base gump - the semi-naked paper doll.
            if (true)
            {
                int bodyID = 12 + (_isElf ? 2 : 0) + (_isFemale ? 1 : 0);
                GumpPic paperdoll;
                AddChildren(paperdoll = new GumpPic(0, 0, (ushort) bodyID, ((Mobile) _sourceEntity).Hue));
                paperdoll.AcceptMouseInput = true;
                paperdoll.IsPaperdoll = true;
            }

            // Loop through the items on the mobile and create the gump pics.
            for (int i = 0; i < s_DrawOrder.Length; i++)
            {
                Item item = ((Mobile) _sourceEntity).Equipment[(int) s_DrawOrder[i]];
                if (item == null)
                    continue;

                bool canPickUp = true;
                switch (s_DrawOrder[i])
                {
                    case PaperDollEquipSlots.FacialHair:
                    case PaperDollEquipSlots.Hair:
                        canPickUp = false;
                        break;
                }

                ItemGumplingPaperdoll itemGumplingPaperdoll;
                AddChildren(itemGumplingPaperdoll = new ItemGumplingPaperdoll(0, 0, item));
                itemGumplingPaperdoll.SlotIndex = i;
                itemGumplingPaperdoll.IsFemale = _isFemale;
                itemGumplingPaperdoll.CanPickUp = canPickUp;
            }

            // If this object has a backpack, add it last.
            if (((Mobile) _sourceEntity).Equipment[(int) PaperDollEquipSlots.Backpack] != null)
            {
                Item backpack = ((Mobile) _sourceEntity).Equipment[(int) PaperDollEquipSlots.Backpack];
                AddChildren(m_Backpack = new GumpPicBackpack(-7, 0, backpack));
                m_Backpack.AcceptMouseInput = true;
                m_Backpack.MouseDoubleClick += On_Doubleclick_Backpack;
            }
        }

        private void OnEntityDisposed(Entity entity)
        {
            Dispose();
        }

        private void On_Doubleclick_Backpack(object sender, EventArgs args)
        {
            Item backpack = ((Mobile) _sourceEntity).Equipment[(int) PaperDollEquipSlots.Backpack];
            GameActions.DoubleClick(backpack);
        }

        private Entity _sourceEntity;

        public Entity SourceEntity
        {
            set
            {
                if (value != _sourceEntity)
                {
                    if (_sourceEntity != null)
                    {
                        _sourceEntity.ClearCallBacks(OnEntityUpdated, OnEntityDisposed);
                        _sourceEntity = null;
                    }

                    if (value is Mobile)
                    {
                        _sourceEntity = value;
                        // update the gump
                        OnEntityUpdated(_sourceEntity);
                        // if the entity changes in the future, update the gump again
                        _sourceEntity.SetCallbacks(OnEntityUpdated, OnEntityDisposed);
                    }
                }
            }
            get => _sourceEntity;
        }

        private enum PaperDollEquipSlots
        {
            Body = 0,
            RightHand = 1,
            LeftHand = 2,
            Footwear = 3,
            Legging = 4,
            Shirt = 5,
            Head = 6,
            Gloves = 7,
            Ring = 8,
            Talisman = 9,
            Neck = 10,
            Hair = 11,
            Belt = 12,
            Chest = 13,
            Bracelet = 14,
            Unused = 15,
            FacialHair = 16,
            Sash = 17,
            Earring = 18,
            Sleeves = 19,
            Back = 20,
            Backpack = 21,
            Robe = 22,
            Skirt = 23
        }

        private static readonly PaperDollEquipSlots[] s_DrawOrder =
        {
            PaperDollEquipSlots.Footwear,
            PaperDollEquipSlots.Legging,
            PaperDollEquipSlots.Shirt,
            PaperDollEquipSlots.Sleeves,
            PaperDollEquipSlots.Gloves,
            PaperDollEquipSlots.Ring,
            PaperDollEquipSlots.Talisman,
            PaperDollEquipSlots.Neck,
            PaperDollEquipSlots.Belt,
            PaperDollEquipSlots.Chest,
            PaperDollEquipSlots.Bracelet,
            PaperDollEquipSlots.Hair,
            PaperDollEquipSlots.FacialHair,
            PaperDollEquipSlots.Head,
            PaperDollEquipSlots.Sash,
            PaperDollEquipSlots.Earring,
            PaperDollEquipSlots.Back,
            PaperDollEquipSlots.Skirt,
            PaperDollEquipSlots.Robe,
            PaperDollEquipSlots.LeftHand,
            PaperDollEquipSlots.RightHand
        };
    }
}