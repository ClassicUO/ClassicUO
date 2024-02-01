using FontStashSharp.Interfaces;
using System;

#if MONOGAME || FNA
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#elif STRIDE
using Stride.Core.Mathematics;
using Stride.Graphics;
using Texture2D = Stride.Graphics.Texture;
#else
using System.Drawing;
using System.Numerics;
using Matrix = System.Numerics.Matrix3x2;
using Texture2D = System.Object;
using Color = FontStashSharp.FSColor;
#endif

namespace FontStashSharp.RichText
{
	public class FSRenderContext
	{
		private IFontStashRenderer _renderer;
		private IFontStashRenderer2 _renderer2;
		private Matrix _transformation;
		private Vector2 _scale;
		private float _rotation;
		private float _layerDepth;

		public void SetRenderer(IFontStashRenderer renderer)
		{
			if (renderer == null)
			{
				throw new ArgumentNullException(nameof(renderer));
			}

			_renderer = renderer;
			_renderer2 = null;
		}

		public void SetRenderer(IFontStashRenderer2 renderer)
		{
			if (renderer == null)
			{
				throw new ArgumentNullException(nameof(renderer));
			}
			_renderer = null;
			_renderer2 = renderer;
		}

		public void Prepare(Vector2 position, Vector2 scale, float rotation, Vector2 origin, float layerDepth)
		{
			_scale = scale;
			_rotation = rotation;
			_layerDepth = layerDepth;
			Utility.BuildTransform(position, _scale, _rotation, origin, out _transformation);
		}

		public void DrawText(string text, SpriteFontBase font, Vector2 pos, Color color, 
			TextStyle textStyle, FontSystemEffect effect, int effectAmount)
		{
			if (string.IsNullOrEmpty(text))
			{
				return;
			}

			pos = pos.Transform(ref _transformation);
			if (_renderer != null)
			{
				font.DrawText(_renderer, text, pos, color, _scale, _rotation, default(Vector2), _layerDepth, 
					textStyle: textStyle, effect: effect, effectAmount: effectAmount);
			}
			else
			{
				font.DrawText(_renderer2, text, pos, color, _scale, _rotation, default(Vector2), _layerDepth,
					textStyle: textStyle, effect: effect, effectAmount: effectAmount);
			}
		}

		public void DrawImage(Texture2D texture, Rectangle sourceRegion, Vector2 position, Vector2 scale, Color color)
		{
			if (_renderer != null)
			{
				position = position.Transform(ref _transformation);
				_renderer.Draw(texture, position, sourceRegion, color, _rotation, _scale, _layerDepth);
			}
			else
			{
				var topLeft = new VertexPositionColorTexture();
				var topRight = new VertexPositionColorTexture();
				var bottomLeft = new VertexPositionColorTexture();
				var bottomRight = new VertexPositionColorTexture();

				var size = new Vector2(sourceRegion.Width, sourceRegion.Height) * _scale * scale;
				_renderer2.DrawQuad(texture, color, position, ref _transformation,
					_layerDepth, size, sourceRegion,
					ref topLeft, ref topRight, ref bottomLeft, ref bottomRight);
			}
		}
	}

}
