using System;
using ClassicUO.Utility.Logging;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Utility.Tests.Logging
{
    public class LogTests : IDisposable
    {
        public void Dispose()
        {
            // Ensure we stop the logger after each test to avoid cross-test pollution
            Log.Stop();
        }

        [Fact]
        public void Start_DoesNotThrow()
        {
            var act = () => Log.Start(LogTypes.All);

            act.Should().NotThrow();
        }

        [Fact]
        public void Stop_DoesNotThrow()
        {
            Log.Start(LogTypes.All);

            var act = () => Log.Stop();

            act.Should().NotThrow();
        }

        [Fact]
        public void Stop_WithoutStart_DoesNotThrow()
        {
            var act = () => Log.Stop();

            act.Should().NotThrow();
        }

        [Fact]
        public void Info_WhenStarted_DoesNotThrow()
        {
            Log.Start(LogTypes.All);

            var act = () => Log.Info("test info message");

            act.Should().NotThrow();
        }

        [Fact]
        public void Warn_WhenStarted_DoesNotThrow()
        {
            Log.Start(LogTypes.All);

            var act = () => Log.Warn("test warning message");

            act.Should().NotThrow();
        }

        [Fact]
        public void Error_WhenStarted_DoesNotThrow()
        {
            Log.Start(LogTypes.All);

            var act = () => Log.Error("test error message");

            act.Should().NotThrow();
        }

        [Fact]
        public void Trace_WhenStarted_DoesNotThrow()
        {
            Log.Start(LogTypes.All);

            var act = () => Log.Trace("test trace message");

            act.Should().NotThrow();
        }

        [Fact]
        public void Panic_WhenStarted_DoesNotThrow()
        {
            Log.Start(LogTypes.All);

            var act = () => Log.Panic("test panic message");

            act.Should().NotThrow();
        }

        [Fact]
        public void PushIndent_DoesNotThrow()
        {
            Log.Start(LogTypes.All);

            var act = () => Log.PushIndent();

            act.Should().NotThrow();
        }

        [Fact]
        public void PopIndent_DoesNotThrow()
        {
            Log.Start(LogTypes.All);

            var act = () => Log.PopIndent();

            act.Should().NotThrow();
        }

        [Fact]
        public void PushAndPopIndent_DoesNotThrow()
        {
            Log.Start(LogTypes.All);

            var act = () =>
            {
                Log.PushIndent();
                Log.Info("indented message");
                Log.PopIndent();
            };

            act.Should().NotThrow();
        }

        [Fact]
        public void Info_WithoutStart_DoesNotThrow()
        {
            // Logger is null when not started, should not throw
            var act = () => Log.Info("message without start");

            act.Should().NotThrow();
        }

        [Fact]
        public void NewLine_WhenStarted_DoesNotThrow()
        {
            Log.Start(LogTypes.All);

            var act = () => Log.NewLine();

            act.Should().NotThrow();
        }

        [Fact]
        public void Clear_WhenStarted_DoesNotThrowOrThrowsIOException()
        {
            Log.Start(LogTypes.All);

            // Log.Clear() calls Console.Clear() internally, which may throw
            // IOException when no console is attached (e.g., in test runners).
            // We verify it doesn't throw anything unexpected.
            Exception caught = null;
            try
            {
                Log.Clear();
            }
            catch (System.IO.IOException)
            {
                // Expected in test environments without a console
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            caught.Should().BeNull();
        }
    }
}
