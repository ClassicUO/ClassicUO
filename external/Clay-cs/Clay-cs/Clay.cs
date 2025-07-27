using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: DisableRuntimeMarshalling]
namespace Clay_cs;

public unsafe delegate Clay_Dimensions ClayMeasureTextDelegate(Clay_StringSlice text, Clay_TextElementConfig* config, void* userData);

public delegate void ClayErrorDelegate(Clay_ErrorData data);

public delegate void ClayOnHoverDelegate(Clay_ElementId id, Clay_PointerData data, nint userData);

public delegate Clay_Vector2 ClayQueryScrollOffsetDelegate(uint elementId);

public static class Clay
{
    internal static readonly ClayStringCollection ClayStrings = new();


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ReadOnlySpan<Clay_ElementId> GetPointerOverIds()
    {
        var array = ClayInterop.Clay_GetPointerOverIds();
        return new ReadOnlySpan<Clay_ElementId>(array.internalArray, array.length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetMaxElementCount()
    {
        return ClayInterop.Clay_GetMaxElementCount();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetMaxElementCount(int maxElementCount)
    {
        ClayInterop.Clay_SetMaxElementCount(maxElementCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetMaxMeasureTextCacheWordCount()
    {
        return ClayInterop.Clay_GetMaxMeasureTextCacheWordCount();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetMaxMeasureTextCacheWordCount(int maxMeasureTextCacheWordCount)
    {
        ClayInterop.Clay_SetMaxMeasureTextCacheWordCount(maxMeasureTextCacheWordCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ResetMeasureTextCache()
    {
        ClayInterop.Clay_ResetMeasureTextCache();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint MinMemorySize() => ClayInterop.Clay_MinMemorySize();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ClayArenaHandle CreateArena(uint memorySize)
    {
        var ptr = Marshal.AllocHGlobal((int)memorySize);
        var arena = ClayInterop.Clay_CreateArenaWithCapacityAndMemory(memorySize, (void*)ptr);
        return new ClayArenaHandle { Arena = arena, Memory = ptr };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Clay_Context* GetCurrentContext()
    {
        return ClayInterop.Clay_GetCurrentContext();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void SetCurrentContext(Clay_Context* context)
    {
        ClayInterop.Clay_SetCurrentContext(context);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void SetMeasureTextFunction(nint measureText)
    {
        ClayInterop.Clay_SetMeasureTextFunction(measureText, null);
    }

    public static unsafe Clay_Context* Initialize(
        ClayArenaHandle handle,
        Clay_Dimensions dimensions,
        nint errorHandler)
    {
        return ClayInterop.Clay_Initialize(handle.Arena, dimensions, new Clay_ErrorHandler { errorHandlerFunction = errorHandler });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetCullingEnabled(bool state)
    {
        ClayInterop.Clay_SetCullingEnabled(state);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetDebugModeEnabled(bool state)
    {
        ClayInterop.Clay_SetDebugModeEnabled(state);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDebugModeEnabled()
    {
        return ClayInterop.Clay_IsDebugModeEnabled();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BeginLayout()
    {
        ClayInterop.Clay_BeginLayout();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe ReadOnlySpan<Clay_RenderCommand> EndLayout()
    {
        var r = ClayInterop.Clay_EndLayout();
        return new(r.internalArray, r.length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe Clay_RenderCommand* RenderCommandArrayGet(Clay_RenderCommandArray arr, int index)
    {
        return ClayInterop.Clay_RenderCommandArray_Get(&arr, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetPointerState(Vector2 position, bool isMouseDown)
    {
        ClayInterop.Clay_SetPointerState(position, isMouseDown);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPointerOver(Clay_ElementId elementId)
    {
        return ClayInterop.Clay_PointerOver(elementId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsHovered()
    {
        return ClayInterop.Clay_Hovered();
    }

    public static unsafe void OnHover(ClayOnHoverDelegate onHover, nint userData = 0)
    {
        var ptr = Marshal.GetFunctionPointerForDelegate(onHover);
        ClayInterop.Clay_OnHover(ptr, userData);
    }

    public static void OnHover(nint fnPtr, nint userData = 0)
    {
        ClayInterop.Clay_OnHover(fnPtr, userData);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Clay_ScrollContainerData GetScrollContainerData(Clay_ElementId id)
    {
        return ClayInterop.Clay_GetScrollContainerData(id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Clay_Vector2 GetScrollOffset()
    {
        return ClayInterop.Clay_GetScrollOffset();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UpdateScrollContainers(bool enableDragScrolling, Vector2 moveDelta, float timeDelta)
    {
        ClayInterop.Clay_UpdateScrollContainers(enableDragScrolling, moveDelta, timeDelta);
    }

    public static unsafe void SetQueryScrollOffsetFunction(ClayQueryScrollOffsetDelegate queryScrollOffsetFunction)
    {
        // var ptr = Marshal.GetFunctionPointerForDelegate(queryScrollOffsetFunction);
        // var castPtr = (delegate* unmanaged[Cdecl]<uint, Clay_Vector2>)ptr;
        // ClayInterop.Clay_SetQueryScrollOffsetFunction(castPtr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetLayoutDimensions(Clay_Dimensions dimensions)
    {
        ClayInterop.Clay_SetLayoutDimensions(dimensions);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Clay_ElementId GetElementId(Clay_String idString)
    {
        return ClayInterop.Clay_GetElementId(idString);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Clay_ElementId GetElementId(Clay_String idString, uint index)
    {
        return ClayInterop.Clay_GetElementIdWithIndex(idString, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Clay_ElementData GetElementData(Clay_ElementId id)
    {
        return ClayInterop.Clay_GetElementData(id);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OpenElement()
    {
        ClayInterop.Clay__OpenElement();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OpenTextElement(ReadOnlySpan<char> text, Clay_TextElementConfig c)
    {
        OpenTextElement(ClayStrings.Get(text), c);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void OpenTextElement(Clay_String text, Clay_TextElementConfig c)
    {
        ClayInterop.Clay__OpenTextElement(text, ClayInterop.Clay__StoreTextElementConfig(c));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ConfigureOpenElement(Clay_ElementDeclaration decl)
    {
        ClayInterop.Clay__ConfigureOpenElement(decl);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CloseElement()
    {
        ClayInterop.Clay__CloseElement();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetParentElementId()
    {
        return ClayInterop.Clay__GetParentElementId();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Clay_ElementId Id(ReadOnlySpan<char> text)
    {
        return Id(ClayStrings.Get(text));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Clay_ElementId Id(Clay_String text)
    {
        return HashId(text, 0, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Clay_ElementId HashId(Clay_String text, uint offset, uint seed)
    {
        return ClayInterop.Clay__HashStringWithOffset(text, offset, seed);
    }
}
