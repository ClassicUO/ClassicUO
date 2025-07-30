using System;
using System.Buffers;

namespace ClassicUO.IO;

public readonly ref struct TempBuffer<T>(int size) : IDisposable
{
    private readonly T[] _rentedBuffer = ArrayPool<T>.Shared.Rent(size);
    private readonly int _size = size;

    public readonly Span<T> Span => _rentedBuffer.AsSpan(0, _size);
    public readonly Memory<T> Memory => _rentedBuffer.AsMemory(0, _size);

    public readonly void Dispose()
    {
        if (_rentedBuffer != null)
        {
            ArrayPool<T>.Shared.Return(_rentedBuffer, clearArray: true);
        }
    }
}