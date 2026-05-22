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
        public static event Action<PingSentArgs> PingSent;

        public static void RaisePingSent(in PingSentArgs e) => Invoke(PingSent, e);

        // ---- Login ----
        public static event Action<AckTalkSentArgs> AckTalkSent;
        public static event Action<SeedSentArgs> SeedSent;
        public static event Action<SeedOldSentArgs> SeedOldSent;
        public static event Action<FirstLoginSentArgs> FirstLoginSent;
        public static event Action<SecondLoginSentArgs> SecondLoginSent;
        public static event Action<SelectServerSentArgs> SelectServerSent;
        public static event Action<SelectCharacterSentArgs> SelectCharacterSent;
        public static event Action<CreateCharacterSentArgs> CreateCharacterSent;
        public static event Action<DeleteCharacterSentArgs> DeleteCharacterSent;
        public static event Action<LogoutNotificationSentArgs> LogoutNotificationSent;
        public static event Action<ClientVersionSentArgs> ClientVersionSent;
        public static event Action<LanguageSentArgs> LanguageSent;
        public static event Action<ClientTypeSentArgs> ClientTypeSent;

        public static void RaiseAckTalkSent(in AckTalkSentArgs e) => Invoke(AckTalkSent, e);
        public static void RaiseSeedSent(in SeedSentArgs e) => Invoke(SeedSent, e);
        public static void RaiseSeedOldSent(in SeedOldSentArgs e) => Invoke(SeedOldSent, e);
        public static void RaiseFirstLoginSent(in FirstLoginSentArgs e) => Invoke(FirstLoginSent, e);
        public static void RaiseSecondLoginSent(in SecondLoginSentArgs e) => Invoke(SecondLoginSent, e);
        public static void RaiseSelectServerSent(in SelectServerSentArgs e) => Invoke(SelectServerSent, e);
        public static void RaiseSelectCharacterSent(in SelectCharacterSentArgs e) => Invoke(SelectCharacterSent, e);
        public static void RaiseCreateCharacterSent(in CreateCharacterSentArgs e) => Invoke(CreateCharacterSent, e);
        public static void RaiseDeleteCharacterSent(in DeleteCharacterSentArgs e) => Invoke(DeleteCharacterSent, e);
        public static void RaiseLogoutNotificationSent(in LogoutNotificationSentArgs e) => Invoke(LogoutNotificationSent, e);
        public static void RaiseClientVersionSent(in ClientVersionSentArgs e) => Invoke(ClientVersionSent, e);
        public static void RaiseLanguageSent(in LanguageSentArgs e) => Invoke(LanguageSent, e);
        public static void RaiseClientTypeSent(in ClientTypeSentArgs e) => Invoke(ClientTypeSent, e);

        // ---- Movement ----
        public static event Action<WalkRequestSentArgs> WalkRequestSent;
        public static event Action<ResyncSentArgs> ResyncSent;
        public static event Action<MultiBoatMoveRequestSentArgs> MultiBoatMoveRequestSent;

        public static void RaiseWalkRequestSent(in WalkRequestSentArgs e) => Invoke(WalkRequestSent, e);
        public static void RaiseResyncSent(in ResyncSentArgs e) => Invoke(ResyncSent, e);
        public static void RaiseMultiBoatMoveRequestSent(in MultiBoatMoveRequestSentArgs e) => Invoke(MultiBoatMoveRequestSent, e);

        // ---- Combat ----
        public static event Action<AttackRequestSentArgs> AttackRequestSent;
        public static event Action<TargetObjectSentArgs> TargetObjectSent;
        public static event Action<TargetXyzSentArgs> TargetXyzSent;
        public static event Action<TargetCancelSentArgs> TargetCancelSent;
        public static event Action<TargetSelectedObjectSentArgs> TargetSelectedObjectSent;
        public static event Action<ChangeWarModeSentArgs> ChangeWarModeSent;
        public static event Action<UseCombatAbilitySentArgs> UseCombatAbilitySent;
        public static event Action<ClickQuestArrowSentArgs> ClickQuestArrowSent;

        public static void RaiseAttackRequestSent(in AttackRequestSentArgs e) => Invoke(AttackRequestSent, e);
        public static void RaiseTargetObjectSent(in TargetObjectSentArgs e) => Invoke(TargetObjectSent, e);
        public static void RaiseTargetXyzSent(in TargetXyzSentArgs e) => Invoke(TargetXyzSent, e);
        public static void RaiseTargetCancelSent(in TargetCancelSentArgs e) => Invoke(TargetCancelSent, e);
        public static void RaiseTargetSelectedObjectSent(in TargetSelectedObjectSentArgs e) => Invoke(TargetSelectedObjectSent, e);
        public static void RaiseChangeWarModeSent(in ChangeWarModeSentArgs e) => Invoke(ChangeWarModeSent, e);
        public static void RaiseUseCombatAbilitySent(in UseCombatAbilitySentArgs e) => Invoke(UseCombatAbilitySent, e);
        public static void RaiseClickQuestArrowSent(in ClickQuestArrowSentArgs e) => Invoke(ClickQuestArrowSent, e);

        // ---- Interaction ----
        public static event Action<DoubleClickSentArgs> DoubleClickSent;
        public static event Action<ClickRequestSentArgs> ClickRequestSent;
        public static event Action<UseSkillSentArgs> UseSkillSent;
        public static event Action<NameRequestSentArgs> NameRequestSent;
        public static event Action<OpenDoorSentArgs> OpenDoorSent;

        public static void RaiseDoubleClickSent(in DoubleClickSentArgs e) => Invoke(DoubleClickSent, e);
        public static void RaiseClickRequestSent(in ClickRequestSentArgs e) => Invoke(ClickRequestSent, e);
        public static void RaiseUseSkillSent(in UseSkillSentArgs e) => Invoke(UseSkillSent, e);
        public static void RaiseNameRequestSent(in NameRequestSentArgs e) => Invoke(NameRequestSent, e);
        public static void RaiseOpenDoorSent(in OpenDoorSentArgs e) => Invoke(OpenDoorSent, e);

        // ---- Items / Equip ----
        public static event Action<PickUpRequestSentArgs> PickUpRequestSent;
        public static event Action<DropRequestSentArgs> DropRequestSent;
        public static event Action<DropRequestOldSentArgs> DropRequestOldSent;
        public static event Action<EquipRequestSentArgs> EquipRequestSent;
        public static event Action<EquipMacroKrSentArgs> EquipMacroKrSent;
        public static event Action<UnequipMacroKrSentArgs> UnequipMacroKrSent;
        public static event Action<DyeDataResponseSentArgs> DyeDataResponseSent;
        public static event Action<RenameRequestSentArgs> RenameRequestSent;

        public static void RaisePickUpRequestSent(in PickUpRequestSentArgs e) => Invoke(PickUpRequestSent, e);
        public static void RaiseDropRequestSent(in DropRequestSentArgs e) => Invoke(DropRequestSent, e);
        public static void RaiseDropRequestOldSent(in DropRequestOldSentArgs e) => Invoke(DropRequestOldSent, e);
        public static void RaiseEquipRequestSent(in EquipRequestSentArgs e) => Invoke(EquipRequestSent, e);
        public static void RaiseEquipMacroKrSent(in EquipMacroKrSentArgs e) => Invoke(EquipMacroKrSent, e);
        public static void RaiseUnequipMacroKrSent(in UnequipMacroKrSentArgs e) => Invoke(UnequipMacroKrSent, e);
        public static void RaiseDyeDataResponseSent(in DyeDataResponseSentArgs e) => Invoke(DyeDataResponseSent, e);
        public static void RaiseRenameRequestSent(in RenameRequestSentArgs e) => Invoke(RenameRequestSent, e);

        // ---- Spells / Abilities ----
        public static event Action<CastSpellSentArgs> CastSpellSent;
        public static event Action<CastSpellFromBookSentArgs> CastSpellFromBookSent;
        public static event Action<OpenSpellBookSentArgs> OpenSpellBookSent;
        public static event Action<StunRequestSentArgs> StunRequestSent;
        public static event Action<DisarmRequestSentArgs> DisarmRequestSent;
        public static event Action<ToggleGargoyleFlyingSentArgs> ToggleGargoyleFlyingSent;
        public static event Action<InvokeVirtueRequestSentArgs> InvokeVirtueRequestSent;
        public static event Action<ChangeRaceRequestSentArgs> ChangeRaceRequestSent;

        public static void RaiseCastSpellSent(in CastSpellSentArgs e) => Invoke(CastSpellSent, e);
        public static void RaiseCastSpellFromBookSent(in CastSpellFromBookSentArgs e) => Invoke(CastSpellFromBookSent, e);
        public static void RaiseOpenSpellBookSent(in OpenSpellBookSentArgs e) => Invoke(OpenSpellBookSent, e);
        public static void RaiseStunRequestSent(in StunRequestSentArgs e) => Invoke(StunRequestSent, e);
        public static void RaiseDisarmRequestSent(in DisarmRequestSentArgs e) => Invoke(DisarmRequestSent, e);
        public static void RaiseToggleGargoyleFlyingSent(in ToggleGargoyleFlyingSentArgs e) => Invoke(ToggleGargoyleFlyingSent, e);
        public static void RaiseInvokeVirtueRequestSent(in InvokeVirtueRequestSentArgs e) => Invoke(InvokeVirtueRequestSent, e);
        public static void RaiseChangeRaceRequestSent(in ChangeRaceRequestSentArgs e) => Invoke(ChangeRaceRequestSent, e);

        // ---- Status / Skills ----
        public static event Action<StatusRequestSentArgs> StatusRequestSent;
        public static event Action<SkillsRequestSentArgs> SkillsRequestSent;
        public static event Action<SkillsStatusRequestSentArgs> SkillsStatusRequestSent;
        public static event Action<StatLockStateRequestSentArgs> StatLockStateRequestSent;
        public static event Action<SkillStatusChangeRequestSentArgs> SkillStatusChangeRequestSent;
        public static event Action<HelpRequestSentArgs> HelpRequestSent;

        public static void RaiseStatusRequestSent(in StatusRequestSentArgs e) => Invoke(StatusRequestSent, e);
        public static void RaiseSkillsRequestSent(in SkillsRequestSentArgs e) => Invoke(SkillsRequestSent, e);
        public static void RaiseSkillsStatusRequestSent(in SkillsStatusRequestSentArgs e) => Invoke(SkillsStatusRequestSent, e);
        public static void RaiseStatLockStateRequestSent(in StatLockStateRequestSentArgs e) => Invoke(StatLockStateRequestSent, e);
        public static void RaiseSkillStatusChangeRequestSent(in SkillStatusChangeRequestSentArgs e) => Invoke(SkillStatusChangeRequestSent, e);
        public static void RaiseHelpRequestSent(in HelpRequestSentArgs e) => Invoke(HelpRequestSent, e);

        // ---- OPL / Cliloc ----
        public static event Action<MegaClilocRequestOldSentArgs> MegaClilocRequestOldSent;
        public static event Action<MegaClilocRequestSentArgs> MegaClilocRequestSent;

        public static void RaiseMegaClilocRequestOldSent(in MegaClilocRequestOldSentArgs e) => Invoke(MegaClilocRequestOldSent, e);
        public static void RaiseMegaClilocRequestSent(in MegaClilocRequestSentArgs e) => Invoke(MegaClilocRequestSent, e);

        // ---- Profile ----
        public static event Action<ProfileRequestSentArgs> ProfileRequestSent;
        public static event Action<ProfileUpdateSentArgs> ProfileUpdateSent;
        public static event Action<TipRequestSentArgs> TipRequestSent;

        public static void RaiseProfileRequestSent(in ProfileRequestSentArgs e) => Invoke(ProfileRequestSent, e);
        public static void RaiseProfileUpdateSent(in ProfileUpdateSentArgs e) => Invoke(ProfileUpdateSent, e);
        public static void RaiseTipRequestSent(in TipRequestSentArgs e) => Invoke(TipRequestSent, e);
        // ---- Chat / Social ----
        public static event Action<EmoteActionSentArgs> EmoteActionSent;
        public static event Action<AsciiSpeechRequestSentArgs> AsciiSpeechRequestSent;
        public static event Action<UnicodeSpeechRequestSentArgs> UnicodeSpeechRequestSent;
        public static event Action<ChatJoinCommandSentArgs> ChatJoinCommandSent;
        public static event Action<ChatCreateChannelCommandSentArgs> ChatCreateChannelCommandSent;
        public static event Action<ChatLeaveChannelCommandSentArgs> ChatLeaveChannelCommandSent;
        public static event Action<ChatMessageCommandSentArgs> ChatMessageCommandSent;
        public static event Action<OpenChatSentArgs> OpenChatSent;
        public static event Action<MapMessageSentArgs> MapMessageSent;
        public static event Action<RazorAckSentArgs> RazorAckSent;

        public static void RaiseEmoteActionSent(in EmoteActionSentArgs e) => Invoke(EmoteActionSent, e);
        public static void RaiseAsciiSpeechRequestSent(in AsciiSpeechRequestSentArgs e) => Invoke(AsciiSpeechRequestSent, e);
        public static void RaiseUnicodeSpeechRequestSent(in UnicodeSpeechRequestSentArgs e) => Invoke(UnicodeSpeechRequestSent, e);
        public static void RaiseChatJoinCommandSent(in ChatJoinCommandSentArgs e) => Invoke(ChatJoinCommandSent, e);
        public static void RaiseChatCreateChannelCommandSent(in ChatCreateChannelCommandSentArgs e) => Invoke(ChatCreateChannelCommandSent, e);
        public static void RaiseChatLeaveChannelCommandSent(in ChatLeaveChannelCommandSentArgs e) => Invoke(ChatLeaveChannelCommandSent, e);
        public static void RaiseChatMessageCommandSent(in ChatMessageCommandSentArgs e) => Invoke(ChatMessageCommandSent, e);
        public static void RaiseOpenChatSent(in OpenChatSentArgs e) => Invoke(OpenChatSent, e);
        public static void RaiseMapMessageSent(in MapMessageSentArgs e) => Invoke(MapMessageSent, e);
        public static void RaiseRazorAckSent(in RazorAckSentArgs e) => Invoke(RazorAckSent, e);

        // ---- Party ----
        public static event Action<PartyInviteRequestSentArgs> PartyInviteRequestSent;
        public static event Action<PartyRemoveRequestSentArgs> PartyRemoveRequestSent;
        public static event Action<PartyChangeLootTypeRequestSentArgs> PartyChangeLootTypeRequestSent;
        public static event Action<PartyAcceptSentArgs> PartyAcceptSent;
        public static event Action<PartyDeclineSentArgs> PartyDeclineSent;
        public static event Action<PartyMessageSentArgs> PartyMessageSent;

        public static void RaisePartyInviteRequestSent(in PartyInviteRequestSentArgs e) => Invoke(PartyInviteRequestSent, e);
        public static void RaisePartyRemoveRequestSent(in PartyRemoveRequestSentArgs e) => Invoke(PartyRemoveRequestSent, e);
        public static void RaisePartyChangeLootTypeRequestSent(in PartyChangeLootTypeRequestSentArgs e) => Invoke(PartyChangeLootTypeRequestSent, e);
        public static void RaisePartyAcceptSent(in PartyAcceptSentArgs e) => Invoke(PartyAcceptSent, e);
        public static void RaisePartyDeclineSent(in PartyDeclineSentArgs e) => Invoke(PartyDeclineSent, e);
        public static void RaisePartyMessageSent(in PartyMessageSentArgs e) => Invoke(PartyMessageSent, e);

        // ---- Trade / Vendor ----
        public static event Action<TradeResponseSentArgs> TradeResponseSent;
        public static event Action<TradeUpdateGoldSentArgs> TradeUpdateGoldSent;
        public static event Action<BuyRequestSentArgs> BuyRequestSent;
        public static event Action<SellRequestSentArgs> SellRequestSent;

        public static void RaiseTradeResponseSent(in TradeResponseSentArgs e) => Invoke(TradeResponseSent, e);
        public static void RaiseTradeUpdateGoldSent(in TradeUpdateGoldSentArgs e) => Invoke(TradeUpdateGoldSent, e);
        public static void RaiseBuyRequestSent(in BuyRequestSentArgs e) => Invoke(BuyRequestSent, e);
        public static void RaiseSellRequestSent(in SellRequestSentArgs e) => Invoke(SellRequestSent, e);

        // ---- Gumps / Menus ----
        public static event Action<GumpResponseSentArgs> GumpResponseSent;
        public static event Action<VirtueGumpResponseSentArgs> VirtueGumpResponseSent;
        public static event Action<MenuResponseSentArgs> MenuResponseSent;
        public static event Action<GrayMenuResponseSentArgs> GrayMenuResponseSent;
        public static event Action<RequestPopupMenuSentArgs> RequestPopupMenuSent;
        public static event Action<PopupMenuSelectionSentArgs> PopupMenuSelectionSent;
        public static event Action<TextEntryDialogResponseSentArgs> TextEntryDialogResponseSent;

        public static void RaiseGumpResponseSent(in GumpResponseSentArgs e) => Invoke(GumpResponseSent, e);
        public static void RaiseVirtueGumpResponseSent(in VirtueGumpResponseSentArgs e) => Invoke(VirtueGumpResponseSent, e);
        public static void RaiseMenuResponseSent(in MenuResponseSentArgs e) => Invoke(MenuResponseSent, e);
        public static void RaiseGrayMenuResponseSent(in GrayMenuResponseSentArgs e) => Invoke(GrayMenuResponseSent, e);
        public static void RaiseRequestPopupMenuSent(in RequestPopupMenuSentArgs e) => Invoke(RequestPopupMenuSent, e);
        public static void RaisePopupMenuSelectionSent(in PopupMenuSelectionSentArgs e) => Invoke(PopupMenuSelectionSent, e);
        public static void RaiseTextEntryDialogResponseSent(in TextEntryDialogResponseSentArgs e) => Invoke(TextEntryDialogResponseSent, e);

        // ---- Prompts ----
        public static event Action<AsciiPromptResponseSentArgs> AsciiPromptResponseSent;
        public static event Action<UnicodePromptResponseSentArgs> UnicodePromptResponseSent;

        public static void RaiseAsciiPromptResponseSent(in AsciiPromptResponseSentArgs e) => Invoke(AsciiPromptResponseSent, e);
        public static void RaiseUnicodePromptResponseSent(in UnicodePromptResponseSentArgs e) => Invoke(UnicodePromptResponseSent, e);
        // ---- Books ----
        public static event Action<BookHeaderChangedOldSentArgs> BookHeaderChangedOldSent;
        public static event Action<BookHeaderChangedSentArgs> BookHeaderChangedSent;
        public static event Action<BookPageDataSentArgs> BookPageDataSent;
        public static event Action<BookPageDataRequestSentArgs> BookPageDataRequestSent;

        public static void RaiseBookHeaderChangedOldSent(in BookHeaderChangedOldSentArgs e) => Invoke(BookHeaderChangedOldSent, e);
        public static void RaiseBookHeaderChangedSent(in BookHeaderChangedSentArgs e) => Invoke(BookHeaderChangedSent, e);
        public static void RaiseBookPageDataSent(in BookPageDataSentArgs e) => Invoke(BookPageDataSent, e);
        public static void RaiseBookPageDataRequestSent(in BookPageDataRequestSentArgs e) => Invoke(BookPageDataRequestSent, e);

        // ---- Bulletin Board ----
        public static event Action<BulletinBoardRequestMessageSentArgs> BulletinBoardRequestMessageSent;
        public static event Action<BulletinBoardRequestMessageSummarySentArgs> BulletinBoardRequestMessageSummarySent;
        public static event Action<BulletinBoardPostMessageSentArgs> BulletinBoardPostMessageSent;
        public static event Action<BulletinBoardRemoveMessageSentArgs> BulletinBoardRemoveMessageSent;

        public static void RaiseBulletinBoardRequestMessageSent(in BulletinBoardRequestMessageSentArgs e) => Invoke(BulletinBoardRequestMessageSent, e);
        public static void RaiseBulletinBoardRequestMessageSummarySent(in BulletinBoardRequestMessageSummarySentArgs e) => Invoke(BulletinBoardRequestMessageSummarySent, e);
        public static void RaiseBulletinBoardPostMessageSent(in BulletinBoardPostMessageSentArgs e) => Invoke(BulletinBoardPostMessageSent, e);
        public static void RaiseBulletinBoardRemoveMessageSent(in BulletinBoardRemoveMessageSentArgs e) => Invoke(BulletinBoardRemoveMessageSent, e);

        // ---- House Customization ----
        public static event Action<CustomHouseDataRequestSentArgs> CustomHouseDataRequestSent;
        public static event Action<CustomHouseBackupSentArgs> CustomHouseBackupSent;
        public static event Action<CustomHouseRestoreSentArgs> CustomHouseRestoreSent;
        public static event Action<CustomHouseCommitSentArgs> CustomHouseCommitSent;
        public static event Action<CustomHouseBuildingExitSentArgs> CustomHouseBuildingExitSent;
        public static event Action<CustomHouseGoToFloorSentArgs> CustomHouseGoToFloorSent;
        public static event Action<CustomHouseSyncSentArgs> CustomHouseSyncSent;
        public static event Action<CustomHouseClearSentArgs> CustomHouseClearSent;
        public static event Action<CustomHouseRevertSentArgs> CustomHouseRevertSent;
        public static event Action<CustomHouseResponseSentArgs> CustomHouseResponseSent;
        public static event Action<CustomHouseAddItemSentArgs> CustomHouseAddItemSent;
        public static event Action<CustomHouseDeleteItemSentArgs> CustomHouseDeleteItemSent;
        public static event Action<CustomHouseAddRoofSentArgs> CustomHouseAddRoofSent;
        public static event Action<CustomHouseDeleteRoofSentArgs> CustomHouseDeleteRoofSent;
        public static event Action<CustomHouseAddStairSentArgs> CustomHouseAddStairSent;

        public static void RaiseCustomHouseDataRequestSent(in CustomHouseDataRequestSentArgs e) => Invoke(CustomHouseDataRequestSent, e);
        public static void RaiseCustomHouseBackupSent(in CustomHouseBackupSentArgs e) => Invoke(CustomHouseBackupSent, e);
        public static void RaiseCustomHouseRestoreSent(in CustomHouseRestoreSentArgs e) => Invoke(CustomHouseRestoreSent, e);
        public static void RaiseCustomHouseCommitSent(in CustomHouseCommitSentArgs e) => Invoke(CustomHouseCommitSent, e);
        public static void RaiseCustomHouseBuildingExitSent(in CustomHouseBuildingExitSentArgs e) => Invoke(CustomHouseBuildingExitSent, e);
        public static void RaiseCustomHouseGoToFloorSent(in CustomHouseGoToFloorSentArgs e) => Invoke(CustomHouseGoToFloorSent, e);
        public static void RaiseCustomHouseSyncSent(in CustomHouseSyncSentArgs e) => Invoke(CustomHouseSyncSent, e);
        public static void RaiseCustomHouseClearSent(in CustomHouseClearSentArgs e) => Invoke(CustomHouseClearSent, e);
        public static void RaiseCustomHouseRevertSent(in CustomHouseRevertSentArgs e) => Invoke(CustomHouseRevertSent, e);
        public static void RaiseCustomHouseResponseSent(in CustomHouseResponseSentArgs e) => Invoke(CustomHouseResponseSent, e);
        public static void RaiseCustomHouseAddItemSent(in CustomHouseAddItemSentArgs e) => Invoke(CustomHouseAddItemSent, e);
        public static void RaiseCustomHouseDeleteItemSent(in CustomHouseDeleteItemSentArgs e) => Invoke(CustomHouseDeleteItemSent, e);
        public static void RaiseCustomHouseAddRoofSent(in CustomHouseAddRoofSentArgs e) => Invoke(CustomHouseAddRoofSent, e);
        public static void RaiseCustomHouseDeleteRoofSent(in CustomHouseDeleteRoofSentArgs e) => Invoke(CustomHouseDeleteRoofSent, e);
        public static void RaiseCustomHouseAddStairSent(in CustomHouseAddStairSentArgs e) => Invoke(CustomHouseAddStairSent, e);

        // ---- World / Window ----
        public static event Action<GameWindowSizeSentArgs> GameWindowSizeSent;
        public static event Action<ClientViewRangeSentArgs> ClientViewRangeSent;
        public static event Action<OpenUoStoreSentArgs> OpenUoStoreSent;
        public static event Action<ShowPublicHouseContentSentArgs> ShowPublicHouseContentSent;
        public static event Action<DeathScreenSentArgs> DeathScreenSent;

        public static void RaiseGameWindowSizeSent(in GameWindowSizeSentArgs e) => Invoke(GameWindowSizeSent, e);
        public static void RaiseClientViewRangeSent(in ClientViewRangeSentArgs e) => Invoke(ClientViewRangeSent, e);
        public static void RaiseOpenUoStoreSent(in OpenUoStoreSentArgs e) => Invoke(OpenUoStoreSent, e);
        public static void RaiseShowPublicHouseContentSent(in ShowPublicHouseContentSentArgs e) => Invoke(ShowPublicHouseContentSent, e);
        public static void RaiseDeathScreenSent(in DeathScreenSentArgs e) => Invoke(DeathScreenSent, e);

        // ---- Meta / Admin ----
        public static event Action<QueryGuildPositionSentArgs> QueryGuildPositionSent;
        public static event Action<QueryPartyPositionSentArgs> QueryPartyPositionSent;
        public static event Action<CloseStatusBarGumpSentArgs> CloseStatusBarGumpSent;
        public static event Action<GuildMenuRequestSentArgs> GuildMenuRequestSent;
        public static event Action<QuestMenuRequestSentArgs> QuestMenuRequestSent;
        public static event Action<EquipLastWeaponSentArgs> EquipLastWeaponSent;

        public static void RaiseQueryGuildPositionSent(in QueryGuildPositionSentArgs e) => Invoke(QueryGuildPositionSent, e);
        public static void RaiseQueryPartyPositionSent(in QueryPartyPositionSentArgs e) => Invoke(QueryPartyPositionSent, e);
        public static void RaiseCloseStatusBarGumpSent(in CloseStatusBarGumpSentArgs e) => Invoke(CloseStatusBarGumpSent, e);
        public static void RaiseGuildMenuRequestSent(in GuildMenuRequestSentArgs e) => Invoke(GuildMenuRequestSent, e);
        public static void RaiseQuestMenuRequestSent(in QuestMenuRequestSentArgs e) => Invoke(QuestMenuRequestSent, e);
        public static void RaiseEquipLastWeaponSent(in EquipLastWeaponSentArgs e) => Invoke(EquipLastWeaponSent, e);

        // ---- UOLive ----
        public static event Action<UoLiveHashResponseSentArgs> UoLiveHashResponseSent;

        public static void RaiseUoLiveHashResponseSent(in UoLiveHashResponseSentArgs e) => Invoke(UoLiveHashResponseSent, e);

        // ---- Plugins ----
        public static event Action<ToPluginsAllSpellsSentArgs> ToPluginsAllSpellsSent;
        public static event Action<ToPluginsAllSkillsSentArgs> ToPluginsAllSkillsSent;

        public static void RaiseToPluginsAllSpellsSent(in ToPluginsAllSpellsSentArgs e) => Invoke(ToPluginsAllSpellsSent, e);
        public static void RaiseToPluginsAllSkillsSent(in ToPluginsAllSkillsSentArgs e) => Invoke(ToPluginsAllSkillsSent, e);

        // Per-category event blocks land here as the outgoing migration phases
        // wire packets in. See OUTGOING-PACKETS-MIGRATION for the rollout plan.

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
                    Log.Error($"OutgoingEventSink handler failed for {typeof(T).Name}: {ex}");
                }
            }
        }

        /// <summary>Clears every subscription. Intended for tests only.</summary>
        public static void ClearAll()
        {
            PingSent = null;

            AckTalkSent = null;
            SeedSent = null;
            SeedOldSent = null;
            FirstLoginSent = null;
            SecondLoginSent = null;
            SelectServerSent = null;
            SelectCharacterSent = null;
            CreateCharacterSent = null;
            DeleteCharacterSent = null;
            LogoutNotificationSent = null;
            ClientVersionSent = null;
            LanguageSent = null;
            ClientTypeSent = null;

            WalkRequestSent = null;
            ResyncSent = null;
            MultiBoatMoveRequestSent = null;

            AttackRequestSent = null;
            TargetObjectSent = null;
            TargetXyzSent = null;
            TargetCancelSent = null;
            TargetSelectedObjectSent = null;
            ChangeWarModeSent = null;
            UseCombatAbilitySent = null;
            ClickQuestArrowSent = null;

            DoubleClickSent = null;
            ClickRequestSent = null;
            UseSkillSent = null;
            NameRequestSent = null;
            OpenDoorSent = null;

            // Items / Equip
            PickUpRequestSent = null;
            DropRequestSent = null;
            DropRequestOldSent = null;
            EquipRequestSent = null;
            EquipMacroKrSent = null;
            UnequipMacroKrSent = null;
            DyeDataResponseSent = null;
            RenameRequestSent = null;

            // Spells / Abilities
            CastSpellSent = null;
            CastSpellFromBookSent = null;
            OpenSpellBookSent = null;
            StunRequestSent = null;
            DisarmRequestSent = null;
            ToggleGargoyleFlyingSent = null;
            InvokeVirtueRequestSent = null;
            ChangeRaceRequestSent = null;

            // Status / Skills
            StatusRequestSent = null;
            SkillsRequestSent = null;
            SkillsStatusRequestSent = null;
            StatLockStateRequestSent = null;
            SkillStatusChangeRequestSent = null;
            HelpRequestSent = null;

            // OPL / Cliloc
            MegaClilocRequestOldSent = null;
            MegaClilocRequestSent = null;

            // Profile
            ProfileRequestSent = null;
            ProfileUpdateSent = null;
            TipRequestSent = null;
            EmoteActionSent = null;
            AsciiSpeechRequestSent = null;
            UnicodeSpeechRequestSent = null;
            ChatJoinCommandSent = null;
            ChatCreateChannelCommandSent = null;
            ChatLeaveChannelCommandSent = null;
            ChatMessageCommandSent = null;
            OpenChatSent = null;
            MapMessageSent = null;
            RazorAckSent = null;

            PartyInviteRequestSent = null;
            PartyRemoveRequestSent = null;
            PartyChangeLootTypeRequestSent = null;
            PartyAcceptSent = null;
            PartyDeclineSent = null;
            PartyMessageSent = null;

            TradeResponseSent = null;
            TradeUpdateGoldSent = null;
            BuyRequestSent = null;
            SellRequestSent = null;

            GumpResponseSent = null;
            VirtueGumpResponseSent = null;
            MenuResponseSent = null;
            GrayMenuResponseSent = null;
            RequestPopupMenuSent = null;
            PopupMenuSelectionSent = null;
            TextEntryDialogResponseSent = null;

            AsciiPromptResponseSent = null;
            UnicodePromptResponseSent = null;
            BookHeaderChangedOldSent = null;
            BookHeaderChangedSent = null;
            BookPageDataSent = null;
            BookPageDataRequestSent = null;

            BulletinBoardRequestMessageSent = null;
            BulletinBoardRequestMessageSummarySent = null;
            BulletinBoardPostMessageSent = null;
            BulletinBoardRemoveMessageSent = null;

            CustomHouseDataRequestSent = null;
            CustomHouseBackupSent = null;
            CustomHouseRestoreSent = null;
            CustomHouseCommitSent = null;
            CustomHouseBuildingExitSent = null;
            CustomHouseGoToFloorSent = null;
            CustomHouseSyncSent = null;
            CustomHouseClearSent = null;
            CustomHouseRevertSent = null;
            CustomHouseResponseSent = null;
            CustomHouseAddItemSent = null;
            CustomHouseDeleteItemSent = null;
            CustomHouseAddRoofSent = null;
            CustomHouseDeleteRoofSent = null;
            CustomHouseAddStairSent = null;

            GameWindowSizeSent = null;
            ClientViewRangeSent = null;
            OpenUoStoreSent = null;
            ShowPublicHouseContentSent = null;
            DeathScreenSent = null;

            QueryGuildPositionSent = null;
            QueryPartyPositionSent = null;
            CloseStatusBarGumpSent = null;
            GuildMenuRequestSent = null;
            QuestMenuRequestSent = null;
            EquipLastWeaponSent = null;

            UoLiveHashResponseSent = null;

            ToPluginsAllSpellsSent = null;
            ToPluginsAllSkillsSent = null;
        }
    }
}
