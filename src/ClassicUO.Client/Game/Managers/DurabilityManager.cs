#region license

// Copyright (c) 2021, andreakarasho
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

using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Gumps;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ClassicUO.Game.Managers
{
    internal class DurabilityManager
    {
        private Dictionary<uint, DurabiltyProp> _itemLayerSlots = new Dictionary<uint, DurabiltyProp>();
        public static readonly Layer[] EquipLayers =
        {
            Layer.Cloak, Layer.Shirt, Layer.Pants, Layer.Shoes, Layer.Legs, Layer.Arms, Layer.Torso, Layer.Tunic,
            Layer.Ring, Layer.Bracelet, Layer.Gloves, Layer.Skirt, Layer.Robe, Layer.Waist, Layer.Necklace,
            Layer.Beard, Layer.Earrings, Layer.Helmet, Layer.OneHanded, Layer.TwoHanded, Layer.Talisman
        };

        public IReadOnlyCollection<DurabiltyProp> Durabilties => _itemLayerSlots.Values;

        public DurabilityManager()
        {
            World.OPL.OPLOnReceive += OnOPLRecieve;
        }

        public bool TryGetDurability(uint serial, out DurabiltyProp durability)
        {
            return _itemLayerSlots.TryGetValue(serial, out durability);
        }

        public void OnOPLRecieve(ObjectPropertiesListManager.OPLEventArgs e)
        {
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                var isItem = SerialHelper.IsValid(e.Serial) && SerialHelper.IsItem(e.Serial);
                if (isItem)
                {
                    if (World.Items.TryGetValue(e.Serial, out var item))
                    {
                        if (!item.IsDestroyed)
                        {
                            if (EquipLayers.Contains(item.Layer) && item.Container == World.Player.Serial)
                            {
                                var durability = ParseDurability((int)item.Serial, e.Data);
                                if (_itemLayerSlots.TryGetValue(item.Serial, out var slot))
                                {
                                    slot.Durabilty = durability.Durabilty;
                                    slot.MaxDurabilty = durability.MaxDurabilty;
                                }
                                else
                                {
                                    _itemLayerSlots.Add(item.Serial, durability);
                                }
                            }
                            else
                            {
                                _itemLayerSlots.Remove(item.Serial);
                            }

                            UIManager.GetGump<DurabilitysGump>()?.RequestUpdateContents();
                        }
                    }
                }
            });
        }
        public static DurabiltyProp ParseDurability(int serial, string data)
        {
            MatchCollection matches = Regex.Matches(data, @"(?<=Durability )(\d*) / (\d*)"); //This should match 45 / 255 for example
            if (matches == null || matches.Count == 0)
            {
                return new DurabiltyProp();
            }

            string[] parts = data.Substring(matches[0].Index, matches[0].Length).Split('/');

            if (int.TryParse(parts[0].Trim(), out int min))
                if (int.TryParse(parts[1].Trim(), out int max))
                    return new DurabiltyProp(serial, min, max);

            return new DurabiltyProp();
        }

    }

    internal class DurabiltyProp
    {
        public int Serial { get; set; }
        public int Durabilty { get; set; }
        public int MaxDurabilty { get; set; }

        public float Percentage
        {
            get
            {
                return MaxDurabilty > 0 ? ((float)Durabilty / (float)MaxDurabilty) : 0;
            }
        }

        public DurabiltyProp(int serial, int current, int max)
        {
            Serial = serial;
            Durabilty = current;
            MaxDurabilty = max;
        }
        public DurabiltyProp() : this(0, 0, 0) { }
    }
}