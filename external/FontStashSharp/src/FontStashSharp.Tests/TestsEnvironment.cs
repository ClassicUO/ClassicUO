using Microsoft.Xna.Framework.Graphics;
using NUnit.Framework;
using System.Reflection;

namespace FontStashSharp.Tests
{
	[SetUpFixture]
	public class TestsEnvironment
	{
		private static TestGame _game;
		private static FontSystem _defaultFontSystem;

		public static Assembly Assembly => typeof(TestsEnvironment).Assembly;

		public static GraphicsDevice GraphicsDevice => _game.GraphicsDevice;

		public static FontSystem DefaultFontSystem
		{
			get
			{
				if (_defaultFontSystem == null)
				{
					var fontSystem = CreateDefaultFontSystem(new FontSystemSettings());
					_defaultFontSystem = fontSystem;
				}

				return _defaultFontSystem;
			}
		}

		[OneTimeSetUp]
		public void SetUp()
		{
			_game = new TestGame();
		}

		public static FontSystem CreateDefaultFontSystem(FontSystemSettings settings)
		{
			var fontSystem = new FontSystem(settings);
			fontSystem.AddFont(Assembly.ReadResourceAsBytes("Resources.DroidSans.ttf"));

			return fontSystem;
		}
	}
}
