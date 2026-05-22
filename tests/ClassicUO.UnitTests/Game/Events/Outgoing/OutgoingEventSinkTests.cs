// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Game.Events.Outgoing;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Events.Outgoing
{
    [Collection("EventSinkSerial")]
    public class OutgoingEventSinkTests
    {
        public OutgoingEventSinkTests()
        {
            OutgoingEventSink.ClearAll();
        }

        [Fact]
        public void RaisePingSent_Should_Invoke_Subscriber_With_Args()
        {
            PingSentArgs? captured = null;
            OutgoingEventSink.PingSent += e => captured = e;

            var expected = new PingSentArgs(0x42);
            OutgoingEventSink.RaisePingSent(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaisePingSent_With_No_Subscribers_Should_Not_Throw()
        {
            var act = () => OutgoingEventSink.RaisePingSent(new PingSentArgs(0));
            act.Should().NotThrow();
        }

        [Fact]
        public void ClearAll_Should_Remove_All_Subscribers()
        {
            int count = 0;
            OutgoingEventSink.PingSent += _ => count++;

            OutgoingEventSink.ClearAll();
            OutgoingEventSink.RaisePingSent(new PingSentArgs(0));

            count.Should().Be(0);
        }

        [Fact]
        public void Throwing_Subscriber_Should_Not_Stop_Other_Subscribers()
        {
            bool secondInvoked = false;
            OutgoingEventSink.PingSent += _ => throw new System.InvalidOperationException("boom");
            OutgoingEventSink.PingSent += _ => secondInvoked = true;

            OutgoingEventSink.RaisePingSent(new PingSentArgs(0));

            secondInvoked.Should().BeTrue();
        }
    }
}
