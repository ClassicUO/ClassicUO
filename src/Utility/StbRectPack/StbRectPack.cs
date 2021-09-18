using System;
using System.Runtime.CompilerServices;

namespace StbRectPackSharp
{
#if !STBSHARP_INTERNAL
	public
#else
	internal
#endif
	static unsafe partial class StbRectPack
	{
		public struct stbrp_context: IDisposable
		{
			public int width;
			public int height;
			public int align;
			public int init_mode;
			public int heuristic;
			public int num_nodes;
			public stbrp_node* active_head;
			public stbrp_node* free_head;
			public stbrp_node* extra;
			public stbrp_node* all_nodes;

            private static ClassicUO.Utility.UnmanagedMemoryPool _all_nodes_pool, _extra_pool;


            public stbrp_context(int nodesCount)
			{
				if (nodesCount <= 0)
				{
					throw new ArgumentOutOfRangeException(nameof(nodesCount));
				}

				width = height = align = init_mode = heuristic = num_nodes = 0;
				active_head = free_head = null;

                if (_all_nodes_pool.Alloc == null)
                {
                    int size = sizeof(stbrp_node);
                    _all_nodes_pool = ClassicUO.Utility.UnsafeMemoryManager.AllocPool(size, nodesCount);
                    _extra_pool = ClassicUO.Utility.UnsafeMemoryManager.AllocPool(size, 2);
                }


                all_nodes = (stbrp_node*)ClassicUO.Utility.UnsafeMemoryManager.Alloc(ref _all_nodes_pool);
                extra = (stbrp_node*)ClassicUO.Utility.UnsafeMemoryManager.Alloc(ref _extra_pool);

                // Allocate nodes
                //all_nodes = (stbrp_node*)CRuntime.malloc(sizeof(stbrp_node) * nodesCount);

                // Allocate extras
                //extra = (stbrp_node*)CRuntime.malloc(sizeof(stbrp_node) * 2);
            }

            public void Dispose()
			{
				if (all_nodes != null)
				{

                    ClassicUO.Utility.UnsafeMemoryManager.Free(ref _all_nodes_pool, all_nodes);
					//CRuntime.free(all_nodes);
					all_nodes = null;
				}

				if (extra != null)
				{

                    ClassicUO.Utility.UnsafeMemoryManager.Free(ref _extra_pool, extra);
                    //CRuntime.free(extra);
                    extra = null;
				}
			}
		}
	}
}
