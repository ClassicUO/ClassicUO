// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game.Entities.Items
{
    /// <summary>
    /// Handles drag-related <see cref="EventSink"/> events for items:
    /// <see cref="EventSink.ItemDragEnded"/>, <see cref="EventSink.ItemDropAccepted"/>,
    /// and <see cref="EventSink.ItemDragAnimation"/>. The first two reset the
    /// game cursor's held item; the animation handler spawns a drag/moving effect
    /// between source and destination entities.
    /// Replaces the legacy <c>OnItemDragEnded</c>, <c>OnItemDropAccepted</c>,
    /// and <c>OnItemDragAnimation</c> handlers from <c>World.Subscribers</c>.
    /// </summary>
    internal sealed class ItemDragHandler : IEventListener
    {
        private readonly World _world;

        public ItemDragHandler(World world) => _world = world;

        public void Subscribe()
        {
            EventSink.ItemDragEnded += OnItemDragEnded;
            EventSink.ItemDropAccepted += OnItemDropAccepted;
            EventSink.ItemDragAnimation += OnItemDragAnimation;
        }

        public void Unsubscribe()
        {
            EventSink.ItemDragEnded -= OnItemDragEnded;
            EventSink.ItemDropAccepted -= OnItemDropAccepted;
            EventSink.ItemDragAnimation -= OnItemDragAnimation;
        }

        private void OnItemDragEnded(ItemDragEndedArgs e)
        {
            Client.Game.UO.GameCursor.ItemHold.Enabled = false;
            Client.Game.UO.GameCursor.ItemHold.Dropped = false;
        }

        private void OnItemDropAccepted(ItemDropAcceptedArgs e)
        {
            Client.Game.UO.GameCursor.ItemHold.Enabled = false;
            Client.Game.UO.GameCursor.ItemHold.Dropped = false;
        }

        private void OnItemDragAnimation(ItemDragAnimationArgs e)
        {
            ushort graphic = e.Graphic;
            if (graphic == 0x0EED) graphic = 0x0EEF;
            else if (graphic == 0x0EEA) graphic = 0x0EEC;
            else if (graphic == 0x0EF0) graphic = 0x0EF2;

            uint source = e.Source;
            uint dest = e.Destination;
            ushort srcX = e.SourceX, srcY = e.SourceY;
            sbyte srcZ = e.SourceZ;
            ushort dstX = e.DestinationX, dstY = e.DestinationY;
            sbyte dstZ = e.DestinationZ;

            Mobile sourceEntity = _world.Mobiles.Get(source);
            if (sourceEntity == null) source = 0;
            else { srcX = sourceEntity.X; srcY = sourceEntity.Y; srcZ = sourceEntity.Z; }

            Mobile destEntity = _world.Mobiles.Get(dest);
            if (destEntity == null) dest = 0;
            else { dstX = destEntity.X; dstY = destEntity.Y; dstZ = destEntity.Z; }

            _world.SpawnEffect(
                !SerialHelper.IsValid(source) || !SerialHelper.IsValid(dest)
                    ? GraphicEffectType.Moving
                    : GraphicEffectType.DragEffect,
                source,
                dest,
                graphic,
                e.Hue,
                srcX, srcY, srcZ,
                dstX, dstY, dstZ,
                5,
                5000,
                true,
                false,
                false,
                GraphicEffectBlendMode.Normal
            );
        }
    }
}
