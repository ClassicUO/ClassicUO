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