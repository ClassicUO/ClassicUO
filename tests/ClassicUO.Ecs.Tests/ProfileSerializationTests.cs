// SPDX-License-Identifier: BSD-2-Clause

using System.Text.Json;
using ClassicUO.Configuration;
using Microsoft.Xna.Framework;
using Xunit;

namespace ClassicUO.Ecs.Tests;

// Regression: Microsoft.Xna.Framework.Point exposes X/Y as fields, which STJ
// source-gen ignores — so without [JsonConverter(typeof(Point2Converter))] the
// Point props serialize to "{}" and window size / positions reset on reload.
public class ProfileSerializationTests
{
    [Fact]
    public void GameWindowSize_RoundTrips()
    {
        var profile = new Profile { GameWindowSize = new Point(1234, 567) };

        var json = JsonSerializer.Serialize(profile, ProfileJsonContext.DefaultToUse.Profile);
        Assert.DoesNotContain("\"game_window_size\": {}", json);

        var loaded = JsonSerializer.Deserialize(json, ProfileJsonContext.DefaultToUse.Profile);
        Assert.Equal(new Point(1234, 567), loaded.GameWindowSize);
    }
}
