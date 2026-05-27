// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Resources;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class NameOverHeadHandlerGump : Gump
    {
        public static Point? LastPosition;

        public override GumpType GumpType => GumpType.NameOverHeadHandler;


        public NameOverHeadHandlerGump(World world) : base(world, 0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;

            if (LastPosition == null)
            {
                X = 100;
                Y = 100;
            }
            else
            {
                X = LastPosition.Value.X;
                Y = LastPosition.Value.Y;
            }

            WantUpdateSize = false;

            LayerOrder = UILayer.Over;

            RadioButton all, mobiles, items, mobilesCorpses;
            AlphaBlendControl alpha;

            Add
            (
                alpha = new AlphaBlendControl(0.7f)
                {
                    Hue = 34
                }
            );


            Add
            (
                all = new RadioButton
                (
                    0,
                    0x00D0,
                    0x00D1,
                    ResGumps.All,
                    color: 0xFFFF
                )
                {
                    IsChecked = World.NameOverHeadManager.TypeAllowed == NameOverheadTypeAllowed.All
                }
            );

            Add
            (
                mobiles = new RadioButton
                (
                    0,
                    0x00D0,
                    0x00D1,
                    ResGumps.MobilesOnly,
                    color: 0xFFFF
                )
                {
                    Y = all.Y + all.Height,
                    IsChecked = World.NameOverHeadManager.TypeAllowed == NameOverheadTypeAllowed.Mobiles
                }
            );

            Add
            (
                items = new RadioButton
                (
                    0,
                    0x00D0,
                    0x00D1,
                    ResGumps.ItemsOnly,
                    color: 0xFFFF
                )
                {
                    Y = mobiles.Y + mobiles.Height,
                    IsChecked = World.NameOverHeadManager.TypeAllowed == NameOverheadTypeAllowed.Items
                }
            );

            Add
            (
                mobilesCorpses = new RadioButton
                (
                    0,
                    0x00D0,
                    0x00D1,
                    ResGumps.MobilesAndCorpsesOnly,
                    color: 0xFFFF
                )
                {
                    Y = items.Y + items.Height,
                    IsChecked = World.NameOverHeadManager.TypeAllowed == NameOverheadTypeAllowed.MobilesCorpses
                }
            );

            alpha.Width = Math.Max(mobilesCorpses.Width, Math.Max(items.Width, Math.Max(all.Width, mobiles.Width)));
            alpha.Height = all.Height + mobiles.Height + items.Height + mobilesCorpses.Height;

            Width = alpha.Width;
            Height = alpha.Height;

            all.ValueChanged += (sender, e) =>
            {
                if (all.IsChecked)
                {
                    World.NameOverHeadManager.TypeAllowed = NameOverheadTypeAllowed.All;
                }
            };

            mobiles.ValueChanged += (sender, e) =>
            {
                if (mobiles.IsChecked)
                {
                    World.NameOverHeadManager.TypeAllowed = NameOverheadTypeAllowed.Mobiles;
                }
            };

            items.ValueChanged += (sender, e) =>
            {
                if (items.IsChecked)
                {
                    World.NameOverHeadManager.TypeAllowed = NameOverheadTypeAllowed.Items;
                }
            };

            mobilesCorpses.ValueChanged += (sender, e) =>
            {
                if (mobilesCorpses.IsChecked)
                {
                    World.NameOverHeadManager.TypeAllowed = NameOverheadTypeAllowed.MobilesCorpses;
                }
            };
        }


        protected override void OnDragEnd(int x, int y)
        {
            LastPosition = new Point(ScreenCoordinateX, ScreenCoordinateY);

            SetInScreen();

            base.OnDragEnd(x, y);
        }
    }
}
