// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Events
{
    internal static class EventSink
    {
        // ---- Chat ----
        public static TypedEvent<ChatMessageArgs> ChatMessage = new();
        public static TypedEvent<UnicodeChatMessageArgs> UnicodeChatMessage = new();
        public static TypedEvent<ClilocMessageArgs> ClilocMessage = new();
        public static TypedEvent<AsciiPromptArgs> AsciiPrompt = new();
        public static TypedEvent<UnicodePromptArgs> UnicodePrompt = new();
        public static TypedEvent<ChatConferenceCreatedArgs> ChatConferenceCreated = new();
        public static TypedEvent<ChatConferenceDestroyedArgs> ChatConferenceDestroyed = new();
        public static TypedEvent<ChatUsernameRequestArgs> ChatUsernameRequest = new();
        public static TypedEvent<ChatClosedArgs> ChatClosed = new();
        public static TypedEvent<ChatUsernameAcceptedArgs> ChatUsernameAccepted = new();
        public static TypedEvent<ChatUserAddedArgs> ChatUserAdded = new();
        public static TypedEvent<ChatUserRemovedArgs> ChatUserRemoved = new();
        public static TypedEvent<ChatClearAllPlayersArgs> ChatClearAllPlayers = new();
        public static TypedEvent<ChatConferenceJoinedArgs> ChatConferenceJoined = new();
        public static TypedEvent<ChatConferenceLeftArgs> ChatConferenceLeft = new();
        public static TypedEvent<ChatTextReceivedArgs> ChatTextReceived = new();
        public static TypedEvent<ChatSystemMessageArgs> ChatSystemMessage = new();

        public static void RaiseChatMessage(in ChatMessageArgs e) => ChatMessage.Raise(e);
        public static void RaiseUnicodeChatMessage(in UnicodeChatMessageArgs e) => UnicodeChatMessage.Raise(e);
        public static void RaiseClilocMessage(in ClilocMessageArgs e) => ClilocMessage.Raise(e);
        public static void RaiseAsciiPrompt(in AsciiPromptArgs e) => AsciiPrompt.Raise(e);
        public static void RaiseUnicodePrompt(in UnicodePromptArgs e) => UnicodePrompt.Raise(e);
        public static void RaiseChatConferenceCreated(in ChatConferenceCreatedArgs e) => ChatConferenceCreated.Raise(e);
        public static void RaiseChatConferenceDestroyed(in ChatConferenceDestroyedArgs e) => ChatConferenceDestroyed.Raise(e);
        public static void RaiseChatUsernameRequest(in ChatUsernameRequestArgs e) => ChatUsernameRequest.Raise(e);
        public static void RaiseChatClosed(in ChatClosedArgs e) => ChatClosed.Raise(e);
        public static void RaiseChatUsernameAccepted(in ChatUsernameAcceptedArgs e) => ChatUsernameAccepted.Raise(e);
        public static void RaiseChatUserAdded(in ChatUserAddedArgs e) => ChatUserAdded.Raise(e);
        public static void RaiseChatUserRemoved(in ChatUserRemovedArgs e) => ChatUserRemoved.Raise(e);
        public static void RaiseChatClearAllPlayers(in ChatClearAllPlayersArgs e) => ChatClearAllPlayers.Raise(e);
        public static void RaiseChatConferenceJoined(in ChatConferenceJoinedArgs e) => ChatConferenceJoined.Raise(e);
        public static void RaiseChatConferenceLeft(in ChatConferenceLeftArgs e) => ChatConferenceLeft.Raise(e);
        public static void RaiseChatTextReceived(in ChatTextReceivedArgs e) => ChatTextReceived.Raise(e);
        public static void RaiseChatSystemMessage(in ChatSystemMessageArgs e) => ChatSystemMessage.Raise(e);

        // ---- Mobiles ----
        public static TypedEvent<MobileSpawnedArgs> MobileSpawned = new();
        public static TypedEvent<MobileUpdatedArgs> MobileUpdated = new();
        public static TypedEvent<PlayerUpdatedArgs> PlayerUpdated = new();
        public static TypedEvent<MobileMovedArgs> MobileMoved = new();
        public static TypedEvent<MobileRemovedArgs> MobileRemoved = new();
        public static TypedEvent<MobileAttributesUpdatedArgs> MobileAttributesUpdated = new();
        public static TypedEvent<HitpointsUpdatedArgs> HitpointsUpdated = new();
        public static TypedEvent<ManaUpdatedArgs> ManaUpdated = new();
        public static TypedEvent<StaminaUpdatedArgs> StaminaUpdated = new();
        public static TypedEvent<WalkDeniedArgs> WalkDenied = new();
        public static TypedEvent<WalkConfirmedArgs> WalkConfirmed = new();
        public static TypedEvent<PlayerMovedArgs> PlayerMoved = new();
        public static TypedEvent<MobileNameChangedArgs> MobileNameChanged = new();
        public static TypedEvent<HealthBarStateChangedArgs> HealthBarStateChanged = new();
        public static TypedEvent<BuffAppliedArgs> BuffApplied = new();
        public static TypedEvent<BuffRemovedArgs> BuffRemoved = new();
        public static TypedEvent<CharacterAnimationArgs> CharacterAnimation = new();
        public static TypedEvent<NewCharacterAnimationArgs> NewCharacterAnimation = new();
        public static TypedEvent<MobileStatusUpdatedArgs> MobileStatusUpdated = new();
        public static TypedEvent<CharacterStatusReceivedArgs> CharacterStatusReceived = new();

        public static void RaiseMobileSpawned(in MobileSpawnedArgs e) => MobileSpawned.Raise(e);
        public static void RaiseMobileUpdated(in MobileUpdatedArgs e) => MobileUpdated.Raise(e);
        public static void RaisePlayerUpdated(in PlayerUpdatedArgs e) => PlayerUpdated.Raise(e);
        public static void RaiseMobileMoved(in MobileMovedArgs e) => MobileMoved.Raise(e);
        public static void RaiseMobileRemoved(in MobileRemovedArgs e) => MobileRemoved.Raise(e);
        public static void RaiseMobileAttributesUpdated(in MobileAttributesUpdatedArgs e) => MobileAttributesUpdated.Raise(e);
        public static void RaiseHitpointsUpdated(in HitpointsUpdatedArgs e) => HitpointsUpdated.Raise(e);
        public static void RaiseManaUpdated(in ManaUpdatedArgs e) => ManaUpdated.Raise(e);
        public static void RaiseStaminaUpdated(in StaminaUpdatedArgs e) => StaminaUpdated.Raise(e);
        public static void RaiseWalkDenied(in WalkDeniedArgs e) => WalkDenied.Raise(e);
        public static void RaiseWalkConfirmed(in WalkConfirmedArgs e) => WalkConfirmed.Raise(e);
        public static void RaisePlayerMoved(in PlayerMovedArgs e) => PlayerMoved.Raise(e);
        public static void RaiseMobileNameChanged(in MobileNameChangedArgs e) => MobileNameChanged.Raise(e);
        public static void RaiseHealthBarStateChanged(in HealthBarStateChangedArgs e) => HealthBarStateChanged.Raise(e);
        public static void RaiseBuffApplied(in BuffAppliedArgs e) => BuffApplied.Raise(e);
        public static void RaiseBuffRemoved(in BuffRemovedArgs e) => BuffRemoved.Raise(e);
        public static void RaiseCharacterAnimation(in CharacterAnimationArgs e) => CharacterAnimation.Raise(e);
        public static void RaiseNewCharacterAnimation(in NewCharacterAnimationArgs e) => NewCharacterAnimation.Raise(e);
        public static void RaiseMobileStatusUpdated(in MobileStatusUpdatedArgs e) => MobileStatusUpdated.Raise(e);
        public static void RaiseCharacterStatusReceived(in CharacterStatusReceivedArgs e) => CharacterStatusReceived.Raise(e);

        // ---- Items ----
        public static TypedEvent<ItemSpawnedArgs> ItemSpawned = new();
        public static TypedEvent<ItemUpdatedArgs> ItemUpdated = new();
        public static TypedEvent<ItemRemovedArgs> ItemRemoved = new();
        public static TypedEvent<ContainerOpenedArgs> ContainerOpened = new();
        public static TypedEvent<ContainerItemAddedArgs> ContainerItemAdded = new();
        public static TypedEvent<ContainerItemsReceivedArgs> ContainerItemsReceived = new();
        public static TypedEvent<ItemEquippedArgs> ItemEquipped = new();
        public static TypedEvent<CorpseEquipmentReceivedArgs> CorpseEquipmentReceived = new();
        public static TypedEvent<DyeDataReceivedArgs> DyeDataReceived = new();
        public static TypedEvent<OplInfoReceivedArgs> OplInfoReceived = new();
        public static TypedEvent<MegaClilocReceivedArgs> MegaClilocReceived = new();
        public static TypedEvent<ItemDragAnimationArgs> ItemDragAnimation = new();
        public static TypedEvent<ItemMoveDeniedArgs> ItemMoveDenied = new();
        public static TypedEvent<ItemDragEndedArgs> ItemDragEnded = new();
        public static TypedEvent<ItemDropAcceptedArgs> ItemDropAccepted = new();
        public static TypedEvent<ShopBuyListReceivedArgs> ShopBuyListReceived = new();
        public static TypedEvent<ShopSellListReceivedArgs> ShopSellListReceived = new();
        public static TypedEvent<TradeWindowOpenArgs> TradeWindowOpened = new();
        public static TypedEvent<TradeWindowClosedArgs> TradeWindowClosed = new();
        public static TypedEvent<TradeWindowAcceptUpdatedArgs> TradeWindowAcceptUpdated = new();
        public static TypedEvent<TradeWindowCurrencyUpdatedArgs> TradeWindowCurrencyUpdated = new();
        public static TypedEvent<CustomHouseReceivedArgs> CustomHouseReceived = new();

        public static void RaiseItemSpawned(in ItemSpawnedArgs e) => ItemSpawned.Raise(e);
        public static void RaiseItemUpdated(in ItemUpdatedArgs e) => ItemUpdated.Raise(e);
        public static void RaiseItemRemoved(in ItemRemovedArgs e) => ItemRemoved.Raise(e);
        public static void RaiseContainerOpened(in ContainerOpenedArgs e) => ContainerOpened.Raise(e);
        public static void RaiseContainerItemAdded(in ContainerItemAddedArgs e) => ContainerItemAdded.Raise(e);
        public static void RaiseContainerItemsReceived(in ContainerItemsReceivedArgs e) => ContainerItemsReceived.Raise(e);
        public static void RaiseItemEquipped(in ItemEquippedArgs e) => ItemEquipped.Raise(e);
        public static void RaiseCorpseEquipmentReceived(in CorpseEquipmentReceivedArgs e) => CorpseEquipmentReceived.Raise(e);
        public static void RaiseDyeDataReceived(in DyeDataReceivedArgs e) => DyeDataReceived.Raise(e);
        public static void RaiseOplInfoReceived(in OplInfoReceivedArgs e) => OplInfoReceived.Raise(e);
        public static void RaiseMegaClilocReceived(in MegaClilocReceivedArgs e) => MegaClilocReceived.Raise(e);
        public static void RaiseItemDragAnimation(in ItemDragAnimationArgs e) => ItemDragAnimation.Raise(e);
        public static void RaiseItemMoveDenied(in ItemMoveDeniedArgs e) => ItemMoveDenied.Raise(e);
        public static void RaiseItemDragEnded(in ItemDragEndedArgs e) => ItemDragEnded.Raise(e);
        public static void RaiseItemDropAccepted(in ItemDropAcceptedArgs e) => ItemDropAccepted.Raise(e);
        public static void RaiseShopBuyListReceived(in ShopBuyListReceivedArgs e) => ShopBuyListReceived.Raise(e);
        public static void RaiseShopSellListReceived(in ShopSellListReceivedArgs e) => ShopSellListReceived.Raise(e);
        public static void RaiseTradeWindowOpened(in TradeWindowOpenArgs e) => TradeWindowOpened.Raise(e);
        public static void RaiseTradeWindowClosed(in TradeWindowClosedArgs e) => TradeWindowClosed.Raise(e);
        public static void RaiseTradeWindowAcceptUpdated(in TradeWindowAcceptUpdatedArgs e) => TradeWindowAcceptUpdated.Raise(e);
        public static void RaiseTradeWindowCurrencyUpdated(in TradeWindowCurrencyUpdatedArgs e) => TradeWindowCurrencyUpdated.Raise(e);
        public static void RaiseCustomHouseReceived(in CustomHouseReceivedArgs e) => CustomHouseReceived.Raise(e);

        // ---- Combat ----
        public static TypedEvent<DamageReceivedArgs> DamageReceived = new();
        public static TypedEvent<WarModeChangedArgs> WarModeChanged = new();
        public static TypedEvent<PlayerDeathArgs> PlayerDeath = new();
        public static TypedEvent<CombatSwingArgs> CombatSwing = new();
        public static TypedEvent<AttackTargetChangedArgs> AttackTargetChanged = new();
        public static TypedEvent<MobileDeathArgs> MobileDeath = new();

        public static void RaiseDamageReceived(in DamageReceivedArgs e) => DamageReceived.Raise(e);
        public static void RaiseWarModeChanged(in WarModeChangedArgs e) => WarModeChanged.Raise(e);
        public static void RaisePlayerDeath(in PlayerDeathArgs e) => PlayerDeath.Raise(e);
        public static void RaiseCombatSwing(in CombatSwingArgs e) => CombatSwing.Raise(e);
        public static void RaiseAttackTargetChanged(in AttackTargetChangedArgs e) => AttackTargetChanged.Raise(e);
        public static void RaiseMobileDeath(in MobileDeathArgs e) => MobileDeath.Raise(e);

        // ---- World ----
        public static TypedEvent<WeatherChangedArgs> WeatherChanged = new();
        public static TypedEvent<SeasonChangedArgs> SeasonChanged = new();
        public static TypedEvent<LightLevelChangedArgs> LightLevelChanged = new();
        public static TypedEvent<ObjectDeletedArgs> ObjectDeleted = new();
        public static TypedEvent<ClientViewRangeChangedArgs> ClientViewRangeChanged = new();
        public static TypedEvent<GraphicEffectSpawnedArgs> GraphicEffectSpawned = new();
        public static TypedEvent<SkillsUpdatedArgs> SkillsUpdated = new();
        public static TypedEvent<SkillListReceivedArgs> SkillListReceived = new();
        public static TypedEvent<TargetCursorReceivedArgs> TargetCursorReceived = new();
        public static TypedEvent<MultiPlacementReceivedArgs> MultiPlacementReceived = new();
        public static TypedEvent<BoatMovingReceivedArgs> BoatMovingReceived = new();
        public static TypedEvent<MapDataReceivedArgs> MapDataReceived = new();
        public static TypedEvent<PathfindingReceivedArgs> PathfindingReceived = new();

        public static void RaiseWeatherChanged(in WeatherChangedArgs e) => WeatherChanged.Raise(e);
        public static void RaiseSeasonChanged(in SeasonChangedArgs e) => SeasonChanged.Raise(e);
        public static void RaiseLightLevelChanged(in LightLevelChangedArgs e) => LightLevelChanged.Raise(e);
        public static void RaiseObjectDeleted(in ObjectDeletedArgs e) => ObjectDeleted.Raise(e);
        public static void RaiseClientViewRangeChanged(in ClientViewRangeChangedArgs e) => ClientViewRangeChanged.Raise(e);
        public static void RaiseGraphicEffectSpawned(in GraphicEffectSpawnedArgs e) => GraphicEffectSpawned.Raise(e);
        public static void RaiseSkillsUpdated(in SkillsUpdatedArgs e) => SkillsUpdated.Raise(e);
        public static void RaiseSkillListReceived(in SkillListReceivedArgs e) => SkillListReceived.Raise(e);
        public static void RaiseTargetCursorReceived(in TargetCursorReceivedArgs e) => TargetCursorReceived.Raise(e);
        public static void RaiseMultiPlacementReceived(in MultiPlacementReceivedArgs e) => MultiPlacementReceived.Raise(e);
        public static void RaiseBoatMovingReceived(in BoatMovingReceivedArgs e) => BoatMovingReceived.Raise(e);
        public static void RaiseMapDataReceived(in MapDataReceivedArgs e) => MapDataReceived.Raise(e);
        public static void RaisePathfindingReceived(in PathfindingReceivedArgs e) => PathfindingReceived.Raise(e);

        // ---- Audio ----
        public static TypedEvent<SoundPlayArgs> SoundPlay = new();
        public static TypedEvent<MusicPlayArgs> MusicPlay = new();
        public static TypedEvent<MusicStopArgs> MusicStop = new();

        public static void RaiseSoundPlay(in SoundPlayArgs e) => SoundPlay.Raise(e);
        public static void RaiseMusicPlay(in MusicPlayArgs e) => MusicPlay.Raise(e);
        public static void RaiseMusicStop(in MusicStopArgs e) => MusicStop.Raise(e);

        // ---- Network / session ----
        public static TypedEvent<ConnectedArgs> Connected = new();
        public static TypedEvent<DisconnectedArgs> Disconnected = new();
        public static TypedEvent<PingReceivedArgs> PingReceived = new();

        public static void RaiseConnected(in ConnectedArgs e) => Connected.Raise(e);
        public static void RaiseDisconnected(in DisconnectedArgs e) => Disconnected.Raise(e);
        public static void RaisePingReceived(in PingReceivedArgs e) => PingReceived.Raise(e);

        // ---- Login ----
        public static TypedEvent<LoginCompletedArgs> LoginCompleted = new();
        public static TypedEvent<LoginRejectedArgs> LoginRejected = new();
        public static TypedEvent<PlayerEnteredWorldArgs> PlayerEnteredWorld = new();
        public static TypedEvent<LogoutReceivedArgs> LogoutReceived = new();
        public static TypedEvent<ServerListReceivedArgs> ServerListReceived = new();
        public static TypedEvent<ServerRelayReceivedArgs> ServerRelayReceived = new();
        public static TypedEvent<CharacterListUpdatedArgs> CharacterListUpdated = new();
        public static TypedEvent<CharacterListReceivedArgs> CharacterListReceived = new();
        public static TypedEvent<LoginDelayReceivedArgs> LoginDelayReceived = new();
        public static TypedEvent<ClientVersionRequestedArgs> ClientVersionRequested = new();
        public static TypedEvent<LockedFeaturesEnabledArgs> LockedFeaturesEnabled = new();

        public static void RaiseLoginCompleted(in LoginCompletedArgs e) => LoginCompleted.Raise(e);
        public static void RaiseLoginRejected(in LoginRejectedArgs e) => LoginRejected.Raise(e);
        public static void RaisePlayerEnteredWorld(in PlayerEnteredWorldArgs e) => PlayerEnteredWorld.Raise(e);
        public static void RaiseLogoutReceived(in LogoutReceivedArgs e) => LogoutReceived.Raise(e);
        public static void RaiseServerListReceived(in ServerListReceivedArgs e) => ServerListReceived.Raise(e);
        public static void RaiseServerRelayReceived(in ServerRelayReceivedArgs e) => ServerRelayReceived.Raise(e);
        public static void RaiseCharacterListUpdated(in CharacterListUpdatedArgs e) => CharacterListUpdated.Raise(e);
        public static void RaiseCharacterListReceived(in CharacterListReceivedArgs e) => CharacterListReceived.Raise(e);
        public static void RaiseLoginDelayReceived(in LoginDelayReceivedArgs e) => LoginDelayReceived.Raise(e);
        public static void RaiseClientVersionRequested(in ClientVersionRequestedArgs e) => ClientVersionRequested.Raise(e);
        public static void RaiseLockedFeaturesEnabled(in LockedFeaturesEnabledArgs e) => LockedFeaturesEnabled.Raise(e);

        // ---- UI ----
        public static TypedEvent<GumpOpenedArgs> GumpOpened = new();
        public static TypedEvent<GumpClosedArgs> GumpClosed = new();
        public static TypedEvent<CompressedGumpOpenedArgs> CompressedGumpOpened = new();
        public static TypedEvent<ContextMenuOpenedArgs> ContextMenuOpened = new();
        public static TypedEvent<PaperdollOpenedArgs> PaperdollOpened = new();
        public static TypedEvent<MapDisplayedArgs> MapDisplayed = new();
        public static TypedEvent<BookOpenedArgs> BookOpened = new();
        public static TypedEvent<BookDataReceivedArgs> BookDataReceived = new();
        public static TypedEvent<TextEntryDialogArgs> TextEntryDialogOpened = new();
        public static TypedEvent<TipWindowDisplayedArgs> TipWindowDisplayed = new();
        public static TypedEvent<BulletinBoardOpenedArgs> BulletinBoardOpened = new();
        public static TypedEvent<BulletinBoardSummaryArgs> BulletinBoardSummary = new();
        public static TypedEvent<BulletinBoardMessageArgs> BulletinBoardMessage = new();
        public static TypedEvent<OpenUrlRequestedArgs> OpenUrlRequested = new();
        public static TypedEvent<CharacterProfileOpenedArgs> CharacterProfileOpened = new();
        public static TypedEvent<VendorWindowClosedArgs> VendorWindowClosed = new();
        public static TypedEvent<QuestArrowDisplayedArgs> QuestArrowDisplayed = new();
        public static TypedEvent<WaypointDisplayedArgs> WaypointDisplayed = new();
        public static TypedEvent<WaypointRemovedArgs> WaypointRemoved = new();

        public static void RaiseGumpOpened(in GumpOpenedArgs e) => GumpOpened.Raise(e);
        public static void RaiseGumpClosed(in GumpClosedArgs e) => GumpClosed.Raise(e);
        public static void RaiseCompressedGumpOpened(in CompressedGumpOpenedArgs e) => CompressedGumpOpened.Raise(e);
        public static void RaiseContextMenuOpened(in ContextMenuOpenedArgs e) => ContextMenuOpened.Raise(e);
        public static void RaisePaperdollOpened(in PaperdollOpenedArgs e) => PaperdollOpened.Raise(e);
        public static void RaiseMapDisplayed(in MapDisplayedArgs e) => MapDisplayed.Raise(e);
        public static void RaiseBookOpened(in BookOpenedArgs e) => BookOpened.Raise(e);
        public static void RaiseBookDataReceived(in BookDataReceivedArgs e) => BookDataReceived.Raise(e);
        public static void RaiseTextEntryDialogOpened(in TextEntryDialogArgs e) => TextEntryDialogOpened.Raise(e);
        public static void RaiseTipWindowDisplayed(in TipWindowDisplayedArgs e) => TipWindowDisplayed.Raise(e);
        public static void RaiseBulletinBoardOpened(in BulletinBoardOpenedArgs e) => BulletinBoardOpened.Raise(e);
        public static void RaiseBulletinBoardSummary(in BulletinBoardSummaryArgs e) => BulletinBoardSummary.Raise(e);
        public static void RaiseBulletinBoardMessage(in BulletinBoardMessageArgs e) => BulletinBoardMessage.Raise(e);
        public static void RaiseOpenUrlRequested(in OpenUrlRequestedArgs e) => OpenUrlRequested.Raise(e);
        public static void RaiseCharacterProfileOpened(in CharacterProfileOpenedArgs e) => CharacterProfileOpened.Raise(e);
        public static void RaiseVendorWindowClosed(in VendorWindowClosedArgs e) => VendorWindowClosed.Raise(e);
        public static void RaiseQuestArrowDisplayed(in QuestArrowDisplayedArgs e) => QuestArrowDisplayed.Raise(e);
        public static void RaiseWaypointDisplayed(in WaypointDisplayedArgs e) => WaypointDisplayed.Raise(e);
        public static void RaiseWaypointRemoved(in WaypointRemovedArgs e) => WaypointRemoved.Raise(e);

        // ---- ExtendedCommand (0xBF) sub-command events ----
        public static TypedEvent<FastWalkStackInitArgs> FastWalkStackInit = new();
        public static TypedEvent<FastWalkStackAddArgs> FastWalkStackAdd = new();
        public static TypedEvent<GenericGumpCloseArgs> GenericGumpClose = new();
        public static TypedEvent<PartyListUpdatedArgs> PartyListUpdated = new();
        public static TypedEvent<PartyChatMessageArgs> PartyChatMessage = new();
        public static TypedEvent<PartyInviteReceivedArgs> PartyInviteReceived = new();
        public static TypedEvent<MapIndexChangedArgs> MapIndexChanged = new();
        public static TypedEvent<CloseStatusbarGumpArgs> CloseStatusbarGump = new();
        public static TypedEvent<EquipInfoArgs> EquipInfoReceived = new();
        public static TypedEvent<PopupMenuArgs> PopupMenuReceived = new();
        public static TypedEvent<CloseUserInterfaceArgs> CloseUserInterface = new();
        public static TypedEvent<MapPatchesEnabledArgs> MapPatchesEnabled = new();
        public static TypedEvent<ExtendedStatsBondedArgs> ExtendedStatsBonded = new();
        public static TypedEvent<ExtendedStatsLocksArgs> ExtendedStatsLocks = new();
        public static TypedEvent<ExtendedStatsAnimationArgs> ExtendedStatsAnimation = new();
        public static TypedEvent<SpellbookContentArgs> SpellbookContent = new();
        public static TypedEvent<HouseRevisionStateArgs> HouseRevisionState = new();
        public static TypedEvent<HouseDesignStateArgs> HouseDesignState = new();
        public static TypedEvent<AbilityIconsResetArgs> AbilityIconsReset = new();
        public static TypedEvent<DamageOverheadArgs> DamageOverhead = new();
        public static TypedEvent<SpellIconToggleArgs> SpellIconToggle = new();
        public static TypedEvent<CharacterSpeedModeArgs> CharacterSpeedMode = new();
        public static TypedEvent<RaceChangeRequestedArgs> RaceChangeRequested = new();
        public static TypedEvent<MobileAnimationFrameArgs> MobileAnimationFrame = new();
        public static TypedEvent<CuoCommandArgs> CuoCommand = new();

        public static void RaiseFastWalkStackInit(in FastWalkStackInitArgs e) => FastWalkStackInit.Raise(e);
        public static void RaiseFastWalkStackAdd(in FastWalkStackAddArgs e) => FastWalkStackAdd.Raise(e);
        public static void RaiseGenericGumpClose(in GenericGumpCloseArgs e) => GenericGumpClose.Raise(e);
        public static void RaisePartyListUpdated(in PartyListUpdatedArgs e) => PartyListUpdated.Raise(e);
        public static void RaisePartyChatMessage(in PartyChatMessageArgs e) => PartyChatMessage.Raise(e);
        public static void RaisePartyInviteReceived(in PartyInviteReceivedArgs e) => PartyInviteReceived.Raise(e);
        public static void RaiseMapIndexChanged(in MapIndexChangedArgs e) => MapIndexChanged.Raise(e);
        public static void RaiseCloseStatusbarGump(in CloseStatusbarGumpArgs e) => CloseStatusbarGump.Raise(e);
        public static void RaiseEquipInfoReceived(in EquipInfoArgs e) => EquipInfoReceived.Raise(e);
        public static void RaisePopupMenuReceived(in PopupMenuArgs e) => PopupMenuReceived.Raise(e);
        public static void RaiseCloseUserInterface(in CloseUserInterfaceArgs e) => CloseUserInterface.Raise(e);
        public static void RaiseMapPatchesEnabled(in MapPatchesEnabledArgs e) => MapPatchesEnabled.Raise(e);
        public static void RaiseExtendedStatsBonded(in ExtendedStatsBondedArgs e) => ExtendedStatsBonded.Raise(e);
        public static void RaiseExtendedStatsLocks(in ExtendedStatsLocksArgs e) => ExtendedStatsLocks.Raise(e);
        public static void RaiseExtendedStatsAnimation(in ExtendedStatsAnimationArgs e) => ExtendedStatsAnimation.Raise(e);
        public static void RaiseSpellbookContent(in SpellbookContentArgs e) => SpellbookContent.Raise(e);
        public static void RaiseHouseRevisionState(in HouseRevisionStateArgs e) => HouseRevisionState.Raise(e);
        public static void RaiseHouseDesignState(in HouseDesignStateArgs e) => HouseDesignState.Raise(e);
        public static void RaiseAbilityIconsReset(in AbilityIconsResetArgs e) => AbilityIconsReset.Raise(e);
        public static void RaiseDamageOverhead(in DamageOverheadArgs e) => DamageOverhead.Raise(e);
        public static void RaiseSpellIconToggle(in SpellIconToggleArgs e) => SpellIconToggle.Raise(e);
        public static void RaiseCharacterSpeedMode(in CharacterSpeedModeArgs e) => CharacterSpeedMode.Raise(e);
        public static void RaiseRaceChangeRequested(in RaceChangeRequestedArgs e) => RaceChangeRequested.Raise(e);
        public static void RaiseMobileAnimationFrame(in MobileAnimationFrameArgs e) => MobileAnimationFrame.Raise(e);
        public static void RaiseCuoCommand(in CuoCommandArgs e) => CuoCommand.Raise(e);

        /// <summary>Clears every subscription. Intended for tests only.</summary>
        public static void ClearAll()
        {
            ChatMessage.Clear();
            UnicodeChatMessage.Clear();
            ClilocMessage.Clear();
            AsciiPrompt.Clear();
            UnicodePrompt.Clear();
            ChatConferenceCreated.Clear();
            ChatConferenceDestroyed.Clear();
            ChatUsernameRequest.Clear();
            ChatClosed.Clear();
            ChatUsernameAccepted.Clear();
            ChatUserAdded.Clear();
            ChatUserRemoved.Clear();
            ChatClearAllPlayers.Clear();
            ChatConferenceJoined.Clear();
            ChatConferenceLeft.Clear();
            ChatTextReceived.Clear();
            ChatSystemMessage.Clear();

            MobileSpawned.Clear();
            MobileUpdated.Clear();
            PlayerUpdated.Clear();
            MobileMoved.Clear();
            MobileRemoved.Clear();
            MobileAttributesUpdated.Clear();
            HitpointsUpdated.Clear();
            ManaUpdated.Clear();
            StaminaUpdated.Clear();
            WalkDenied.Clear();
            WalkConfirmed.Clear();
            PlayerMoved.Clear();
            MobileNameChanged.Clear();
            HealthBarStateChanged.Clear();
            BuffApplied.Clear();
            BuffRemoved.Clear();
            CharacterAnimation.Clear();
            NewCharacterAnimation.Clear();
            MobileStatusUpdated.Clear();
            CharacterStatusReceived.Clear();

            ItemSpawned.Clear();
            ItemUpdated.Clear();
            ItemRemoved.Clear();
            ContainerOpened.Clear();
            ContainerItemAdded.Clear();
            ContainerItemsReceived.Clear();
            ItemEquipped.Clear();
            CorpseEquipmentReceived.Clear();
            DyeDataReceived.Clear();
            OplInfoReceived.Clear();
            MegaClilocReceived.Clear();
            ItemDragAnimation.Clear();
            ItemMoveDenied.Clear();
            ItemDragEnded.Clear();
            ItemDropAccepted.Clear();
            ShopBuyListReceived.Clear();
            ShopSellListReceived.Clear();
            TradeWindowOpened.Clear();
            TradeWindowClosed.Clear();
            TradeWindowAcceptUpdated.Clear();
            TradeWindowCurrencyUpdated.Clear();
            CustomHouseReceived.Clear();

            DamageReceived.Clear();
            WarModeChanged.Clear();
            PlayerDeath.Clear();
            CombatSwing.Clear();
            AttackTargetChanged.Clear();
            MobileDeath.Clear();

            WeatherChanged.Clear();
            SeasonChanged.Clear();
            LightLevelChanged.Clear();
            ObjectDeleted.Clear();
            ClientViewRangeChanged.Clear();
            GraphicEffectSpawned.Clear();
            SkillsUpdated.Clear();
            SkillListReceived.Clear();
            TargetCursorReceived.Clear();
            MultiPlacementReceived.Clear();
            BoatMovingReceived.Clear();
            MapDataReceived.Clear();
            PathfindingReceived.Clear();

            SoundPlay.Clear();
            MusicPlay.Clear();
            MusicStop.Clear();

            Connected.Clear();
            Disconnected.Clear();
            PingReceived.Clear();

            LoginCompleted.Clear();
            LoginRejected.Clear();
            PlayerEnteredWorld.Clear();
            LogoutReceived.Clear();
            ServerListReceived.Clear();
            ServerRelayReceived.Clear();
            CharacterListUpdated.Clear();
            CharacterListReceived.Clear();
            LoginDelayReceived.Clear();
            ClientVersionRequested.Clear();
            LockedFeaturesEnabled.Clear();

            GumpOpened.Clear();
            GumpClosed.Clear();
            CompressedGumpOpened.Clear();
            ContextMenuOpened.Clear();
            PaperdollOpened.Clear();
            MapDisplayed.Clear();
            BookOpened.Clear();
            BookDataReceived.Clear();
            TextEntryDialogOpened.Clear();
            TipWindowDisplayed.Clear();
            BulletinBoardOpened.Clear();
            BulletinBoardSummary.Clear();
            BulletinBoardMessage.Clear();
            OpenUrlRequested.Clear();
            CharacterProfileOpened.Clear();
            VendorWindowClosed.Clear();
            QuestArrowDisplayed.Clear();
            WaypointDisplayed.Clear();
            WaypointRemoved.Clear();

            FastWalkStackInit.Clear();
            FastWalkStackAdd.Clear();
            GenericGumpClose.Clear();
            PartyListUpdated.Clear();
            PartyChatMessage.Clear();
            PartyInviteReceived.Clear();
            MapIndexChanged.Clear();
            CloseStatusbarGump.Clear();
            EquipInfoReceived.Clear();
            PopupMenuReceived.Clear();
            CloseUserInterface.Clear();
            MapPatchesEnabled.Clear();
            ExtendedStatsBonded.Clear();
            ExtendedStatsLocks.Clear();
            ExtendedStatsAnimation.Clear();
            SpellbookContent.Clear();
            HouseRevisionState.Clear();
            HouseDesignState.Clear();
            AbilityIconsReset.Clear();
            DamageOverhead.Clear();
            SpellIconToggle.Clear();
            CharacterSpeedMode.Clear();
            RaceChangeRequested.Clear();
            MobileAnimationFrame.Clear();
            CuoCommand.Clear();
        }
    }
}
