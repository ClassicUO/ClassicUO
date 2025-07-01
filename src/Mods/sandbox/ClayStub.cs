using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Clay_cs;

public unsafe partial struct Clay_String
{
    public bool isStaticallyAllocated;

    [NativeTypeName("int32_t")]
    public int length;

    [NativeTypeName("const char *")]
    public sbyte* chars;
}

public unsafe partial struct Clay_StringSlice
{
    [NativeTypeName("int32_t")]
    public int length;

    [NativeTypeName("const char *")]
    public sbyte* chars;

    [NativeTypeName("const char *")]
    public sbyte* baseChars;
}

public partial struct Clay_Context
{
}

public unsafe partial struct Clay_Arena
{
    [NativeTypeName("uintptr_t")]
    public nuint nextAllocation;

    [NativeTypeName("size_t")]
    public nuint capacity;

    [NativeTypeName("char *")]
    public sbyte* memory;
}

public partial struct Clay_Dimensions
{
    public float width;

    public float height;
}

public partial struct Clay_Vector2
{
    public float x;

    public float y;
}

public partial struct Clay_Color
{
    public float r;

    public float g;

    public float b;

    public float a;
}

public partial struct Clay_BoundingBox
{
    public float x;

    public float y;

    public float width;

    public float height;
}

public partial struct Clay_ElementId
{
    [NativeTypeName("uint32_t")]
    public uint id;

    [NativeTypeName("uint32_t")]
    public uint offset;

    [NativeTypeName("uint32_t")]
    public uint baseId;

    public Clay_String stringId;
}

public unsafe partial struct Clay_ElementIdArray
{
    [NativeTypeName("int32_t")]
    public int capacity;

    [NativeTypeName("int32_t")]
    public int length;

    public Clay_ElementId* internalArray;
}

public partial struct Clay_CornerRadius
{
    public float topLeft;

    public float topRight;

    public float bottomLeft;

    public float bottomRight;
}

[NativeTypeName("uint8_t")]
public enum Clay_LayoutDirection : byte
{
    CLAY_LEFT_TO_RIGHT,
    CLAY_TOP_TO_BOTTOM,
}

[NativeTypeName("uint8_t")]
public enum Clay_LayoutAlignmentX : byte
{
    CLAY_ALIGN_X_LEFT,
    CLAY_ALIGN_X_RIGHT,
    CLAY_ALIGN_X_CENTER,
}

[NativeTypeName("uint8_t")]
public enum Clay_LayoutAlignmentY : byte
{
    CLAY_ALIGN_Y_TOP,
    CLAY_ALIGN_Y_BOTTOM,
    CLAY_ALIGN_Y_CENTER,
}

[NativeTypeName("uint8_t")]
public enum Clay__SizingType : byte
{
    CLAY__SIZING_TYPE_FIT,
    CLAY__SIZING_TYPE_GROW,
    CLAY__SIZING_TYPE_PERCENT,
    CLAY__SIZING_TYPE_FIXED,
}

public partial struct Clay_ChildAlignment
{
    public Clay_LayoutAlignmentX x;

    public Clay_LayoutAlignmentY y;
}

public partial struct Clay_SizingMinMax
{
    public float min;

    public float max;
}

public partial struct Clay_SizingAxis
{
    [NativeTypeName("__AnonymousRecord_clay_L380_C3")]
    public ClaySizingUnion size;

    public Clay__SizingType type;

    [StructLayout(LayoutKind.Explicit)]
    public partial struct ClaySizingUnion
    {
        [FieldOffset(0)]
        public Clay_SizingMinMax minMax;

        [FieldOffset(0)]
        public float percent;
    }
}

public partial struct Clay_Sizing
{
    public Clay_SizingAxis width;

    public Clay_SizingAxis height;
}

public partial struct Clay_Padding
{
    [NativeTypeName("uint16_t")]
    public ushort left;

    [NativeTypeName("uint16_t")]
    public ushort right;

    [NativeTypeName("uint16_t")]
    public ushort top;

    [NativeTypeName("uint16_t")]
    public ushort bottom;
}

public partial struct Clay_LayoutConfig
{
    public Clay_Sizing sizing;

    public Clay_Padding padding;

    [NativeTypeName("uint16_t")]
    public ushort childGap;

    public Clay_ChildAlignment childAlignment;

    public Clay_LayoutDirection layoutDirection;
}

[NativeTypeName("uint8_t")]
public enum Clay_TextElementConfigWrapMode : byte
{
    CLAY_TEXT_WRAP_WORDS,
    CLAY_TEXT_WRAP_NEWLINES,
    CLAY_TEXT_WRAP_NONE,
}

[NativeTypeName("uint8_t")]
public enum Clay_TextAlignment : byte
{
    CLAY_TEXT_ALIGN_LEFT,
    CLAY_TEXT_ALIGN_CENTER,
    CLAY_TEXT_ALIGN_RIGHT,
}

public unsafe partial struct Clay_TextElementConfig
{
    public void* userData;

    public Clay_Color textColor;

    [NativeTypeName("uint16_t")]
    public ushort fontId;

    [NativeTypeName("uint16_t")]
    public ushort fontSize;

    [NativeTypeName("uint16_t")]
    public ushort letterSpacing;

    [NativeTypeName("uint16_t")]
    public ushort lineHeight;

    public Clay_TextElementConfigWrapMode wrapMode;

    public Clay_TextAlignment textAlignment;
}

public partial struct Clay_AspectRatioElementConfig
{
    public float aspectRatio;
}

public partial struct Clay__Clay_AspectRatioElementConfigWrapper
{
    public Clay_AspectRatioElementConfig wrapped;
}

public unsafe partial struct Clay_ImageElementConfig
{
    public void* imageData;
}

[NativeTypeName("uint8_t")]
public enum Clay_FloatingAttachPointType : byte
{
    CLAY_ATTACH_POINT_LEFT_TOP,
    CLAY_ATTACH_POINT_LEFT_CENTER,
    CLAY_ATTACH_POINT_LEFT_BOTTOM,
    CLAY_ATTACH_POINT_CENTER_TOP,
    CLAY_ATTACH_POINT_CENTER_CENTER,
    CLAY_ATTACH_POINT_CENTER_BOTTOM,
    CLAY_ATTACH_POINT_RIGHT_TOP,
    CLAY_ATTACH_POINT_RIGHT_CENTER,
    CLAY_ATTACH_POINT_RIGHT_BOTTOM,
}

public partial struct Clay_FloatingAttachPoints
{
    public Clay_FloatingAttachPointType element;

    public Clay_FloatingAttachPointType parent;
}

[NativeTypeName("uint8_t")]
public enum Clay_PointerCaptureMode : byte
{
    CLAY_POINTER_CAPTURE_MODE_CAPTURE,
    CLAY_POINTER_CAPTURE_MODE_PASSTHROUGH,
}

[NativeTypeName("uint8_t")]
public enum Clay_FloatingAttachToElement : byte
{
    CLAY_ATTACH_TO_NONE,
    CLAY_ATTACH_TO_PARENT,
    CLAY_ATTACH_TO_ELEMENT_WITH_ID,
    CLAY_ATTACH_TO_ROOT,
}

[NativeTypeName("uint8_t")]
public enum Clay_FloatingClipToElement : byte
{
    CLAY_CLIP_TO_NONE,
    CLAY_CLIP_TO_ATTACHED_PARENT,
}

public partial struct Clay_FloatingElementConfig
{
    public Clay_Vector2 offset;

    public Clay_Dimensions expand;

    [NativeTypeName("uint32_t")]
    public uint parentId;

    [NativeTypeName("int16_t")]
    public short zIndex;

    public Clay_FloatingAttachPoints attachPoints;

    public Clay_PointerCaptureMode pointerCaptureMode;

    public Clay_FloatingAttachToElement attachTo;

    public Clay_FloatingClipToElement clipTo;
}

public unsafe partial struct Clay_CustomElementConfig
{
    public void* customData;
}

public partial struct Clay_ClipElementConfig
{
    public bool horizontal;

    public bool vertical;

    public Clay_Vector2 childOffset;
}

public partial struct Clay__Clay_ClipElementConfigWrapper
{
    public Clay_ClipElementConfig wrapped;
}

public partial struct Clay_BorderWidth
{
    [NativeTypeName("uint16_t")]
    public ushort left;

    [NativeTypeName("uint16_t")]
    public ushort right;

    [NativeTypeName("uint16_t")]
    public ushort top;

    [NativeTypeName("uint16_t")]
    public ushort bottom;

    [NativeTypeName("uint16_t")]
    public ushort betweenChildren;
}

public partial struct Clay_BorderElementConfig
{
    public Clay_Color color;

    public Clay_BorderWidth width;
}

public partial struct Clay_TextRenderData
{
    public Clay_StringSlice stringContents;

    public Clay_Color textColor;

    [NativeTypeName("uint16_t")]
    public ushort fontId;

    [NativeTypeName("uint16_t")]
    public ushort fontSize;

    [NativeTypeName("uint16_t")]
    public ushort letterSpacing;

    [NativeTypeName("uint16_t")]
    public ushort lineHeight;
}

public partial struct Clay_RectangleRenderData
{
    public Clay_Color backgroundColor;

    public Clay_CornerRadius cornerRadius;
}

public unsafe partial struct Clay_ImageRenderData
{
    public Clay_Color backgroundColor;

    public Clay_CornerRadius cornerRadius;

    public void* imageData;
}

public unsafe partial struct Clay_CustomRenderData
{
    public Clay_Color backgroundColor;

    public Clay_CornerRadius cornerRadius;

    public void* customData;
}

public partial struct Clay_ScrollRenderData
{
    public bool horizontal;

    public bool vertical;
}

public partial struct Clay_BorderRenderData
{
    public Clay_Color color;

    public Clay_CornerRadius cornerRadius;

    public Clay_BorderWidth width;
}

[StructLayout(LayoutKind.Explicit)]
public partial struct Clay_RenderData
{
    [FieldOffset(0)]
    public Clay_RectangleRenderData rectangle;

    [FieldOffset(0)]
    public Clay_TextRenderData text;

    [FieldOffset(0)]
    public Clay_ImageRenderData image;

    [FieldOffset(0)]
    public Clay_CustomRenderData custom;

    [FieldOffset(0)]
    public Clay_BorderRenderData border;

    [FieldOffset(0)]
    [NativeTypeName("Clay_ClipRenderData")]
    public Clay_ScrollRenderData clip;
}

public unsafe partial struct Clay_ScrollContainerData
{
    public Clay_Vector2* scrollPosition;

    public Clay_Dimensions scrollContainerDimensions;

    public Clay_Dimensions contentDimensions;

    public Clay_ClipElementConfig config;

    public bool found;
}

public partial struct Clay_ElementData
{
    public Clay_BoundingBox boundingBox;

    public bool found;
}

[NativeTypeName("uint8_t")]
public enum Clay_RenderCommandType : byte
{
    CLAY_RENDER_COMMAND_TYPE_NONE,
    CLAY_RENDER_COMMAND_TYPE_RECTANGLE,
    CLAY_RENDER_COMMAND_TYPE_BORDER,
    CLAY_RENDER_COMMAND_TYPE_TEXT,
    CLAY_RENDER_COMMAND_TYPE_IMAGE,
    CLAY_RENDER_COMMAND_TYPE_SCISSOR_START,
    CLAY_RENDER_COMMAND_TYPE_SCISSOR_END,
    CLAY_RENDER_COMMAND_TYPE_CUSTOM,
}

public unsafe partial struct Clay_RenderCommand
{
    public Clay_BoundingBox boundingBox;

    public Clay_RenderData renderData;

    public void* userData;

    [NativeTypeName("uint32_t")]
    public uint id;

    [NativeTypeName("int16_t")]
    public short zIndex;

    public Clay_RenderCommandType commandType;
}

public unsafe partial struct Clay_RenderCommandArray
{
    [NativeTypeName("int32_t")]
    public int capacity;

    [NativeTypeName("int32_t")]
    public int length;

    public Clay_RenderCommand* internalArray;
}

[NativeTypeName("uint8_t")]
public enum Clay_PointerDataInteractionState : byte
{
    CLAY_POINTER_DATA_PRESSED_THIS_FRAME,
    CLAY_POINTER_DATA_PRESSED,
    CLAY_POINTER_DATA_RELEASED_THIS_FRAME,
    CLAY_POINTER_DATA_RELEASED,
}

public partial struct Clay_PointerData
{
    public Clay_Vector2 position;

    public Clay_PointerDataInteractionState state;
}

public unsafe partial struct Clay_ElementDeclaration
{
    public Clay_ElementId id;

    public Clay_LayoutConfig layout;

    public Clay_Color backgroundColor;

    public Clay_CornerRadius cornerRadius;

    public Clay_AspectRatioElementConfig aspectRatio;

    public Clay_ImageElementConfig image;

    public Clay_FloatingElementConfig floating;

    public Clay_CustomElementConfig custom;

    public Clay_ClipElementConfig clip;

    public Clay_BorderElementConfig border;

    public void* userData;
}

public partial struct Clay__Clay_ElementDeclarationWrapper
{
    public Clay_ElementDeclaration wrapped;
}

[NativeTypeName("uint8_t")]
public enum Clay_ErrorType : byte
{
    CLAY_ERROR_TYPE_TEXT_MEASUREMENT_FUNCTION_NOT_PROVIDED,
    CLAY_ERROR_TYPE_ARENA_CAPACITY_EXCEEDED,
    CLAY_ERROR_TYPE_ELEMENTS_CAPACITY_EXCEEDED,
    CLAY_ERROR_TYPE_TEXT_MEASUREMENT_CAPACITY_EXCEEDED,
    CLAY_ERROR_TYPE_DUPLICATE_ID,
    CLAY_ERROR_TYPE_FLOATING_CONTAINER_PARENT_NOT_FOUND,
    CLAY_ERROR_TYPE_PERCENTAGE_OVER_1,
    CLAY_ERROR_TYPE_INTERNAL_ERROR,
}

public unsafe partial struct Clay_ErrorData
{
    public Clay_ErrorType errorType;

    public Clay_String errorText;

    public void* userData;
}

public unsafe partial struct Clay_ErrorHandler
{
    [NativeTypeName("void (*)(Clay_ErrorData)")]
    public IntPtr errorHandlerFunction;

    public void* userData;
}


/// <summary>Defines the type of a member as it was used in the native signature.</summary>
[AttributeUsage(
    AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field |
    AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = true)]
[Conditional("DEBUG")]
internal sealed partial class NativeTypeNameAttribute : Attribute
{
    private readonly string _name;

    /// <summary>Initializes a new instance of the <see cref="NativeTypeNameAttribute" /> class.</summary>
    /// <param name="name">The name of the type that was used in the native signature.</param>
    public NativeTypeNameAttribute(string name)
    {
        _name = name;
    }

    /// <summary>Gets the name of the type that was used in the native signature.</summary>
    public string Name => _name;
}

/// <summary>Defines the annotation found in a native declaration.</summary>
[AttributeUsage(
    AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field |
    AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true, Inherited = false)]
[Conditional("DEBUG")]
internal sealed partial class NativeAnnotationAttribute : Attribute
{
    private readonly string _annotation;

    /// <summary>Initializes a new instance of the <see cref="NativeAnnotationAttribute" /> class.</summary>
    /// <param name="annotation">The annotation that was used in the native declaration.</param>
    public NativeAnnotationAttribute(string annotation)
    {
        _annotation = annotation;
    }

    /// <summary>Gets the annotation that was used in the native declaration.</summary>
    public string Annotation => _annotation;
}

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
                min = min,
                max = max
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
                    min = min,
                    max = max
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

// public partial struct Clay_Vector2
// {
//     public static implicit operator Clay_Vector2(Vector2 v) => new Clay_Vector2 { x = v.X, y = v.Y };
//     public static implicit operator Vector2(Clay_Vector2 v) => new Vector2(v.x, v.y);
// }

public partial struct Clay_Dimensions
{
    public Clay_Dimensions(float width, float height)
    {
        this.width = width;
        this.height = height;
    }
}
