#region license

// Copyright (c) 2021, andreakarasho
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
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ClassicUO.Utility
{
    public unsafe struct UnmanagedMemoryPool
    {
        public byte* Alloc;
        public void* Free;
        public int BlockSize;
        public int NumBlocks;
    }


    // UnmanagedMemoryPool and stuff from --> https://www.jacksondunstan.com/articles/3770
    public static unsafe class UnsafeMemoryManager
    {
        public static readonly int SizeOfPointer = sizeof(void*);

        public static readonly int MinimumPoolBlockSize = SizeOfPointer;


        public static void Memset(void* ptr, byte value, int count)
        {
            long* c = (long*) ptr;

            count /= 8;

            for (int i = 0; i < count; ++i)
            {
                *c++ = (long) value;
            }
        }

        public static IntPtr Alloc(int size)
        {
            size = ((size + 7) & (-8));

            IntPtr ptr = Marshal.AllocHGlobal(size);

            return ptr;
        }

        public static IntPtr Calloc(int size)
        {
            IntPtr ptr = Alloc(size);

            Memset((void*) ptr, 0, size);

            return ptr;
        }

        public static void* Alloc(ref UnmanagedMemoryPool pool)
        {
            void* pRet = pool.Free;

            pool.Free = *((byte**) pool.Free);

            return pRet;
        }

        public static void* Calloc(ref UnmanagedMemoryPool pool)
        {
            void* ptr = Alloc(ref pool);

            Memset(ptr, 0, pool.BlockSize);

            return ptr;
        }

        public static UnmanagedMemoryPool AllocPool(int blockSize, int numBlocks)
        {
            Debug.Assert(blockSize >= MinimumPoolBlockSize);
            Debug.Assert(numBlocks > 0);

            blockSize = ((blockSize + 7) & (-8));

            UnmanagedMemoryPool pool = new UnmanagedMemoryPool();
            pool.Free = null;
            pool.NumBlocks = numBlocks;
            pool.BlockSize = blockSize;

            pool.Alloc = (byte*) Alloc(blockSize * numBlocks);

            FreeAll(&pool);

            return pool;
        }

        public static void Free(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        public static void Free(UnmanagedMemoryPool* pool, void* ptr)
        {
            if (ptr != null)
            {
                void** pHead = (void**) ptr;
                *pHead = pool->Free;
                pool->Free = pHead;
            }
        }

        public static void Free(ref UnmanagedMemoryPool pool, void* ptr)
        {
            if (ptr != null)
            {
                void** pHead = (void**)ptr;
                *pHead = pool.Free;
                pool.Free = pHead;
            }
        }

        public static void FreeAll(UnmanagedMemoryPool* pool)
        {
            void** pCur = (void**) pool->Alloc;
            byte* pNext = pool->Alloc + pool->BlockSize;

            for (int i = 0, count = pool->NumBlocks - 1; i < count; ++i)
            {
                *pCur = pNext;
                pCur = (void**) pNext;
                pNext += pool->BlockSize;
            }

            *pCur = default(void*);

            pool->Free = pool->Alloc;
        }

        public static void FreePool(UnmanagedMemoryPool* pool)
        {
            Free((IntPtr) pool->Alloc);
            pool->Alloc = null;
            pool->Free = null;
        }
    }
}