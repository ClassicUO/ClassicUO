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
        }
    }
}
