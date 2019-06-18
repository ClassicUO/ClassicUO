using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;
using ClassicUO.Utility;

namespace ClassicUO.Game.Managers
{
    static class PoolsManager
    {
        private static QueuedPool<Tile> _tilePool;
        private static QueuedPool<Chunk> _chunkPool;
        private static QueuedPool<Land> _landPool;
        private static QueuedPool<Static> _staticPool;
        private static QueuedPool<Multi> _multiPool;

        private static QueuedPool<Item> _itemPool;
        private static QueuedPool<Mobile> _mobilePool;


        public static void Initialize()
        {
            _chunkPool = new QueuedPool<Chunk>(200);
            _tilePool = new QueuedPool<Tile>(20_000);
            _landPool = new QueuedPool<Land>(20_000);
            _staticPool = new QueuedPool<Static>(20_000);
            _multiPool = new QueuedPool<Multi>(20_000);

            _itemPool = new QueuedPool<Item>(5000);
            _mobilePool = new QueuedPool<Mobile>(5000);
        }


        public static Chunk GetChunk()
            => _chunkPool.GetOne();

        public static Tile GetTile()
            => _tilePool.GetOne();

        public static Land GetLand()
            => _landPool.GetOne();

        public static Static GetStatic()
            => _staticPool.GetOne();

        public static Multi GetMulti()
            => _multiPool.GetOne();

        public static Mobile GetMobile()
            => _mobilePool.GetOne();

        public static Item GetItem()
            => _itemPool.GetOne();



        public static void PushChunk(Chunk c)
            => _chunkPool.ReturnOne(c);

        public static void PushTile(Tile t)
            => _tilePool.ReturnOne(t);

        public static void PushLand(Land l)
            => _landPool.ReturnOne(l);

        public static void PushStatic(Static s)
            => _staticPool.ReturnOne(s);

        public static void PushMulti(Multi m)
            => _multiPool.ReturnOne(m);

        public static void PushMobile(Mobile m)
            => _mobilePool.ReturnOne(m);

        public static void PushItem(Item i)
            => _itemPool.ReturnOne(i);
    }
}
