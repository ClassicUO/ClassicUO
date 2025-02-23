using System.Numerics;
using System.Runtime.CompilerServices;

namespace Clay_cs;

public partial struct Clay_Color
{
	public Clay_Color(float r, float g, float b, float a = 255)
	{
		this.r = r;
		this.g = g;
		this.b = b;
		this.a = a;
	}
}

public partial struct Clay_SizingAxis
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Clay_SizingAxis Grow() => Grow(0, 0);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Clay_SizingAxis Grow(float value) => Grow(value, value);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Clay_SizingAxis Grow(float min, float max) => new()
	{
		type = Clay__SizingType.CLAY__SIZING_TYPE_GROW,
		size = new ClaySizingUnion
		{
			minMax = new Clay_SizingMinMax
			{
				min = min, max = max
			}
		}
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Clay_SizingAxis Fit(float min, float max)
	{
		return new Clay_SizingAxis
		{
			type = Clay__SizingType.CLAY__SIZING_TYPE_FIT,
			size = new ClaySizingUnion
			{
				minMax = new Clay_SizingMinMax
				{
					min = min, max = max
				}
			}
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Clay_SizingAxis Fixed(float size) => new()
	{
		type = Clay__SizingType.CLAY__SIZING_TYPE_FIXED,
		size = new ClaySizingUnion
		{
			minMax = new Clay_SizingMinMax
			{
				min = size,
				max = size,
			}
		}
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Clay_SizingAxis Percent(float percent) => new()
	{
		type = Clay__SizingType.CLAY__SIZING_TYPE_PERCENT,
		size = new ClaySizingUnion
		{
			percent = percent,
		}
	};
}

public partial struct Clay_Padding
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Clay_Padding HorVer(ushort leftRight, ushort topBot) => new()
	{
		left = leftRight,
		right = leftRight,
		top = topBot,
		bottom = topBot,
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Clay_Padding Hor(ushort value) => new()
	{
		left = value,
		right = value,
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Clay_Padding Ver(ushort value) => new()
	{
		top = value,
		bottom = value,
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Clay_Padding All(ushort value) => new()
	{
		bottom = value,
		left = value,
		right = value,
		top = value,
	};
}

public partial struct Clay_CornerRadius
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Clay_CornerRadius All(ushort value) => new()
	{
		bottomLeft = value,
		bottomRight = value,
		topLeft = value,
		topRight = value,
	};
}

public partial struct Clay_Sizing
{
	public Clay_Sizing(Clay_SizingAxis width, Clay_SizingAxis height)
	{
		this.width = width;
		this.height = height;
	}
}

public partial struct Clay_ChildAlignment
{
	public Clay_ChildAlignment(Clay_LayoutAlignmentX x, Clay_LayoutAlignmentY y)
	{
		this.x = x;
		this.y = y;
	}
}

public partial struct Clay_Vector2
{
	public static implicit operator Clay_Vector2(Vector2 v) => new Clay_Vector2 { x = v.X, y = v.Y };
	public static implicit operator Vector2(Clay_Vector2 v) => new Vector2(v.x, v.y);
}

public partial struct Clay_Dimensions
{
	public Clay_Dimensions(float width, float height)
	{
		this.width = width;
		this.height = height;
	}
}
