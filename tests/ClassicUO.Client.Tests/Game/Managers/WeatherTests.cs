using ClassicUO.Client.Tests;
using ClassicUO.Game;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Game.Managers
{
    public class WeatherTests
    {
        private readonly World _world;
        private readonly Weather _weather;

        public WeatherTests()
        {
            _world = TestHelpers.CreateTestWorld();
            _weather = _world.Weather;
        }

        [Fact]
        public void Weather_IsNotNull()
        {
            _weather.Should().NotBeNull();
        }

        [Fact]
        public void CurrentWeather_DefaultNull()
        {
            _weather.CurrentWeather.Should().BeNull();
        }

        [Fact]
        public void Type_DefaultIsRain()
        {
            // Default enum value 0 = WT_RAIN
            _weather.Type.Should().Be(WeatherType.WT_RAIN);
        }

        [Fact]
        public void Count_DefaultZero()
        {
            _weather.Count.Should().Be(0);
        }

        [Fact]
        public void CurrentCount_DefaultZero()
        {
            _weather.CurrentCount.Should().Be(0);
        }

        [Fact]
        public void Temperature_DefaultZero()
        {
            _weather.Temperature.Should().Be(0);
        }

        [Fact]
        public void Wind_DefaultZero()
        {
            _weather.Wind.Should().Be(0);
        }

        [Fact]
        public void ScaledCount_DefaultZero()
        {
            _weather.ScaledCount.Should().Be(0);
        }

        [Fact]
        public void Reset_ClearsAllProperties()
        {
            _weather.Reset();

            _weather.Type.Should().Be(0);
            _weather.Count.Should().Be(0);
            _weather.CurrentCount.Should().Be(0);
            _weather.Temperature.Should().Be(0);
            _weather.Wind.Should().Be(0);
            _weather.CurrentWeather.Should().BeNull();
        }

        [Fact]
        public void Reset_CanBeCalledMultipleTimes()
        {
            _weather.Reset();
            _weather.Reset();

            _weather.CurrentWeather.Should().BeNull();
        }
    }
}
