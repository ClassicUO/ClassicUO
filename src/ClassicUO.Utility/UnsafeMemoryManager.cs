// SPDX-License-Identifier: BSD-2-Clause

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