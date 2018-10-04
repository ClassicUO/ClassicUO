using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Gumps.Controls;

namespace ClassicUO.Game.Gumps.UIGumps
{
    class ContainerGump : Gump
    {
        private readonly ContainerData _data;
        private readonly Item _item;

        public ContainerGump(Item item, Graphic gumpid) : base(item.Serial, 0)
        {
            _item = item;
            _data = ContainerManager.Get(gumpid);

            CanMove = true;

            AddChildren(new GumpPicContainer(0, 0, _data.Graphic, 0, item));
        }


        protected override void OnInitialize()
        {
            foreach (Item item in _item.Items)
                AddChildren(new ItemGumpling(item));

            _item.SetCallbacks(OnItemUpdated, OnItemDisposed);
        }


        public override void Dispose()
        {
            _item.ClearCallBacks(OnItemUpdated, OnItemDisposed);

            base.Dispose();
        }

        private void OnItemUpdated(GameObject obj)
        {
            List<GumpControl> toremove = Children.Where(s => s is ItemGumpling ctrl && !_item.Items.Contains(ctrl.Item))
                .ToList();

            if (toremove.Count > 0)
                toremove.ForEach(RemoveChildren);

            foreach (Item item in _item.Items)
            {
                bool control = false;

                foreach (GumpControl child in Children)
                {
                    if (child is ItemGumpling ctrl && ctrl.Item == item)
                    {
                        control = true;
                        break;
                    }
                }

                if (!control)
                    AddChildren(new ItemGumpling(item));

            }
        }

        private void OnItemDisposed(GameObject obj)
        {
            Dispose();
        }
    }
}
