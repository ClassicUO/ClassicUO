using ClassicUO.Assets;
using ClassicUO.Configuration;
using ClassicUO.Ecs;
using ClassicUO.Game.Data;
using ClassicUO.Network;
using ClassicUO.Utility;
using Xunit;

namespace ClassicUO.Ecs.Tests;

// Unit tests for InGamePacketsPlugin's pure, side-effect-free decoders. The
// packet observers themselves need a live network feed (no headless harness),
// so these target the standalone helpers the handlers delegate to (made
// internal for testing).
public class InGamePacketsTests
{
    [Fact]
    public void ComputeBodyConvFlags_maps_each_feature_flag()
    {
        Assert.Equal(
            BodyConvFlags.Anim1 | BodyConvFlags.Anim2,
            InGamePacketsPlugin.ComputeBodyConvFlags(LockedFeatureFlags.UOR));
        Assert.Equal(BodyConvFlags.Anim1, InGamePacketsPlugin.ComputeBodyConvFlags(LockedFeatureFlags.LBR));
        Assert.Equal(BodyConvFlags.Anim2, InGamePacketsPlugin.ComputeBodyConvFlags(LockedFeatureFlags.AOS));
        Assert.Equal((BodyConvFlags)0, InGamePacketsPlugin.ComputeBodyConvFlags(0));
    }

    [Fact]
    public void ComputeBodyConvFlags_combines_multiple_flags()
    {
        var result = InGamePacketsPlugin.ComputeBodyConvFlags(LockedFeatureFlags.UOR | LockedFeatureFlags.SE);
        Assert.Equal(BodyConvFlags.Anim1 | BodyConvFlags.Anim2 | BodyConvFlags.Anim3, result);
    }

    [Fact]
    public void ApplyClilocColor_wraps_known_clilocs()
    {
        Assert.Equal("<basefont color=#FFCC33>gold</basefont>",
            InGamePacketsPlugin.ApplyClilocColor(1062613, "gold", ClientVersion.CV_200));
        Assert.Equal("<basefont color=#b66dff>x</basefont>",
            InGamePacketsPlugin.ApplyClilocColor(1159561, "x", ClientVersion.CV_200));
    }

    [Fact]
    public void ApplyClilocColor_gates_1080418_on_client_version()
    {
        Assert.Equal("<basefont color=#40a4fe>x</basefont>",
            InGamePacketsPlugin.ApplyClilocColor(1080418, "x", ClientVersion.CV_60143));
        Assert.Equal("x",
            InGamePacketsPlugin.ApplyClilocColor(1080418, "x", ClientVersion.CV_200));
    }

    [Fact]
    public void ApplyClilocColor_leaves_unknown_clilocs_untouched()
    {
        Assert.Equal("plain", InGamePacketsPlugin.ApplyClilocColor(99999, "plain", ClientVersion.CV_60143));
    }

    [Fact]
    public void IsIgnoredChat_drops_guild_and_alliance_when_profile_opts_out()
    {
        Assert.True(InGamePacketsPlugin.IsIgnoredChat(MessageType.Guild, new Profile { IgnoreGuildMessages = true }));
        Assert.True(InGamePacketsPlugin.IsIgnoredChat(MessageType.Alliance, new Profile { IgnoreAllianceMessages = true }));
    }

    [Fact]
    public void IsIgnoredChat_keeps_messages_when_not_ignored()
    {
        Assert.False(InGamePacketsPlugin.IsIgnoredChat(MessageType.Guild, new Profile { IgnoreGuildMessages = false }));
        Assert.False(InGamePacketsPlugin.IsIgnoredChat(MessageType.Alliance, new Profile { IgnoreAllianceMessages = false }));
        Assert.False(InGamePacketsPlugin.IsIgnoredChat(MessageType.System, new Profile { IgnoreGuildMessages = true, IgnoreAllianceMessages = true }));
    }
}
