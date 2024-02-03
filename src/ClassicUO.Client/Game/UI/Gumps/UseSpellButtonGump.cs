#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Xml;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using ClassicUO.Assets;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps
{
    internal class UseSpellButtonGump : AnchorableGump
    {
        const ushort LOCK_GRAPHIC = 0x1086;

        private GumpPic _background;
        private SpellDefinition _spell;

        private readonly MacroManager _mm;

        public bool ShowEdit =>
            Keyboard.Ctrl && Keyboard.Alt && ProfileManager.CurrentProfile.FastSpellsAssign;

        public UseSpellButtonGump(World world) : base(world,0, 0)
        {
            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;

            _mm = world.Macros;
        }

        public UseSpellButtonGump(World world, SpellDefinition spell) : this(world)
        {
            _spell = spell;
            BuildGump();
        }

        public override GumpType GumpType => GumpType.SpellButton;

        public int SpellID => _spell?.ID ?? 0;

        public ushort Hue
        {
            set => _background.Hue = value;
        }

        private void BuildGump()
        {
            Add(
                _background = new GumpPic(0, 0, (ushort)_spell.GumpIconSmallID, 0)
                {
                    AcceptMouseInput = false
                }
            );

            int cliloc = GetSpellTooltip(_spell.ID);

            if (cliloc != 0)
            {
                SetTooltip(ClilocLoader.Instance.GetString(cliloc), 80);
            }

            WantUpdateSize = true;
            AcceptMouseInput = true;
            GroupMatrixWidth = 44;
            GroupMatrixHeight = 44;
            AnchorType = ANCHOR_TYPE.SPELL;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            base.Draw(batcher, x, y);

            if (ShowEdit)
            {
                Vector3 hueVector = ShaderHueTranslator.GetHueVector(0);

                ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(LOCK_GRAPHIC);

                if (gumpInfo.Texture != null)
                {
                    if (
                        UIManager.MouseOverControl != null
                        && (
                            UIManager.MouseOverControl == this
                            || UIManager.MouseOverControl.RootParent == this
                        )
                    )
                    {
                        hueVector.X = 34;
                        hueVector.Y = 1;
                    }

                    batcher.Draw(
                        gumpInfo.Texture,
                        new Vector2(x + (Width - gumpInfo.UV.Width), y),
                        gumpInfo.UV,
                        hueVector
                    );
                }
            }

            return true;
        }

        private int GetSpellsId()
        {
            var rawSpellId = _spell.ID % 100;
            // Mysticism Spells Id start from 678
            if (rawSpellId > 78)
                return rawSpellId - 78;
            return rawSpellId;
        }

        private static int GetSpellTooltip(int id)
        {
            if (id >= 1 && id <= 64) // Magery
            {
                return 3002011 + (id - 1);
            }

            if (id >= 101 && id <= 117) // necro
            {
                return 1060509 + (id - 101);
            }

            if (id >= 201 && id <= 210)
            {
                return 1060585 + (id - 201);
            }

            if (id >= 401 && id <= 406)
            {
                return 1060595 + (id - 401);
            }

            if (id >= 501 && id <= 508)
            {
                return 1060610 + (id - 501);
            }

            if (id >= 601 && id <= 616)
            {
                return 1071026 + (id - 601);
            }

            if (id >= 678 && id <= 693)
            {
                return 1031678 + (id - 678);
            }

            if (id >= 701 && id <= 745)
            {
                if (id <= 706)
                {
                    return 1115612 + (id - 701);
                }

                if (id <= 745)
                {
                    return 1155896 + (id - 707);
                }
            }

            return 0;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, button);

            Point offset = Mouse.LDragOffset;

            if (button == MouseButtonType.Left && ShowEdit)
            {
                Macro mCast = Macro.CreateFastMacro(
                    _spell.Name,
                    MacroType.CastSpell,
                    (MacroSubType)GetSpellsId() + SpellBookDefinition.GetSpellsGroup(_spell.ID)
                );
                if (_mm.FindMacro(_spell.Name) == null)
                {
                    _mm.MoveToBack(mCast);
                }
                GameActions.OpenMacroGump(World, _spell.Name);
            }

            if (
                ProfileManager.CurrentProfile.CastSpellsByOneClick
                && button == MouseButtonType.Left
                && Math.Abs(offset.X) < 5
                && Math.Abs(offset.Y) < 5
            )
            {
                GameActions.CastSpell(_spell.ID);
            }
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (
                !ProfileManager.CurrentProfile.CastSpellsByOneClick
                && button == MouseButtonType.Left
            )
            {
                GameActions.CastSpell(_spell.ID);

                return true;
            }

            return false;
        }

        public override void Save(XmlTextWriter writer)
        {
            base.Save(writer);
            writer.WriteAttributeString("id", _spell.ID.ToString());
        }

        public override void Restore(XmlElement xml)
        {
            base.Restore(xml);
            _spell = SpellDefinition.FullIndexGetSpell(int.Parse(xml.GetAttribute("id")));
            BuildGump();
        }
    }
}
