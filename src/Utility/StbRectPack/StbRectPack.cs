using System;
using System.Runtime.CompilerServices;

namespace StbRectPackSharp
{
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


            public stbrp_context(int nodesCount)
			{
				if (nodesCount <= 0)
				{
					throw new ArgumentOutOfRangeException(nameof(nodesCount));
				}

				width = height = align = init_mode = heuristic = num_nodes = 0;
				active_head = free_head = null;

                //Allocate nodes
                all_nodes = (stbrp_node*)CRuntime.malloc(sizeof(stbrp_node) * nodesCount);

                //Allocate extras
                extra = (stbrp_node*)CRuntime.malloc(sizeof(stbrp_node) * 2);
            }

            public void Dispose()
			{
				if (all_nodes != null)
				{
					CRuntime.free(all_nodes);
					all_nodes = null;
				}

				if (extra != null)
				{
                    CRuntime.free(extra);
                    extra = null;
				}
			}
		}
	}
}
