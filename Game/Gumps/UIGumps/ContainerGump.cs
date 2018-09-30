using System;
using System.Collections.Generic;
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
            _item.SetCallbacks(OnItemUpdated, OnItemDisposed);

            CanMove = true;

            AddChildren(new GumpPicContainer(0,0, _data.Graphic, 0, item));

          
        }


        protected override void OnInitialize()
        {
            foreach (Item y in _item.Items)
            {
                AddChildren(new ItemGumpling(y));
            }
            base.OnInitialize();
        }

        public override void Dispose()
        {
            _item.ClearCallBacks(OnItemUpdated, OnItemDisposed);

            base.Dispose();
        }

        private void OnItemUpdated(GameObject obj)
        {
            foreach (Item item in _item.Items)
            {
                AddChildren(new ItemGumpling(item));
            }
        }

        private void OnItemDisposed(GameObject obj)
        {

        }
    }
}
