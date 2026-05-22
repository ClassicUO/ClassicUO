// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Events.Outgoing;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Events.Outgoing
{
    [Collection("EventSinkSerial")]
    public class LoginMovementCombatSentTests
    {
        public LoginMovementCombatSentTests()
        {
            OutgoingEventSink.ClearAll();
        }

        // ---- Login ----

        [Fact]
        public void RaiseAckTalkSent_Should_Invoke_Subscriber()
        {
            AckTalkSentArgs? captured = null;
            OutgoingEventSink.AckTalkSent += e => captured = e;

            OutgoingEventSink.RaiseAckTalkSent(new AckTalkSentArgs());

            captured.Should().NotBeNull();
        }

        [Fact]
        public void RaiseSeedSent_Should_Invoke_Subscriber_With_Args()
        {
            SeedSentArgs? captured = null;
            OutgoingEventSink.SeedSent += e => captured = e;

            var expected = new SeedSentArgs(0xDEADBEEF, 7, 0, 89, 4);
            OutgoingEventSink.RaiseSeedSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseSeedOldSent_Should_Invoke_Subscriber_With_Args()
        {
            SeedOldSentArgs? captured = null;
            OutgoingEventSink.SeedOldSent += e => captured = e;

            var expected = new SeedOldSentArgs(0x12345678);
            OutgoingEventSink.RaiseSeedOldSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseFirstLoginSent_Should_Invoke_Subscriber_With_Args()
        {
            FirstLoginSentArgs? captured = null;
            OutgoingEventSink.FirstLoginSent += e => captured = e;

            var expected = new FirstLoginSentArgs("alice", "hunter2");
            OutgoingEventSink.RaiseFirstLoginSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseSecondLoginSent_Should_Invoke_Subscriber_With_Args()
        {
            SecondLoginSentArgs? captured = null;
            OutgoingEventSink.SecondLoginSent += e => captured = e;

            var expected = new SecondLoginSentArgs("bob", "pwd", 0xCAFEBABE);
            OutgoingEventSink.RaiseSecondLoginSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseSelectServerSent_Should_Invoke_Subscriber_With_Args()
        {
            SelectServerSentArgs? captured = null;
            OutgoingEventSink.SelectServerSent += e => captured = e;

            var expected = new SelectServerSentArgs(3);
            OutgoingEventSink.RaiseSelectServerSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseSelectCharacterSent_Should_Invoke_Subscriber_With_Args()
        {
            SelectCharacterSentArgs? captured = null;
            OutgoingEventSink.SelectCharacterSent += e => captured = e;

            var expected = new SelectCharacterSentArgs(0, "Hero", 0x7F000001);
            OutgoingEventSink.RaiseSelectCharacterSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseCreateCharacterSent_Should_Invoke_Subscriber_With_Args()
        {
            CreateCharacterSentArgs? captured = null;
            OutgoingEventSink.CreateCharacterSent += e => captured = e;

            // PlayerMobile constructor needs heavy infra; null is acceptable for arg-passthrough test.
            var expected = new CreateCharacterSentArgs(null, 1, 0x7F000001, 0, 0, 4);
            OutgoingEventSink.RaiseCreateCharacterSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseDeleteCharacterSent_Should_Invoke_Subscriber_With_Args()
        {
            DeleteCharacterSentArgs? captured = null;
            OutgoingEventSink.DeleteCharacterSent += e => captured = e;

            var expected = new DeleteCharacterSentArgs(2, 0x7F000001);
            OutgoingEventSink.RaiseDeleteCharacterSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseLogoutNotificationSent_Should_Invoke_Subscriber()
        {
            LogoutNotificationSentArgs? captured = null;
            OutgoingEventSink.LogoutNotificationSent += e => captured = e;

            OutgoingEventSink.RaiseLogoutNotificationSent(new LogoutNotificationSentArgs());

            captured.Should().NotBeNull();
        }

        [Fact]
        public void RaiseClientVersionSent_Should_Invoke_Subscriber_With_Args()
        {
            ClientVersionSentArgs? captured = null;
            OutgoingEventSink.ClientVersionSent += e => captured = e;

            var expected = new ClientVersionSentArgs("7.0.89.4");
            OutgoingEventSink.RaiseClientVersionSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseLanguageSent_Should_Invoke_Subscriber_With_Args()
        {
            LanguageSentArgs? captured = null;
            OutgoingEventSink.LanguageSent += e => captured = e;

            var expected = new LanguageSentArgs("ENU");
            OutgoingEventSink.RaiseLanguageSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseClientTypeSent_Should_Invoke_Subscriber()
        {
            ClientTypeSentArgs? captured = null;
            OutgoingEventSink.ClientTypeSent += e => captured = e;

            OutgoingEventSink.RaiseClientTypeSent(new ClientTypeSentArgs());

            captured.Should().NotBeNull();
        }

        // ---- Movement ----

        [Fact]
        public void RaiseWalkRequestSent_Should_Invoke_Subscriber_With_Args()
        {
            WalkRequestSentArgs? captured = null;
            OutgoingEventSink.WalkRequestSent += e => captured = e;

            var expected = new WalkRequestSentArgs(Direction.North, 7, true, 0x11223344);
            OutgoingEventSink.RaiseWalkRequestSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseResyncSent_Should_Invoke_Subscriber()
        {
            ResyncSentArgs? captured = null;
            OutgoingEventSink.ResyncSent += e => captured = e;

            OutgoingEventSink.RaiseResyncSent(new ResyncSentArgs());

            captured.Should().NotBeNull();
        }

        [Fact]
        public void RaiseMultiBoatMoveRequestSent_Should_Invoke_Subscriber_With_Args()
        {
            MultiBoatMoveRequestSentArgs? captured = null;
            OutgoingEventSink.MultiBoatMoveRequestSent += e => captured = e;

            var expected = new MultiBoatMoveRequestSentArgs(0x40000001u, Direction.East, 2);
            OutgoingEventSink.RaiseMultiBoatMoveRequestSent(expected);

            captured.Should().Be(expected);
        }

        // ---- Combat ----

        [Fact]
        public void RaiseAttackRequestSent_Should_Invoke_Subscriber_With_Args()
        {
            AttackRequestSentArgs? captured = null;
            OutgoingEventSink.AttackRequestSent += e => captured = e;

            var expected = new AttackRequestSentArgs(0x12345678);
            OutgoingEventSink.RaiseAttackRequestSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseTargetObjectSent_Should_Invoke_Subscriber_With_Args()
        {
            TargetObjectSentArgs? captured = null;
            OutgoingEventSink.TargetObjectSent += e => captured = e;

            var expected = new TargetObjectSentArgs(0xAABBCCDD, 0x190, 1000, 2000, (sbyte)-5, 0xDEAD, 1);
            OutgoingEventSink.RaiseTargetObjectSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseTargetXyzSent_Should_Invoke_Subscriber_With_Args()
        {
            TargetXyzSentArgs? captured = null;
            OutgoingEventSink.TargetXyzSent += e => captured = e;

            var expected = new TargetXyzSentArgs(0x190, 1000, 2000, (sbyte)5, 0xBEEF, 0);
            OutgoingEventSink.RaiseTargetXyzSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseTargetCancelSent_Should_Invoke_Subscriber_With_Args()
        {
            TargetCancelSentArgs? captured = null;
            OutgoingEventSink.TargetCancelSent += e => captured = e;

            var expected = new TargetCancelSentArgs(CursorTarget.Object, 0xDEAD, 1);
            OutgoingEventSink.RaiseTargetCancelSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseTargetSelectedObjectSent_Should_Invoke_Subscriber_With_Args()
        {
            TargetSelectedObjectSentArgs? captured = null;
            OutgoingEventSink.TargetSelectedObjectSent += e => captured = e;

            var expected = new TargetSelectedObjectSentArgs(0x11112222, 0x33334444);
            OutgoingEventSink.RaiseTargetSelectedObjectSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseChangeWarModeSent_Should_Invoke_Subscriber_With_Args()
        {
            ChangeWarModeSentArgs? captured = null;
            OutgoingEventSink.ChangeWarModeSent += e => captured = e;

            var expected = new ChangeWarModeSentArgs(true);
            OutgoingEventSink.RaiseChangeWarModeSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseUseCombatAbilitySent_Should_Invoke_Subscriber_With_Args()
        {
            UseCombatAbilitySentArgs? captured = null;
            OutgoingEventSink.UseCombatAbilitySent += e => captured = e;

            var expected = new UseCombatAbilitySentArgs(3);
            OutgoingEventSink.RaiseUseCombatAbilitySent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseClickQuestArrowSent_Should_Invoke_Subscriber_With_Args()
        {
            ClickQuestArrowSentArgs? captured = null;
            OutgoingEventSink.ClickQuestArrowSent += e => captured = e;

            var expected = new ClickQuestArrowSentArgs(true);
            OutgoingEventSink.RaiseClickQuestArrowSent(expected);

            captured.Should().Be(expected);
        }

        // ---- Interaction ----

        [Fact]
        public void RaiseDoubleClickSent_Should_Invoke_Subscriber_With_Args()
        {
            DoubleClickSentArgs? captured = null;
            OutgoingEventSink.DoubleClickSent += e => captured = e;

            var expected = new DoubleClickSentArgs(0xDEADBEEF);
            OutgoingEventSink.RaiseDoubleClickSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseClickRequestSent_Should_Invoke_Subscriber_With_Args()
        {
            ClickRequestSentArgs? captured = null;
            OutgoingEventSink.ClickRequestSent += e => captured = e;

            var expected = new ClickRequestSentArgs(0xCAFEBABE);
            OutgoingEventSink.RaiseClickRequestSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseUseSkillSent_Should_Invoke_Subscriber_With_Args()
        {
            UseSkillSentArgs? captured = null;
            OutgoingEventSink.UseSkillSent += e => captured = e;

            var expected = new UseSkillSentArgs(42);
            OutgoingEventSink.RaiseUseSkillSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseNameRequestSent_Should_Invoke_Subscriber_With_Args()
        {
            NameRequestSentArgs? captured = null;
            OutgoingEventSink.NameRequestSent += e => captured = e;

            var expected = new NameRequestSentArgs(0x11223344);
            OutgoingEventSink.RaiseNameRequestSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseOpenDoorSent_Should_Invoke_Subscriber()
        {
            OpenDoorSentArgs? captured = null;
            OutgoingEventSink.OpenDoorSent += e => captured = e;

            OutgoingEventSink.RaiseOpenDoorSent(new OpenDoorSentArgs());

            captured.Should().NotBeNull();
        }
    }
}
