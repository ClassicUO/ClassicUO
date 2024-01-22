#if PLATFORM_AGNOSTIC

// This code had been borrowed from https://www.monogame.net/

// MIT License - Copyright (C) The Mono.Xna Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Text;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Numerics;

namespace FontStashSharp
{
	/// <summary>
	/// Describes a 32-bit packed color.
	/// </summary>
	[DataContract]
	[DebuggerDisplay("{DebugDisplayString,nq}")]
	public struct FSColor : IEquatable<FSColor>
	{
		static FSColor()
		{
			Transparent = new FSColor(0);
			AliceBlue = new FSColor(0xfffff8f0);
			AntiqueWhite = new FSColor(0xffd7ebfa);
			Aqua = new FSColor(0xffffff00);
			Aquamarine = new FSColor(0xffd4ff7f);
			Azure = new FSColor(0xfffffff0);
			Beige = new FSColor(0xffdcf5f5);
			Bisque = new FSColor(0xffc4e4ff);
			Black = new FSColor(0xff000000);
			BlanchedAlmond = new FSColor(0xffcdebff);
			Blue = new FSColor(0xffff0000);
			BlueViolet = new FSColor(0xffe22b8a);
			Brown = new FSColor(0xff2a2aa5);
			BurlyWood = new FSColor(0xff87b8de);
			CadetBlue = new FSColor(0xffa09e5f);
			Chartreuse = new FSColor(0xff00ff7f);
			Chocolate = new FSColor(0xff1e69d2);
			Coral = new FSColor(0xff507fff);
			CornflowerBlue = new FSColor(0xffed9564);
			Cornsilk = new FSColor(0xffdcf8ff);
			Crimson = new FSColor(0xff3c14dc);
			Cyan = new FSColor(0xffffff00);
			DarkBlue = new FSColor(0xff8b0000);
			DarkCyan = new FSColor(0xff8b8b00);
			DarkGoldenrod = new FSColor(0xff0b86b8);
			DarkGray = new FSColor(0xffa9a9a9);
			DarkGreen = new FSColor(0xff006400);
			DarkKhaki = new FSColor(0xff6bb7bd);
			DarkMagenta = new FSColor(0xff8b008b);
			DarkOliveGreen = new FSColor(0xff2f6b55);
			DarkOrange = new FSColor(0xff008cff);
			DarkOrchid = new FSColor(0xffcc3299);
			DarkRed = new FSColor(0xff00008b);
			DarkSalmon = new FSColor(0xff7a96e9);
			DarkSeaGreen = new FSColor(0xff8bbc8f);
			DarkSlateBlue = new FSColor(0xff8b3d48);
			DarkSlateGray = new FSColor(0xff4f4f2f);
			DarkTurquoise = new FSColor(0xffd1ce00);
			DarkViolet = new FSColor(0xffd30094);
			DeepPink = new FSColor(0xff9314ff);
			DeepSkyBlue = new FSColor(0xffffbf00);
			DimGray = new FSColor(0xff696969);
			DodgerBlue = new FSColor(0xffff901e);
			Firebrick = new FSColor(0xff2222b2);
			FloralWhite = new FSColor(0xfff0faff);
			ForestGreen = new FSColor(0xff228b22);
			Fuchsia = new FSColor(0xffff00ff);
			Gainsboro = new FSColor(0xffdcdcdc);
			GhostWhite = new FSColor(0xfffff8f8);
			Gold = new FSColor(0xff00d7ff);
			Goldenrod = new FSColor(0xff20a5da);
			Gray = new FSColor(0xff808080);
			Green = new FSColor(0xff008000);
			GreenYellow = new FSColor(0xff2fffad);
			Honeydew = new FSColor(0xfff0fff0);
			HotPink = new FSColor(0xffb469ff);
			IndianRed = new FSColor(0xff5c5ccd);
			Indigo = new FSColor(0xff82004b);
			Ivory = new FSColor(0xfff0ffff);
			Khaki = new FSColor(0xff8ce6f0);
			Lavender = new FSColor(0xfffae6e6);
			LavenderBlush = new FSColor(0xfff5f0ff);
			LawnGreen = new FSColor(0xff00fc7c);
			LemonChiffon = new FSColor(0xffcdfaff);
			LightBlue = new FSColor(0xffe6d8ad);
			LightCoral = new FSColor(0xff8080f0);
			LightCyan = new FSColor(0xffffffe0);
			LightGoldenrodYellow = new FSColor(0xffd2fafa);
			LightGray = new FSColor(0xffd3d3d3);
			LightGreen = new FSColor(0xff90ee90);
			LightPink = new FSColor(0xffc1b6ff);
			LightSalmon = new FSColor(0xff7aa0ff);
			LightSeaGreen = new FSColor(0xffaab220);
			LightSkyBlue = new FSColor(0xffface87);
			LightSlateGray = new FSColor(0xff998877);
			LightSteelBlue = new FSColor(0xffdec4b0);
			LightYellow = new FSColor(0xffe0ffff);
			Lime = new FSColor(0xff00ff00);
			LimeGreen = new FSColor(0xff32cd32);
			Linen = new FSColor(0xffe6f0fa);
			Magenta = new FSColor(0xffff00ff);
			Maroon = new FSColor(0xff000080);
			MediumAquamarine = new FSColor(0xffaacd66);
			MediumBlue = new FSColor(0xffcd0000);
			MediumOrchid = new FSColor(0xffd355ba);
			MediumPurple = new FSColor(0xffdb7093);
			MediumSeaGreen = new FSColor(0xff71b33c);
			MediumSlateBlue = new FSColor(0xffee687b);
			MediumSpringGreen = new FSColor(0xff9afa00);
			MediumTurquoise = new FSColor(0xffccd148);
			MediumVioletRed = new FSColor(0xff8515c7);
			MidnightBlue = new FSColor(0xff701919);
			MintCream = new FSColor(0xfffafff5);
			MistyRose = new FSColor(0xffe1e4ff);
			Moccasin = new FSColor(0xffb5e4ff);
			MonoGameOrange = new FSColor(0xff003ce7);
			NavajoWhite = new FSColor(0xffaddeff);
			Navy = new FSColor(0xff800000);
			OldLace = new FSColor(0xffe6f5fd);
			Olive = new FSColor(0xff008080);
			OliveDrab = new FSColor(0xff238e6b);
			Orange = new FSColor(0xff00a5ff);
			OrangeRed = new FSColor(0xff0045ff);
			Orchid = new FSColor(0xffd670da);
			PaleGoldenrod = new FSColor(0xffaae8ee);
			PaleGreen = new FSColor(0xff98fb98);
			PaleTurquoise = new FSColor(0xffeeeeaf);
			PaleVioletRed = new FSColor(0xff9370db);
			PapayaWhip = new FSColor(0xffd5efff);
			PeachPuff = new FSColor(0xffb9daff);
			Peru = new FSColor(0xff3f85cd);
			Pink = new FSColor(0xffcbc0ff);
			Plum = new FSColor(0xffdda0dd);
			PowderBlue = new FSColor(0xffe6e0b0);
			Purple = new FSColor(0xff800080);
			Red = new FSColor(0xff0000ff);
			RosyBrown = new FSColor(0xff8f8fbc);
			RoyalBlue = new FSColor(0xffe16941);
			SaddleBrown = new FSColor(0xff13458b);
			Salmon = new FSColor(0xff7280fa);
			SandyBrown = new FSColor(0xff60a4f4);
			SeaGreen = new FSColor(0xff578b2e);
			SeaShell = new FSColor(0xffeef5ff);
			Sienna = new FSColor(0xff2d52a0);
			Silver = new FSColor(0xffc0c0c0);
			SkyBlue = new FSColor(0xffebce87);
			SlateBlue = new FSColor(0xffcd5a6a);
			SlateGray = new FSColor(0xff908070);
			Snow = new FSColor(0xfffafaff);
			SpringGreen = new FSColor(0xff7fff00);
			SteelBlue = new FSColor(0xffb48246);
			Tan = new FSColor(0xff8cb4d2);
			Teal = new FSColor(0xff808000);
			Thistle = new FSColor(0xffd8bfd8);
			Tomato = new FSColor(0xff4763ff);
			Turquoise = new FSColor(0xffd0e040);
			Violet = new FSColor(0xffee82ee);
			Wheat = new FSColor(0xffb3def5);
			White = new FSColor(uint.MaxValue);
			WhiteSmoke = new FSColor(0xfff5f5f5);
			Yellow = new FSColor(0xff00ffff);
			YellowGreen = new FSColor(0xff32cd9a);
		}

		// Stored as RGBA with R in the least significant octet:
		// |-------|-------|-------|-------
		// A       B       G       R
		private uint _packedValue;

		/// <summary>
		/// Constructs an RGBA color from a packed value.
		/// The value is a 32-bit unsigned integer, with R in the least significant octet.
		/// </summary>
		/// <param name="packedValue">The packed value.</param>
		public FSColor(uint packedValue)
		{
			_packedValue = packedValue;
		}

		/// <summary>
		/// Constructs an RGBA color from the XYZW unit length components of a vector.
		/// </summary>
		/// <param name="color">A <see cref="Vector4"/> representing color.</param>
		public FSColor(Vector4 color)
				: this((int)(color.X * 255), (int)(color.Y * 255), (int)(color.Z * 255), (int)(color.W * 255))
		{
		}

		/// <summary>
		/// Constructs an RGBA color from the XYZ unit length components of a vector. Alpha value will be opaque.
		/// </summary>
		/// <param name="color">A <see cref="Vector3"/> representing color.</param>
		public FSColor(Vector3 color)
				: this((int)(color.X * 255), (int)(color.Y * 255), (int)(color.Z * 255))
		{
		}

		/// <summary>
		/// Constructs an RGBA color from a <see cref="FSColor"/> and an alpha value.
		/// </summary>
		/// <param name="color">A <see cref="FSColor"/> for RGB values of new <see cref="FSColor"/> instance.</param>
		/// <param name="alpha">The alpha component value from 0 to 255.</param>
		public FSColor(FSColor color, int alpha)
		{
			if ((alpha & 0xFFFFFF00) != 0)
			{
				var clampedA = (uint)Utility.Clamp(alpha, byte.MinValue, byte.MaxValue);

				_packedValue = (color._packedValue & 0x00FFFFFF) | (clampedA << 24);
			}
			else
			{
				_packedValue = (color._packedValue & 0x00FFFFFF) | ((uint)alpha << 24);
			}
		}

		/// <summary>
		/// Constructs an RGBA color from color and alpha value.
		/// </summary>
		/// <param name="color">A <see cref="FSColor"/> for RGB values of new <see cref="FSColor"/> instance.</param>
		/// <param name="alpha">Alpha component value from 0.0f to 1.0f.</param>
		public FSColor(FSColor color, float alpha) :
				this(color, (int)(alpha * 255))
		{
		}

		/// <summary>
		/// Constructs an RGBA color from scalars representing red, green and blue values. Alpha value will be opaque.
		/// </summary>
		/// <param name="r">Red component value from 0.0f to 1.0f.</param>
		/// <param name="g">Green component value from 0.0f to 1.0f.</param>
		/// <param name="b">Blue component value from 0.0f to 1.0f.</param>
		public FSColor(float r, float g, float b)
				: this((int)(r * 255), (int)(g * 255), (int)(b * 255))
		{
		}

		/// <summary>
		/// Constructs an RGBA color from scalars representing red, green, blue and alpha values.
		/// </summary>
		/// <param name="r">Red component value from 0.0f to 1.0f.</param>
		/// <param name="g">Green component value from 0.0f to 1.0f.</param>
		/// <param name="b">Blue component value from 0.0f to 1.0f.</param>
		/// <param name="alpha">Alpha component value from 0.0f to 1.0f.</param>
		public FSColor(float r, float g, float b, float alpha)
				: this((int)(r * 255), (int)(g * 255), (int)(b * 255), (int)(alpha * 255))
		{
		}

		/// <summary>
		/// Constructs an RGBA color from scalars representing red, green and blue values. Alpha value will be opaque.
		/// </summary>
		/// <param name="r">Red component value from 0 to 255.</param>
		/// <param name="g">Green component value from 0 to 255.</param>
		/// <param name="b">Blue component value from 0 to 255.</param>
		public FSColor(int r, int g, int b)
		{
			_packedValue = 0xFF000000; // A = 255

			if (((r | g | b) & 0xFFFFFF00) != 0)
			{
				var clampedR = (uint)Utility.Clamp(r, Byte.MinValue, Byte.MaxValue);
				var clampedG = (uint)Utility.Clamp(g, Byte.MinValue, Byte.MaxValue);
				var clampedB = (uint)Utility.Clamp(b, Byte.MinValue, Byte.MaxValue);

				_packedValue |= (clampedB << 16) | (clampedG << 8) | (clampedR);
			}
			else
			{
				_packedValue |= ((uint)b << 16) | ((uint)g << 8) | ((uint)r);
			}
		}

		/// <summary>
		/// Constructs an RGBA color from scalars representing red, green, blue and alpha values.
		/// </summary>
		/// <param name="r">Red component value from 0 to 255.</param>
		/// <param name="g">Green component value from 0 to 255.</param>
		/// <param name="b">Blue component value from 0 to 255.</param>
		/// <param name="alpha">Alpha component value from 0 to 255.</param>
		public FSColor(int r, int g, int b, int alpha)
		{
			if (((r | g | b | alpha) & 0xFFFFFF00) != 0)
			{
				var clampedR = (uint)Utility.Clamp(r, Byte.MinValue, Byte.MaxValue);
				var clampedG = (uint)Utility.Clamp(g, Byte.MinValue, Byte.MaxValue);
				var clampedB = (uint)Utility.Clamp(b, Byte.MinValue, Byte.MaxValue);
				var clampedA = (uint)Utility.Clamp(alpha, Byte.MinValue, Byte.MaxValue);

				_packedValue = (clampedA << 24) | (clampedB << 16) | (clampedG << 8) | (clampedR);
			}
			else
			{
				_packedValue = ((uint)alpha << 24) | ((uint)b << 16) | ((uint)g << 8) | ((uint)r);
			}
		}

		/// <summary>
		/// Constructs an RGBA color from scalars representing red, green, blue and alpha values.
		/// </summary>
		/// <remarks>
		/// This overload sets the values directly without clamping, and may therefore be faster than the other overloads.
		/// </remarks>
		/// <param name="r"></param>
		/// <param name="g"></param>
		/// <param name="b"></param>
		/// <param name="alpha"></param>
		public FSColor(byte r, byte g, byte b, byte alpha)
		{
			_packedValue = ((uint)alpha << 24) | ((uint)b << 16) | ((uint)g << 8) | (r);
		}

		/// <summary>
		/// Gets or sets the blue component.
		/// </summary>
		[DataMember]
		public byte B
		{
			get
			{
				unchecked
				{
					return (byte)(this._packedValue >> 16);
				}
			}
			set
			{
				this._packedValue = (this._packedValue & 0xff00ffff) | ((uint)value << 16);
			}
		}

		/// <summary>
		/// Gets or sets the green component.
		/// </summary>
		[DataMember]
		public byte G
		{
			get
			{
				unchecked
				{
					return (byte)(this._packedValue >> 8);
				}
			}
			set
			{
				this._packedValue = (this._packedValue & 0xffff00ff) | ((uint)value << 8);
			}
		}

		/// <summary>
		/// Gets or sets the red component.
		/// </summary>
		[DataMember]
		public byte R
		{
			get
			{
				unchecked
				{
					return (byte)this._packedValue;
				}
			}
			set
			{
				this._packedValue = (this._packedValue & 0xffffff00) | value;
			}
		}

		/// <summary>
		/// Gets or sets the alpha component.
		/// </summary>
		[DataMember]
		public byte A
		{
			get
			{
				unchecked
				{
					return (byte)(this._packedValue >> 24);
				}
			}
			set
			{
				this._packedValue = (this._packedValue & 0x00ffffff) | ((uint)value << 24);
			}
		}

		/// <summary>
		/// Compares whether two <see cref="FSColor"/> instances are equal.
		/// </summary>
		/// <param name="a"><see cref="FSColor"/> instance on the left of the equal sign.</param>
		/// <param name="b"><see cref="FSColor"/> instance on the right of the equal sign.</param>
		/// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
		public static bool operator ==(FSColor a, FSColor b)
		{
			return (a._packedValue == b._packedValue);
		}

		/// <summary>
		/// Compares whether two <see cref="FSColor"/> instances are not equal.
		/// </summary>
		/// <param name="a"><see cref="FSColor"/> instance on the left of the not equal sign.</param>
		/// <param name="b"><see cref="FSColor"/> instance on the right of the not equal sign.</param>
		/// <returns><c>true</c> if the instances are not equal; <c>false</c> otherwise.</returns>	
		public static bool operator !=(FSColor a, FSColor b)
		{
			return (a._packedValue != b._packedValue);
		}

		/// <summary>
		/// Gets the hash code of this <see cref="FSColor"/>.
		/// </summary>
		/// <returns>Hash code of this <see cref="FSColor"/>.</returns>
		public override int GetHashCode()
		{
			return this._packedValue.GetHashCode();
		}

		/// <summary>
		/// Compares whether current instance is equal to specified object.
		/// </summary>
		/// <param name="obj">The <see cref="FSColor"/> to compare.</param>
		/// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
		public override bool Equals(object obj)
		{
			return ((obj is FSColor) && this.Equals((FSColor)obj));
		}

		#region RgbaColor32 Bank

		/// <summary>
		/// Transparent color (R:0,G:0,B:0,A:0).
		/// </summary>
		public static FSColor Transparent
		{
			get;
			private set;
		}

		/// <summary>
		/// AliceBlue color (R:240,G:248,B:255,A:255).
		/// </summary>
		public static FSColor AliceBlue
		{
			get;
			private set;
		}

		/// <summary>
		/// AntiqueWhite color (R:250,G:235,B:215,A:255).
		/// </summary>
		public static FSColor AntiqueWhite
		{
			get;
			private set;
		}

		/// <summary>
		/// Aqua color (R:0,G:255,B:255,A:255).
		/// </summary>
		public static FSColor Aqua
		{
			get;
			private set;
		}

		/// <summary>
		/// Aquamarine color (R:127,G:255,B:212,A:255).
		/// </summary>
		public static FSColor Aquamarine
		{
			get;
			private set;
		}

		/// <summary>
		/// Azure color (R:240,G:255,B:255,A:255).
		/// </summary>
		public static FSColor Azure
		{
			get;
			private set;
		}

		/// <summary>
		/// Beige color (R:245,G:245,B:220,A:255).
		/// </summary>
		public static FSColor Beige
		{
			get;
			private set;
		}

		/// <summary>
		/// Bisque color (R:255,G:228,B:196,A:255).
		/// </summary>
		public static FSColor Bisque
		{
			get;
			private set;
		}

		/// <summary>
		/// Black color (R:0,G:0,B:0,A:255).
		/// </summary>
		public static FSColor Black
		{
			get;
			private set;
		}

		/// <summary>
		/// BlanchedAlmond color (R:255,G:235,B:205,A:255).
		/// </summary>
		public static FSColor BlanchedAlmond
		{
			get;
			private set;
		}

		/// <summary>
		/// Blue color (R:0,G:0,B:255,A:255).
		/// </summary>
		public static FSColor Blue
		{
			get;
			private set;
		}

		/// <summary>
		/// BlueViolet color (R:138,G:43,B:226,A:255).
		/// </summary>
		public static FSColor BlueViolet
		{
			get;
			private set;
		}

		/// <summary>
		/// Brown color (R:165,G:42,B:42,A:255).
		/// </summary>
		public static FSColor Brown
		{
			get;
			private set;
		}

		/// <summary>
		/// BurlyWood color (R:222,G:184,B:135,A:255).
		/// </summary>
		public static FSColor BurlyWood
		{
			get;
			private set;
		}

		/// <summary>
		/// CadetBlue color (R:95,G:158,B:160,A:255).
		/// </summary>
		public static FSColor CadetBlue
		{
			get;
			private set;
		}

		/// <summary>
		/// Chartreuse color (R:127,G:255,B:0,A:255).
		/// </summary>
		public static FSColor Chartreuse
		{
			get;
			private set;
		}

		/// <summary>
		/// Chocolate color (R:210,G:105,B:30,A:255).
		/// </summary>
		public static FSColor Chocolate
		{
			get;
			private set;
		}

		/// <summary>
		/// Coral color (R:255,G:127,B:80,A:255).
		/// </summary>
		public static FSColor Coral
		{
			get;
			private set;
		}

		/// <summary>
		/// CornflowerBlue color (R:100,G:149,B:237,A:255).
		/// </summary>
		public static FSColor CornflowerBlue
		{
			get;
			private set;
		}

		/// <summary>
		/// Cornsilk color (R:255,G:248,B:220,A:255).
		/// </summary>
		public static FSColor Cornsilk
		{
			get;
			private set;
		}

		/// <summary>
		/// Crimson color (R:220,G:20,B:60,A:255).
		/// </summary>
		public static FSColor Crimson
		{
			get;
			private set;
		}

		/// <summary>
		/// Cyan color (R:0,G:255,B:255,A:255).
		/// </summary>
		public static FSColor Cyan
		{
			get;
			private set;
		}

		/// <summary>
		/// DarkBlue color (R:0,G:0,B:139,A:255).
		/// </summary>
		public static FSColor DarkBlue
		{
			get;
			private set;
		}

		/// <summary>
		/// DarkCyan color (R:0,G:139,B:139,A:255).
		/// </summary>
		public static FSColor DarkCyan
		{
			get;
			private set;
		}

		/// <summary>
		/// DarkGoldenrod color (R:184,G:134,B:11,A:255).
		/// </summary>
		public static FSColor DarkGoldenrod
		{
			get;
			private set;
		}

		/// <summary>
		/// DarkGray color (R:169,G:169,B:169,A:255).
		/// </summary>
		public static FSColor DarkGray
		{
			get;
			private set;
		}

		/// <summary>
		/// DarkGreen color (R:0,G:100,B:0,A:255).
		/// </summary>
		public static FSColor DarkGreen
		{
			get;
			private set;
		}

		/// <summary>
		/// DarkKhaki color (R:189,G:183,B:107,A:255).
		/// </summary>
		public static FSColor DarkKhaki
		{
			get;
			private set;
		}

		/// <summary>
		/// DarkMagenta color (R:139,G:0,B:139,A:255).
		/// </summary>
		public static FSColor DarkMagenta
		{
			get;
			private set;
		}

		/// <summary>
		/// DarkOliveGreen color (R:85,G:107,B:47,A:255).
		/// </summary>
		public static FSColor DarkOliveGreen
		{
			get;
			private set;
		}

		/// <summary>
		/// DarkOrange color (R:255,G:140,B:0,A:255).
		/// </summary>
		public static FSColor DarkOrange
		{
			get;
			private set;
		}

		/// <summary>
		/// DarkOrchid color (R:153,G:50,B:204,A:255).
		/// </summary>
		public static FSColor DarkOrchid
		{
			get;
			private set;
		}

		/// <summary>
		/// DarkRed color (R:139,G:0,B:0,A:255).
		/// </summary>
		public static FSColor DarkRed
		{
			get;
			private set;
		}

		/// <summary>
		/// DarkSalmon color (R:233,G:150,B:122,A:255).
		/// </summary>
		public static FSColor DarkSalmon
		{
			get;
			private set;
		}

		/// <summary>
		/// DarkSeaGreen color (R:143,G:188,B:139,A:255).
		/// </summary>
		public static FSColor DarkSeaGreen
		{
			get;
			private set;
		}

		/// <summary>
		/// DarkSlateBlue color (R:72,G:61,B:139,A:255).
		/// </summary>
		public static FSColor DarkSlateBlue
		{
			get;
			private set;
		}

		/// <summary>
		/// DarkSlateGray color (R:47,G:79,B:79,A:255).
		/// </summary>
		public static FSColor DarkSlateGray
		{
			get;
			private set;
		}

		/// <summary>
		/// DarkTurquoise color (R:0,G:206,B:209,A:255).
		/// </summary>
		public static FSColor DarkTurquoise
		{
			get;
			private set;
		}

		/// <summary>
		/// DarkViolet color (R:148,G:0,B:211,A:255).
		/// </summary>
		public static FSColor DarkViolet
		{
			get;
			private set;
		}

		/// <summary>
		/// DeepPink color (R:255,G:20,B:147,A:255).
		/// </summary>
		public static FSColor DeepPink
		{
			get;
			private set;
		}

		/// <summary>
		/// DeepSkyBlue color (R:0,G:191,B:255,A:255).
		/// </summary>
		public static FSColor DeepSkyBlue
		{
			get;
			private set;
		}

		/// <summary>
		/// DimGray color (R:105,G:105,B:105,A:255).
		/// </summary>
		public static FSColor DimGray
		{
			get;
			private set;
		}

		/// <summary>
		/// DodgerBlue color (R:30,G:144,B:255,A:255).
		/// </summary>
		public static FSColor DodgerBlue
		{
			get;
			private set;
		}

		/// <summary>
		/// Firebrick color (R:178,G:34,B:34,A:255).
		/// </summary>
		public static FSColor Firebrick
		{
			get;
			private set;
		}

		/// <summary>
		/// FloralWhite color (R:255,G:250,B:240,A:255).
		/// </summary>
		public static FSColor FloralWhite
		{
			get;
			private set;
		}

		/// <summary>
		/// ForestGreen color (R:34,G:139,B:34,A:255).
		/// </summary>
		public static FSColor ForestGreen
		{
			get;
			private set;
		}

		/// <summary>
		/// Fuchsia color (R:255,G:0,B:255,A:255).
		/// </summary>
		public static FSColor Fuchsia
		{
			get;
			private set;
		}

		/// <summary>
		/// Gainsboro color (R:220,G:220,B:220,A:255).
		/// </summary>
		public static FSColor Gainsboro
		{
			get;
			private set;
		}

		/// <summary>
		/// GhostWhite color (R:248,G:248,B:255,A:255).
		/// </summary>
		public static FSColor GhostWhite
		{
			get;
			private set;
		}
		/// <summary>
		/// Gold color (R:255,G:215,B:0,A:255).
		/// </summary>
		public static FSColor Gold
		{
			get;
			private set;
		}

		/// <summary>
		/// Goldenrod color (R:218,G:165,B:32,A:255).
		/// </summary>
		public static FSColor Goldenrod
		{
			get;
			private set;
		}

		/// <summary>
		/// Gray color (R:128,G:128,B:128,A:255).
		/// </summary>
		public static FSColor Gray
		{
			get;
			private set;
		}

		/// <summary>
		/// Green color (R:0,G:128,B:0,A:255).
		/// </summary>
		public static FSColor Green
		{
			get;
			private set;
		}

		/// <summary>
		/// GreenYellow color (R:173,G:255,B:47,A:255).
		/// </summary>
		public static FSColor GreenYellow
		{
			get;
			private set;
		}

		/// <summary>
		/// Honeydew color (R:240,G:255,B:240,A:255).
		/// </summary>
		public static FSColor Honeydew
		{
			get;
			private set;
		}

		/// <summary>
		/// HotPink color (R:255,G:105,B:180,A:255).
		/// </summary>
		public static FSColor HotPink
		{
			get;
			private set;
		}

		/// <summary>
		/// IndianRed color (R:205,G:92,B:92,A:255).
		/// </summary>
		public static FSColor IndianRed
		{
			get;
			private set;
		}

		/// <summary>
		/// Indigo color (R:75,G:0,B:130,A:255).
		/// </summary>
		public static FSColor Indigo
		{
			get;
			private set;
		}

		/// <summary>
		/// Ivory color (R:255,G:255,B:240,A:255).
		/// </summary>
		public static FSColor Ivory
		{
			get;
			private set;
		}

		/// <summary>
		/// Khaki color (R:240,G:230,B:140,A:255).
		/// </summary>
		public static FSColor Khaki
		{
			get;
			private set;
		}

		/// <summary>
		/// Lavender color (R:230,G:230,B:250,A:255).
		/// </summary>
		public static FSColor Lavender
		{
			get;
			private set;
		}

		/// <summary>
		/// LavenderBlush color (R:255,G:240,B:245,A:255).
		/// </summary>
		public static FSColor LavenderBlush
		{
			get;
			private set;
		}

		/// <summary>
		/// LawnGreen color (R:124,G:252,B:0,A:255).
		/// </summary>
		public static FSColor LawnGreen
		{
			get;
			private set;
		}

		/// <summary>
		/// LemonChiffon color (R:255,G:250,B:205,A:255).
		/// </summary>
		public static FSColor LemonChiffon
		{
			get;
			private set;
		}

		/// <summary>
		/// LightBlue color (R:173,G:216,B:230,A:255).
		/// </summary>
		public static FSColor LightBlue
		{
			get;
			private set;
		}

		/// <summary>
		/// LightCoral color (R:240,G:128,B:128,A:255).
		/// </summary>
		public static FSColor LightCoral
		{
			get;
			private set;
		}

		/// <summary>
		/// LightCyan color (R:224,G:255,B:255,A:255).
		/// </summary>
		public static FSColor LightCyan
		{
			get;
			private set;
		}

		/// <summary>
		/// LightGoldenrodYellow color (R:250,G:250,B:210,A:255).
		/// </summary>
		public static FSColor LightGoldenrodYellow
		{
			get;
			private set;
		}

		/// <summary>
		/// LightGray color (R:211,G:211,B:211,A:255).
		/// </summary>
		public static FSColor LightGray
		{
			get;
			private set;
		}

		/// <summary>
		/// LightGreen color (R:144,G:238,B:144,A:255).
		/// </summary>
		public static FSColor LightGreen
		{
			get;
			private set;
		}

		/// <summary>
		/// LightPink color (R:255,G:182,B:193,A:255).
		/// </summary>
		public static FSColor LightPink
		{
			get;
			private set;
		}

		/// <summary>
		/// LightSalmon color (R:255,G:160,B:122,A:255).
		/// </summary>
		public static FSColor LightSalmon
		{
			get;
			private set;
		}

		/// <summary>
		/// LightSeaGreen color (R:32,G:178,B:170,A:255).
		/// </summary>
		public static FSColor LightSeaGreen
		{
			get;
			private set;
		}

		/// <summary>
		/// LightSkyBlue color (R:135,G:206,B:250,A:255).
		/// </summary>
		public static FSColor LightSkyBlue
		{
			get;
			private set;
		}

		/// <summary>
		/// LightSlateGray color (R:119,G:136,B:153,A:255).
		/// </summary>
		public static FSColor LightSlateGray
		{
			get;
			private set;
		}

		/// <summary>
		/// LightSteelBlue color (R:176,G:196,B:222,A:255).
		/// </summary>
		public static FSColor LightSteelBlue
		{
			get;
			private set;
		}

		/// <summary>
		/// LightYellow color (R:255,G:255,B:224,A:255).
		/// </summary>
		public static FSColor LightYellow
		{
			get;
			private set;
		}

		/// <summary>
		/// Lime color (R:0,G:255,B:0,A:255).
		/// </summary>
		public static FSColor Lime
		{
			get;
			private set;
		}

		/// <summary>
		/// LimeGreen color (R:50,G:205,B:50,A:255).
		/// </summary>
		public static FSColor LimeGreen
		{
			get;
			private set;
		}

		/// <summary>
		/// Linen color (R:250,G:240,B:230,A:255).
		/// </summary>
		public static FSColor Linen
		{
			get;
			private set;
		}

		/// <summary>
		/// Magenta color (R:255,G:0,B:255,A:255).
		/// </summary>
		public static FSColor Magenta
		{
			get;
			private set;
		}

		/// <summary>
		/// Maroon color (R:128,G:0,B:0,A:255).
		/// </summary>
		public static FSColor Maroon
		{
			get;
			private set;
		}

		/// <summary>
		/// MediumAquamarine color (R:102,G:205,B:170,A:255).
		/// </summary>
		public static FSColor MediumAquamarine
		{
			get;
			private set;
		}

		/// <summary>
		/// MediumBlue color (R:0,G:0,B:205,A:255).
		/// </summary>
		public static FSColor MediumBlue
		{
			get;
			private set;
		}

		/// <summary>
		/// MediumOrchid color (R:186,G:85,B:211,A:255).
		/// </summary>
		public static FSColor MediumOrchid
		{
			get;
			private set;
		}

		/// <summary>
		/// MediumPurple color (R:147,G:112,B:219,A:255).
		/// </summary>
		public static FSColor MediumPurple
		{
			get;
			private set;
		}

		/// <summary>
		/// MediumSeaGreen color (R:60,G:179,B:113,A:255).
		/// </summary>
		public static FSColor MediumSeaGreen
		{
			get;
			private set;
		}

		/// <summary>
		/// MediumSlateBlue color (R:123,G:104,B:238,A:255).
		/// </summary>
		public static FSColor MediumSlateBlue
		{
			get;
			private set;
		}

		/// <summary>
		/// MediumSpringGreen color (R:0,G:250,B:154,A:255).
		/// </summary>
		public static FSColor MediumSpringGreen
		{
			get;
			private set;
		}

		/// <summary>
		/// MediumTurquoise color (R:72,G:209,B:204,A:255).
		/// </summary>
		public static FSColor MediumTurquoise
		{
			get;
			private set;
		}

		/// <summary>
		/// MediumVioletRed color (R:199,G:21,B:133,A:255).
		/// </summary>
		public static FSColor MediumVioletRed
		{
			get;
			private set;
		}

		/// <summary>
		/// MidnightBlue color (R:25,G:25,B:112,A:255).
		/// </summary>
		public static FSColor MidnightBlue
		{
			get;
			private set;
		}

		/// <summary>
		/// MintCream color (R:245,G:255,B:250,A:255).
		/// </summary>
		public static FSColor MintCream
		{
			get;
			private set;
		}

		/// <summary>
		/// MistyRose color (R:255,G:228,B:225,A:255).
		/// </summary>
		public static FSColor MistyRose
		{
			get;
			private set;
		}

		/// <summary>
		/// Moccasin color (R:255,G:228,B:181,A:255).
		/// </summary>
		public static FSColor Moccasin
		{
			get;
			private set;
		}

		/// <summary>
		/// MonoGame orange theme color (R:231,G:60,B:0,A:255).
		/// </summary>
		public static FSColor MonoGameOrange
		{
			get;
			private set;
		}

		/// <summary>
		/// NavajoWhite color (R:255,G:222,B:173,A:255).
		/// </summary>
		public static FSColor NavajoWhite
		{
			get;
			private set;
		}

		/// <summary>
		/// Navy color (R:0,G:0,B:128,A:255).
		/// </summary>
		public static FSColor Navy
		{
			get;
			private set;
		}

		/// <summary>
		/// OldLace color (R:253,G:245,B:230,A:255).
		/// </summary>
		public static FSColor OldLace
		{
			get;
			private set;
		}

		/// <summary>
		/// Olive color (R:128,G:128,B:0,A:255).
		/// </summary>
		public static FSColor Olive
		{
			get;
			private set;
		}

		/// <summary>
		/// OliveDrab color (R:107,G:142,B:35,A:255).
		/// </summary>
		public static FSColor OliveDrab
		{
			get;
			private set;
		}

		/// <summary>
		/// Orange color (R:255,G:165,B:0,A:255).
		/// </summary>
		public static FSColor Orange
		{
			get;
			private set;
		}

		/// <summary>
		/// OrangeRed color (R:255,G:69,B:0,A:255).
		/// </summary>
		public static FSColor OrangeRed
		{
			get;
			private set;
		}

		/// <summary>
		/// Orchid color (R:218,G:112,B:214,A:255).
		/// </summary>
		public static FSColor Orchid
		{
			get;
			private set;
		}

		/// <summary>
		/// PaleGoldenrod color (R:238,G:232,B:170,A:255).
		/// </summary>
		public static FSColor PaleGoldenrod
		{
			get;
			private set;
		}

		/// <summary>
		/// PaleGreen color (R:152,G:251,B:152,A:255).
		/// </summary>
		public static FSColor PaleGreen
		{
			get;
			private set;
		}

		/// <summary>
		/// PaleTurquoise color (R:175,G:238,B:238,A:255).
		/// </summary>
		public static FSColor PaleTurquoise
		{
			get;
			private set;
		}
		/// <summary>
		/// PaleVioletRed color (R:219,G:112,B:147,A:255).
		/// </summary>
		public static FSColor PaleVioletRed
		{
			get;
			private set;
		}

		/// <summary>
		/// PapayaWhip color (R:255,G:239,B:213,A:255).
		/// </summary>
		public static FSColor PapayaWhip
		{
			get;
			private set;
		}

		/// <summary>
		/// PeachPuff color (R:255,G:218,B:185,A:255).
		/// </summary>
		public static FSColor PeachPuff
		{
			get;
			private set;
		}

		/// <summary>
		/// Peru color (R:205,G:133,B:63,A:255).
		/// </summary>
		public static FSColor Peru
		{
			get;
			private set;
		}

		/// <summary>
		/// Pink color (R:255,G:192,B:203,A:255).
		/// </summary>
		public static FSColor Pink
		{
			get;
			private set;
		}

		/// <summary>
		/// Plum color (R:221,G:160,B:221,A:255).
		/// </summary>
		public static FSColor Plum
		{
			get;
			private set;
		}

		/// <summary>
		/// PowderBlue color (R:176,G:224,B:230,A:255).
		/// </summary>
		public static FSColor PowderBlue
		{
			get;
			private set;
		}

		/// <summary>
		///  Purple color (R:128,G:0,B:128,A:255).
		/// </summary>
		public static FSColor Purple
		{
			get;
			private set;
		}

		/// <summary>
		/// Red color (R:255,G:0,B:0,A:255).
		/// </summary>
		public static FSColor Red
		{
			get;
			private set;
		}

		/// <summary>
		/// RosyBrown color (R:188,G:143,B:143,A:255).
		/// </summary>
		public static FSColor RosyBrown
		{
			get;
			private set;
		}

		/// <summary>
		/// RoyalBlue color (R:65,G:105,B:225,A:255).
		/// </summary>
		public static FSColor RoyalBlue
		{
			get;
			private set;
		}

		/// <summary>
		/// SaddleBrown color (R:139,G:69,B:19,A:255).
		/// </summary>
		public static FSColor SaddleBrown
		{
			get;
			private set;
		}

		/// <summary>
		/// Salmon color (R:250,G:128,B:114,A:255).
		/// </summary>
		public static FSColor Salmon
		{
			get;
			private set;
		}

		/// <summary>
		/// SandyBrown color (R:244,G:164,B:96,A:255).
		/// </summary>
		public static FSColor SandyBrown
		{
			get;
			private set;
		}

		/// <summary>
		/// SeaGreen color (R:46,G:139,B:87,A:255).
		/// </summary>
		public static FSColor SeaGreen
		{
			get;
			private set;
		}

		/// <summary>
		/// SeaShell color (R:255,G:245,B:238,A:255).
		/// </summary>
		public static FSColor SeaShell
		{
			get;
			private set;
		}

		/// <summary>
		/// Sienna color (R:160,G:82,B:45,A:255).
		/// </summary>
		public static FSColor Sienna
		{
			get;
			private set;
		}

		/// <summary>
		/// Silver color (R:192,G:192,B:192,A:255).
		/// </summary>
		public static FSColor Silver
		{
			get;
			private set;
		}

		/// <summary>
		/// SkyBlue color (R:135,G:206,B:235,A:255).
		/// </summary>
		public static FSColor SkyBlue
		{
			get;
			private set;
		}

		/// <summary>
		/// SlateBlue color (R:106,G:90,B:205,A:255).
		/// </summary>
		public static FSColor SlateBlue
		{
			get;
			private set;
		}

		/// <summary>
		/// SlateGray color (R:112,G:128,B:144,A:255).
		/// </summary>
		public static FSColor SlateGray
		{
			get;
			private set;
		}

		/// <summary>
		/// Snow color (R:255,G:250,B:250,A:255).
		/// </summary>
		public static FSColor Snow
		{
			get;
			private set;
		}

		/// <summary>
		/// SpringGreen color (R:0,G:255,B:127,A:255).
		/// </summary>
		public static FSColor SpringGreen
		{
			get;
			private set;
		}

		/// <summary>
		/// SteelBlue color (R:70,G:130,B:180,A:255).
		/// </summary>
		public static FSColor SteelBlue
		{
			get;
			private set;
		}

		/// <summary>
		/// Tan color (R:210,G:180,B:140,A:255).
		/// </summary>
		public static FSColor Tan
		{
			get;
			private set;
		}

		/// <summary>
		/// Teal color (R:0,G:128,B:128,A:255).
		/// </summary>
		public static FSColor Teal
		{
			get;
			private set;
		}

		/// <summary>
		/// Thistle color (R:216,G:191,B:216,A:255).
		/// </summary>
		public static FSColor Thistle
		{
			get;
			private set;
		}

		/// <summary>
		/// Tomato color (R:255,G:99,B:71,A:255).
		/// </summary>
		public static FSColor Tomato
		{
			get;
			private set;
		}

		/// <summary>
		/// Turquoise color (R:64,G:224,B:208,A:255).
		/// </summary>
		public static FSColor Turquoise
		{
			get;
			private set;
		}

		/// <summary>
		/// Violet color (R:238,G:130,B:238,A:255).
		/// </summary>
		public static FSColor Violet
		{
			get;
			private set;
		}

		/// <summary>
		/// Wheat color (R:245,G:222,B:179,A:255).
		/// </summary>
		public static FSColor Wheat
		{
			get;
			private set;
		}

		/// <summary>
		/// White color (R:255,G:255,B:255,A:255).
		/// </summary>
		public static FSColor White
		{
			get;
			private set;
		}

		/// <summary>
		/// WhiteSmoke color (R:245,G:245,B:245,A:255).
		/// </summary>
		public static FSColor WhiteSmoke
		{
			get;
			private set;
		}

		/// <summary>
		/// Yellow color (R:255,G:255,B:0,A:255).
		/// </summary>
		public static FSColor Yellow
		{
			get;
			private set;
		}

		/// <summary>
		/// YellowGreen color (R:154,G:205,B:50,A:255).
		/// </summary>
		public static FSColor YellowGreen
		{
			get;
			private set;
		}
		#endregion

		/// <summary>
		/// Performs linear interpolation of <see cref="FSColor"/>.
		/// </summary>
		/// <param name="value1">Source <see cref="FSColor"/>.</param>
		/// <param name="value2">Destination <see cref="FSColor"/>.</param>
		/// <param name="amount">Interpolation factor.</param>
		/// <returns>Interpolated <see cref="FSColor"/>.</returns>
		public static FSColor Lerp(FSColor value1, FSColor value2, Single amount)
		{
			amount = Utility.Clamp(amount, 0, 1);
			return new FSColor(
					(int)Utility.Lerp(value1.R, value2.R, amount),
					(int)Utility.Lerp(value1.G, value2.G, amount),
					(int)Utility.Lerp(value1.B, value2.B, amount),
					(int)Utility.Lerp(value1.A, value2.A, amount));
		}

		/// <summary>
		/// <see cref="FSColor.Lerp"/> should be used instead of this function.
		/// </summary>
		/// <returns>Interpolated <see cref="FSColor"/>.</returns>
		[Obsolete("RgbaColor32.Lerp should be used instead of this function.")]
		public static FSColor LerpPrecise(FSColor value1, FSColor value2, Single amount)
		{
			amount = Utility.Clamp(amount, 0, 1);
			return new FSColor(
					(int)Utility.LerpPrecise(value1.R, value2.R, amount),
					(int)Utility.LerpPrecise(value1.G, value2.G, amount),
					(int)Utility.LerpPrecise(value1.B, value2.B, amount),
					(int)Utility.LerpPrecise(value1.A, value2.A, amount));
		}

		/// <summary>
		/// Multiply <see cref="FSColor"/> by value.
		/// </summary>
		/// <param name="value">Source <see cref="FSColor"/>.</param>
		/// <param name="scale">Multiplicator.</param>
		/// <returns>Multiplication result.</returns>
		public static FSColor Multiply(FSColor value, float scale)
		{
			return new FSColor((int)(value.R * scale), (int)(value.G * scale), (int)(value.B * scale), (int)(value.A * scale));
		}

		/// <summary>
		/// Multiply <see cref="FSColor"/> by value.
		/// </summary>
		/// <param name="value">Source <see cref="FSColor"/>.</param>
		/// <param name="scale">Multiplicator.</param>
		/// <returns>Multiplication result.</returns>
		public static FSColor operator *(FSColor value, float scale)
		{
			return new FSColor((int)(value.R * scale), (int)(value.G * scale), (int)(value.B * scale), (int)(value.A * scale));
		}

		public static FSColor operator *(float scale, FSColor value)
		{
			return new FSColor((int)(value.R * scale), (int)(value.G * scale), (int)(value.B * scale), (int)(value.A * scale));
		}

		/// <summary>
		/// Gets a <see cref="Vector3"/> representation for this object.
		/// </summary>
		/// <returns>A <see cref="Vector3"/> representation for this object.</returns>
		public Vector3 ToVector3()
		{
			return new Vector3(R / 255.0f, G / 255.0f, B / 255.0f);
		}

		/// <summary>
		/// Gets a <see cref="Vector4"/> representation for this object.
		/// </summary>
		/// <returns>A <see cref="Vector4"/> representation for this object.</returns>
		public Vector4 ToVector4()
		{
			return new Vector4(R / 255.0f, G / 255.0f, B / 255.0f, A / 255.0f);
		}

		/// <summary>
		/// Gets or sets packed value of this <see cref="FSColor"/>.
		/// </summary>
		public UInt32 PackedValue
		{
			get { return _packedValue; }
			set { _packedValue = value; }
		}


		internal string DebugDisplayString
		{
			get
			{
				return string.Concat(
						this.R.ToString(), "  ",
						this.G.ToString(), "  ",
						this.B.ToString(), "  ",
						this.A.ToString()
				);
			}
		}


		/// <summary>
		/// Returns a <see cref="String"/> representation of this <see cref="FSColor"/> in the format:
		/// {R:[red] G:[green] B:[blue] A:[alpha]}
		/// </summary>
		/// <returns><see cref="String"/> representation of this <see cref="FSColor"/>.</returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder(25);
			sb.Append("{R:");
			sb.Append(R);
			sb.Append(" G:");
			sb.Append(G);
			sb.Append(" B:");
			sb.Append(B);
			sb.Append(" A:");
			sb.Append(A);
			sb.Append("}");
			return sb.ToString();
		}

		/// <summary>
		/// Translate a non-premultipled alpha <see cref="FSColor"/> to a <see cref="FSColor"/> that contains premultiplied alpha.
		/// </summary>
		/// <param name="vector">A <see cref="Vector4"/> representing color.</param>
		/// <returns>A <see cref="FSColor"/> which contains premultiplied alpha data.</returns>
		public static FSColor FromNonPremultiplied(Vector4 vector)
		{
			return new FSColor(vector.X * vector.W, vector.Y * vector.W, vector.Z * vector.W, vector.W);
		}

		/// <summary>
		/// Translate a non-premultipled alpha <see cref="FSColor"/> to a <see cref="FSColor"/> that contains premultiplied alpha.
		/// </summary>
		/// <param name="r">Red component value.</param>
		/// <param name="g">Green component value.</param>
		/// <param name="b">Blue component value.</param>
		/// <param name="a">Alpha component value.</param>
		/// <returns>A <see cref="FSColor"/> which contains premultiplied alpha data.</returns>
		public static FSColor FromNonPremultiplied(int r, int g, int b, int a)
		{
			return new FSColor(r * a / 255, g * a / 255, b * a / 255, a);
		}

		#region IEquatable<RgbaColor32> Members

		/// <summary>
		/// Compares whether current instance is equal to specified <see cref="FSColor"/>.
		/// </summary>
		/// <param name="other">The <see cref="FSColor"/> to compare.</param>
		/// <returns><c>true</c> if the instances are equal; <c>false</c> otherwise.</returns>
		public bool Equals(FSColor other)
		{
			return this.PackedValue == other.PackedValue;
		}

		#endregion

		/// <summary>
		/// Deconstruction method for <see cref="FSColor"/>.
		/// </summary>
		/// <param name="r">Red component value from 0 to 255.</param>
		/// <param name="g">Green component value from 0 to 255.</param>
		/// <param name="b">Blue component value from 0 to 255.</param>
		public void Deconstruct(out byte r, out byte g, out byte b)
		{
			r = R;
			g = G;
			b = B;
		}

		/// <summary>
		/// Deconstruction method for <see cref="FSColor"/>.
		/// </summary>
		/// <param name="r">Red component value from 0.0f to 1.0f.</param>
		/// <param name="g">Green component value from 0.0f to 1.0f.</param>
		/// <param name="b">Blue component value from 0.0f to 1.0f.</param>
		public void Deconstruct(out float r, out float g, out float b)
		{
			r = R / 255f;
			g = G / 255f;
			b = B / 255f;
		}

		/// <summary>
		/// Deconstruction method for <see cref="FSColor"/> with Alpha.
		/// </summary>
		/// <param name="r">Red component value from 0 to 255.</param>
		/// <param name="g">Green component value from 0 to 255.</param>
		/// <param name="b">Blue component value from 0 to 255.</param>
		/// <param name="a">Alpha component value from 0 to 255.</param>
		public void Deconstruct(out byte r, out byte g, out byte b, out byte a)
		{
			r = R;
			g = G;
			b = B;
			a = A;
		}

		/// <summary>
		/// Deconstruction method for <see cref="FSColor"/> with Alpha.
		/// </summary>
		/// <param name="r">Red component value from 0.0f to 1.0f.</param>
		/// <param name="g">Green component value from 0.0f to 1.0f.</param>
		/// <param name="b">Blue component value from 0.0f to 1.0f.</param>
		/// <param name="a">Alpha component value from 0.0f to 1.0f.</param>
		public void Deconstruct(out float r, out float g, out float b, out float a)
		{
			r = R / 255f;
			g = G / 255f;
			b = B / 255f;
			a = A / 255f;
		}
	}
}

#endif