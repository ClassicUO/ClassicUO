using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ClassicUO.Ecs;


internal static class DataExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T As<T>(this Span<byte> buf) where T : unmanaged
        => As<byte, T>(buf);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TTo As<TFrom, TTo>(this Span<TFrom> buf)
        where TFrom : unmanaged
        where TTo : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(sizeof(TFrom) * buf.Length, sizeof(TTo));

        return MemoryMarshal.Read<TTo>(MemoryMarshal.AsBytes(buf));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<byte> AsBytes<T>(this T val)
        where T : unmanaged
    {
        return MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref val, 1));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<byte> AsBytes<T>(this Span<T> val)
        where T : unmanaged
    {
        return MemoryMarshal.AsBytes(val);
    }
}
