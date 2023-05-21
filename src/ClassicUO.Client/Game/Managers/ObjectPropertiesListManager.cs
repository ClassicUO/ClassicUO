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

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClassicUO.Game.GameObjects;
using ClassicUO.Network;

namespace ClassicUO.Game.Managers
{
    internal sealed class ObjectPropertiesListManager
    {
        private readonly Dictionary<uint, ItemProperty> _itemsProperties = new Dictionary<uint, ItemProperty>();

        public delegate void OPLOnReceiveEvent(OPLEventArgs args);
        public event OPLOnReceiveEvent OPLOnReceive;

        public void Add(uint serial, uint revision, string name, string data, int namecliloc)
        {
            if (!_itemsProperties.TryGetValue(serial, out ItemProperty prop))
            {
                prop = new ItemProperty();
                _itemsProperties[serial] = prop;
            }

            prop.Serial = serial;
            prop.Revision = revision;
            prop.Name = name;
            prop.Data = data;
            prop.NameCliloc = namecliloc;

            OPLOnReceive?.Invoke(new OPLEventArgs(serial, name, data));
        }

        public class OPLEventArgs : System.EventArgs
        {
            public readonly uint Serial;
            public readonly string Name;
            public readonly string Data;

            public OPLEventArgs(uint serial, string name, string data)
            {
                Serial = serial;
                Name = name;
                Data = data;
            }
        }

        public bool Contains(uint serial)
        {
            if (_itemsProperties.TryGetValue(serial, out ItemProperty p))
            {
                return true; //p.Revision != 0;  <-- revision == 0 can contain the name.
            }

            // if we don't have the OPL of this item, let's request it to the server.
            // Original client seems asking for OPL when character is not running. 
            // We'll ask OPL when mouse is over an object.
            PacketHandlers.AddMegaClilocRequest(serial);

            return false;
        }

        public bool IsRevisionEquals(uint serial, uint revision)
        {
            if (_itemsProperties.TryGetValue(serial, out ItemProperty prop))
            {
                return (revision & ~0x40000000) == prop.Revision || // remove the mask
                       revision == prop.Revision;                   // if mask removing didn't work, try a simple compare.
            }

            return false;
        }

        public bool TryGetRevision(uint serial, out uint revision)
        {
            if (_itemsProperties.TryGetValue(serial, out ItemProperty p))
            {
                revision = p.Revision;

                return true;
            }

            revision = 0;

            return false;
        }

        public bool TryGetNameAndData(uint serial, out string name, out string data)
        {
            if (_itemsProperties.TryGetValue(serial, out ItemProperty p))
            {
                name = p.Name;
                data = p.Data;

                return true;
            }

            name = data = null;

            return false;
        }

        public int GetNameCliloc(uint serial)
        {
            if (_itemsProperties.TryGetValue(serial, out ItemProperty p))
            {
                return p.NameCliloc;
            }

            return 0;
        }

        public ItemPropertiesData TryGetItemPropertiesData(uint serial)
        {
            if (Contains(serial))
                if (World.Items.TryGetValue(serial, out Item item))
                    return new ItemPropertiesData(item);
            return null;
        }

        public void Remove(uint serial)
        {
            _itemsProperties.Remove(serial);
        }

        public void Clear()
        {
            _itemsProperties.Clear();
        }
    }

    internal class ItemProperty
    {
        public bool IsEmpty => string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(Data);
        public string Data;
        public string Name;
        public uint Revision;
        public uint Serial;
        public int NameCliloc;

        public string CreateData(bool extended)
        {
            return string.Empty;
        }
    }

    internal class ItemPropertiesData
    {
        public readonly bool HasData = false;
        public readonly string Name;
        public readonly string RawData;
        public readonly uint serial;
        public string[] RawLines;
        public List<SinglePropertyData> singlePropertyData = new List<SinglePropertyData>();

        public ItemPropertiesData(Item item)
        {
            serial = item.Serial;
            if (World.OPL.TryGetNameAndData(item.Serial, out Name, out RawData))
            {
                HasData = true;
                processData();
            }
        }

        private void processData()
        {
            RawLines = RawData.Split(new string[] { "\n", "<br>" }, StringSplitOptions.None);

            foreach (string line in RawLines)
            {
                singlePropertyData.Add(new SinglePropertyData(line));
            }
        }

        public bool GenerateComparisonTooltip(ItemPropertiesData comparedTo, out string compiledToolTip)
        {
            if (!HasData)
            {
                compiledToolTip = null;
                return false;
            }

            string finalTooltip = "";

            foreach (SinglePropertyData thisItem in singlePropertyData)
            {
                bool foundMatch = false;
                foreach(SinglePropertyData secondItem in comparedTo.singlePropertyData)
                {
                    if(String.Equals(thisItem.Name, secondItem.Name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        foundMatch = true;
                        finalTooltip += thisItem.Name;

                        if(thisItem.FirstValue > -1 && secondItem.FirstValue > -1)
                        {
                            double diff = secondItem.FirstValue - thisItem.FirstValue;
                            finalTooltip += $" {thisItem.FirstValue}({(diff < 0 ? "-" : "+")}{diff})";
                        }

                        if (thisItem.SecondValue > -1 && secondItem.SecondValue > -1)
                        {
                            double diff = secondItem.SecondValue - thisItem.SecondValue;
                            finalTooltip += $" {thisItem.SecondValue}({(diff < 0 ? "-" : "+")}{diff})";
                        }

                        finalTooltip += "\n";
                        break;
                    }
                }
                if (!foundMatch)
                    finalTooltip += thisItem.ToString() + "\n";
            }

            compiledToolTip = finalTooltip;
            return true;
        }

        public class SinglePropertyData
        {
            public readonly string Name;
            public readonly double FirstValue = -1;
            public readonly double SecondValue = -1;

            public SinglePropertyData(string line)
            {
                string pattern = @"(\d+(\.)?\d+)";
                MatchCollection matches = Regex.Matches(line, pattern, RegexOptions.IgnoreCase);

                Match nameMatch = Regex.Match(line, @"(\D+)");
                if (nameMatch.Success)
                    Name = nameMatch.Value;

                if (matches.Count > 0)
                {
                    double.TryParse(matches[0].Value, out FirstValue);
                    if (matches.Count > 1)
                    {
                        double.TryParse(matches[1].Value, out SecondValue);
                    }
                }
            }

            public override string ToString()
            {
                string output = "";

                if(Name != null)
                    output += Name;

                if (FirstValue > -1)
                    output += $" {FirstValue}";

                if (SecondValue > -1)
                    output += $" {SecondValue}";

                return output;
            }
        }
    }
}