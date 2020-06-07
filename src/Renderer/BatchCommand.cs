using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ClassicUO.Renderer
{
    [StructLayout(LayoutKind.Explicit, Size = 216)]
    unsafe struct batch_cmd
    {
        [FieldOffset(0)] public int type;


		[FieldOffset(0)] public cmd_viewport viewport;
        [FieldOffset(0)] public cmd_scissor scissor;

        [FieldOffset(0)] public cmd_blend_factor new_blend_factor;
        [FieldOffset(0)] public cmd_new_blend_state new_blend_state;
        [FieldOffset(0)] public cmd_new_rasterize_state new_rasterize_state;
        [FieldOffset(0)] public cmd_new_stencil_state new_stencil_state;
        [FieldOffset(0)] public cmd_new_sampler_state new_sampler_state;

        [FieldOffset(0)] public cmd_set_blend_state set_blend_state;
        [FieldOffset(0)] public cmd_set_rasterize_state set_rasterize_state;
        [FieldOffset(0)] public cmd_set_stencil_state set_stencil_state;
        [FieldOffset(0)] public cmd_set_sampler_state set_sampler_state;


        [FieldOffset(0)] public cmd_set_vertex_buffer set_vertex_buffer;
        [FieldOffset(0)] public cmd_set_index_buffer set_index_buffer;
        [FieldOffset(0)] public cmd_create_vertex_buffer create_vertex_buffer;
        [FieldOffset(0)] public cmd_create_index_buffer create_index_buffer;
        [FieldOffset(0)] public cmd_set_vertex_data set_vertex_data;
        [FieldOffset(0)] public cmd_set_index_data set_index_data;

        [FieldOffset(0)] public cmd_create_effect create_effect;
        [FieldOffset(0)] public cmd_create_basic_effect create_basic_effect;


        [FieldOffset(0)] public cmd_create_texture_2d create_texture_2d;
        [FieldOffset(0)] public cmd_set_texture_data_2d set_texture_data_2d;


        [FieldOffset(0)] public cmd_indexed_primitive_data indexed_primitive_data;
        [FieldOffset(0)] public cmd_destroy_resource destroy_resource;


        public static readonly int SIZE = sizeof(batch_cmd);
    }



    [StructLayout(LayoutKind.Sequential)]
    struct cmd_viewport
    {
        public int type;

        public int x;
        public int y;
        public int w;
        public int h;
	}

    [StructLayout(LayoutKind.Sequential)]
    struct cmd_scissor
    {
        public int type;

		public int x;
        public int y;
        public int w;
        public int h;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct cmd_blend_factor
    {
        public int type;

        public Color color;
    }

    [StructLayout(LayoutKind.Sequential)]
	struct cmd_new_blend_state
    {
        public int type;

        public IntPtr id;
		public BlendFunction alpha_blend_func;
        public Blend alpha_dest_blend;
        public Blend alpha_src_blend;
        public BlendFunction color_blend_func;
        public Blend color_dest_blend;
        public Blend color_src_blend;
        public ColorWriteChannels color_write_channels_0;
        public ColorWriteChannels color_write_channels_1;
        public ColorWriteChannels color_write_channels_2;
        public ColorWriteChannels color_write_channels_3;
        public Color blend_factor;
        public int multiple_sample_mask;
	}

    [StructLayout(LayoutKind.Sequential)]
	struct cmd_new_rasterize_state
    {
        public int type;

        public IntPtr id;
        public CullMode cull_mode;
        public FillMode fill_mode;
        public float depth_bias;
        public bool multi_sample_aa;
        public bool scissor_test_enabled;
        public float slope_scale_depth_bias;
	}

	[StructLayout(LayoutKind.Sequential)]
	struct cmd_new_stencil_state
    {
        public int type;

        public IntPtr id;
        public bool depth_buffer_enabled;
        public bool depth_buffer_write_enabled;
        public CompareFunction depth_buffer_func;
        public bool stencil_enabled;
        public CompareFunction stencil_func;
        public StencilOperation stencil_pass;
        public StencilOperation stencil_fail;
        public StencilOperation stencil_depth_buffer_fail;
        public bool two_sided_stencil_mode;
        public CompareFunction counter_clockwise_stencil_func;
        public StencilOperation counter_clockwise_stencil_fail;
        public StencilOperation counter_clockwise_stencil_pass;
        public StencilOperation counter_clockwise_stencil_depth_buffer_fail;
        public int stencil_mask;
        public int stencil_write_mask;
        public int reference_stencil;
	}

    [StructLayout(LayoutKind.Sequential)]
    struct cmd_new_sampler_state
	{
        public int type;

        public IntPtr id;
        public int index;
		public TextureFilter filter;
        public TextureAddressMode address_u;
        public TextureAddressMode address_v;
        public TextureAddressMode address_w;
        public int max_anisotropy;
        public int max_mip_level;
        public float mip_map_level_of_detail_bias;
	}

    [StructLayout(LayoutKind.Sequential)]
    struct cmd_set_blend_state
    {
        public int type;

        public IntPtr id;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct cmd_set_rasterize_state
    {
        public int type;

        public IntPtr id;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct cmd_set_stencil_state
    {
        public int type;

        public IntPtr id;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct cmd_set_sampler_state
    {
        public int type;

        public IntPtr id;
        public int index;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct cmd_set_vertex_data
	{
        public int type;

        public IntPtr id;
        public IntPtr vertex_buffer_ptr;
        public int vertex_buffer_length;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct cmd_set_index_data
    {
        public int type;

        public IntPtr id;
        public IntPtr indices_buffer_ptr;
        public int indices_buffer_length;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct cmd_set_vertex_buffer
    {
        public int type;

        public IntPtr id;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct cmd_set_index_buffer
    {
        public int type;

        public IntPtr id;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct cmd_create_vertex_buffer
    {
        public int type;

        public IntPtr id;
        public int vertex_elements_count;
        public int size;
        public int decl_count;
        public unsafe vertex_declaration* declarations;
        public BufferUsage buffer_usage;
        public bool is_dynamic;
    }

    [StructLayout(LayoutKind.Sequential)]
	struct vertex_declaration
    {
		public int offset;
        public VertexElementFormat format;
        public VertexElementUsage usage;
        public int usage_index;
	}

    [StructLayout(LayoutKind.Sequential)]
    struct cmd_create_index_buffer
    {
        public int type;

        public IntPtr id;
        public IndexElementSize index_element_size;
        public int index_count;
        public BufferUsage buffer_usage;
        public bool is_dynamic;
    }

	[StructLayout(LayoutKind.Sequential)]
    struct cmd_create_effect
    {
        public int type;

        public IntPtr id;
        public IntPtr code;
        public int length;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct cmd_create_texture_2d
    {
        public int type;

        public IntPtr id;
        public SurfaceFormat format;
        public int width;
        public int height;
        public int level_count;
        public bool is_render_target;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct cmd_set_texture_data_2d
	{
        public int type;

        public IntPtr id;
        public SurfaceFormat format;
        public int x;
        public int y;
		public int width;
        public int height;
        public int level;
        public IntPtr data;
        public int data_length;
    }


    [StructLayout(LayoutKind.Sequential)]
    struct cmd_indexed_primitive_data
    {
        public int type;

        public IntPtr texture_id;
        public PrimitiveType primitive_type;
        public int base_vertex;
        public int min_vertex_index;
        public int num_vertices;
        public int start_index;
        public int primitive_count;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct cmd_create_basic_effect
    {
        public int type;

        public IntPtr id;
        public Matrix world;
        public Matrix view;
        public Matrix projection;
        public bool texture_enabled;
        public IntPtr texture_id;
        public bool vertex_color_enabled;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct cmd_set_basic_effect
    {
        public int type;

        public IntPtr id;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct cmd_destroy_resource
    {
        public int type;

        public IntPtr id;
    }
}
