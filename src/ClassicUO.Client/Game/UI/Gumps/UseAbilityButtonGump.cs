// SPDX-License-Identifier: BSD-2-Clause

using System.Xml;
using ClassicUO.Game.Data;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;

namespace ClassicUO.Game.UI.Gumps
{
    internal class UseAbilityButtonGump : AnchorableGump
    {
        private GumpPic _button;

        public UseAbilityButtonGump(World world) : base(world, 0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
        }

        public UseAbilityButtonGump(World world, bool primary) : this(world)
        {
            IsPrimary = primary;
            BuildGump();
        }

        public override GumpType GumpType => GumpType.AbilityButton;

        public int Index { get; private set; }
        public bool IsPrimary { get; private set; }

        private void BuildGump()
        {
            Clear();
            Index = ((byte)World.Player.Abilities[IsPrimary ? 0 : 1] & 0x7F);

            ref readonly AbilityDefinition def = ref AbilityData.Abilities[Index - 1];

            _button = new GumpPic(0, 0, def.Icon, 0)
            {
                AcceptMouseInput = false
            };

            Add(_button);

            SetTooltip(Client.Game.UO.FileManager.Clilocs.GetString(1028838 + (Index - 1)), 80);

            WantUpdateSize = true;
            AcceptMouseInput = true;
            GroupMatrixWidth = 44;
            GroupMatrixHeight = 44;
            AnchorType = ANCHOR_TYPE.SPELL;
        }


        protected override void UpdateContents()
        {
            BuildGump();
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                if (IsPrimary)
                {
                    GameActions.UsePrimaryAbility(World);
                }
                else
                {
                    GameActions.UseSecondaryAbility(World);
                }

                return true;
            }

            return false;
        }


        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
            {
                return false;
            }

            byte index = (byte) World.Player.Abilities[IsPrimary ? 0 : 1];

            if ((index & 0x80) != 0)
            {
                _button.Hue = 38;
            }
            else if (_button.Hue != 0)
            {
                _button.Hue = 0;
            }


            return base.Draw(batcher, x, y);
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("isprimary", IsPrimary.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);
            IsPrimary = bool.Parse(xml.GetAttribute("isprimary"));
            BuildGump();
        }
    }
}