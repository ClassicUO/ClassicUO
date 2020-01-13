#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
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


//using ClassicUO.Game.GameObjects;
//using ClassicUO.Game.Map;
//using ClassicUO.Utility;

//namespace ClassicUO.Game.Managers
//{
//    internal static class PoolsManager
//    {
//        private static QueuedPool<Tile> _tilePool;
//        private static QueuedPool<Chunk> _chunkPool;
//        private static QueuedPool<Land> _landPool;
//        private static QueuedPool<Static> _staticPool;
//        private static QueuedPool<Multi> _multiPool;

//        private static QueuedPool<Item> _itemPool;
//        private static QueuedPool<Mobile> _mobilePool;


//        public static void Initialize()
//        {
//            _chunkPool = new QueuedPool<Chunk>(2000);
//            _tilePool = new QueuedPool<Tile>(20_000);
//            _landPool = new QueuedPool<Land>(20_000);
//            _staticPool = new QueuedPool<Static>(40_000);
//            _multiPool = new QueuedPool<Multi>(20_000);

//            _itemPool = new QueuedPool<Item>(5000);
//            _mobilePool = new QueuedPool<Mobile>(5000);
//        }


//        public static Chunk GetChunk()
//        {
//            return _chunkPool.GetOne();
//        }

//        public static Tile GetTile()
//        {
//            return _tilePool.GetOne();
//        }

//        public static Land GetLand()
//        {
//            return _landPool.GetOne();
//        }

//        public static Static GetStatic()
//        {
//            return _staticPool.GetOne();
//        }

//        public static Multi GetMulti()
//        {
//            return _multiPool.GetOne();
//        }

//        public static Mobile GetMobile()
//        {
//            return _mobilePool.GetOne();
//        }

//        public static Item GetItem()
//        {
//            return _itemPool.GetOne();
//        }



//        public static void PushChunk(Chunk c)
//        {
//            _chunkPool.ReturnOne(c);
//        }

//        public static void PushTile(Tile t)
//        {
//            _tilePool.ReturnOne(t);
//        }

//        public static void PushLand(Land l)
//        {
//            _landPool.ReturnOne(l);
//        }

//        public static void PushStatic(Static s)
//        {
//            _staticPool.ReturnOne(s);
//        }

//        public static void PushMulti(Multi m)
//        {
//            _multiPool.ReturnOne(m);
//        }

//        public static void PushMobile(Mobile m)
//        {
//            _mobilePool.ReturnOne(m);
//        }

//        public static void PushItem(Item i)
//        {
//            _itemPool.ReturnOne(i);
//        }
//    }
//}