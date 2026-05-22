// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events.Outgoing;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Events.Outgoing
{
    [Collection("EventSinkSerial")]
    public class BooksHouseMiscSentTests
    {
        public BooksHouseMiscSentTests()
        {
            OutgoingEventSink.ClearAll();
        }

        // ---- Books ----

        [Fact]
        public void RaiseBookHeaderChangedOldSent_Should_Invoke_Subscriber()
        {
            BookHeaderChangedOldSentArgs? captured = null;
            OutgoingEventSink.BookHeaderChangedOldSent += e => captured = e;

            var expected = new BookHeaderChangedOldSentArgs(0x40000001u, "title", "author");
            OutgoingEventSink.RaiseBookHeaderChangedOldSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseBookHeaderChangedSent_Should_Invoke_Subscriber()
        {
            BookHeaderChangedSentArgs? captured = null;
            OutgoingEventSink.BookHeaderChangedSent += e => captured = e;

            var expected = new BookHeaderChangedSentArgs(0x40000002u, "title2", "author2");
            OutgoingEventSink.RaiseBookHeaderChangedSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseBookPageDataSent_Should_Invoke_Subscriber()
        {
            BookPageDataSentArgs? captured = null;
            OutgoingEventSink.BookPageDataSent += e => captured = e;

            var lines = new[] { "line1", "line2" };
            var expected = new BookPageDataSentArgs(0x40000003u, lines, 1);
            OutgoingEventSink.RaiseBookPageDataSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseBookPageDataRequestSent_Should_Invoke_Subscriber()
        {
            BookPageDataRequestSentArgs? captured = null;
            OutgoingEventSink.BookPageDataRequestSent += e => captured = e;

            var expected = new BookPageDataRequestSentArgs(0x40000004u, 5);
            OutgoingEventSink.RaiseBookPageDataRequestSent(expected);

            captured.Should().Be(expected);
        }

        // ---- Bulletin Board ----

        [Fact]
        public void RaiseBulletinBoardRequestMessageSent_Should_Invoke_Subscriber()
        {
            BulletinBoardRequestMessageSentArgs? captured = null;
            OutgoingEventSink.BulletinBoardRequestMessageSent += e => captured = e;

            var expected = new BulletinBoardRequestMessageSentArgs(0x40000010u, 0x40000011u);
            OutgoingEventSink.RaiseBulletinBoardRequestMessageSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseBulletinBoardRequestMessageSummarySent_Should_Invoke_Subscriber()
        {
            BulletinBoardRequestMessageSummarySentArgs? captured = null;
            OutgoingEventSink.BulletinBoardRequestMessageSummarySent += e => captured = e;

            var expected = new BulletinBoardRequestMessageSummarySentArgs(0x40000012u, 0x40000013u);
            OutgoingEventSink.RaiseBulletinBoardRequestMessageSummarySent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseBulletinBoardPostMessageSent_Should_Invoke_Subscriber()
        {
            BulletinBoardPostMessageSentArgs? captured = null;
            OutgoingEventSink.BulletinBoardPostMessageSent += e => captured = e;

            var expected = new BulletinBoardPostMessageSentArgs(0x40000014u, 0x40000015u, "subject", "body");
            OutgoingEventSink.RaiseBulletinBoardPostMessageSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseBulletinBoardRemoveMessageSent_Should_Invoke_Subscriber()
        {
            BulletinBoardRemoveMessageSentArgs? captured = null;
            OutgoingEventSink.BulletinBoardRemoveMessageSent += e => captured = e;

            var expected = new BulletinBoardRemoveMessageSentArgs(0x40000016u, 0x40000017u);
            OutgoingEventSink.RaiseBulletinBoardRemoveMessageSent(expected);

            captured.Should().Be(expected);
        }

        // ---- House Customization ----

        [Fact]
        public void RaiseCustomHouseDataRequestSent_Should_Invoke_Subscriber()
        {
            CustomHouseDataRequestSentArgs? captured = null;
            OutgoingEventSink.CustomHouseDataRequestSent += e => captured = e;

            var expected = new CustomHouseDataRequestSentArgs(0x40000020u);
            OutgoingEventSink.RaiseCustomHouseDataRequestSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseCustomHouseBackupSent_Should_Invoke_Subscriber()
        {
            CustomHouseBackupSentArgs? captured = null;
            OutgoingEventSink.CustomHouseBackupSent += e => captured = e;

            var expected = new CustomHouseBackupSentArgs();
            OutgoingEventSink.RaiseCustomHouseBackupSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseCustomHouseRestoreSent_Should_Invoke_Subscriber()
        {
            CustomHouseRestoreSentArgs? captured = null;
            OutgoingEventSink.CustomHouseRestoreSent += e => captured = e;

            var expected = new CustomHouseRestoreSentArgs();
            OutgoingEventSink.RaiseCustomHouseRestoreSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseCustomHouseCommitSent_Should_Invoke_Subscriber()
        {
            CustomHouseCommitSentArgs? captured = null;
            OutgoingEventSink.CustomHouseCommitSent += e => captured = e;

            var expected = new CustomHouseCommitSentArgs();
            OutgoingEventSink.RaiseCustomHouseCommitSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseCustomHouseBuildingExitSent_Should_Invoke_Subscriber()
        {
            CustomHouseBuildingExitSentArgs? captured = null;
            OutgoingEventSink.CustomHouseBuildingExitSent += e => captured = e;

            var expected = new CustomHouseBuildingExitSentArgs();
            OutgoingEventSink.RaiseCustomHouseBuildingExitSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseCustomHouseGoToFloorSent_Should_Invoke_Subscriber()
        {
            CustomHouseGoToFloorSentArgs? captured = null;
            OutgoingEventSink.CustomHouseGoToFloorSent += e => captured = e;

            var expected = new CustomHouseGoToFloorSentArgs(2);
            OutgoingEventSink.RaiseCustomHouseGoToFloorSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseCustomHouseSyncSent_Should_Invoke_Subscriber()
        {
            CustomHouseSyncSentArgs? captured = null;
            OutgoingEventSink.CustomHouseSyncSent += e => captured = e;

            var expected = new CustomHouseSyncSentArgs();
            OutgoingEventSink.RaiseCustomHouseSyncSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseCustomHouseClearSent_Should_Invoke_Subscriber()
        {
            CustomHouseClearSentArgs? captured = null;
            OutgoingEventSink.CustomHouseClearSent += e => captured = e;

            var expected = new CustomHouseClearSentArgs();
            OutgoingEventSink.RaiseCustomHouseClearSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseCustomHouseRevertSent_Should_Invoke_Subscriber()
        {
            CustomHouseRevertSentArgs? captured = null;
            OutgoingEventSink.CustomHouseRevertSent += e => captured = e;

            var expected = new CustomHouseRevertSentArgs();
            OutgoingEventSink.RaiseCustomHouseRevertSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseCustomHouseResponseSent_Should_Invoke_Subscriber()
        {
            CustomHouseResponseSentArgs? captured = null;
            OutgoingEventSink.CustomHouseResponseSent += e => captured = e;

            var expected = new CustomHouseResponseSentArgs();
            OutgoingEventSink.RaiseCustomHouseResponseSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseCustomHouseAddItemSent_Should_Invoke_Subscriber()
        {
            CustomHouseAddItemSentArgs? captured = null;
            OutgoingEventSink.CustomHouseAddItemSent += e => captured = e;

            var expected = new CustomHouseAddItemSentArgs(0x1234, 10, 20);
            OutgoingEventSink.RaiseCustomHouseAddItemSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseCustomHouseDeleteItemSent_Should_Invoke_Subscriber()
        {
            CustomHouseDeleteItemSentArgs? captured = null;
            OutgoingEventSink.CustomHouseDeleteItemSent += e => captured = e;

            var expected = new CustomHouseDeleteItemSentArgs(0x1235, 11, 21, 1);
            OutgoingEventSink.RaiseCustomHouseDeleteItemSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseCustomHouseAddRoofSent_Should_Invoke_Subscriber()
        {
            CustomHouseAddRoofSentArgs? captured = null;
            OutgoingEventSink.CustomHouseAddRoofSent += e => captured = e;

            var expected = new CustomHouseAddRoofSentArgs(0x1236, 12, 22, 2);
            OutgoingEventSink.RaiseCustomHouseAddRoofSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseCustomHouseDeleteRoofSent_Should_Invoke_Subscriber()
        {
            CustomHouseDeleteRoofSentArgs? captured = null;
            OutgoingEventSink.CustomHouseDeleteRoofSent += e => captured = e;

            var expected = new CustomHouseDeleteRoofSentArgs(0x1237, 13, 23, 3);
            OutgoingEventSink.RaiseCustomHouseDeleteRoofSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseCustomHouseAddStairSent_Should_Invoke_Subscriber()
        {
            CustomHouseAddStairSentArgs? captured = null;
            OutgoingEventSink.CustomHouseAddStairSent += e => captured = e;

            var expected = new CustomHouseAddStairSentArgs(0x1238, 14, 24);
            OutgoingEventSink.RaiseCustomHouseAddStairSent(expected);

            captured.Should().Be(expected);
        }

        // ---- World / Window ----

        [Fact]
        public void RaiseGameWindowSizeSent_Should_Invoke_Subscriber()
        {
            GameWindowSizeSentArgs? captured = null;
            OutgoingEventSink.GameWindowSizeSent += e => captured = e;

            var expected = new GameWindowSizeSentArgs(800, 600);
            OutgoingEventSink.RaiseGameWindowSizeSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseClientViewRangeSent_Should_Invoke_Subscriber()
        {
            ClientViewRangeSentArgs? captured = null;
            OutgoingEventSink.ClientViewRangeSent += e => captured = e;

            var expected = new ClientViewRangeSentArgs(18);
            OutgoingEventSink.RaiseClientViewRangeSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseOpenUoStoreSent_Should_Invoke_Subscriber()
        {
            OpenUoStoreSentArgs? captured = null;
            OutgoingEventSink.OpenUoStoreSent += e => captured = e;

            var expected = new OpenUoStoreSentArgs();
            OutgoingEventSink.RaiseOpenUoStoreSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseShowPublicHouseContentSent_Should_Invoke_Subscriber()
        {
            ShowPublicHouseContentSentArgs? captured = null;
            OutgoingEventSink.ShowPublicHouseContentSent += e => captured = e;

            var expected = new ShowPublicHouseContentSentArgs(true);
            OutgoingEventSink.RaiseShowPublicHouseContentSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseDeathScreenSent_Should_Invoke_Subscriber()
        {
            DeathScreenSentArgs? captured = null;
            OutgoingEventSink.DeathScreenSent += e => captured = e;

            var expected = new DeathScreenSentArgs();
            OutgoingEventSink.RaiseDeathScreenSent(expected);

            captured.Should().Be(expected);
        }

        // ---- Meta / Admin ----

        [Fact]
        public void RaiseQueryGuildPositionSent_Should_Invoke_Subscriber()
        {
            QueryGuildPositionSentArgs? captured = null;
            OutgoingEventSink.QueryGuildPositionSent += e => captured = e;

            var expected = new QueryGuildPositionSentArgs();
            OutgoingEventSink.RaiseQueryGuildPositionSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseQueryPartyPositionSent_Should_Invoke_Subscriber()
        {
            QueryPartyPositionSentArgs? captured = null;
            OutgoingEventSink.QueryPartyPositionSent += e => captured = e;

            var expected = new QueryPartyPositionSentArgs();
            OutgoingEventSink.RaiseQueryPartyPositionSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseCloseStatusBarGumpSent_Should_Invoke_Subscriber()
        {
            CloseStatusBarGumpSentArgs? captured = null;
            OutgoingEventSink.CloseStatusBarGumpSent += e => captured = e;

            var expected = new CloseStatusBarGumpSentArgs(0x40000030u);
            OutgoingEventSink.RaiseCloseStatusBarGumpSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseGuildMenuRequestSent_Should_Invoke_Subscriber()
        {
            GuildMenuRequestSentArgs? captured = null;
            OutgoingEventSink.GuildMenuRequestSent += e => captured = e;

            var expected = new GuildMenuRequestSentArgs();
            OutgoingEventSink.RaiseGuildMenuRequestSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseQuestMenuRequestSent_Should_Invoke_Subscriber()
        {
            QuestMenuRequestSentArgs? captured = null;
            OutgoingEventSink.QuestMenuRequestSent += e => captured = e;

            var expected = new QuestMenuRequestSentArgs();
            OutgoingEventSink.RaiseQuestMenuRequestSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseEquipLastWeaponSent_Should_Invoke_Subscriber()
        {
            EquipLastWeaponSentArgs? captured = null;
            OutgoingEventSink.EquipLastWeaponSent += e => captured = e;

            var expected = new EquipLastWeaponSentArgs();
            OutgoingEventSink.RaiseEquipLastWeaponSent(expected);

            captured.Should().Be(expected);
        }

        // ---- UOLive ----

        [Fact]
        public void RaiseUoLiveHashResponseSent_Should_Invoke_Subscriber()
        {
            UoLiveHashResponseSentArgs? captured = null;
            OutgoingEventSink.UoLiveHashResponseSent += e => captured = e;

            var checksums = new ushort[] { 0x1111, 0x2222, 0x3333 };
            var expected = new UoLiveHashResponseSentArgs(42u, 1, checksums);
            OutgoingEventSink.RaiseUoLiveHashResponseSent(expected);

            captured.Should().Be(expected);
        }

        // ---- Plugins ----

        [Fact]
        public void RaiseToPluginsAllSpellsSent_Should_Invoke_Subscriber()
        {
            ToPluginsAllSpellsSentArgs? captured = null;
            OutgoingEventSink.ToPluginsAllSpellsSent += e => captured = e;

            var expected = new ToPluginsAllSpellsSentArgs();
            OutgoingEventSink.RaiseToPluginsAllSpellsSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseToPluginsAllSkillsSent_Should_Invoke_Subscriber()
        {
            ToPluginsAllSkillsSentArgs? captured = null;
            OutgoingEventSink.ToPluginsAllSkillsSent += e => captured = e;

            var expected = new ToPluginsAllSkillsSentArgs();
            OutgoingEventSink.RaiseToPluginsAllSkillsSent(expected);

            captured.Should().Be(expected);
        }
    }
}
