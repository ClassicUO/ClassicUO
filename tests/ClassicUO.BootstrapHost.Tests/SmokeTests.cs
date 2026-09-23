// SPDX-License-Identifier: BSD-2-Clause

using System.IO;
using FluentAssertions;
using Xunit;

namespace ClassicUO.BootstrapHost.Tests;

/// <summary>
/// Smoke tests that drive the plugin pipeline without cuo.dll. Each test
/// stages a fresh temp <c>Plugins/HelloPlugin/</c>, raises events via
/// <see cref="HostBridge"/>'s test-only hooks, and asserts against the
/// sample plugin's log file.
/// </summary>
public sealed class SmokeTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _pluginsRoot;
    private readonly string _logPath;

    public SmokeTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "cuo-bootstraphost-tests", Guid.NewGuid().ToString("N"));
        _pluginsRoot = Path.Combine(_tempRoot, "Plugins");
        var pluginDir = Path.Combine(_pluginsRoot, "HelloPlugin");
        Directory.CreateDirectory(pluginDir);

        var fixtureDll = Path.Combine(AppContext.BaseDirectory, "Fixtures", "HelloPlugin", "HelloPlugin.dll");
        File.Exists(fixtureDll).Should().BeTrue($"HelloPlugin fixture should be staged at {fixtureDll}");
        File.Copy(fixtureDll, Path.Combine(pluginDir, "HelloPlugin.dll"));

        _logPath = Path.Combine(_tempRoot, "events.log");
        Environment.SetEnvironmentVariable("CUO_PLUGIN_TEST_LOG", _logPath);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("CUO_PLUGIN_TEST_LOG", null);
        try { Directory.Delete(_tempRoot, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public void Discovers_and_initializes_sample_plugin()
    {
        var bridge = new HostBridge();
        bridge.LoadPluginsForTest(_pluginsRoot);

        bridge.Plugins.Should().HaveCount(1);
        bridge.Plugins[0].Id.Should().Be("HelloPlugin");
        bridge.Plugins[0].PluginInstance.Should().NotBeNull();
        ReadLog().Should().Contain("OnInitialize");
    }

    [Fact]
    public void Lifecycle_events_reach_the_plugin()
    {
        var bridge = new HostBridge();
        bridge.LoadPluginsForTest(_pluginsRoot);

        bridge.TestRaiseConnected();
        bridge.TestRaiseTick();
        bridge.TestRaisePlayerPositionChanged(100, 200, 5);
        bridge.TestRaiseDisconnected();
        bridge.TestRaiseClosing();

        var log = ReadLog();
        log.Should().Contain("Connected");
        log.Should().Contain("Tick");
        log.Should().Contain("Pos:100,200,5");
        log.Should().Contain("Disconnected");
        log.Should().Contain("Closing");
    }

    [Fact]
    public void Hotkey_handler_can_block_default_behavior()
    {
        var bridge = new HostBridge();
        bridge.LoadPluginsForTest(_pluginsRoot);

        // Sample plugin returns false (blocks) for key 999, true otherwise.
        bridge.TestRaiseHotkey(key: 42, mod: 0, pressed: true).Should().BeTrue("non-blocking key allows default");
        bridge.TestRaiseHotkey(key: 999, mod: 0, pressed: true).Should().BeFalse("test sentinel key is blocked");

        var log = ReadLog();
        log.Should().Contain("Hotkey:42/0/True");
        log.Should().Contain("Hotkey:999/0/True");
    }

    [Fact]
    public void Packet_handler_observes_span_and_can_block()
    {
        var bridge = new HostBridge();
        bridge.LoadPluginsForTest(_pluginsRoot);

        Span<byte> normal = stackalloc byte[] { 0x11, 0x22, 0x33 };
        Span<byte> sentinel = stackalloc byte[] { 0x99, 0xAA };

        bridge.TestRaisePacketIn(normal).Should().BeTrue("non-sentinel packet flows through");
        bridge.TestRaisePacketIn(sentinel).Should().BeFalse("0x99 sentinel packet is blocked");

        var log = ReadLog();
        log.Should().Contain("PacketIn:len=3,id=0x11");
        log.Should().Contain("PacketIn:len=2,id=0x99");
    }

    private string ReadLog()
    {
        if (!File.Exists(_logPath)) return string.Empty;
        // The plugin appends; we read the snapshot at assert time.
        return File.ReadAllText(_logPath);
    }
}
