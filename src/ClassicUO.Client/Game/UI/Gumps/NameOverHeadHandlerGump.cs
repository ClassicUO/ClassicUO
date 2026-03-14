// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game.Managers;
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
            CanCloseWithRightClick = false; //Prevent accidentally closing when stay active is enabled

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

            AlphaBlendControl alpha;
            Checkbox stayActive, cbAll, cbItems, cbCorpses, cbInnocent, cbAlly, cbGray, cbCriminal, cbEnemy, cbMurderer, cbInvulnerable;

            Add
            (
                alpha = new AlphaBlendControl(0.7f)
                {
                    Hue = 34
                }
            );

            Add
            (
                stayActive = new Checkbox
                (
                    0x00D2,
                    0x00D3,
                    ResGumps.StayActive,
                    color: 0xFFFF
                )
                {
                    IsChecked = world.NameOverHeadManager.IsToggled,
                }
            );
            stayActive.ValueChanged += (sender, e) => world.NameOverHeadManager.IsToggled = stayActive.IsChecked;

            var typeAllowed = World.NameOverHeadManager.TypeAllowed;

            int currentY = stayActive.Y + stayActive.Height;
            int maxWidth = stayActive.Width;

            Checkbox MakeCheckbox(string label, bool isChecked, ref int y)
            {
                var cb = new Checkbox(0x00D2, 0x00D3, label, color: 0xFFFF)
                {
                    IsChecked = isChecked,
                    Y = y
                };
                y += cb.Height;
                if (cb.Width > maxWidth)
                    maxWidth = cb.Width;
                return cb;
            }

            Add(cbAll = MakeCheckbox(ResGumps.All, typeAllowed == NameOverheadTypeAllowed.All, ref currentY));
            Add(cbItems = MakeCheckbox("Items", typeAllowed.HasFlag(NameOverheadTypeAllowed.Items), ref currentY));
            Add(cbCorpses = MakeCheckbox("Corpses", typeAllowed.HasFlag(NameOverheadTypeAllowed.Corpses), ref currentY));
            Add(cbInnocent = MakeCheckbox("Innocent", typeAllowed.HasFlag(NameOverheadTypeAllowed.Innocent), ref currentY));
            Add(cbAlly = MakeCheckbox("Ally", typeAllowed.HasFlag(NameOverheadTypeAllowed.Ally), ref currentY));
            Add(cbGray = MakeCheckbox("Gray", typeAllowed.HasFlag(NameOverheadTypeAllowed.Gray), ref currentY));
            Add(cbCriminal = MakeCheckbox("Criminal", typeAllowed.HasFlag(NameOverheadTypeAllowed.Criminal), ref currentY));
            Add(cbEnemy = MakeCheckbox("Enemy", typeAllowed.HasFlag(NameOverheadTypeAllowed.Enemy), ref currentY));
            Add(cbMurderer = MakeCheckbox("Murderer", typeAllowed.HasFlag(NameOverheadTypeAllowed.Murderer), ref currentY));
            Add(cbInvulnerable = MakeCheckbox("Invulnerable", typeAllowed.HasFlag(NameOverheadTypeAllowed.Invulnerable), ref currentY));

            alpha.Width = maxWidth;
            alpha.Height = currentY;

            Width = alpha.Width;
            Height = alpha.Height;

            // Track individual checkboxes for "All" logic
            var individualBoxes = new[]
            {
                (cb: cbItems, flag: NameOverheadTypeAllowed.Items),
                (cb: cbCorpses, flag: NameOverheadTypeAllowed.Corpses),
                (cb: cbInnocent, flag: NameOverheadTypeAllowed.Innocent),
                (cb: cbAlly, flag: NameOverheadTypeAllowed.Ally),
                (cb: cbGray, flag: NameOverheadTypeAllowed.Gray),
                (cb: cbCriminal, flag: NameOverheadTypeAllowed.Criminal),
                (cb: cbEnemy, flag: NameOverheadTypeAllowed.Enemy),
                (cb: cbMurderer, flag: NameOverheadTypeAllowed.Murderer),
                (cb: cbInvulnerable, flag: NameOverheadTypeAllowed.Invulnerable),
            };

            bool suppressCascade = false;

            cbAll.ValueChanged += (sender, e) =>
            {
                if (suppressCascade)
                    return;

                suppressCascade = true;

                foreach (var (cb, _) in individualBoxes)
                {
                    cb.IsChecked = cbAll.IsChecked;
                }

                World.NameOverHeadManager.TypeAllowed = cbAll.IsChecked
                    ? NameOverheadTypeAllowed.All
                    : NameOverheadTypeAllowed.None;

                suppressCascade = false;
            };

            foreach (var (cb, flag) in individualBoxes)
            {
                cb.ValueChanged += (sender, e) =>
                {
                    if (suppressCascade)
                        return;

                    suppressCascade = true;

                    if (cb.IsChecked)
                        World.NameOverHeadManager.TypeAllowed |= flag;
                    else
                        World.NameOverHeadManager.TypeAllowed &= ~flag;

                    cbAll.IsChecked = World.NameOverHeadManager.TypeAllowed == NameOverheadTypeAllowed.All;

                    suppressCascade = false;
                };
            }
        }


        protected override void OnDragEnd(int x, int y)
        {
            LastPosition = new Point(ScreenCoordinateX, ScreenCoordinateY);

            SetInScreen();

            base.OnDragEnd(x, y);
        }
    }
}
