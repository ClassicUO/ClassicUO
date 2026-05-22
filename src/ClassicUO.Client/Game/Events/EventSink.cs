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
        public static event Action<ChatConferenceCreatedArgs> ChatConferenceCreated;
        public static event Action<ChatConferenceDestroyedArgs> ChatConferenceDestroyed;
        public static event Action<ChatUsernameRequestArgs> ChatUsernameRequest;
        public static event Action<ChatClosedArgs> ChatClosed;
        public static event Action<ChatUsernameAcceptedArgs> ChatUsernameAccepted;
        public static event Action<ChatUserAddedArgs> ChatUserAdded;
        public static event Action<ChatUserRemovedArgs> ChatUserRemoved;
        public static event Action<ChatClearAllPlayersArgs> ChatClearAllPlayers;
        public static event Action<ChatConferenceJoinedArgs> ChatConferenceJoined;
        public static event Action<ChatConferenceLeftArgs> ChatConferenceLeft;
        public static event Action<ChatTextReceivedArgs> ChatTextReceived;
        public static event Action<ChatSystemMessageArgs> ChatSystemMessage;

        public static void RaiseChatMessage(in ChatMessageArgs e) => Invoke(ChatMessage, e);
        public static void RaiseUnicodeChatMessage(in UnicodeChatMessageArgs e) => Invoke(UnicodeChatMessage, e);
        public static void RaiseClilocMessage(in ClilocMessageArgs e) => Invoke(ClilocMessage, e);
        public static void RaiseAsciiPrompt(in AsciiPromptArgs e) => Invoke(AsciiPrompt, e);
        public static void RaiseUnicodePrompt(in UnicodePromptArgs e) => Invoke(UnicodePrompt, e);
        public static void RaiseChatConferenceCreated(in ChatConferenceCreatedArgs e) => Invoke(ChatConferenceCreated, e);
        public static void RaiseChatConferenceDestroyed(in ChatConferenceDestroyedArgs e) => Invoke(ChatConferenceDestroyed, e);
        public static void RaiseChatUsernameRequest(in ChatUsernameRequestArgs e) => Invoke(ChatUsernameRequest, e);
        public static void RaiseChatClosed(in ChatClosedArgs e) => Invoke(ChatClosed, e);
        public static void RaiseChatUsernameAccepted(in ChatUsernameAcceptedArgs e) => Invoke(ChatUsernameAccepted, e);
        public static void RaiseChatUserAdded(in ChatUserAddedArgs e) => Invoke(ChatUserAdded, e);
        public static void RaiseChatUserRemoved(in ChatUserRemovedArgs e) => Invoke(ChatUserRemoved, e);
        public static void RaiseChatClearAllPlayers(in ChatClearAllPlayersArgs e) => Invoke(ChatClearAllPlayers, e);
        public static void RaiseChatConferenceJoined(in ChatConferenceJoinedArgs e) => Invoke(ChatConferenceJoined, e);
        public static void RaiseChatConferenceLeft(in ChatConferenceLeftArgs e) => Invoke(ChatConferenceLeft, e);
        public static void RaiseChatTextReceived(in ChatTextReceivedArgs e) => Invoke(ChatTextReceived, e);
        public static void RaiseChatSystemMessage(in ChatSystemMessageArgs e) => Invoke(ChatSystemMessage, e);

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
        public static event Action<CharacterStatusReceivedArgs> CharacterStatusReceived;

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
        public static void RaiseCharacterStatusReceived(in CharacterStatusReceivedArgs e) => Invoke(CharacterStatusReceived, e);

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
        public static event Action<TradeWindowOpenArgs> TradeWindowOpened;
        public static event Action<TradeWindowClosedArgs> TradeWindowClosed;
        public static event Action<TradeWindowAcceptUpdatedArgs> TradeWindowAcceptUpdated;
        public static event Action<TradeWindowCurrencyUpdatedArgs> TradeWindowCurrencyUpdated;
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
        public static void RaiseTradeWindowOpened(in TradeWindowOpenArgs e) => Invoke(TradeWindowOpened, e);
        public static void RaiseTradeWindowClosed(in TradeWindowClosedArgs e) => Invoke(TradeWindowClosed, e);
        public static void RaiseTradeWindowAcceptUpdated(in TradeWindowAcceptUpdatedArgs e) => Invoke(TradeWindowAcceptUpdated, e);
        public static void RaiseTradeWindowCurrencyUpdated(in TradeWindowCurrencyUpdatedArgs e) => Invoke(TradeWindowCurrencyUpdated, e);
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
        public static event Action<MusicStopArgs> MusicStop;

        public static void RaiseSoundPlay(in SoundPlayArgs e) => Invoke(SoundPlay, e);
        public static void RaiseMusicPlay(in MusicPlayArgs e) => Invoke(MusicPlay, e);
        public static void RaiseMusicStop(in MusicStopArgs e) => Invoke(MusicStop, e);

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
        public static event Action<BulletinBoardOpenedArgs> BulletinBoardOpened;
        public static event Action<BulletinBoardSummaryArgs> BulletinBoardSummary;
        public static event Action<BulletinBoardMessageArgs> BulletinBoardMessage;
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
        public static void RaiseBulletinBoardOpened(in BulletinBoardOpenedArgs e) => Invoke(BulletinBoardOpened, e);
        public static void RaiseBulletinBoardSummary(in BulletinBoardSummaryArgs e) => Invoke(BulletinBoardSummary, e);
        public static void RaiseBulletinBoardMessage(in BulletinBoardMessageArgs e) => Invoke(BulletinBoardMessage, e);
        public static void RaiseOpenUrlRequested(in OpenUrlRequestedArgs e) => Invoke(OpenUrlRequested, e);
        public static void RaiseCharacterProfileOpened(in CharacterProfileOpenedArgs e) => Invoke(CharacterProfileOpened, e);
        public static void RaiseVendorWindowClosed(in VendorWindowClosedArgs e) => Invoke(VendorWindowClosed, e);
        public static void RaiseQuestArrowDisplayed(in QuestArrowDisplayedArgs e) => Invoke(QuestArrowDisplayed, e);
        public static void RaiseWaypointDisplayed(in WaypointDisplayedArgs e) => Invoke(WaypointDisplayed, e);
        public static void RaiseWaypointRemoved(in WaypointRemovedArgs e) => Invoke(WaypointRemoved, e);

        // ---- ExtendedCommand (0xBF) sub-command events ----
        public static event Action<FastWalkStackInitArgs> FastWalkStackInit;
        public static event Action<FastWalkStackAddArgs> FastWalkStackAdd;
        public static event Action<GenericGumpCloseArgs> GenericGumpClose;
        public static event Action<PartyListUpdatedArgs> PartyListUpdated;
        public static event Action<PartyChatMessageArgs> PartyChatMessage;
        public static event Action<PartyInviteReceivedArgs> PartyInviteReceived;
        public static event Action<MapIndexChangedArgs> MapIndexChanged;
        public static event Action<CloseStatusbarGumpArgs> CloseStatusbarGump;
        public static event Action<EquipInfoArgs> EquipInfoReceived;
        public static event Action<PopupMenuArgs> PopupMenuReceived;
        public static event Action<CloseUserInterfaceArgs> CloseUserInterface;
        public static event Action<MapPatchesEnabledArgs> MapPatchesEnabled;
        public static event Action<ExtendedStatsBondedArgs> ExtendedStatsBonded;
        public static event Action<ExtendedStatsLocksArgs> ExtendedStatsLocks;
        public static event Action<ExtendedStatsAnimationArgs> ExtendedStatsAnimation;
        public static event Action<SpellbookContentArgs> SpellbookContent;
        public static event Action<HouseRevisionStateArgs> HouseRevisionState;
        public static event Action<HouseDesignStateArgs> HouseDesignState;
        public static event Action<AbilityIconsResetArgs> AbilityIconsReset;
        public static event Action<DamageOverheadArgs> DamageOverhead;
        public static event Action<SpellIconToggleArgs> SpellIconToggle;
        public static event Action<CharacterSpeedModeArgs> CharacterSpeedMode;
        public static event Action<RaceChangeRequestedArgs> RaceChangeRequested;
        public static event Action<MobileAnimationFrameArgs> MobileAnimationFrame;
        public static event Action<CuoCommandArgs> CuoCommand;

        public static void RaiseFastWalkStackInit(in FastWalkStackInitArgs e) => Invoke(FastWalkStackInit, e);
        public static void RaiseFastWalkStackAdd(in FastWalkStackAddArgs e) => Invoke(FastWalkStackAdd, e);
        public static void RaiseGenericGumpClose(in GenericGumpCloseArgs e) => Invoke(GenericGumpClose, e);
        public static void RaisePartyListUpdated(in PartyListUpdatedArgs e) => Invoke(PartyListUpdated, e);
        public static void RaisePartyChatMessage(in PartyChatMessageArgs e) => Invoke(PartyChatMessage, e);
        public static void RaisePartyInviteReceived(in PartyInviteReceivedArgs e) => Invoke(PartyInviteReceived, e);
        public static void RaiseMapIndexChanged(in MapIndexChangedArgs e) => Invoke(MapIndexChanged, e);
        public static void RaiseCloseStatusbarGump(in CloseStatusbarGumpArgs e) => Invoke(CloseStatusbarGump, e);
        public static void RaiseEquipInfoReceived(in EquipInfoArgs e) => Invoke(EquipInfoReceived, e);
        public static void RaisePopupMenuReceived(in PopupMenuArgs e) => Invoke(PopupMenuReceived, e);
        public static void RaiseCloseUserInterface(in CloseUserInterfaceArgs e) => Invoke(CloseUserInterface, e);
        public static void RaiseMapPatchesEnabled(in MapPatchesEnabledArgs e) => Invoke(MapPatchesEnabled, e);
        public static void RaiseExtendedStatsBonded(in ExtendedStatsBondedArgs e) => Invoke(ExtendedStatsBonded, e);
        public static void RaiseExtendedStatsLocks(in ExtendedStatsLocksArgs e) => Invoke(ExtendedStatsLocks, e);
        public static void RaiseExtendedStatsAnimation(in ExtendedStatsAnimationArgs e) => Invoke(ExtendedStatsAnimation, e);
        public static void RaiseSpellbookContent(in SpellbookContentArgs e) => Invoke(SpellbookContent, e);
        public static void RaiseHouseRevisionState(in HouseRevisionStateArgs e) => Invoke(HouseRevisionState, e);
        public static void RaiseHouseDesignState(in HouseDesignStateArgs e) => Invoke(HouseDesignState, e);
        public static void RaiseAbilityIconsReset(in AbilityIconsResetArgs e) => Invoke(AbilityIconsReset, e);
        public static void RaiseDamageOverhead(in DamageOverheadArgs e) => Invoke(DamageOverhead, e);
        public static void RaiseSpellIconToggle(in SpellIconToggleArgs e) => Invoke(SpellIconToggle, e);
        public static void RaiseCharacterSpeedMode(in CharacterSpeedModeArgs e) => Invoke(CharacterSpeedMode, e);
        public static void RaiseRaceChangeRequested(in RaceChangeRequestedArgs e) => Invoke(RaceChangeRequested, e);
        public static void RaiseMobileAnimationFrame(in MobileAnimationFrameArgs e) => Invoke(MobileAnimationFrame, e);
        public static void RaiseCuoCommand(in CuoCommandArgs e) => Invoke(CuoCommand, e);

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
            ChatConferenceCreated = null;
            ChatConferenceDestroyed = null;
            ChatUsernameRequest = null;
            ChatClosed = null;
            ChatUsernameAccepted = null;
            ChatUserAdded = null;
            ChatUserRemoved = null;
            ChatClearAllPlayers = null;
            ChatConferenceJoined = null;
            ChatConferenceLeft = null;
            ChatTextReceived = null;
            ChatSystemMessage = null;

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
            CharacterStatusReceived = null;

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
            TradeWindowOpened = null;
            TradeWindowClosed = null;
            TradeWindowAcceptUpdated = null;
            TradeWindowCurrencyUpdated = null;
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
            MusicStop = null;

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
            BulletinBoardOpened = null;
            BulletinBoardSummary = null;
            BulletinBoardMessage = null;
            OpenUrlRequested = null;
            CharacterProfileOpened = null;
            VendorWindowClosed = null;
            QuestArrowDisplayed = null;
            WaypointDisplayed = null;
            WaypointRemoved = null;

            FastWalkStackInit = null;
            FastWalkStackAdd = null;
            GenericGumpClose = null;
            PartyListUpdated = null;
            PartyChatMessage = null;
            PartyInviteReceived = null;
            MapIndexChanged = null;
            CloseStatusbarGump = null;
            EquipInfoReceived = null;
            PopupMenuReceived = null;
            CloseUserInterface = null;
            MapPatchesEnabled = null;
            ExtendedStatsBonded = null;
            ExtendedStatsLocks = null;
            ExtendedStatsAnimation = null;
            SpellbookContent = null;
            HouseRevisionState = null;
            HouseDesignState = null;
            AbilityIconsReset = null;
            DamageOverhead = null;
            SpellIconToggle = null;
            CharacterSpeedMode = null;
            RaceChangeRequested = null;
            MobileAnimationFrame = null;
            CuoCommand = null;
        }
    }
}
