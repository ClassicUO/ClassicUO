// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Events.Outgoing
{
    /// <summary>
    /// Static hub for outgoing-packet events. Mirrors <see cref="EventSink"/>
    /// but for the client-to-server direction.
    /// <para/>
    /// Every <c>NetClient.Send_*</c> extension raises one typed event here
    /// before serializing bytes. Subscribers (logging, plugins, replay,
    /// instrumentation) observe the typed parameters without parsing wire
    /// format. The caller API is unchanged — <c>Send_*</c> still writes the
    /// packet directly.
    /// </summary>
    internal static class OutgoingEventSink
    {
        // ---- Network / session ----
        public static TypedEvent<PingSentArgs> PingSent = new();

        public static void RaisePingSent(in PingSentArgs e) => PingSent.Raise(e);

        // ---- Login ----
        public static TypedEvent<AckTalkSentArgs> AckTalkSent = new();
        public static TypedEvent<SeedSentArgs> SeedSent = new();
        public static TypedEvent<SeedOldSentArgs> SeedOldSent = new();
        public static TypedEvent<FirstLoginSentArgs> FirstLoginSent = new();
        public static TypedEvent<SecondLoginSentArgs> SecondLoginSent = new();
        public static TypedEvent<SelectServerSentArgs> SelectServerSent = new();
        public static TypedEvent<SelectCharacterSentArgs> SelectCharacterSent = new();
        public static TypedEvent<CreateCharacterSentArgs> CreateCharacterSent = new();
        public static TypedEvent<DeleteCharacterSentArgs> DeleteCharacterSent = new();
        public static TypedEvent<LogoutNotificationSentArgs> LogoutNotificationSent = new();
        public static TypedEvent<ClientVersionSentArgs> ClientVersionSent = new();
        public static TypedEvent<LanguageSentArgs> LanguageSent = new();
        public static TypedEvent<ClientTypeSentArgs> ClientTypeSent = new();

        public static void RaiseAckTalkSent(in AckTalkSentArgs e) => AckTalkSent.Raise(e);
        public static void RaiseSeedSent(in SeedSentArgs e) => SeedSent.Raise(e);
        public static void RaiseSeedOldSent(in SeedOldSentArgs e) => SeedOldSent.Raise(e);
        public static void RaiseFirstLoginSent(in FirstLoginSentArgs e) => FirstLoginSent.Raise(e);
        public static void RaiseSecondLoginSent(in SecondLoginSentArgs e) => SecondLoginSent.Raise(e);
        public static void RaiseSelectServerSent(in SelectServerSentArgs e) => SelectServerSent.Raise(e);
        public static void RaiseSelectCharacterSent(in SelectCharacterSentArgs e) => SelectCharacterSent.Raise(e);
        public static void RaiseCreateCharacterSent(in CreateCharacterSentArgs e) => CreateCharacterSent.Raise(e);
        public static void RaiseDeleteCharacterSent(in DeleteCharacterSentArgs e) => DeleteCharacterSent.Raise(e);
        public static void RaiseLogoutNotificationSent(in LogoutNotificationSentArgs e) => LogoutNotificationSent.Raise(e);
        public static void RaiseClientVersionSent(in ClientVersionSentArgs e) => ClientVersionSent.Raise(e);
        public static void RaiseLanguageSent(in LanguageSentArgs e) => LanguageSent.Raise(e);
        public static void RaiseClientTypeSent(in ClientTypeSentArgs e) => ClientTypeSent.Raise(e);

        // ---- Movement ----
        public static TypedEvent<WalkRequestSentArgs> WalkRequestSent = new();
        public static TypedEvent<ResyncSentArgs> ResyncSent = new();
        public static TypedEvent<MultiBoatMoveRequestSentArgs> MultiBoatMoveRequestSent = new();

        public static void RaiseWalkRequestSent(in WalkRequestSentArgs e) => WalkRequestSent.Raise(e);
        public static void RaiseResyncSent(in ResyncSentArgs e) => ResyncSent.Raise(e);
        public static void RaiseMultiBoatMoveRequestSent(in MultiBoatMoveRequestSentArgs e) => MultiBoatMoveRequestSent.Raise(e);

        // ---- Combat ----
        public static TypedEvent<AttackRequestSentArgs> AttackRequestSent = new();
        public static TypedEvent<TargetObjectSentArgs> TargetObjectSent = new();
        public static TypedEvent<TargetXyzSentArgs> TargetXyzSent = new();
        public static TypedEvent<TargetCancelSentArgs> TargetCancelSent = new();
        public static TypedEvent<TargetSelectedObjectSentArgs> TargetSelectedObjectSent = new();
        public static TypedEvent<ChangeWarModeSentArgs> ChangeWarModeSent = new();
        public static TypedEvent<UseCombatAbilitySentArgs> UseCombatAbilitySent = new();
        public static TypedEvent<ClickQuestArrowSentArgs> ClickQuestArrowSent = new();

        public static void RaiseAttackRequestSent(in AttackRequestSentArgs e) => AttackRequestSent.Raise(e);
        public static void RaiseTargetObjectSent(in TargetObjectSentArgs e) => TargetObjectSent.Raise(e);
        public static void RaiseTargetXyzSent(in TargetXyzSentArgs e) => TargetXyzSent.Raise(e);
        public static void RaiseTargetCancelSent(in TargetCancelSentArgs e) => TargetCancelSent.Raise(e);
        public static void RaiseTargetSelectedObjectSent(in TargetSelectedObjectSentArgs e) => TargetSelectedObjectSent.Raise(e);
        public static void RaiseChangeWarModeSent(in ChangeWarModeSentArgs e) => ChangeWarModeSent.Raise(e);
        public static void RaiseUseCombatAbilitySent(in UseCombatAbilitySentArgs e) => UseCombatAbilitySent.Raise(e);
        public static void RaiseClickQuestArrowSent(in ClickQuestArrowSentArgs e) => ClickQuestArrowSent.Raise(e);

        // ---- Interaction ----
        public static TypedEvent<DoubleClickSentArgs> DoubleClickSent = new();
        public static TypedEvent<ClickRequestSentArgs> ClickRequestSent = new();
        public static TypedEvent<UseSkillSentArgs> UseSkillSent = new();
        public static TypedEvent<NameRequestSentArgs> NameRequestSent = new();
        public static TypedEvent<OpenDoorSentArgs> OpenDoorSent = new();

        public static void RaiseDoubleClickSent(in DoubleClickSentArgs e) => DoubleClickSent.Raise(e);
        public static void RaiseClickRequestSent(in ClickRequestSentArgs e) => ClickRequestSent.Raise(e);
        public static void RaiseUseSkillSent(in UseSkillSentArgs e) => UseSkillSent.Raise(e);
        public static void RaiseNameRequestSent(in NameRequestSentArgs e) => NameRequestSent.Raise(e);
        public static void RaiseOpenDoorSent(in OpenDoorSentArgs e) => OpenDoorSent.Raise(e);

        // ---- Items / Equip ----
        public static TypedEvent<PickUpRequestSentArgs> PickUpRequestSent = new();
        public static TypedEvent<DropRequestSentArgs> DropRequestSent = new();
        public static TypedEvent<DropRequestOldSentArgs> DropRequestOldSent = new();
        public static TypedEvent<EquipRequestSentArgs> EquipRequestSent = new();
        public static TypedEvent<EquipMacroKrSentArgs> EquipMacroKrSent = new();
        public static TypedEvent<UnequipMacroKrSentArgs> UnequipMacroKrSent = new();
        public static TypedEvent<DyeDataResponseSentArgs> DyeDataResponseSent = new();
        public static TypedEvent<RenameRequestSentArgs> RenameRequestSent = new();

        public static void RaisePickUpRequestSent(in PickUpRequestSentArgs e) => PickUpRequestSent.Raise(e);
        public static void RaiseDropRequestSent(in DropRequestSentArgs e) => DropRequestSent.Raise(e);
        public static void RaiseDropRequestOldSent(in DropRequestOldSentArgs e) => DropRequestOldSent.Raise(e);
        public static void RaiseEquipRequestSent(in EquipRequestSentArgs e) => EquipRequestSent.Raise(e);
        public static void RaiseEquipMacroKrSent(in EquipMacroKrSentArgs e) => EquipMacroKrSent.Raise(e);
        public static void RaiseUnequipMacroKrSent(in UnequipMacroKrSentArgs e) => UnequipMacroKrSent.Raise(e);
        public static void RaiseDyeDataResponseSent(in DyeDataResponseSentArgs e) => DyeDataResponseSent.Raise(e);
        public static void RaiseRenameRequestSent(in RenameRequestSentArgs e) => RenameRequestSent.Raise(e);

        // ---- Spells / Abilities ----
        public static TypedEvent<CastSpellSentArgs> CastSpellSent = new();
        public static TypedEvent<CastSpellFromBookSentArgs> CastSpellFromBookSent = new();
        public static TypedEvent<OpenSpellBookSentArgs> OpenSpellBookSent = new();
        public static TypedEvent<StunRequestSentArgs> StunRequestSent = new();
        public static TypedEvent<DisarmRequestSentArgs> DisarmRequestSent = new();
        public static TypedEvent<ToggleGargoyleFlyingSentArgs> ToggleGargoyleFlyingSent = new();
        public static TypedEvent<InvokeVirtueRequestSentArgs> InvokeVirtueRequestSent = new();
        public static TypedEvent<ChangeRaceRequestSentArgs> ChangeRaceRequestSent = new();

        public static void RaiseCastSpellSent(in CastSpellSentArgs e) => CastSpellSent.Raise(e);
        public static void RaiseCastSpellFromBookSent(in CastSpellFromBookSentArgs e) => CastSpellFromBookSent.Raise(e);
        public static void RaiseOpenSpellBookSent(in OpenSpellBookSentArgs e) => OpenSpellBookSent.Raise(e);
        public static void RaiseStunRequestSent(in StunRequestSentArgs e) => StunRequestSent.Raise(e);
        public static void RaiseDisarmRequestSent(in DisarmRequestSentArgs e) => DisarmRequestSent.Raise(e);
        public static void RaiseToggleGargoyleFlyingSent(in ToggleGargoyleFlyingSentArgs e) => ToggleGargoyleFlyingSent.Raise(e);
        public static void RaiseInvokeVirtueRequestSent(in InvokeVirtueRequestSentArgs e) => InvokeVirtueRequestSent.Raise(e);
        public static void RaiseChangeRaceRequestSent(in ChangeRaceRequestSentArgs e) => ChangeRaceRequestSent.Raise(e);

        // ---- Status / Skills ----
        public static TypedEvent<StatusRequestSentArgs> StatusRequestSent = new();
        public static TypedEvent<SkillsRequestSentArgs> SkillsRequestSent = new();
        public static TypedEvent<SkillsStatusRequestSentArgs> SkillsStatusRequestSent = new();
        public static TypedEvent<StatLockStateRequestSentArgs> StatLockStateRequestSent = new();
        public static TypedEvent<SkillStatusChangeRequestSentArgs> SkillStatusChangeRequestSent = new();
        public static TypedEvent<HelpRequestSentArgs> HelpRequestSent = new();

        public static void RaiseStatusRequestSent(in StatusRequestSentArgs e) => StatusRequestSent.Raise(e);
        public static void RaiseSkillsRequestSent(in SkillsRequestSentArgs e) => SkillsRequestSent.Raise(e);
        public static void RaiseSkillsStatusRequestSent(in SkillsStatusRequestSentArgs e) => SkillsStatusRequestSent.Raise(e);
        public static void RaiseStatLockStateRequestSent(in StatLockStateRequestSentArgs e) => StatLockStateRequestSent.Raise(e);
        public static void RaiseSkillStatusChangeRequestSent(in SkillStatusChangeRequestSentArgs e) => SkillStatusChangeRequestSent.Raise(e);
        public static void RaiseHelpRequestSent(in HelpRequestSentArgs e) => HelpRequestSent.Raise(e);

        // ---- OPL / Cliloc ----
        public static TypedEvent<MegaClilocRequestOldSentArgs> MegaClilocRequestOldSent = new();
        public static TypedEvent<MegaClilocRequestSentArgs> MegaClilocRequestSent = new();

        public static void RaiseMegaClilocRequestOldSent(in MegaClilocRequestOldSentArgs e) => MegaClilocRequestOldSent.Raise(e);
        public static void RaiseMegaClilocRequestSent(in MegaClilocRequestSentArgs e) => MegaClilocRequestSent.Raise(e);

        // ---- Profile ----
        public static TypedEvent<ProfileRequestSentArgs> ProfileRequestSent = new();
        public static TypedEvent<ProfileUpdateSentArgs> ProfileUpdateSent = new();
        public static TypedEvent<TipRequestSentArgs> TipRequestSent = new();

        public static void RaiseProfileRequestSent(in ProfileRequestSentArgs e) => ProfileRequestSent.Raise(e);
        public static void RaiseProfileUpdateSent(in ProfileUpdateSentArgs e) => ProfileUpdateSent.Raise(e);
        public static void RaiseTipRequestSent(in TipRequestSentArgs e) => TipRequestSent.Raise(e);
        // ---- Chat / Social ----
        public static TypedEvent<EmoteActionSentArgs> EmoteActionSent = new();
        public static TypedEvent<AsciiSpeechRequestSentArgs> AsciiSpeechRequestSent = new();
        public static TypedEvent<UnicodeSpeechRequestSentArgs> UnicodeSpeechRequestSent = new();
        public static TypedEvent<ChatJoinCommandSentArgs> ChatJoinCommandSent = new();
        public static TypedEvent<ChatCreateChannelCommandSentArgs> ChatCreateChannelCommandSent = new();
        public static TypedEvent<ChatLeaveChannelCommandSentArgs> ChatLeaveChannelCommandSent = new();
        public static TypedEvent<ChatMessageCommandSentArgs> ChatMessageCommandSent = new();
        public static TypedEvent<OpenChatSentArgs> OpenChatSent = new();
        public static TypedEvent<MapMessageSentArgs> MapMessageSent = new();
        public static TypedEvent<RazorAckSentArgs> RazorAckSent = new();

        public static void RaiseEmoteActionSent(in EmoteActionSentArgs e) => EmoteActionSent.Raise(e);
        public static void RaiseAsciiSpeechRequestSent(in AsciiSpeechRequestSentArgs e) => AsciiSpeechRequestSent.Raise(e);
        public static void RaiseUnicodeSpeechRequestSent(in UnicodeSpeechRequestSentArgs e) => UnicodeSpeechRequestSent.Raise(e);
        public static void RaiseChatJoinCommandSent(in ChatJoinCommandSentArgs e) => ChatJoinCommandSent.Raise(e);
        public static void RaiseChatCreateChannelCommandSent(in ChatCreateChannelCommandSentArgs e) => ChatCreateChannelCommandSent.Raise(e);
        public static void RaiseChatLeaveChannelCommandSent(in ChatLeaveChannelCommandSentArgs e) => ChatLeaveChannelCommandSent.Raise(e);
        public static void RaiseChatMessageCommandSent(in ChatMessageCommandSentArgs e) => ChatMessageCommandSent.Raise(e);
        public static void RaiseOpenChatSent(in OpenChatSentArgs e) => OpenChatSent.Raise(e);
        public static void RaiseMapMessageSent(in MapMessageSentArgs e) => MapMessageSent.Raise(e);
        public static void RaiseRazorAckSent(in RazorAckSentArgs e) => RazorAckSent.Raise(e);

        // ---- Party ----
        public static TypedEvent<PartyInviteRequestSentArgs> PartyInviteRequestSent = new();
        public static TypedEvent<PartyRemoveRequestSentArgs> PartyRemoveRequestSent = new();
        public static TypedEvent<PartyChangeLootTypeRequestSentArgs> PartyChangeLootTypeRequestSent = new();
        public static TypedEvent<PartyAcceptSentArgs> PartyAcceptSent = new();
        public static TypedEvent<PartyDeclineSentArgs> PartyDeclineSent = new();
        public static TypedEvent<PartyMessageSentArgs> PartyMessageSent = new();

        public static void RaisePartyInviteRequestSent(in PartyInviteRequestSentArgs e) => PartyInviteRequestSent.Raise(e);
        public static void RaisePartyRemoveRequestSent(in PartyRemoveRequestSentArgs e) => PartyRemoveRequestSent.Raise(e);
        public static void RaisePartyChangeLootTypeRequestSent(in PartyChangeLootTypeRequestSentArgs e) => PartyChangeLootTypeRequestSent.Raise(e);
        public static void RaisePartyAcceptSent(in PartyAcceptSentArgs e) => PartyAcceptSent.Raise(e);
        public static void RaisePartyDeclineSent(in PartyDeclineSentArgs e) => PartyDeclineSent.Raise(e);
        public static void RaisePartyMessageSent(in PartyMessageSentArgs e) => PartyMessageSent.Raise(e);

        // ---- Trade / Vendor ----
        public static TypedEvent<TradeResponseSentArgs> TradeResponseSent = new();
        public static TypedEvent<TradeUpdateGoldSentArgs> TradeUpdateGoldSent = new();
        public static TypedEvent<BuyRequestSentArgs> BuyRequestSent = new();
        public static TypedEvent<SellRequestSentArgs> SellRequestSent = new();

        public static void RaiseTradeResponseSent(in TradeResponseSentArgs e) => TradeResponseSent.Raise(e);
        public static void RaiseTradeUpdateGoldSent(in TradeUpdateGoldSentArgs e) => TradeUpdateGoldSent.Raise(e);
        public static void RaiseBuyRequestSent(in BuyRequestSentArgs e) => BuyRequestSent.Raise(e);
        public static void RaiseSellRequestSent(in SellRequestSentArgs e) => SellRequestSent.Raise(e);

        // ---- Gumps / Menus ----
        public static TypedEvent<GumpResponseSentArgs> GumpResponseSent = new();
        public static TypedEvent<VirtueGumpResponseSentArgs> VirtueGumpResponseSent = new();
        public static TypedEvent<MenuResponseSentArgs> MenuResponseSent = new();
        public static TypedEvent<GrayMenuResponseSentArgs> GrayMenuResponseSent = new();
        public static TypedEvent<RequestPopupMenuSentArgs> RequestPopupMenuSent = new();
        public static TypedEvent<PopupMenuSelectionSentArgs> PopupMenuSelectionSent = new();
        public static TypedEvent<TextEntryDialogResponseSentArgs> TextEntryDialogResponseSent = new();

        public static void RaiseGumpResponseSent(in GumpResponseSentArgs e) => GumpResponseSent.Raise(e);
        public static void RaiseVirtueGumpResponseSent(in VirtueGumpResponseSentArgs e) => VirtueGumpResponseSent.Raise(e);
        public static void RaiseMenuResponseSent(in MenuResponseSentArgs e) => MenuResponseSent.Raise(e);
        public static void RaiseGrayMenuResponseSent(in GrayMenuResponseSentArgs e) => GrayMenuResponseSent.Raise(e);
        public static void RaiseRequestPopupMenuSent(in RequestPopupMenuSentArgs e) => RequestPopupMenuSent.Raise(e);
        public static void RaisePopupMenuSelectionSent(in PopupMenuSelectionSentArgs e) => PopupMenuSelectionSent.Raise(e);
        public static void RaiseTextEntryDialogResponseSent(in TextEntryDialogResponseSentArgs e) => TextEntryDialogResponseSent.Raise(e);

        // ---- Prompts ----
        public static TypedEvent<AsciiPromptResponseSentArgs> AsciiPromptResponseSent = new();
        public static TypedEvent<UnicodePromptResponseSentArgs> UnicodePromptResponseSent = new();

        public static void RaiseAsciiPromptResponseSent(in AsciiPromptResponseSentArgs e) => AsciiPromptResponseSent.Raise(e);
        public static void RaiseUnicodePromptResponseSent(in UnicodePromptResponseSentArgs e) => UnicodePromptResponseSent.Raise(e);
        // ---- Books ----
        public static TypedEvent<BookHeaderChangedOldSentArgs> BookHeaderChangedOldSent = new();
        public static TypedEvent<BookHeaderChangedSentArgs> BookHeaderChangedSent = new();
        public static TypedEvent<BookPageDataSentArgs> BookPageDataSent = new();
        public static TypedEvent<BookPageDataRequestSentArgs> BookPageDataRequestSent = new();

        public static void RaiseBookHeaderChangedOldSent(in BookHeaderChangedOldSentArgs e) => BookHeaderChangedOldSent.Raise(e);
        public static void RaiseBookHeaderChangedSent(in BookHeaderChangedSentArgs e) => BookHeaderChangedSent.Raise(e);
        public static void RaiseBookPageDataSent(in BookPageDataSentArgs e) => BookPageDataSent.Raise(e);
        public static void RaiseBookPageDataRequestSent(in BookPageDataRequestSentArgs e) => BookPageDataRequestSent.Raise(e);

        // ---- Bulletin Board ----
        public static TypedEvent<BulletinBoardRequestMessageSentArgs> BulletinBoardRequestMessageSent = new();
        public static TypedEvent<BulletinBoardRequestMessageSummarySentArgs> BulletinBoardRequestMessageSummarySent = new();
        public static TypedEvent<BulletinBoardPostMessageSentArgs> BulletinBoardPostMessageSent = new();
        public static TypedEvent<BulletinBoardRemoveMessageSentArgs> BulletinBoardRemoveMessageSent = new();

        public static void RaiseBulletinBoardRequestMessageSent(in BulletinBoardRequestMessageSentArgs e) => BulletinBoardRequestMessageSent.Raise(e);
        public static void RaiseBulletinBoardRequestMessageSummarySent(in BulletinBoardRequestMessageSummarySentArgs e) => BulletinBoardRequestMessageSummarySent.Raise(e);
        public static void RaiseBulletinBoardPostMessageSent(in BulletinBoardPostMessageSentArgs e) => BulletinBoardPostMessageSent.Raise(e);
        public static void RaiseBulletinBoardRemoveMessageSent(in BulletinBoardRemoveMessageSentArgs e) => BulletinBoardRemoveMessageSent.Raise(e);

        // ---- House Customization ----
        public static TypedEvent<CustomHouseDataRequestSentArgs> CustomHouseDataRequestSent = new();
        public static TypedEvent<CustomHouseBackupSentArgs> CustomHouseBackupSent = new();
        public static TypedEvent<CustomHouseRestoreSentArgs> CustomHouseRestoreSent = new();
        public static TypedEvent<CustomHouseCommitSentArgs> CustomHouseCommitSent = new();
        public static TypedEvent<CustomHouseBuildingExitSentArgs> CustomHouseBuildingExitSent = new();
        public static TypedEvent<CustomHouseGoToFloorSentArgs> CustomHouseGoToFloorSent = new();
        public static TypedEvent<CustomHouseSyncSentArgs> CustomHouseSyncSent = new();
        public static TypedEvent<CustomHouseClearSentArgs> CustomHouseClearSent = new();
        public static TypedEvent<CustomHouseRevertSentArgs> CustomHouseRevertSent = new();
        public static TypedEvent<CustomHouseResponseSentArgs> CustomHouseResponseSent = new();
        public static TypedEvent<CustomHouseAddItemSentArgs> CustomHouseAddItemSent = new();
        public static TypedEvent<CustomHouseDeleteItemSentArgs> CustomHouseDeleteItemSent = new();
        public static TypedEvent<CustomHouseAddRoofSentArgs> CustomHouseAddRoofSent = new();
        public static TypedEvent<CustomHouseDeleteRoofSentArgs> CustomHouseDeleteRoofSent = new();
        public static TypedEvent<CustomHouseAddStairSentArgs> CustomHouseAddStairSent = new();

        public static void RaiseCustomHouseDataRequestSent(in CustomHouseDataRequestSentArgs e) => CustomHouseDataRequestSent.Raise(e);
        public static void RaiseCustomHouseBackupSent(in CustomHouseBackupSentArgs e) => CustomHouseBackupSent.Raise(e);
        public static void RaiseCustomHouseRestoreSent(in CustomHouseRestoreSentArgs e) => CustomHouseRestoreSent.Raise(e);
        public static void RaiseCustomHouseCommitSent(in CustomHouseCommitSentArgs e) => CustomHouseCommitSent.Raise(e);
        public static void RaiseCustomHouseBuildingExitSent(in CustomHouseBuildingExitSentArgs e) => CustomHouseBuildingExitSent.Raise(e);
        public static void RaiseCustomHouseGoToFloorSent(in CustomHouseGoToFloorSentArgs e) => CustomHouseGoToFloorSent.Raise(e);
        public static void RaiseCustomHouseSyncSent(in CustomHouseSyncSentArgs e) => CustomHouseSyncSent.Raise(e);
        public static void RaiseCustomHouseClearSent(in CustomHouseClearSentArgs e) => CustomHouseClearSent.Raise(e);
        public static void RaiseCustomHouseRevertSent(in CustomHouseRevertSentArgs e) => CustomHouseRevertSent.Raise(e);
        public static void RaiseCustomHouseResponseSent(in CustomHouseResponseSentArgs e) => CustomHouseResponseSent.Raise(e);
        public static void RaiseCustomHouseAddItemSent(in CustomHouseAddItemSentArgs e) => CustomHouseAddItemSent.Raise(e);
        public static void RaiseCustomHouseDeleteItemSent(in CustomHouseDeleteItemSentArgs e) => CustomHouseDeleteItemSent.Raise(e);
        public static void RaiseCustomHouseAddRoofSent(in CustomHouseAddRoofSentArgs e) => CustomHouseAddRoofSent.Raise(e);
        public static void RaiseCustomHouseDeleteRoofSent(in CustomHouseDeleteRoofSentArgs e) => CustomHouseDeleteRoofSent.Raise(e);
        public static void RaiseCustomHouseAddStairSent(in CustomHouseAddStairSentArgs e) => CustomHouseAddStairSent.Raise(e);

        // ---- World / Window ----
        public static TypedEvent<GameWindowSizeSentArgs> GameWindowSizeSent = new();
        public static TypedEvent<ClientViewRangeSentArgs> ClientViewRangeSent = new();
        public static TypedEvent<OpenUoStoreSentArgs> OpenUoStoreSent = new();
        public static TypedEvent<ShowPublicHouseContentSentArgs> ShowPublicHouseContentSent = new();
        public static TypedEvent<DeathScreenSentArgs> DeathScreenSent = new();

        public static void RaiseGameWindowSizeSent(in GameWindowSizeSentArgs e) => GameWindowSizeSent.Raise(e);
        public static void RaiseClientViewRangeSent(in ClientViewRangeSentArgs e) => ClientViewRangeSent.Raise(e);
        public static void RaiseOpenUoStoreSent(in OpenUoStoreSentArgs e) => OpenUoStoreSent.Raise(e);
        public static void RaiseShowPublicHouseContentSent(in ShowPublicHouseContentSentArgs e) => ShowPublicHouseContentSent.Raise(e);
        public static void RaiseDeathScreenSent(in DeathScreenSentArgs e) => DeathScreenSent.Raise(e);

        // ---- Meta / Admin ----
        public static TypedEvent<QueryGuildPositionSentArgs> QueryGuildPositionSent = new();
        public static TypedEvent<QueryPartyPositionSentArgs> QueryPartyPositionSent = new();
        public static TypedEvent<CloseStatusBarGumpSentArgs> CloseStatusBarGumpSent = new();
        public static TypedEvent<GuildMenuRequestSentArgs> GuildMenuRequestSent = new();
        public static TypedEvent<QuestMenuRequestSentArgs> QuestMenuRequestSent = new();
        public static TypedEvent<EquipLastWeaponSentArgs> EquipLastWeaponSent = new();

        public static void RaiseQueryGuildPositionSent(in QueryGuildPositionSentArgs e) => QueryGuildPositionSent.Raise(e);
        public static void RaiseQueryPartyPositionSent(in QueryPartyPositionSentArgs e) => QueryPartyPositionSent.Raise(e);
        public static void RaiseCloseStatusBarGumpSent(in CloseStatusBarGumpSentArgs e) => CloseStatusBarGumpSent.Raise(e);
        public static void RaiseGuildMenuRequestSent(in GuildMenuRequestSentArgs e) => GuildMenuRequestSent.Raise(e);
        public static void RaiseQuestMenuRequestSent(in QuestMenuRequestSentArgs e) => QuestMenuRequestSent.Raise(e);
        public static void RaiseEquipLastWeaponSent(in EquipLastWeaponSentArgs e) => EquipLastWeaponSent.Raise(e);

        // ---- UOLive ----
        public static TypedEvent<UoLiveHashResponseSentArgs> UoLiveHashResponseSent = new();

        public static void RaiseUoLiveHashResponseSent(in UoLiveHashResponseSentArgs e) => UoLiveHashResponseSent.Raise(e);

        // ---- Plugins ----
        public static TypedEvent<ToPluginsAllSpellsSentArgs> ToPluginsAllSpellsSent = new();
        public static TypedEvent<ToPluginsAllSkillsSentArgs> ToPluginsAllSkillsSent = new();

        public static void RaiseToPluginsAllSpellsSent(in ToPluginsAllSpellsSentArgs e) => ToPluginsAllSpellsSent.Raise(e);
        public static void RaiseToPluginsAllSkillsSent(in ToPluginsAllSkillsSentArgs e) => ToPluginsAllSkillsSent.Raise(e);

        // Per-category event blocks land here as the outgoing migration phases
        // wire packets in. See OUTGOING-PACKETS-MIGRATION for the rollout plan.


        /// <summary>Clears every subscription. Intended for tests only.</summary>
        public static void ClearAll()
        {
            PingSent.Clear();

            AckTalkSent.Clear();
            SeedSent.Clear();
            SeedOldSent.Clear();
            FirstLoginSent.Clear();
            SecondLoginSent.Clear();
            SelectServerSent.Clear();
            SelectCharacterSent.Clear();
            CreateCharacterSent.Clear();
            DeleteCharacterSent.Clear();
            LogoutNotificationSent.Clear();
            ClientVersionSent.Clear();
            LanguageSent.Clear();
            ClientTypeSent.Clear();

            WalkRequestSent.Clear();
            ResyncSent.Clear();
            MultiBoatMoveRequestSent.Clear();

            AttackRequestSent.Clear();
            TargetObjectSent.Clear();
            TargetXyzSent.Clear();
            TargetCancelSent.Clear();
            TargetSelectedObjectSent.Clear();
            ChangeWarModeSent.Clear();
            UseCombatAbilitySent.Clear();
            ClickQuestArrowSent.Clear();

            DoubleClickSent.Clear();
            ClickRequestSent.Clear();
            UseSkillSent.Clear();
            NameRequestSent.Clear();
            OpenDoorSent.Clear();

            // Items / Equip
            PickUpRequestSent.Clear();
            DropRequestSent.Clear();
            DropRequestOldSent.Clear();
            EquipRequestSent.Clear();
            EquipMacroKrSent.Clear();
            UnequipMacroKrSent.Clear();
            DyeDataResponseSent.Clear();
            RenameRequestSent.Clear();

            // Spells / Abilities
            CastSpellSent.Clear();
            CastSpellFromBookSent.Clear();
            OpenSpellBookSent.Clear();
            StunRequestSent.Clear();
            DisarmRequestSent.Clear();
            ToggleGargoyleFlyingSent.Clear();
            InvokeVirtueRequestSent.Clear();
            ChangeRaceRequestSent.Clear();

            // Status / Skills
            StatusRequestSent.Clear();
            SkillsRequestSent.Clear();
            SkillsStatusRequestSent.Clear();
            StatLockStateRequestSent.Clear();
            SkillStatusChangeRequestSent.Clear();
            HelpRequestSent.Clear();

            // OPL / Cliloc
            MegaClilocRequestOldSent.Clear();
            MegaClilocRequestSent.Clear();

            // Profile
            ProfileRequestSent.Clear();
            ProfileUpdateSent.Clear();
            TipRequestSent.Clear();
            EmoteActionSent.Clear();
            AsciiSpeechRequestSent.Clear();
            UnicodeSpeechRequestSent.Clear();
            ChatJoinCommandSent.Clear();
            ChatCreateChannelCommandSent.Clear();
            ChatLeaveChannelCommandSent.Clear();
            ChatMessageCommandSent.Clear();
            OpenChatSent.Clear();
            MapMessageSent.Clear();
            RazorAckSent.Clear();

            PartyInviteRequestSent.Clear();
            PartyRemoveRequestSent.Clear();
            PartyChangeLootTypeRequestSent.Clear();
            PartyAcceptSent.Clear();
            PartyDeclineSent.Clear();
            PartyMessageSent.Clear();

            TradeResponseSent.Clear();
            TradeUpdateGoldSent.Clear();
            BuyRequestSent.Clear();
            SellRequestSent.Clear();

            GumpResponseSent.Clear();
            VirtueGumpResponseSent.Clear();
            MenuResponseSent.Clear();
            GrayMenuResponseSent.Clear();
            RequestPopupMenuSent.Clear();
            PopupMenuSelectionSent.Clear();
            TextEntryDialogResponseSent.Clear();

            AsciiPromptResponseSent.Clear();
            UnicodePromptResponseSent.Clear();
            BookHeaderChangedOldSent.Clear();
            BookHeaderChangedSent.Clear();
            BookPageDataSent.Clear();
            BookPageDataRequestSent.Clear();

            BulletinBoardRequestMessageSent.Clear();
            BulletinBoardRequestMessageSummarySent.Clear();
            BulletinBoardPostMessageSent.Clear();
            BulletinBoardRemoveMessageSent.Clear();

            CustomHouseDataRequestSent.Clear();
            CustomHouseBackupSent.Clear();
            CustomHouseRestoreSent.Clear();
            CustomHouseCommitSent.Clear();
            CustomHouseBuildingExitSent.Clear();
            CustomHouseGoToFloorSent.Clear();
            CustomHouseSyncSent.Clear();
            CustomHouseClearSent.Clear();
            CustomHouseRevertSent.Clear();
            CustomHouseResponseSent.Clear();
            CustomHouseAddItemSent.Clear();
            CustomHouseDeleteItemSent.Clear();
            CustomHouseAddRoofSent.Clear();
            CustomHouseDeleteRoofSent.Clear();
            CustomHouseAddStairSent.Clear();

            GameWindowSizeSent.Clear();
            ClientViewRangeSent.Clear();
            OpenUoStoreSent.Clear();
            ShowPublicHouseContentSent.Clear();
            DeathScreenSent.Clear();

            QueryGuildPositionSent.Clear();
            QueryPartyPositionSent.Clear();
            CloseStatusBarGumpSent.Clear();
            GuildMenuRequestSent.Clear();
            QuestMenuRequestSent.Clear();
            EquipLastWeaponSent.Clear();

            UoLiveHashResponseSent.Clear();

            ToPluginsAllSpellsSent.Clear();
            ToPluginsAllSkillsSent.Clear();
        }
    }
}
