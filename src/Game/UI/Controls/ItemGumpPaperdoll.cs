#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Utility.Logging;

using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal class ItemGumpPaperdoll : ItemGump
    {
        private readonly bool _isPartialHue;
        private const int MALE_OFFSET = 50000;
        private const int FEMALE_OFFSET = 60000;

        public ItemGumpPaperdoll(int x, int y, Item item, Mobile owner, bool transparent = false) : base(item)
        {
            X = x;
            Y = y;
            Mobile = owner;
            HighlightOnMouseOver = false;

            if (transparent)
                Alpha = 0.5f;

            _isPartialHue = item.ItemData.IsPartialHue;

            int offset = owner.IsFemale ? FEMALE_OFFSET : MALE_OFFSET;

            ushort id = Item.ItemData.AnimID;

            if (FileManager.Animations.EquipConversions.TryGetValue(Mobile.Graphic, out var dict))
            {
                if (dict.TryGetValue(id, out EquipConvData data))
                {
                    if (data.Gump > MALE_OFFSET)
                        id = (ushort) (data.Gump >= FEMALE_OFFSET ? data.Gump - FEMALE_OFFSET : data.Gump - MALE_OFFSET);
                    else
                        id = data.Gump;
                }
            }

            Texture = FileManager.Gumps.GetTexture((ushort)(id + offset));

            if (owner.IsFemale && Texture == null)
                Texture = FileManager.Gumps.GetTexture((ushort)(id + MALE_OFFSET));

            if (Texture == null)
            {
                if (item.Layer != Layer.Face)
                    Log.Message(LogTypes.Error, $"No texture found for Item ({item.Serial}) {item.Graphic} {item.ItemData.Name} {item.Layer}");
                Dispose();
                return;
            }

            Width = Texture.Width;
            Height = Texture.Height;

            WantUpdateSize = false;
        }

        public int SlotIndex { get; set; }

        public Mobile Mobile { get; set; }

        public override void Update(double totalMS, double frameMS)
        {
            base.Update(totalMS, frameMS);

            if (Mobile == null || Mobile.IsDestroyed)
                Dispose();

            if (IsDisposed)
                return;

            Texture.Ticks = (long) totalMS;
        }

        public override bool Draw(Batcher2D batcher, int x, int y)
        {
            if (IsDisposed)
                return false;
            Vector3 hue = Vector3.Zero;
            ShaderHuesTraslator.GetHueVector(ref hue, Item.Hue & 0x3FFF, _isPartialHue, Alpha, false);

            return batcher.Draw2D(Texture, x, y, hue);
        }


        protected override bool Contains(int x, int y)
        {
            return Texture.Contains(x, y);
        }


        protected override void OnMouseUp(int x, int y, MouseButton button)
        {
            base.OnMouseUp(x, y, MouseButton.None); // workaround to avoid clickeddrag

            if (button == MouseButton.Left)
            {
                GameScene gs = Engine.SceneManager.GetScene<GameScene>();

                if (TargetManager.IsTargeting)
                {
                    if (Mouse.IsDragging && Mouse.LDroppedOffset != Point.Zero)
                    {
                        if (gs == null || !gs.IsHoldingItem || !gs.IsMouseOverUI)
                        {
                            return;
                        }

                        gs.WearHeldItem(Mobile);
                        return;
                    }

                    switch (TargetManager.TargetingState)
                    {
                        case CursorTarget.Position:
                        case CursorTarget.Object:
                            gs.SelectedObject = Item;


                            if (Item != null)
                            {
                                TargetManager.TargetGameObject(Item);
                                Mouse.LastLeftButtonClickTime = 0;
                            }

                            break;

                        case CursorTarget.SetTargetClientSide:
                            gs.SelectedObject = Item;

                            if (Item != null)
                            {
                                TargetManager.TargetGameObject(Item);
                                Mouse.LastLeftButtonClickTime = 0;
                                Engine.UI.Add(new InfoGump(Item));
                            }
                            break;
                    }
                }
                else
                {
                    if (gs == null || !gs.IsHoldingItem || !gs.IsMouseOverUI)
                    {
                        return;
                    }
                    
                    if (Item == Mobile.Equipment[(int) Layer.Backpack])
                        gs.DropHeldItemToContainer(Item);
                    else
                        gs.WearHeldItem(Mobile);

                }
            }

        }
    }
}