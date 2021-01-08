#region license

// Copyright (C) 2020 project dust765
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
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
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.Managers;

using System.IO;
using ClassicUO.Utility;
using System.Collections.Generic;

using ClassicUO.Renderer;
using ClassicUO.Configuration;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.InteropServices.Runtime.UOClassicCombat
{
    //ITEM DEFINITION ON TO LOOT LIST
    //FROM https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1?view=netcore-3.1
    public  class ToLootItem : IEquatable<ToLootItem>
    {
        public uint ToLootItemSerial { get; set; }
        public uint ToLootItemContainerSerial { get; set; }
        public int ToLootItemPriority { get; set; }
        public ushort ToLootItemAmount { get; set; }
        public ushort ToLootLootAmount { get; set; }

        public uint ToLootItemTick { get; set; }

        public override string ToString()
        {
            return "S#: " + ToLootItemSerial + "CS#: " + ToLootItemContainerSerial + "Prio: " + ToLootItemPriority + "   #: " + ToLootItemAmount + "   L#: " + ToLootLootAmount +  "   Tick: " + ToLootItemTick;
        }
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            ToLootItem objAsPart = obj as ToLootItem;
            if (objAsPart == null) return false;
            else return Equals(objAsPart);
        }
        public object obj;
        public object obj1;
        public override int GetHashCode() => ToLootItem.Equals(obj, obj1).GetHashCode();
                
        public bool Equals(ToLootItem other)
        {
            if (other == null) return false;
            return (this.ToLootItemSerial.Equals(other.ToLootItemSerial));
        }
    }
    //
    internal class UOClassicCombatAL : Gump
    {
        //MAIN UI CONSTRUCT
        private readonly AlphaBlendControl _background;
        private readonly Label _title;

        //UI
        private Label _uiTextQueue, _uiTextQueueSize;
        private Checkbox _uiCboxEnableAL, _uiCboxEnableSL;
        private Checkbox _uiCboxEnableALLow, _uiCboxEnableSLLow;

        //CONSTANTS
        private const byte FONT = 0xFF;
        private const ushort HUE_FONT = 999, HUE_FONTS_YELLOW = 0x35, HUE_FONTS_RED = 0x26, HUE_FONTS_GREEN = 0x3F, HUE_FONTS_BLUE = 0x5D;

        //MAIN UI CONSTRUCT
        
        //TIMESTAMPS
        private uint _tickLastActionTime;
        private uint _tickLastQueueProcessTime = Time.Ticks;


        //OPTIONS TO VARS
        private bool UCCAL_EnableAL = ProfileManager.CurrentProfile.UOClassicCombatAL_EnableAL;
        private bool UCCAL_EnableSL = ProfileManager.CurrentProfile.UOClassicCombatAL_EnableSL;
        private bool UCCAL_EnableALLow = ProfileManager.CurrentProfile.UOClassicCombatAL_EnableALLow;
        private bool UCCAL_EnableSLLow = ProfileManager.CurrentProfile.UOClassicCombatAL_EnableSLLow;
        private uint UCCAL_LootDelay = ProfileManager.CurrentProfile.UOClassicCombatAL_LootDelay; //DELAY BETWEEN TWO ITEMS TO LOOT 500MS DEFAULT
        private uint UCCAL_QueueSpeed = ProfileManager.CurrentProfile.UOClassicCombatAL_QueueSpeed; //DELAY BETWEEN RUNNING THE QUEUE 100MS DEFAULT
        public static uint UCCAL_PurgeDelay = ProfileManager.CurrentProfile.UOClassicCombatAL_PurgeDelay; //KICK ITEMS OLDER THAN THIS 10SECS DEFAULT
        //
        private bool UCCAL_EnableLootAboveID = ProfileManager.CurrentProfile.UOClassicCombatAL_EnableLootAboveID;
        private uint UCCAL_LootAboveID = ProfileManager.CurrentProfile.UOClassicCombatAL_LootAboveID;
        //
        public static uint UCCAL_SL_Gray = ProfileManager.CurrentProfile.UOClassicCombatAL_SL_Gray;
        public static uint UCCAL_SL_Blue = ProfileManager.CurrentProfile.UOClassicCombatAL_SL_Blue;
        public static uint UCCAL_SL_Green = ProfileManager.CurrentProfile.UOClassicCombatAL_SL_Green;
        public static uint UCCAL_SL_Red = ProfileManager.CurrentProfile.UOClassicCombatAL_SL_Red;
        //OPTIONS TO VARS

        //RNG CURRENTLY NOT IN USE
        private uint _tickLastRNGCalced = Time.Ticks;
        private int _varRNGtoWait = 0;
        private bool _waitRNG;
        private uint _tickWaitRNG = Time.Ticks;
        private bool _doneWaitRNG;

        //LOOT LIST FROM TXT
        public static readonly List<ushort> ALList = new List<ushort>();
        public static readonly List<ushort> ALListLow = new List<ushort>();

        //TO LOOT LIST QUEUE
        public static readonly List<ToLootItem> ToLootList = new List<ToLootItem>();

        public UOClassicCombatAL() : base(0, 0)
        {
            CanMove = true;
            CanCloseWithEsc = false;
            CanCloseWithRightClick = false;
            AcceptMouseInput = true;
            AcceptKeyboardInput = false;

            //MAIN CONSTRUCT
            Width = 141;
            Height = 125;

            //ACTUAL UI
            
            Add(_background = new AlphaBlendControl()
            {
                Alpha = 0.6f,
                Width = Width,
                Height = Height
            });

            Add(_background);

            _title = new Label("UCC -AL-", true, HUE_FONTS_BLUE, 0, 1, FontStyle.BlackBorder)
            {
                X = 2,
                Y = 2
            };
            Add(_title);

            //PERMANENT UI
            _uiCboxEnableAL = new Checkbox(0x00D2, 0x00D3, "AL", FONT, HUE_FONT)
            {
                X = 2,
                Y = _title.Bounds.Bottom + 2,
                IsChecked = ProfileManager.CurrentProfile.UOClassicCombatAL_EnableAL
            };
            _uiCboxEnableAL.ValueChanged += (sender, e) =>
            {
                ProfileManager.CurrentProfile.UOClassicCombatAL_EnableAL = _uiCboxEnableAL.IsChecked;
                UpdateVars();
            };
            Add(_uiCboxEnableAL);
            //
            _uiCboxEnableSL = new Checkbox(0x00D2, 0x00D3, "SL", FONT, HUE_FONT)
            {
                X = 2,
                Y = _uiCboxEnableAL.Bounds.Bottom + 2,
                IsChecked = ProfileManager.CurrentProfile.UOClassicCombatAL_EnableSL
            };
            _uiCboxEnableSL.ValueChanged += (sender, e) =>
            {
                ProfileManager.CurrentProfile.UOClassicCombatAL_EnableSL = _uiCboxEnableSL.IsChecked;
                UpdateVars();
            };
            Add(_uiCboxEnableSL);
            ////
            _uiCboxEnableALLow = new Checkbox(0x00D2, 0x00D3, "ALLow", FONT, HUE_FONT)
            {
                X = 2,
                Y = _uiCboxEnableSL.Bounds.Bottom + 2,
                IsChecked = ProfileManager.CurrentProfile.UOClassicCombatAL_EnableALLow
            };
            _uiCboxEnableALLow.ValueChanged += (sender, e) =>
            {
                ProfileManager.CurrentProfile.UOClassicCombatAL_EnableALLow = _uiCboxEnableALLow.IsChecked;
                UpdateVars();
            };
            Add(_uiCboxEnableALLow);
            //
            _uiCboxEnableSLLow = new Checkbox(0x00D2, 0x00D3, "SLLow", FONT, HUE_FONT)
            {
                X = 2,
                Y = _uiCboxEnableALLow.Bounds.Bottom + 2,
                IsChecked = ProfileManager.CurrentProfile.UOClassicCombatAL_EnableSLLow
            };
            _uiCboxEnableSLLow.ValueChanged += (sender, e) =>
            {
                ProfileManager.CurrentProfile.UOClassicCombatAL_EnableSLLow = _uiCboxEnableSLLow.IsChecked;
                UpdateVars();
            };
            Add(_uiCboxEnableSLLow);
            ////
            _uiTextQueue = new Label("Queue: ", true, HUE_FONTS_GREEN, 0, 1, FontStyle.BlackBorder)
            {
                X = 2,
                Y = _uiCboxEnableSLLow.Bounds.Bottom + 2
            };
            Add(_uiTextQueue);
            _uiTextQueueSize = new Label("0", true, HUE_FONTS_GREEN, 0, 1, FontStyle.BlackBorder)
            {
                X = _uiTextQueue.Bounds.Right + 2,
                Y = _uiCboxEnableSLLow.Bounds.Bottom + 2
            };
            Add(_uiTextQueueSize);
            //PERMANENT UI

            //UPDATE VARS FROM PROFILE
            UpdateVars();

            //READ FILE
            LoadFile();

            //COPY PASTED
            LayerOrder = UILayer.Over;
            WantUpdateSize = false;

        }
        //MAIN
        public override void Update(double totalMS, double frameMS)
        {
            if (World.Player == null || World.Player.IsDestroyed)
                return;

            if (!UCCAL_EnableAL && !UCCAL_EnableALLow) //DO NOTHING IF BOTH ARE DISABLED
                return;

            //UPDATE COUNTERS
            UpdateCounters();

            //LIST MAINTENANCE
            if (Time.Ticks >= _tickLastQueueProcessTime + UCCAL_QueueSpeed)
            {
                //PURGE OLD ITEMS
                PurgeQueue();

                //SORT THE LIST BY PRIORITY
                SortQueue();
            }

            //PROCESSQUEUE
            ProcessQueue();

            base.Update(totalMS, frameMS);
        }
        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            return base.Draw(batcher, x, y);
        }
        protected override void OnDragEnd(int x, int y)
        {
            base.OnDragEnd(x, y);
            ProfileManager.CurrentProfile.UOClassicCombatALLocation = Location;
        }
        public void UpdateVars()
        {
            //UPDATE VARS
            UCCAL_EnableAL = ProfileManager.CurrentProfile.UOClassicCombatAL_EnableAL;
            UCCAL_EnableSL = ProfileManager.CurrentProfile.UOClassicCombatAL_EnableSL;
            UCCAL_EnableALLow = ProfileManager.CurrentProfile.UOClassicCombatAL_EnableALLow;
            UCCAL_EnableSLLow = ProfileManager.CurrentProfile.UOClassicCombatAL_EnableSLLow;

            //SET TOGGLE INCASE CHANGED
            _uiCboxEnableAL.IsChecked = ProfileManager.CurrentProfile.UOClassicCombatAL_EnableAL;
            _uiCboxEnableSL.IsChecked = ProfileManager.CurrentProfile.UOClassicCombatAL_EnableSL;
            _uiCboxEnableALLow.IsChecked = ProfileManager.CurrentProfile.UOClassicCombatAL_EnableALLow;
            _uiCboxEnableSLLow.IsChecked = ProfileManager.CurrentProfile.UOClassicCombatAL_EnableSLLow;

            //DELAYS
            UCCAL_LootDelay = ProfileManager.CurrentProfile.UOClassicCombatAL_LootDelay;
            UCCAL_PurgeDelay = ProfileManager.CurrentProfile.UOClassicCombatAL_PurgeDelay;
            UCCAL_QueueSpeed = ProfileManager.CurrentProfile.UOClassicCombatAL_QueueSpeed;

            //LOOTABOVEID
            UCCAL_EnableLootAboveID = ProfileManager.CurrentProfile.UOClassicCombatAL_EnableLootAboveID;
            UCCAL_LootAboveID = ProfileManager.CurrentProfile.UOClassicCombatAL_LootAboveID;

            //COLORS
            UCCAL_SL_Gray = ProfileManager.CurrentProfile.UOClassicCombatAL_SL_Gray;
            UCCAL_SL_Blue = ProfileManager.CurrentProfile.UOClassicCombatAL_SL_Blue;
            UCCAL_SL_Green = ProfileManager.CurrentProfile.UOClassicCombatAL_SL_Green;
            UCCAL_SL_Red = ProfileManager.CurrentProfile.UOClassicCombatAL_SL_Red;
    }
        public override void Dispose()
        {
            base.Dispose();
        }
        //MAIN LOOP METHODS
        private void UpdateCounters()
        {
            _uiTextQueueSize.Text = $"{ToLootList.Count}";
        }
        private void ProcessQueue()
        {
            if (Time.Ticks <= _tickLastQueueProcessTime + UCCAL_QueueSpeed)
                return;


            if (ToLootList.Count <= 0) //NOTHING TO PROCESS
            {
                //SET TIME FOR LAST FULL ProcessQueue() RUN
                _tickLastQueueProcessTime = Time.Ticks;
                return;
            }

            for (int i = ToLootList.Count - 1; i > -1; i--)
            {
                if (Time.Ticks <= _tickLastActionTime + UCCAL_LootDelay) //break out of loop if just looted an item ie. shorter than delay
                    break;

                if (ItemHold.Enabled) //dont do while dragging
                {
                    _tickLastQueueProcessTime = Time.Ticks;
                    break;
                }

                if (TargetManager.IsTargeting) //dont do while targeting
                {
                    _tickLastQueueProcessTime = Time.Ticks;
                    break;
                }

                //SOME CHECKS ON THE ITEM
                Item lootitem = World.Items.Get(ToLootList[i].ToLootItemSerial);

                if (lootitem == null)
                {
                    ToLootList.RemoveAt(i);
                    continue; //GO TO NEXT ITEM IN FOR LOOP
                }

                if (!lootitem.IsLootable)
                {
                    ToLootList.RemoveAt(i);
                    continue; //GO TO NEXT ITEM IN FOR LOOP
                }

                //SOME CHEKS ON THE CORPSE
                Item corpse = World.Items.Get(ToLootList[i].ToLootItemContainerSerial);

                if (corpse == null)
                {
                    ToLootList.RemoveAt(i);
                    continue; //GO TO NEXT ITEM IN FOR LOOP
                }

                if (corpse.Distance >= 3)
                {
                    ToLootList.RemoveAt(i);
                    continue; //GO TO NEXT ITEM IN FOR LOOP
                }

                //SOME NEEDED STUFF
                Item backpack = World.Player.FindItemByLayer(Layer.Backpack);

                if (backpack == null)
                {
                    _tickLastQueueProcessTime = Time.Ticks;
                    break;
                }

                GameScene gs = Client.Game.GetScene<GameScene>();

                //CHECK AMOUNT
                ushort amount = 0;
                if (ToLootList[i].ToLootItemAmount > ToLootList[i].ToLootLootAmount)
                {
                    amount = ToLootList[i].ToLootLootAmount;
                }
                else
                {
                    amount = ToLootList[i].ToLootItemAmount;
                }

                //LOOT IT
                if (ToLootList[i].ToLootItemAmount == 1)
                {
                    //IF A SINGLE ITEM, DROP AT TOP LEFT IN BACKPACK
                    GameActions.PickUp(ToLootList[i].ToLootItemSerial, 0, 0, amount);
                    GameActions.DropItem(ItemHold.Serial, 0xFFFF, 0xFFFF, 0, backpack.Serial);
                }
                else
                {
                    //IF AMOUNT MERGE TO BACKPACK
                    GameActions.GrabItem(ToLootList[i].ToLootItemSerial, Convert.ToUInt16(amount), backpack);
                }

                //SET TIMERS
                _tickLastActionTime = Time.Ticks;
                _tickLastQueueProcessTime = Time.Ticks;

                //NOTIFICATIONS
                if (lootitem.Name != null)
                    GameActions.Print($"UCC AL: ALed an item:\t {lootitem.Name}");
                else
                    GameActions.Print($"UCC AL: ALed an item");

                ToLootList.RemoveAt(i);

                //TESTING WITH A BREAK HERE
                break;
            }
        }
        //CHECK METHODS
        public void CheckLootItem(Item tempToLootItem)
        {
            //CHECK IF LOOT ABOVE ID IS ENABLED AND ITEM IS ABOVE THE SET ID
            if (UCCAL_EnableLootAboveID)
            {
                if (tempToLootItem.Graphic >= UCCAL_LootAboveID)
                {
                    ushort Amount = 0;

                    int index = ALList.IndexOf(tempToLootItem.Graphic);
                    Amount = ALList[index + 1];

                    //ADD IT TO TOLOOTLIST
                    ToLootList.Add(new ToLootItem() { ToLootItemSerial = tempToLootItem.Serial, ToLootItemContainerSerial = tempToLootItem.Container, ToLootItemPriority = index, ToLootItemAmount = tempToLootItem.Amount, ToLootLootAmount = Amount, ToLootItemTick = Time.Ticks });

                    //return to not add an item twice incase its in the list too
                    return;
                }
            }

            //CHECK IF IN LIST FROM TXT
            if (ALList.Contains(tempToLootItem.Graphic))
            {
                ushort Amount = 0;

                int index = ALList.IndexOf(tempToLootItem.Graphic);
                Amount = ALList[index + 1];

                //ADD IT TO TOLOOTLIST
                ToLootList.Add(new ToLootItem() { ToLootItemSerial = tempToLootItem.Serial, ToLootItemContainerSerial = tempToLootItem.Container, ToLootItemPriority = index, ToLootItemAmount = tempToLootItem.Amount, ToLootLootAmount = Amount, ToLootItemTick = Time.Ticks });
            }
        }
        public void CheckLootItemLow(Item tempToLootItem)
        {
            //CHECK IF IN LOW LIST FROM TXT
            if (ALListLow.Contains(tempToLootItem.Graphic))
            {
                ushort Amount = 0;

                int index = ALListLow.IndexOf(tempToLootItem.Graphic);
                Amount = ALListLow[index + 1];
                
                //ADD IT TO TOLOOTLIST
                //Low list has a fixed priority of 100 + N
                ToLootList.Add(new ToLootItem() { ToLootItemSerial = tempToLootItem.Serial, ToLootItemContainerSerial = tempToLootItem.Container, ToLootItemPriority = index + 100, ToLootItemAmount = tempToLootItem.Amount, ToLootLootAmount = Amount, ToLootItemTick = Time.Ticks });
            }
        }
        public static bool CheckSL(Item tempCorpse)
        {
            if (tempCorpse.LootFlag != 0xFF/*null*/ && tempCorpse.LootFlag == UCCAL_SL_Gray) //grey
                return true;

            if (tempCorpse.LootFlag != 0xFF/*null*/ && tempCorpse.LootFlag == UCCAL_SL_Red) //red
                return true;

            if (tempCorpse.LootFlag != 0xFF/*null*/ && tempCorpse.LootFlag == UCCAL_SL_Green) //green
                return true;

            if (tempCorpse.LootFlag != 0xFF/*null*/ && tempCorpse.LootFlag == UCCAL_SL_Blue) //blue
                return false;

            return false;
        }
        public static void SortQueue()
        {
            //actualy sort the list from lowest to highest priority, because the queue needs to be ran through from the back,
            //removing items from front in a list could cause problems
            ToLootList.Sort((x, y) => y.ToLootItemPriority.CompareTo(x.ToLootItemPriority));
        }
        public static void PurgeQueue()
        {
            for (int i = ToLootList.Count - 1; i > -1; i--)
            {
                if (ToLootList[i].ToLootItemTick + UCCAL_PurgeDelay <= Time.Ticks)
                {
                    ToLootList.RemoveAt(i);
                }
            }
        }
        public static void UpdatePurgeQueue(uint corpse)
        {
            for (int i = ToLootList.Count - 1; i > -1; i--)
            {
                if (ToLootList[i].ToLootItemContainerSerial == corpse)
                {
                    ToLootList.RemoveAt(i);
                }
            }
        }
        //TRIGGERS
        public void OpenCorpseTrigger(uint corpseserial)
        {
            Item _corpse = World.Items.Get(corpseserial);

            if (_corpse == null)
                return;

            //POSSIBLE SAFETY CHECKS  <--------------------------------------------------------------------------------------------------------
            //_corpse.OnGround && _corpse.Distance > 3

            //OPENING CORPSE IS AN ACTION
            _tickLastActionTime = Time.Ticks;

            //CHECK ITEMS IN CORPSE
            for (var i = _corpse.Items; i != null; i = i.Next)
            {
                Item it = (Item) i;

                if (it.IsLootable)
                {

                    if (UCCAL_EnableAL)
                    {
                        //CHECK SL
                        if (UCCAL_EnableSL && !CheckSL(_corpse))
                        {
                            //DO NOTHING
                        }
                        else
                        {
                            CheckLootItem(it); //PASS TO LOOT LIST CHECK
                        }
                    }
                    if (UCCAL_EnableALLow)
                    {
                        //CHECK SL
                        if (UCCAL_EnableSLLow && !CheckSL(_corpse))
                        {
                            //DO NOTHING
                        }
                        else
                        {
                            CheckLootItemLow(it); //PASS TO LOW LOOT LIST CHECK
                        }
                    }
                }
            }
        }
        public void UpdateCorpseTrigger(uint corpseserial)
        {
            Item _corpse = World.Items.Get(corpseserial);

            if (_corpse == null)
                return;

            //POSSIBLE SAFETY CHECKS  <--------------------------------------------------------------------------------------------------------
            //_corpse.OnGround && _corpse.Distance > 3

            //DO UPDATE PURGE
            UpdatePurgeQueue(_corpse.Serial);

            //CHECK ITEMS IN CORPSE
            for (var i = _corpse.Items; i != null; i = i.Next)
            {
                Item it = (Item)i;

                if (it.IsLootable)
                {

                    if (UCCAL_EnableAL)
                    {
                        //CHECK SL
                        if (UCCAL_EnableSL && !CheckSL(_corpse))
                        {
                            //DO NOTHING
                        }
                        else
                        {
                            CheckLootItem(it); //PASS TO LOOT LIST CHECK
                        }
                    }
                    if (UCCAL_EnableALLow)
                    {
                        //CHECK SL
                        if (UCCAL_EnableSLLow && !CheckSL(_corpse))
                        {
                            //DO NOTHING
                        }
                        else
                        {
                            CheckLootItemLow(it); //PASS TO LOW LOOT LIST CHECK
                        }
                    }
                }
            }
        }
        //GET AUTO LOOT LIST FROM FILE OR CREATE IT
        public static void LoadFile()
        {
            string path = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Client");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            string ALlist = Path.Combine(path, "ALlist.txt");
            string ALlistlow = Path.Combine(path, "ALlistlow.txt");

            if (!File.Exists(ALlist))
            {
                using (StreamWriter writer = new StreamWriter(ALlist))
                {
                    ushort[] items = {
                        //NOTE - some stuff is commented out, because its above the 0x5780 (22400) LootAboveID
                        //NOTE - some stuff is commented out, because mobs may have them equipped
                        //STRANGELANDS
                        //Drink
                        //0xAD1D, 0xB069,
                        //Food
                        0x1E88, 0x1E89,
                        0x1E90, 0x1E91,
                        //Jars
                        //0xAD20, 0xAD21, 0xAD22, 0xAD23, 0xAD24,
                        //Books
                        0x0FBD, 0x0FBE,
                        0x1C13,
                        0x42BF,
                        //NON STRANGELANDS ----------------------------------------------------------
                        //cores
                        0xf91,
                        //extracts
                        0xefc,
                        //phylactery
                        0x240,
                        //skillscrolls
                        0x227a,
                        //skillballs
                        0x5740,
                        //mcd
                        0x42BF,
                        //-------
                        //backpack dye
                        0xeff,
                        //shield & hair dye
                        0xf03,
                        //furniture dye
                        //0x7161,
                        //headwear dye
                        0xf02,
                        //runebook dye
                        0xefe,
                        //-------
                        //tmap
                        0x14EB, 0x14EC,
                        //deed
                        0x14EF, 0x14F0,
                        //-------
                        //hanging lantern OFF BECAUSE SOME MOBS MAY HAVE EM EQUIPPED
                        //0x0A15, 0x0A16, 0x0A17, 0x0A18,
                        //0x0A1A, 0x0A1B, 0x0A1C, 0x0A1D,
                        //lantern OFF BECAUSE SOME MOBS MAY HAVE EM EQUIPPED
                        //0x0A22, 0x0A23,0x0A24, 0x0A25,
                        //-------
                        //cloth (folded, cut cloth)
                        0x175D, 0x175E, 0x175F, 0x1760, 0x1761,0x1762,0x1763, 0x1764, 0x1765, 0x1766, 0x1767, 0x1768,
                        //-------
                        //footwear OFF BECAUSE SOME MOBS MAY HAVE EM EQUIPPED
                        //0x170B, 0x170C, 0x170D, 0x170E, 0x170F, 0x1710, 0x1711, 0x1712,
                        //-------
                        //statue (small)
                        0x42BB,
                        //statue (big)
                        //0xA615,
                        //-------
                        //skull (bossloot)
                        //0xA9B5,
                        ////-------
                        //chain links
                        //0xA8C6,

                    };

                    for (int i = 0; i < items.Length; i++)
                    {
                        ushort graphic = items[i];
                        ushort amount = 9999;

                        writer.WriteLine($"{graphic}={amount}");
                    }
                }
            }

            if (!File.Exists(ALlistlow))
            {
                using (StreamWriter writer = new StreamWriter(ALlistlow))
                {
                    ushort[] items = {
                        //ADD LOW PRIO STUFF HERE
                        0xeed, //gold
                        0x4202, 0x0E73, //modded gold
                        0xF7A, 0xF7B, 0xF8C, 0xF8D, 0xF84, 0xF85, 0xF86, 0xF88, //mage reags
                        0xf3f //arrow
                    };

                    for (int i = 0; i < items.Length; i++)
                    {
                        ushort graphic = items[i];
                        ushort amount = 9999;

                        writer.WriteLine($"{graphic}={amount}");
                    }
                }
            }

            TextFileParser ALlistParser = new TextFileParser(File.ReadAllText(ALlist), new[] { ' ', '\t', ',', '=' }, new[] { '#', ';' }, new[] { '"', '"' });
            TextFileParser ALlistParserLow = new TextFileParser(File.ReadAllText(ALlistlow), new[] { ' ', '\t', ',', '=' }, new[] { '#', ';' }, new[] { '"', '"' });

            while (!ALlistParser.IsEOF())
            {
                var ss = ALlistParser.ReadTokens();
                if (ss != null && ss.Count != 0)
                {
                    if (ushort.TryParse(ss[0], out ushort graphic))
                    {
                        ALList.Add(graphic);
                    }

                    if (ushort.TryParse(ss[1], out ushort amount))
                    {
                        ALList.Add(amount);
                    }
                }
            }

            while (!ALlistParserLow.IsEOF())
            {
                var ss = ALlistParserLow.ReadTokens();
                if (ss != null && ss.Count != 0)
                {
                    if (ushort.TryParse(ss[0], out ushort graphic))
                    {
                        ALListLow.Add(graphic);
                    }

                    if (ushort.TryParse(ss[1], out ushort amount))
                    {
                        ALListLow.Add(amount);
                    }
                }
            }
        }
    }
}