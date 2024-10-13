#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Numerics;

namespace ClassicUO.Network.Sockets;

#nullable enable

internal sealed class Pipe
{
    private readonly byte[] _buffer;
    private readonly uint _mask;
    private uint _readIndex;
    private uint _writeIndex;

    public int Length => (int)(_writeIndex - _readIndex);

    public Pipe(uint size = 4096)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, (uint)int.MaxValue + 1);

        uint roundedSize = BitOperations.RoundUpToPowerOf2(size);
        _mask = roundedSize - 1;
        _buffer = new byte[roundedSize];
    }

    public void Clear()
    {
        _readIndex = 0;
        _writeIndex = 0;
    }

    public Span<byte> GetAvailableSpanToWrite()
    {
        int readIndex = (int)(_readIndex & _mask);
        int writeIndex = (int)(_writeIndex & _mask);

        if (readIndex > writeIndex)
            return _buffer.AsSpan(writeIndex..readIndex);

        return _buffer.AsSpan(writeIndex);
    }

    public void CommitWrited(int size)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, _buffer.Length - Length);

        _writeIndex += (uint)size;
    }

    public Span<byte> GetAvailableSpanToRead()
    {
        int readIndex = (int)(_readIndex & _mask);
        int writeIndex = (int)(_writeIndex & _mask);

        if (readIndex > writeIndex)
            return _buffer.AsSpan(readIndex);

        return _buffer.AsSpan(readIndex..writeIndex);
    }

    public Memory<byte> GetAvailableMemoryToRead()
    {
        int readIndex = (int)(_readIndex & _mask);
        int writeIndex = (int)(_writeIndex & _mask);

        if (readIndex > writeIndex)
            return _buffer.AsMemory(readIndex);

        return _buffer.AsMemory(readIndex..writeIndex);
    }

    public void CommitRead(int size)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, Length);

        _readIndex += (uint)size;
    }
}