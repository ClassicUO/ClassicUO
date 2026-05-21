// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Events
{
    internal static class EventSink
    {
        // ---- Chat ----
        public static event Action<ChatMessageArgs> ChatMessage;
        public static event Action<UnicodeChatMessageArgs> UnicodeChatMessage;
        public static event Action<ClilocMessageArgs> ClilocMessage;
        public static event Action<AsciiPromptArgs> AsciiPrompt;
        public static event Action<UnicodePromptArgs> UnicodePrompt;
        public static event Action<ChannelChatReceivedArgs> ChannelChatReceived;

        public static void RaiseChatMessage(in ChatMessageArgs e) => Invoke(ChatMessage, e);
        public static void RaiseUnicodeChatMessage(in UnicodeChatMessageArgs e) => Invoke(UnicodeChatMessage, e);
        public static void RaiseClilocMessage(in ClilocMessageArgs e) => Invoke(ClilocMessage, e);
        public static void RaiseAsciiPrompt(in AsciiPromptArgs e) => Invoke(AsciiPrompt, e);
        public static void RaiseUnicodePrompt(in UnicodePromptArgs e) => Invoke(UnicodePrompt, e);
        public static void RaiseChannelChatReceived(in ChannelChatReceivedArgs e) => Invoke(ChannelChatReceived, e);

        // ---- Mobiles ----
        public static event Action<MobileSpawnedArgs> MobileSpawned;
        public static event Action<MobileUpdatedArgs> MobileUpdated;
        public static event Action<PlayerUpdatedArgs> PlayerUpdated;
        public static event Action<MobileMovedArgs> MobileMoved;
        public static event Action<MobileRemovedArgs> MobileRemoved;
        public static event Action<MobileAttributesUpdatedArgs> MobileAttributesUpdated;
        public static event Action<HitpointsUpdatedArgs> HitpointsUpdated;
        public static event Action<ManaUpdatedArgs> ManaUpdated;
        public static event Action<StaminaUpdatedArgs> StaminaUpdated;
        public static event Action<WalkDeniedArgs> WalkDenied;
        public static event Action<WalkConfirmedArgs> WalkConfirmed;
        public static event Action<PlayerMovedArgs> PlayerMoved;
        public static event Action<MobileNameChangedArgs> MobileNameChanged;
        public static event Action<HealthBarStateChangedArgs> HealthBarStateChanged;
        public static event Action<BuffAppliedArgs> BuffApplied;
        public static event Action<BuffRemovedArgs> BuffRemoved;
        public static event Action<CharacterAnimationArgs> CharacterAnimation;
        public static event Action<NewCharacterAnimationArgs> NewCharacterAnimation;
        public static event Action<MobileStatusUpdatedArgs> MobileStatusUpdated;

        public static void RaiseMobileSpawned(in MobileSpawnedArgs e) => Invoke(MobileSpawned, e);
        public static void RaiseMobileUpdated(in MobileUpdatedArgs e) => Invoke(MobileUpdated, e);
        public static void RaisePlayerUpdated(in PlayerUpdatedArgs e) => Invoke(PlayerUpdated, e);
        public static void RaiseMobileMoved(in MobileMovedArgs e) => Invoke(MobileMoved, e);
        public static void RaiseMobileRemoved(in MobileRemovedArgs e) => Invoke(MobileRemoved, e);
        public static void RaiseMobileAttributesUpdated(in MobileAttributesUpdatedArgs e) => Invoke(MobileAttributesUpdated, e);
        public static void RaiseHitpointsUpdated(in HitpointsUpdatedArgs e) => Invoke(HitpointsUpdated, e);
        public static void RaiseManaUpdated(in ManaUpdatedArgs e) => Invoke(ManaUpdated, e);
        public static void RaiseStaminaUpdated(in StaminaUpdatedArgs e) => Invoke(StaminaUpdated, e);
        public static void RaiseWalkDenied(in WalkDeniedArgs e) => Invoke(WalkDenied, e);
        public static void RaiseWalkConfirmed(in WalkConfirmedArgs e) => Invoke(WalkConfirmed, e);
        public static void RaisePlayerMoved(in PlayerMovedArgs e) => Invoke(PlayerMoved, e);
        public static void RaiseMobileNameChanged(in MobileNameChangedArgs e) => Invoke(MobileNameChanged, e);
        public static void RaiseHealthBarStateChanged(in HealthBarStateChangedArgs e) => Invoke(HealthBarStateChanged, e);
        public static void RaiseBuffApplied(in BuffAppliedArgs e) => Invoke(BuffApplied, e);
        public static void RaiseBuffRemoved(in BuffRemovedArgs e) => Invoke(BuffRemoved, e);
        public static void RaiseCharacterAnimation(in CharacterAnimationArgs e) => Invoke(CharacterAnimation, e);
        public static void RaiseNewCharacterAnimation(in NewCharacterAnimationArgs e) => Invoke(NewCharacterAnimation, e);
        public static void RaiseMobileStatusUpdated(in MobileStatusUpdatedArgs e) => Invoke(MobileStatusUpdated, e);

        // ---- Items ----
        public static event Action<ItemSpawnedArgs> ItemSpawned;
        public static event Action<ItemUpdatedArgs> ItemUpdated;
        public static event Action<ItemRemovedArgs> ItemRemoved;
        public static event Action<ContainerOpenedArgs> ContainerOpened;
        public static event Action<ContainerItemAddedArgs> ContainerItemAdded;
        public static event Action<ContainerItemsReceivedArgs> ContainerItemsReceived;
        public static event Action<ItemEquippedArgs> ItemEquipped;
        public static event Action<CorpseEquipmentReceivedArgs> CorpseEquipmentReceived;
        public static event Action<DyeDataReceivedArgs> DyeDataReceived;
        public static event Action<OplInfoReceivedArgs> OplInfoReceived;
        public static event Action<MegaClilocReceivedArgs> MegaClilocReceived;
        public static event Action<ItemDragAnimationArgs> ItemDragAnimation;
        public static event Action<ItemMoveDeniedArgs> ItemMoveDenied;
        public static event Action<ItemDragEndedArgs> ItemDragEnded;
        public static event Action<ItemDropAcceptedArgs> ItemDropAccepted;
        public static event Action<ShopBuyListReceivedArgs> ShopBuyListReceived;
        public static event Action<ShopSellListReceivedArgs> ShopSellListReceived;
        public static event Action<TradeWindowArgs> TradeWindow;
        public static event Action<CustomHouseReceivedArgs> CustomHouseReceived;

        public static void RaiseItemSpawned(in ItemSpawnedArgs e) => Invoke(ItemSpawned, e);
        public static void RaiseItemUpdated(in ItemUpdatedArgs e) => Invoke(ItemUpdated, e);
        public static void RaiseItemRemoved(in ItemRemovedArgs e) => Invoke(ItemRemoved, e);
        public static void RaiseContainerOpened(in ContainerOpenedArgs e) => Invoke(ContainerOpened, e);
        public static void RaiseContainerItemAdded(in ContainerItemAddedArgs e) => Invoke(ContainerItemAdded, e);
        public static void RaiseContainerItemsReceived(in ContainerItemsReceivedArgs e) => Invoke(ContainerItemsReceived, e);
        public static void RaiseItemEquipped(in ItemEquippedArgs e) => Invoke(ItemEquipped, e);
        public static void RaiseCorpseEquipmentReceived(in CorpseEquipmentReceivedArgs e) => Invoke(CorpseEquipmentReceived, e);
        public static void RaiseDyeDataReceived(in DyeDataReceivedArgs e) => Invoke(DyeDataReceived, e);
        public static void RaiseOplInfoReceived(in OplInfoReceivedArgs e) => Invoke(OplInfoReceived, e);
        public static void RaiseMegaClilocReceived(in MegaClilocReceivedArgs e) => Invoke(MegaClilocReceived, e);
        public static void RaiseItemDragAnimation(in ItemDragAnimationArgs e) => Invoke(ItemDragAnimation, e);
        public static void RaiseItemMoveDenied(in ItemMoveDeniedArgs e) => Invoke(ItemMoveDenied, e);
        public static void RaiseItemDragEnded(in ItemDragEndedArgs e) => Invoke(ItemDragEnded, e);
        public static void RaiseItemDropAccepted(in ItemDropAcceptedArgs e) => Invoke(ItemDropAccepted, e);
        public static void RaiseShopBuyListReceived(in ShopBuyListReceivedArgs e) => Invoke(ShopBuyListReceived, e);
        public static void RaiseShopSellListReceived(in ShopSellListReceivedArgs e) => Invoke(ShopSellListReceived, e);
        public static void RaiseTradeWindow(in TradeWindowArgs e) => Invoke(TradeWindow, e);
        public static void RaiseCustomHouseReceived(in CustomHouseReceivedArgs e) => Invoke(CustomHouseReceived, e);

        // ---- Combat ----
        public static event Action<DamageReceivedArgs> DamageReceived;
        public static event Action<WarModeChangedArgs> WarModeChanged;
        public static event Action<PlayerDeathArgs> PlayerDeath;
        public static event Action<CombatSwingArgs> CombatSwing;
        public static event Action<AttackTargetChangedArgs> AttackTargetChanged;
        public static event Action<MobileDeathArgs> MobileDeath;

        public static void RaiseDamageReceived(in DamageReceivedArgs e) => Invoke(DamageReceived, e);
        public static void RaiseWarModeChanged(in WarModeChangedArgs e) => Invoke(WarModeChanged, e);
        public static void RaisePlayerDeath(in PlayerDeathArgs e) => Invoke(PlayerDeath, e);
        public static void RaiseCombatSwing(in CombatSwingArgs e) => Invoke(CombatSwing, e);
        public static void RaiseAttackTargetChanged(in AttackTargetChangedArgs e) => Invoke(AttackTargetChanged, e);
        public static void RaiseMobileDeath(in MobileDeathArgs e) => Invoke(MobileDeath, e);

        // ---- World ----
        public static event Action<WeatherChangedArgs> WeatherChanged;
        public static event Action<SeasonChangedArgs> SeasonChanged;
        public static event Action<LightLevelChangedArgs> LightLevelChanged;
        public static event Action<ObjectDeletedArgs> ObjectDeleted;
        public static event Action<ClientViewRangeChangedArgs> ClientViewRangeChanged;
        public static event Action<GraphicEffectSpawnedArgs> GraphicEffectSpawned;
        public static event Action<SkillsUpdatedArgs> SkillsUpdated;
        public static event Action<TargetCursorReceivedArgs> TargetCursorReceived;
        public static event Action<MultiPlacementReceivedArgs> MultiPlacementReceived;
        public static event Action<BoatMovingReceivedArgs> BoatMovingReceived;
        public static event Action<MapDataReceivedArgs> MapDataReceived;
        public static event Action<PathfindingReceivedArgs> PathfindingReceived;

        public static void RaiseWeatherChanged(in WeatherChangedArgs e) => Invoke(WeatherChanged, e);
        public static void RaiseSeasonChanged(in SeasonChangedArgs e) => Invoke(SeasonChanged, e);
        public static void RaiseLightLevelChanged(in LightLevelChangedArgs e) => Invoke(LightLevelChanged, e);
        public static void RaiseObjectDeleted(in ObjectDeletedArgs e) => Invoke(ObjectDeleted, e);
        public static void RaiseClientViewRangeChanged(in ClientViewRangeChangedArgs e) => Invoke(ClientViewRangeChanged, e);
        public static void RaiseGraphicEffectSpawned(in GraphicEffectSpawnedArgs e) => Invoke(GraphicEffectSpawned, e);
        public static void RaiseSkillsUpdated(in SkillsUpdatedArgs e) => Invoke(SkillsUpdated, e);
        public static void RaiseTargetCursorReceived(in TargetCursorReceivedArgs e) => Invoke(TargetCursorReceived, e);
        public static void RaiseMultiPlacementReceived(in MultiPlacementReceivedArgs e) => Invoke(MultiPlacementReceived, e);
        public static void RaiseBoatMovingReceived(in BoatMovingReceivedArgs e) => Invoke(BoatMovingReceived, e);
        public static void RaiseMapDataReceived(in MapDataReceivedArgs e) => Invoke(MapDataReceived, e);
        public static void RaisePathfindingReceived(in PathfindingReceivedArgs e) => Invoke(PathfindingReceived, e);

        // ---- Audio ----
        public static event Action<SoundPlayArgs> SoundPlay;
        public static event Action<MusicPlayArgs> MusicPlay;

        public static void RaiseSoundPlay(in SoundPlayArgs e) => Invoke(SoundPlay, e);
        public static void RaiseMusicPlay(in MusicPlayArgs e) => Invoke(MusicPlay, e);

        // ---- Network / session ----
        public static event Action<ConnectedArgs> Connected;
        public static event Action<DisconnectedArgs> Disconnected;
        public static event Action<PingReceivedArgs> PingReceived;

        public static void RaiseConnected(in ConnectedArgs e) => Invoke(Connected, e);
        public static void RaiseDisconnected(in DisconnectedArgs e) => Invoke(Disconnected, e);
        public static void RaisePingReceived(in PingReceivedArgs e) => Invoke(PingReceived, e);

        // ---- Login ----
        public static event Action<LoginCompletedArgs> LoginCompleted;
        public static event Action<LoginRejectedArgs> LoginRejected;
        public static event Action<PlayerEnteredWorldArgs> PlayerEnteredWorld;
        public static event Action<LogoutReceivedArgs> LogoutReceived;
        public static event Action<ServerListReceivedArgs> ServerListReceived;
        public static event Action<ServerRelayReceivedArgs> ServerRelayReceived;
        public static event Action<CharacterListUpdatedArgs> CharacterListUpdated;
        public static event Action<CharacterListReceivedArgs> CharacterListReceived;
        public static event Action<LoginDelayReceivedArgs> LoginDelayReceived;
        public static event Action<ClientVersionRequestedArgs> ClientVersionRequested;
        public static event Action<LockedFeaturesEnabledArgs> LockedFeaturesEnabled;

        public static void RaiseLoginCompleted(in LoginCompletedArgs e) => Invoke(LoginCompleted, e);
        public static void RaiseLoginRejected(in LoginRejectedArgs e) => Invoke(LoginRejected, e);
        public static void RaisePlayerEnteredWorld(in PlayerEnteredWorldArgs e) => Invoke(PlayerEnteredWorld, e);
        public static void RaiseLogoutReceived(in LogoutReceivedArgs e) => Invoke(LogoutReceived, e);
        public static void RaiseServerListReceived(in ServerListReceivedArgs e) => Invoke(ServerListReceived, e);
        public static void RaiseServerRelayReceived(in ServerRelayReceivedArgs e) => Invoke(ServerRelayReceived, e);
        public static void RaiseCharacterListUpdated(in CharacterListUpdatedArgs e) => Invoke(CharacterListUpdated, e);
        public static void RaiseCharacterListReceived(in CharacterListReceivedArgs e) => Invoke(CharacterListReceived, e);
        public static void RaiseLoginDelayReceived(in LoginDelayReceivedArgs e) => Invoke(LoginDelayReceived, e);
        public static void RaiseClientVersionRequested(in ClientVersionRequestedArgs e) => Invoke(ClientVersionRequested, e);
        public static void RaiseLockedFeaturesEnabled(in LockedFeaturesEnabledArgs e) => Invoke(LockedFeaturesEnabled, e);

        // ---- UI ----
        public static event Action<GumpOpenedArgs> GumpOpened;
        public static event Action<GumpClosedArgs> GumpClosed;
        public static event Action<CompressedGumpOpenedArgs> CompressedGumpOpened;
        public static event Action<ContextMenuOpenedArgs> ContextMenuOpened;
        public static event Action<PaperdollOpenedArgs> PaperdollOpened;
        public static event Action<MapDisplayedArgs> MapDisplayed;
        public static event Action<BookOpenedArgs> BookOpened;
        public static event Action<BookDataReceivedArgs> BookDataReceived;
        public static event Action<TextEntryDialogArgs> TextEntryDialogOpened;
        public static event Action<TipWindowDisplayedArgs> TipWindowDisplayed;
        public static event Action<BulletinBoardDataReceivedArgs> BulletinBoardDataReceived;
        public static event Action<OpenUrlRequestedArgs> OpenUrlRequested;
        public static event Action<CharacterProfileOpenedArgs> CharacterProfileOpened;
        public static event Action<VendorWindowClosedArgs> VendorWindowClosed;
        public static event Action<QuestArrowDisplayedArgs> QuestArrowDisplayed;
        public static event Action<WaypointDisplayedArgs> WaypointDisplayed;
        public static event Action<WaypointRemovedArgs> WaypointRemoved;

        public static void RaiseGumpOpened(in GumpOpenedArgs e) => Invoke(GumpOpened, e);
        public static void RaiseGumpClosed(in GumpClosedArgs e) => Invoke(GumpClosed, e);
        public static void RaiseCompressedGumpOpened(in CompressedGumpOpenedArgs e) => Invoke(CompressedGumpOpened, e);
        public static void RaiseContextMenuOpened(in ContextMenuOpenedArgs e) => Invoke(ContextMenuOpened, e);
        public static void RaisePaperdollOpened(in PaperdollOpenedArgs e) => Invoke(PaperdollOpened, e);
        public static void RaiseMapDisplayed(in MapDisplayedArgs e) => Invoke(MapDisplayed, e);
        public static void RaiseBookOpened(in BookOpenedArgs e) => Invoke(BookOpened, e);
        public static void RaiseBookDataReceived(in BookDataReceivedArgs e) => Invoke(BookDataReceived, e);
        public static void RaiseTextEntryDialogOpened(in TextEntryDialogArgs e) => Invoke(TextEntryDialogOpened, e);
        public static void RaiseTipWindowDisplayed(in TipWindowDisplayedArgs e) => Invoke(TipWindowDisplayed, e);
        public static void RaiseBulletinBoardDataReceived(in BulletinBoardDataReceivedArgs e) => Invoke(BulletinBoardDataReceived, e);
        public static void RaiseOpenUrlRequested(in OpenUrlRequestedArgs e) => Invoke(OpenUrlRequested, e);
        public static void RaiseCharacterProfileOpened(in CharacterProfileOpenedArgs e) => Invoke(CharacterProfileOpened, e);
        public static void RaiseVendorWindowClosed(in VendorWindowClosedArgs e) => Invoke(VendorWindowClosed, e);
        public static void RaiseQuestArrowDisplayed(in QuestArrowDisplayedArgs e) => Invoke(QuestArrowDisplayed, e);
        public static void RaiseWaypointDisplayed(in WaypointDisplayedArgs e) => Invoke(WaypointDisplayed, e);
        public static void RaiseWaypointRemoved(in WaypointRemovedArgs e) => Invoke(WaypointRemoved, e);

        private static void Invoke<T>(Action<T> handler, in T args)
        {
            if (handler is null) return;

            foreach (var d in handler.GetInvocationList())
            {
                try
                {
                    ((Action<T>)d)(args);
                }
                catch (Exception ex)
                {
                    Log.Error($"EventSink handler failed for {typeof(T).Name}: {ex}");
                }
            }
        }

        /// <summary>Clears every subscription. Intended for tests only.</summary>
        public static void ClearAll()
        {
            ChatMessage = null;
            UnicodeChatMessage = null;
            ClilocMessage = null;
            AsciiPrompt = null;
            UnicodePrompt = null;
            ChannelChatReceived = null;

            MobileSpawned = null;
            MobileUpdated = null;
            PlayerUpdated = null;
            MobileMoved = null;
            MobileRemoved = null;
            MobileAttributesUpdated = null;
            HitpointsUpdated = null;
            ManaUpdated = null;
            StaminaUpdated = null;
            WalkDenied = null;
            WalkConfirmed = null;
            PlayerMoved = null;
            MobileNameChanged = null;
            HealthBarStateChanged = null;
            BuffApplied = null;
            BuffRemoved = null;
            CharacterAnimation = null;
            NewCharacterAnimation = null;
            MobileStatusUpdated = null;

            ItemSpawned = null;
            ItemUpdated = null;
            ItemRemoved = null;
            ContainerOpened = null;
            ContainerItemAdded = null;
            ContainerItemsReceived = null;
            ItemEquipped = null;
            CorpseEquipmentReceived = null;
            DyeDataReceived = null;
            OplInfoReceived = null;
            MegaClilocReceived = null;
            ItemDragAnimation = null;
            ItemMoveDenied = null;
            ItemDragEnded = null;
            ItemDropAccepted = null;
            ShopBuyListReceived = null;
            ShopSellListReceived = null;
            TradeWindow = null;
            CustomHouseReceived = null;

            DamageReceived = null;
            WarModeChanged = null;
            PlayerDeath = null;
            CombatSwing = null;
            AttackTargetChanged = null;
            MobileDeath = null;

            WeatherChanged = null;
            SeasonChanged = null;
            LightLevelChanged = null;
            ObjectDeleted = null;
            ClientViewRangeChanged = null;
            GraphicEffectSpawned = null;
            SkillsUpdated = null;
            TargetCursorReceived = null;
            MultiPlacementReceived = null;
            BoatMovingReceived = null;
            MapDataReceived = null;
            PathfindingReceived = null;

            SoundPlay = null;
            MusicPlay = null;

            Connected = null;
            Disconnected = null;
            PingReceived = null;

            LoginCompleted = null;
            LoginRejected = null;
            PlayerEnteredWorld = null;
            LogoutReceived = null;
            ServerListReceived = null;
            ServerRelayReceived = null;
            CharacterListUpdated = null;
            CharacterListReceived = null;
            LoginDelayReceived = null;
            ClientVersionRequested = null;
            LockedFeaturesEnabled = null;

            GumpOpened = null;
            GumpClosed = null;
            CompressedGumpOpened = null;
            ContextMenuOpened = null;
            PaperdollOpened = null;
            MapDisplayed = null;
            BookOpened = null;
            BookDataReceived = null;
            TextEntryDialogOpened = null;
            TipWindowDisplayed = null;
            BulletinBoardDataReceived = null;
            OpenUrlRequested = null;
            CharacterProfileOpened = null;
            VendorWindowClosed = null;
            QuestArrowDisplayed = null;
            WaypointDisplayed = null;
            WaypointRemoved = null;
        }
    }
}
