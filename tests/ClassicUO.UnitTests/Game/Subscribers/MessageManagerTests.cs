// SPDX-License-Identifier: BSD-2-Clause

using System;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Events;
using ClassicUO.Game.Managers;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Subscribers
{
    // Subscriber-side behavioral tests for MessageManager.
    //
    // We pass a null World — the prompt handlers under test never dereference
    // it, and avoiding `new World()` keeps us from re-wiring every other
    // manager's EventSink subscriptions (which collides with PacketHandlersTests
    // running in parallel against the same static EventSink). EventSink.ClearAll()
    // in the ctor wipes any stale subscriptions before each test.
    //
    // Skipped paths (documented inline below):
    //   - OnChatMessage / OnUnicodeChatMessage / OnClilocMessage:
    //     all route through HandleMessage, which dereferences
    //     ProfileManager.CurrentProfile (null in unit-test context) and the
    //     static Client.Game.UO.FileManager.Clilocs / Fonts. Cannot exercise
    //     these without bootstrapping the full game shell.
    public class MessageManagerTests : IDisposable
    {
        public MessageManagerTests()
        {
            EventSink.ClearAll();
        }

        public void Dispose()
        {
            EventSink.ClearAll();
        }

        private static MessageManager NewManager() => new MessageManager(world: null);

        // ---- AsciiPrompt → PromptData ----

        [Fact]
        public void AsciiPrompt_Sets_PromptData_With_ASCII_Tag_And_Id()
        {
            var manager = NewManager();

            EventSink.RaiseAsciiPrompt(new AsciiPromptArgs(0xABCD_1234UL));

            manager.PromptData.Prompt.Should().Be(ConsolePrompt.ASCII);
            manager.PromptData.Data.Should().Be(0xABCD_1234UL);
        }

        [Fact]
        public void AsciiPrompt_Fires_ServerPromptChanged()
        {
            var manager = NewManager();
            PromptData? captured = null;
            manager.ServerPromptChanged += (_, data) => captured = data;

            EventSink.RaiseAsciiPrompt(new AsciiPromptArgs(0x42UL));

            captured.Should().NotBeNull();
            captured.Value.Prompt.Should().Be(ConsolePrompt.ASCII);
            captured.Value.Data.Should().Be(0x42UL);
        }

        // ---- UnicodePrompt → PromptData ----

        [Fact]
        public void UnicodePrompt_Sets_PromptData_With_Unicode_Tag_And_Id()
        {
            var manager = NewManager();

            EventSink.RaiseUnicodePrompt(new UnicodePromptArgs(0xDEAD_BEEFUL));

            manager.PromptData.Prompt.Should().Be(ConsolePrompt.Unicode);
            manager.PromptData.Data.Should().Be(0xDEAD_BEEFUL);
        }

        [Fact]
        public void UnicodePrompt_Fires_ServerPromptChanged()
        {
            var manager = NewManager();
            PromptData? captured = null;
            manager.ServerPromptChanged += (_, data) => captured = data;

            EventSink.RaiseUnicodePrompt(new UnicodePromptArgs(7UL));

            captured.Should().NotBeNull();
            captured.Value.Prompt.Should().Be(ConsolePrompt.Unicode);
            captured.Value.Data.Should().Be(7UL);
        }

        // ---- Prompt overwrite semantics ----

        [Fact]
        public void Second_Prompt_Overwrites_First()
        {
            var manager = NewManager();

            EventSink.RaiseAsciiPrompt(new AsciiPromptArgs(1UL));
            EventSink.RaiseUnicodePrompt(new UnicodePromptArgs(2UL));

            manager.PromptData.Prompt.Should().Be(ConsolePrompt.Unicode);
            manager.PromptData.Data.Should().Be(2UL);
        }

        [Fact]
        public void Multiple_Prompts_Fire_ServerPromptChanged_Each_Time()
        {
            var manager = NewManager();
            int count = 0;
            manager.ServerPromptChanged += (_, _) => count++;

            EventSink.RaiseAsciiPrompt(new AsciiPromptArgs(1UL));
            EventSink.RaiseAsciiPrompt(new AsciiPromptArgs(2UL));
            EventSink.RaiseUnicodePrompt(new UnicodePromptArgs(3UL));

            count.Should().Be(3);
        }

        // ---- Unsubscribe stops the wiring ----

        [Fact]
        public void Unsubscribe_Stops_PromptData_From_Updating()
        {
            var manager = NewManager();
            EventSink.RaiseAsciiPrompt(new AsciiPromptArgs(0xAAUL));
            manager.PromptData.Data.Should().Be(0xAAUL);

            manager.Unsubscribe();

            EventSink.RaiseAsciiPrompt(new AsciiPromptArgs(0xBBUL));
            manager.PromptData.Data.Should().Be(0xAAUL); // unchanged
        }
    }
}
