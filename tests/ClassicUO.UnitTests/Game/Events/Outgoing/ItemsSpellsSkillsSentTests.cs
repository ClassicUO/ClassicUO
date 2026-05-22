// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Data;
using ClassicUO.Game.Events.Outgoing;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Events.Outgoing
{
    [Collection("EventSinkSerial")]
    public class ItemsSpellsSkillsSentTests
    {
        public ItemsSpellsSkillsSentTests()
        {
            OutgoingEventSink.ClearAll();
        }

        // ---- Items / Equip ----

        [Fact]
        public void RaisePickUpRequestSent_Should_Invoke_Subscriber_With_Args()
        {
            PickUpRequestSentArgs? captured = null;
            OutgoingEventSink.PickUpRequestSent += e => captured = e;

            var expected = new PickUpRequestSentArgs(0x4000_0001u, 5);
            OutgoingEventSink.RaisePickUpRequestSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseDropRequestSent_Should_Invoke_Subscriber_With_Args()
        {
            DropRequestSentArgs? captured = null;
            OutgoingEventSink.DropRequestSent += e => captured = e;

            var expected = new DropRequestSentArgs(0x4000_0010u, 1500, 1600, -5, 0, 0x4000_0020u);
            OutgoingEventSink.RaiseDropRequestSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseDropRequestOldSent_Should_Invoke_Subscriber_With_Args()
        {
            DropRequestOldSentArgs? captured = null;
            OutgoingEventSink.DropRequestOldSent += e => captured = e;

            var expected = new DropRequestOldSentArgs(0x4000_0010u, 100, 200, 10, 0x4000_0030u);
            OutgoingEventSink.RaiseDropRequestOldSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseEquipRequestSent_Should_Invoke_Subscriber_With_Args()
        {
            EquipRequestSentArgs? captured = null;
            OutgoingEventSink.EquipRequestSent += e => captured = e;

            var expected = new EquipRequestSentArgs(0x4000_0100u, Layer.OneHanded, 0x4000_0200u);
            OutgoingEventSink.RaiseEquipRequestSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseEquipMacroKrSent_Should_Invoke_Subscriber_With_Args()
        {
            EquipMacroKrSentArgs? captured = null;
            OutgoingEventSink.EquipMacroKrSent += e => captured = e;

            var serials = new uint[] { 0x4000_0001u, 0x4000_0002u };
            var expected = new EquipMacroKrSentArgs(serials);
            OutgoingEventSink.RaiseEquipMacroKrSent(expected);

            captured.Should().NotBeNull();
            captured!.Value.Serials.Should().BeEquivalentTo(serials);
        }

        [Fact]
        public void RaiseUnequipMacroKrSent_Should_Invoke_Subscriber_With_Args()
        {
            UnequipMacroKrSentArgs? captured = null;
            OutgoingEventSink.UnequipMacroKrSent += e => captured = e;

            var layers = new[] { Layer.OneHanded, Layer.TwoHanded };
            var expected = new UnequipMacroKrSentArgs(layers);
            OutgoingEventSink.RaiseUnequipMacroKrSent(expected);

            captured.Should().NotBeNull();
            captured!.Value.Layers.Should().BeEquivalentTo(layers);
        }

        [Fact]
        public void RaiseDyeDataResponseSent_Should_Invoke_Subscriber_With_Args()
        {
            DyeDataResponseSentArgs? captured = null;
            OutgoingEventSink.DyeDataResponseSent += e => captured = e;

            var expected = new DyeDataResponseSentArgs(0x4000_0500u, 0x0FAB, 0x0042);
            OutgoingEventSink.RaiseDyeDataResponseSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseRenameRequestSent_Should_Invoke_Subscriber_With_Args()
        {
            RenameRequestSentArgs? captured = null;
            OutgoingEventSink.RenameRequestSent += e => captured = e;

            var expected = new RenameRequestSentArgs(0x4000_0600u, "Rex");
            OutgoingEventSink.RaiseRenameRequestSent(expected);

            captured.Should().Be(expected);
        }

        // ---- Spells / Abilities ----

        [Fact]
        public void RaiseCastSpellSent_Should_Invoke_Subscriber_With_Args()
        {
            CastSpellSentArgs? captured = null;
            OutgoingEventSink.CastSpellSent += e => captured = e;

            var expected = new CastSpellSentArgs(42);
            OutgoingEventSink.RaiseCastSpellSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseCastSpellFromBookSent_Should_Invoke_Subscriber_With_Args()
        {
            CastSpellFromBookSentArgs? captured = null;
            OutgoingEventSink.CastSpellFromBookSent += e => captured = e;

            var expected = new CastSpellFromBookSentArgs(7, 0x4000_0700u);
            OutgoingEventSink.RaiseCastSpellFromBookSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseOpenSpellBookSent_Should_Invoke_Subscriber_With_Args()
        {
            OpenSpellBookSentArgs? captured = null;
            OutgoingEventSink.OpenSpellBookSent += e => captured = e;

            var expected = new OpenSpellBookSentArgs(0x01);
            OutgoingEventSink.RaiseOpenSpellBookSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseStunRequestSent_Should_Invoke_Subscriber()
        {
            int count = 0;
            OutgoingEventSink.StunRequestSent += _ => count++;

            OutgoingEventSink.RaiseStunRequestSent(new StunRequestSentArgs());

            count.Should().Be(1);
        }

        [Fact]
        public void RaiseDisarmRequestSent_Should_Invoke_Subscriber()
        {
            int count = 0;
            OutgoingEventSink.DisarmRequestSent += _ => count++;

            OutgoingEventSink.RaiseDisarmRequestSent(new DisarmRequestSentArgs());

            count.Should().Be(1);
        }

        [Fact]
        public void RaiseToggleGargoyleFlyingSent_Should_Invoke_Subscriber()
        {
            int count = 0;
            OutgoingEventSink.ToggleGargoyleFlyingSent += _ => count++;

            OutgoingEventSink.RaiseToggleGargoyleFlyingSent(new ToggleGargoyleFlyingSentArgs());

            count.Should().Be(1);
        }

        [Fact]
        public void RaiseInvokeVirtueRequestSent_Should_Invoke_Subscriber_With_Args()
        {
            InvokeVirtueRequestSentArgs? captured = null;
            OutgoingEventSink.InvokeVirtueRequestSent += e => captured = e;

            var expected = new InvokeVirtueRequestSentArgs(3);
            OutgoingEventSink.RaiseInvokeVirtueRequestSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseChangeRaceRequestSent_Should_Invoke_Subscriber_With_Args()
        {
            ChangeRaceRequestSentArgs? captured = null;
            OutgoingEventSink.ChangeRaceRequestSent += e => captured = e;

            var expected = new ChangeRaceRequestSentArgs(0x83EA, 0x203B, 0x0461, 0x203C, 0x0462);
            OutgoingEventSink.RaiseChangeRaceRequestSent(expected);

            captured.Should().Be(expected);
        }

        // ---- Status / Skills ----

        [Fact]
        public void RaiseStatusRequestSent_Should_Invoke_Subscriber_With_Args()
        {
            StatusRequestSentArgs? captured = null;
            OutgoingEventSink.StatusRequestSent += e => captured = e;

            var expected = new StatusRequestSentArgs(0x4000_0800u);
            OutgoingEventSink.RaiseStatusRequestSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseSkillsRequestSent_Should_Invoke_Subscriber_With_Args()
        {
            SkillsRequestSentArgs? captured = null;
            OutgoingEventSink.SkillsRequestSent += e => captured = e;

            var expected = new SkillsRequestSentArgs(0x4000_0900u);
            OutgoingEventSink.RaiseSkillsRequestSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseSkillsStatusRequestSent_Should_Invoke_Subscriber_With_Args()
        {
            SkillsStatusRequestSentArgs? captured = null;
            OutgoingEventSink.SkillsStatusRequestSent += e => captured = e;

            var expected = new SkillsStatusRequestSentArgs(12, 1);
            OutgoingEventSink.RaiseSkillsStatusRequestSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseStatLockStateRequestSent_Should_Invoke_Subscriber_With_Args()
        {
            StatLockStateRequestSentArgs? captured = null;
            OutgoingEventSink.StatLockStateRequestSent += e => captured = e;

            var expected = new StatLockStateRequestSentArgs(2, Lock.Locked);
            OutgoingEventSink.RaiseStatLockStateRequestSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseSkillStatusChangeRequestSent_Should_Invoke_Subscriber_With_Args()
        {
            SkillStatusChangeRequestSentArgs? captured = null;
            OutgoingEventSink.SkillStatusChangeRequestSent += e => captured = e;

            var expected = new SkillStatusChangeRequestSentArgs(20, 2);
            OutgoingEventSink.RaiseSkillStatusChangeRequestSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseHelpRequestSent_Should_Invoke_Subscriber()
        {
            int count = 0;
            OutgoingEventSink.HelpRequestSent += _ => count++;

            OutgoingEventSink.RaiseHelpRequestSent(new HelpRequestSentArgs());

            count.Should().Be(1);
        }

        // ---- OPL / Cliloc ----

        [Fact]
        public void RaiseMegaClilocRequestOldSent_Should_Invoke_Subscriber_With_Args()
        {
            MegaClilocRequestOldSentArgs? captured = null;
            OutgoingEventSink.MegaClilocRequestOldSent += e => captured = e;

            var expected = new MegaClilocRequestOldSentArgs(0x4000_0A00u);
            OutgoingEventSink.RaiseMegaClilocRequestOldSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseMegaClilocRequestSent_Should_Invoke_Subscriber_With_Args()
        {
            MegaClilocRequestSentArgs? captured = null;
            OutgoingEventSink.MegaClilocRequestSent += e => captured = e;

            var serials = new uint[] { 0x4000_0001u, 0x4000_0002u, 0x4000_0003u };
            var expected = new MegaClilocRequestSentArgs(serials);
            OutgoingEventSink.RaiseMegaClilocRequestSent(expected);

            captured.Should().NotBeNull();
            captured!.Value.Serials.Should().BeEquivalentTo(serials);
        }

        // ---- Profile ----

        [Fact]
        public void RaiseProfileRequestSent_Should_Invoke_Subscriber_With_Args()
        {
            ProfileRequestSentArgs? captured = null;
            OutgoingEventSink.ProfileRequestSent += e => captured = e;

            var expected = new ProfileRequestSentArgs(0x4000_0B00u);
            OutgoingEventSink.RaiseProfileRequestSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseProfileUpdateSent_Should_Invoke_Subscriber_With_Args()
        {
            ProfileUpdateSentArgs? captured = null;
            OutgoingEventSink.ProfileUpdateSent += e => captured = e;

            var expected = new ProfileUpdateSentArgs(0x4000_0B00u, "Hello, world!");
            OutgoingEventSink.RaiseProfileUpdateSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseTipRequestSent_Should_Invoke_Subscriber_With_Args()
        {
            TipRequestSentArgs? captured = null;
            OutgoingEventSink.TipRequestSent += e => captured = e;

            var expected = new TipRequestSentArgs(99, 0x01);
            OutgoingEventSink.RaiseTipRequestSent(expected);

            captured.Should().Be(expected);
        }

        // ---- Cross-cutting ----

        [Fact]
        public void ClearAll_Should_Remove_All_Items_Spells_Skills_Subscribers()
        {
            int pickUpCount = 0, castSpellCount = 0, statusCount = 0,
                profileCount = 0, megaClilocCount = 0;

            OutgoingEventSink.PickUpRequestSent += _ => pickUpCount++;
            OutgoingEventSink.CastSpellSent += _ => castSpellCount++;
            OutgoingEventSink.StatusRequestSent += _ => statusCount++;
            OutgoingEventSink.ProfileRequestSent += _ => profileCount++;
            OutgoingEventSink.MegaClilocRequestOldSent += _ => megaClilocCount++;

            OutgoingEventSink.ClearAll();

            OutgoingEventSink.RaisePickUpRequestSent(new PickUpRequestSentArgs(1, 1));
            OutgoingEventSink.RaiseCastSpellSent(new CastSpellSentArgs(1));
            OutgoingEventSink.RaiseStatusRequestSent(new StatusRequestSentArgs(1));
            OutgoingEventSink.RaiseProfileRequestSent(new ProfileRequestSentArgs(1));
            OutgoingEventSink.RaiseMegaClilocRequestOldSent(new MegaClilocRequestOldSentArgs(1));

            pickUpCount.Should().Be(0);
            castSpellCount.Should().Be(0);
            statusCount.Should().Be(0);
            profileCount.Should().Be(0);
            megaClilocCount.Should().Be(0);
        }

        [Fact]
        public void Throwing_Subscriber_Should_Not_Stop_Other_Subscribers()
        {
            bool secondInvoked = false;
            OutgoingEventSink.PickUpRequestSent += _ => throw new System.InvalidOperationException("boom");
            OutgoingEventSink.PickUpRequestSent += _ => secondInvoked = true;

            OutgoingEventSink.RaisePickUpRequestSent(new PickUpRequestSentArgs(1, 1));

            secondInvoked.Should().BeTrue();
        }

        [Fact]
        public void Raise_With_No_Subscribers_Should_Not_Throw()
        {
            var act = () =>
            {
                OutgoingEventSink.RaiseHelpRequestSent(new HelpRequestSentArgs());
                OutgoingEventSink.RaiseStunRequestSent(new StunRequestSentArgs());
                OutgoingEventSink.RaiseDisarmRequestSent(new DisarmRequestSentArgs());
                OutgoingEventSink.RaiseToggleGargoyleFlyingSent(new ToggleGargoyleFlyingSentArgs());
            };

            act.Should().NotThrow();
        }
    }
}
