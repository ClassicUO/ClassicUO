using ClassicUO.Game.Events;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game.Events
{
    public class EventSinkTests
    {
        public EventSinkTests()
        {
            EventSink.ClearAll();
        }

        [Fact]
        public void RaiseSoundPlay_Should_Invoke_Subscriber_With_Args()
        {
            SoundPlayArgs? captured = null;
            EventSink.SoundPlay += e => captured = e;

            var expected = new SoundPlayArgs(0x1234, 0x5678, 100, 200, 5);
            EventSink.RaiseSoundPlay(expected);

            captured.Should().Be(expected);
        }

        [Fact]
        public void RaiseSoundPlay_With_No_Subscribers_Should_Not_Throw()
        {
            var act = () => EventSink.RaiseSoundPlay(new SoundPlayArgs(1, 1, 0, 0, 0));
            act.Should().NotThrow();
        }

        [Fact]
        public void ClearAll_Should_Remove_All_Subscribers()
        {
            int count = 0;
            EventSink.SoundPlay += _ => count++;

            EventSink.ClearAll();
            EventSink.RaiseSoundPlay(new SoundPlayArgs(1, 1, 0, 0, 0));

            count.Should().Be(0);
        }

        [Fact]
        public void Multiple_Subscribers_Should_All_Receive_Event()
        {
            int a = 0, b = 0;
            EventSink.SoundPlay += _ => a++;
            EventSink.SoundPlay += _ => b++;

            EventSink.RaiseSoundPlay(new SoundPlayArgs(1, 1, 0, 0, 0));

            a.Should().Be(1);
            b.Should().Be(1);
        }

        [Fact]
        public void Throwing_Subscriber_Should_Not_Stop_Other_Subscribers()
        {
            bool secondInvoked = false;
            EventSink.SoundPlay += _ => throw new System.InvalidOperationException("boom");
            EventSink.SoundPlay += _ => secondInvoked = true;

            EventSink.RaiseSoundPlay(new SoundPlayArgs(1, 1, 0, 0, 0));

            secondInvoked.Should().BeTrue();
        }
    }
}
