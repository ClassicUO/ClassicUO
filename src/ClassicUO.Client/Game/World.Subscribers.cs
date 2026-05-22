// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game
{
    internal sealed partial class World
    {
        private readonly System.Collections.Generic.List<IEventListener> _listeners = new();

        /// <summary>
        /// Register an <see cref="IEventListener"/> so its
        /// <c>Subscribe()</c> / <c>Unsubscribe()</c> are driven centrally
        /// from <see cref="SubscribeEvents"/> / <see cref="UnsubscribeEvents"/>.
        /// Returns the listener for fluent assignment.
        /// </summary>
        internal T RegisterListener<T>(T listener) where T : IEventListener
        {
            _listeners.Add(listener);
            return listener;
        }

        private void SubscribeEvents()
        {
            EventSink.GraphicEffectSpawned += OnGraphicEffectSpawned;
            EventSink.BuffApplied += OnBuffApplied;
            EventSink.BuffRemoved += OnBuffRemoved;
            EventSink.ObjectDeleted += OnObjectDeleted;
            EventSink.ItemDragEnded += OnItemDragEnded;
            EventSink.ItemDropAccepted += OnItemDropAccepted;
            EventSink.ItemDragAnimation += OnItemDragAnimation;
            EventSink.TipWindowDisplayed += OnTipWindowDisplayed;
            EventSink.OpenUrlRequested += OnOpenUrlRequested;
            EventSink.VendorWindowClosed += OnVendorWindowClosed;
            EventSink.CharacterProfileOpened += OnCharacterProfileOpened;
            EventSink.TextEntryDialogOpened += OnTextEntryDialogOpened;
            EventSink.DyeDataReceived += OnDyeDataReceived;

            foreach (var listener in _listeners) listener.Subscribe();
        }

        public void UnsubscribeEvents()
        {
            foreach (var listener in _listeners) listener.Unsubscribe();

            EventSink.GraphicEffectSpawned -= OnGraphicEffectSpawned;
            EventSink.BuffApplied -= OnBuffApplied;
            EventSink.BuffRemoved -= OnBuffRemoved;
            EventSink.ObjectDeleted -= OnObjectDeleted;
            EventSink.ItemDragEnded -= OnItemDragEnded;
            EventSink.ItemDropAccepted -= OnItemDropAccepted;
            EventSink.ItemDragAnimation -= OnItemDragAnimation;
            EventSink.TipWindowDisplayed -= OnTipWindowDisplayed;
            EventSink.OpenUrlRequested -= OnOpenUrlRequested;
            EventSink.VendorWindowClosed -= OnVendorWindowClosed;
            EventSink.CharacterProfileOpened -= OnCharacterProfileOpened;
            EventSink.TextEntryDialogOpened -= OnTextEntryDialogOpened;
            EventSink.DyeDataReceived -= OnDyeDataReceived;
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

            Mobile sourceEntity = Mobiles.Get(source);
            if (sourceEntity == null) source = 0;
            else { srcX = sourceEntity.X; srcY = sourceEntity.Y; srcZ = sourceEntity.Z; }

            Mobile destEntity = Mobiles.Get(dest);
            if (destEntity == null) dest = 0;
            else { dstX = destEntity.X; dstY = destEntity.Y; dstZ = destEntity.Z; }

            SpawnEffect(
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

        private void OnTipWindowDisplayed(TipWindowDisplayedArgs e)
        {
            int x = e.Flag == 0 ? 200 : 20;
            int y = e.Flag == 0 ? 100 : 20;
            UIManager.Add(new TipNoticeGump(this, e.TipId, e.Flag, e.Text) { X = x, Y = y });
        }

        private void OnOpenUrlRequested(OpenUrlRequestedArgs e)
        {
            if (!string.IsNullOrEmpty(e.Url))
            {
                Utility.Platforms.PlatformHelper.LaunchBrowser(e.Url);
            }
        }

        private void OnVendorWindowClosed(VendorWindowClosedArgs e)
        {
            UIManager.GetGump<ShopGump>(e.VendorSerial)?.Dispose();
        }

        private void OnCharacterProfileOpened(CharacterProfileOpenedArgs e)
        {
            if (Player == null) return;

            UIManager.GetGump<ProfileGump>(e.Serial)?.Dispose();
            UIManager.Add(new ProfileGump(this, e.Serial, e.Header, e.Footer, e.Body, e.Serial == Player.Serial));
        }

        private void OnTextEntryDialogOpened(TextEntryDialogArgs e)
        {
            UIManager.Add(new TextEntryDialogGump(
                this,
                e.Serial,
                143,
                172,
                0,
                (int)e.MaxLength,
                e.Text,
                e.Description,
                e.ButtonId,
                e.ParentId
            )
            {
                CanCloseWithRightClick = true
            });
        }

        private void OnDyeDataReceived(DyeDataReceivedArgs e)
        {
            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(0x0906);
            int x = (Client.Game.ClientBounds.Width >> 1) - (gumpInfo.UV.Width >> 1);
            int y = (Client.Game.ClientBounds.Height >> 1) - (gumpInfo.UV.Height >> 1);

            ColorPickerGump gump = UIManager.GetGump<ColorPickerGump>(e.Serial);
            if (gump == null || gump.IsDisposed || gump.Graphic != e.Graphic)
            {
                gump?.Dispose();
                UIManager.Add(new ColorPickerGump(this, e.Serial, e.Graphic, x, y, null));
            }
        }

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

        }

        private void OnObjectDeleted(ObjectDeletedArgs e)
        {
            if (Player == null || Player.Serial == e.Serial) return;

            uint serial = e.Serial;
            Entity entity = Get(serial);
            if (entity == null) return;

            bool updateAbilities = false;

            if (entity is Item it)
            {
                uint cont = it.Container & 0x7FFFFFFF;

                if (SerialHelper.IsValid(it.Container))
                {
                    Entity top = Get(it.RootContainer);

                    if (top != null && top == Player)
                    {
                        updateAbilities = it.Layer == Layer.OneHanded || it.Layer == Layer.TwoHanded;
                        Item tradeBoxItem = Player.GetSecureTradeBox();
                        if (tradeBoxItem != null)
                        {
                            UIManager.GetTradingGump(tradeBoxItem)?.RequestUpdateContents();
                        }
                    }

                    if (cont == Player.Serial && it.Layer == Layer.Invalid)
                    {
                        Client.Game.UO.GameCursor.ItemHold.Enabled = false;
                    }

                    if (it.Layer != Layer.Invalid)
                    {
                        UIManager.GetGump<PaperDollGump>(cont)?.RequestUpdateContents();
                    }

                    UIManager.GetGump<ContainerGump>(cont)?.RequestUpdateContents();

                    var profile = ProfileManager.CurrentProfile;
                    if (top != null && top.Graphic == 0x2006 && profile != null
                        && (profile.GridLootType == 1 || profile.GridLootType == 2))
                    {
                        UIManager.GetGump<GridLootGump>(cont)?.RequestUpdateContents();
                    }

                    if (it.Graphic == 0x0EB0)
                    {
                        UIManager.GetGump<BulletinBoardItem>(serial)?.Dispose();
                        UIManager.GetGump<BulletinBoardGump>()?.RemoveBulletinObject(serial);
                    }
                }
            }

            if (CorpseManager.Exists(0, serial)) return;

            if (entity is Mobile)
            {
                RemoveMobile(serial, true);
            }
            else
            {
                Item item = (Item)entity;

                if (item.IsMulti)
                {
                    HouseManager.Remove(serial);
                }

                Entity cont = Get(item.Container);

                if (cont != null)
                {
                    cont.Remove(item);

                    if (item.Layer != Layer.Invalid)
                    {
                        UIManager.GetGump<PaperDollGump>(cont)?.RequestUpdateContents();
                    }
                }
                else if (item.IsMulti)
                {
                    UIManager.GetGump<MiniMapGump>()?.RequestUpdateContents();
                }

                RemoveItem(serial, true);

                if (updateAbilities)
                {
                    Player.UpdateAbilities();
                }
            }
        }

        private void OnGraphicEffectSpawned(GraphicEffectSpawnedArgs e)
        {
            SpawnEffect(
                (GraphicEffectType)e.EffectType,
                e.Source,
                e.Target,
                e.Graphic,
                (ushort)e.Hue,
                e.SourceX,
                e.SourceY,
                e.SourceZ,
                e.TargetX,
                e.TargetY,
                e.TargetZ,
                e.Speed,
                e.Duration,
                e.FixedDirection,
                e.DoesExplode,
                false,
                (GraphicEffectBlendMode)e.BlendMode
            );
        }

        private void OnBuffApplied(BuffAppliedArgs e)
        {
            if (Player == null) return;
            if (e.IconId >= BuffTable.Table.Length) return;

            bool alreadyExists = Player.IsBuffIconExists((BuffIconType)e.IconType);
            Player.AddBuff((BuffIconType)e.IconType, BuffTable.Table[e.IconId], e.Timer, e.Text);

            if (!alreadyExists)
            {
                UIManager.GetGump<BuffGump>()?.RequestUpdateContents();
            }
        }

        private void OnBuffRemoved(BuffRemovedArgs e)
        {
            if (Player == null) return;

            Player.RemoveBuff((BuffIconType)e.IconType);
            UIManager.GetGump<BuffGump>()?.RequestUpdateContents();
        }

    }
}
