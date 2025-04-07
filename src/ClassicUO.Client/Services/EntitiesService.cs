// using System;
// using System.Collections.Generic;
// using ClassicUO.Game;
// using ClassicUO.Game.GameObjects;
// using ClassicUO.Game.UI.Gumps;

// namespace ClassicUO.Services;

// internal class EntitiesService : IService
// {
//     public bool IsGame => Player != null;
//     public PlayerMobile? Player { get; private set; }
//     public Dictionary<uint, Item> Items { get; } = [];
//     public Dictionary<uint, Mobile> Mobiles { get; } = [];

//     public event EventHandler<uint> OnPlayerCreated, OnMobileCreated, OnItemCreated;
//     public event EventHandler<uint> OnPlayerDeleted, OnMobileDeleted, OnItemDeleted;

//     public void CreatePlayer(World world, uint serial)
//     {
//         Player = new PlayerMobile(world, serial);
//         Mobiles.Add(serial, Player);

//         OnPlayerCreated?.Invoke(world, serial);
//     }

//     public void DeletePlayer()
//     {
//         var world = Player?.World;
//         var serial = Player?.Serial ?? 0;
//         Player?.Destroy();
//         Player = null;

//         if (serial != 0)
//             OnPlayerDeleted?.Invoke(world, serial);

//         Mobiles.Remove(serial);
//     }

//     public void Clear()
//     {
//         foreach (Mobile mobile in Mobiles.Values)
//             RemoveMobile(mobile);

//         foreach (Item item in Items.Values)
//             RemoveItem(item);

//         Items.Clear();
//         Mobiles.Clear();
//         DeletePlayer();
//     }

//     public bool Contains(uint serial)
//     {
//         if (SerialHelper.IsItem(serial))
//         {
//             return Items.Contains(serial);
//         }

//         return SerialHelper.IsMobile(serial) && Mobiles.Contains(serial);
//     }

//     public Entity? Get(uint serial)
//     {
//         Entity? ent;

//         if (SerialHelper.IsMobile(serial))
//         {
//             ent = Mobiles.Get(serial);

//             if (ent == null)
//             {
//                 ent = Items.Get(serial);
//             }
//         }
//         else
//         {
//             ent = Items.Get(serial);

//             if (ent == null)
//             {
//                 ent = Mobiles.Get(serial);
//             }
//         }

//         if (ent != null && ent.IsDestroyed)
//         {
//             ent = null;
//         }

//         return ent;
//     }

//     public Item GetOrCreateItem(World world, uint serial)
//     {
//         var item = Items.Get(serial);

//         if (item != null && item.IsDestroyed)
//         {
//             Items.Remove(serial);
//             item = null;
//         }

//         if (item == null /*|| item.IsDestroyed*/)
//         {
//             item = Item.Create(world, serial);
//             Items.Add(item);
//             OnItemCreated?.Invoke(world, serial);
//         }

//         return item;
//     }

//     public Mobile GetOrCreateMobile(World world, uint serial)
//     {
//         var mob = Mobiles.Get(serial);

//         if (mob != null && mob.IsDestroyed)
//         {
//             Mobiles.Remove(serial);
//             mob = null;
//         }

//         if (mob == null /*|| mob.IsDestroyed*/)
//         {
//             mob = Mobile.Create(world, serial);
//             Mobiles.Add(mob);
//             OnMobileCreated?.Invoke(world, serial);
//         }

//         return mob;
//     }

//     public void RemoveItemFromContainer(uint serial)
//     {
//         var it = Items.Get(serial);

//         if (it != null)
//         {
//             RemoveItemFromContainer(it);
//         }
//     }

//     public void RemoveItemFromContainer(Item obj)
//     {
//         uint containerSerial = obj.Container;

//         // if entity is running the "dying" animation we have to reset container too.
//         // SerialHelper.IsValid(containerSerial) is not ideal in this case
//         if (containerSerial != 0xFFFF_FFFF)
//         {
//             if (SerialHelper.IsMobile(containerSerial))
//             {
//                 ServiceProvider.Get<GuiService>().GetGump<PaperDollGump>(containerSerial)?.RequestUpdateContents();
//             }
//             else if (SerialHelper.IsItem(containerSerial))
//             {
//                 ServiceProvider.Get<GuiService>().GetGump<ContainerGump>(containerSerial)?.RequestUpdateContents();
//             }

//             var container = Get(containerSerial);

//             if (container != null)
//             {
//                 container.Remove(obj);
//             }

//             obj.Container = 0xFFFF_FFFF;
//         }

//         obj.Next = null;
//         obj.Previous = null;
//         obj.RemoveFromTile();
//     }

//     public bool RemoveItem(uint serial, bool forceRemove = false)
//     {
//         var item = Items.Get(serial);

//         if (item == null || item.IsDestroyed)
//         {
//             return false;
//         }

//         var first = item.Items;
//         RemoveItemFromContainer(item);

//         while (first != null)
//         {
//             var next = first.Next;

//             if (first is Item it)
//                 RemoveItem(it.Serial, forceRemove);

//             first = next;
//         }

//         OPL.Remove(serial);
//         item.Destroy();

//         if (forceRemove)
//         {
//             Items.Remove(serial);
//             OnItemDeleted?.Invoke(item.World, serial);
//         }

//         return true;
//     }

//     public bool RemoveMobile(uint serial, bool forceRemove = false)
//     {
//         var mobile = Mobiles.Get(serial);

//         if (mobile == null || mobile.IsDestroyed)
//         {
//             return false;
//         }

//         var first = mobile.Items;

//         while (first != null)
//         {
//             var next = first.Next;

//             if (first is Item it)
//                 RemoveItem(it.Serial, forceRemove);

//             first = next;
//         }

//         OPL.Remove(serial);
//         mobile.Destroy();

//         if (forceRemove)
//         {
//             Mobiles.Remove(serial);
//             OnMobileDeleted?.Invoke(mobile.World, serial);
//         }

//         return true;
//     }
// }