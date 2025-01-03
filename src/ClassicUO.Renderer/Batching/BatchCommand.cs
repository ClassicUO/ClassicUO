// SPDX-License-Identifier: BSD-2-Clause

using System.Runtime.InteropServices;

namespace ClassicUO.Renderer.Batching
{
    [StructLayout(LayoutKind.Explicit, Size = 216)]
    public unsafe struct BatchCommand
    {
        [FieldOffset(0)] public int type;


        [FieldOffset(0)] public ViewportCommand ViewportCommand;
        [FieldOffset(0)] public ScissorCommand ScissorCommand;

        [FieldOffset(0)] public BlendFactorCommand NewBlendFactorCommand;
        [FieldOffset(0)] public CreateBlendStateCommand NewCreateBlendStateCommand;
        [FieldOffset(0)] public CreateRasterizerStateCommand NewRasterizeStateCommand;
        [FieldOffset(0)] public CreateStencilStateCommand NewCreateStencilStateCommand;
        [FieldOffset(0)] public CreateSamplerStateCommand NewCreateSamplerStateCommand;

        [FieldOffset(0)] public SetBlendStateCommand SetBlendStateCommand;
        [FieldOffset(0)] public SetRasterizerStateCommand SetRasterizerStateCommand;
        [FieldOffset(0)] public SetStencilStateCommand SetStencilStateCommand;
        [FieldOffset(0)] public SetSamplerStateCommand SetSamplerStateCommand;


        [FieldOffset(0)] public SetVertexBufferCommand SetVertexBufferCommand;
        [FieldOffset(0)] public SetIndexBufferCommand SetIndexBufferCommand;
        [FieldOffset(0)] public CreateVertexBufferCommand CreateVertexBufferCommand;
        [FieldOffset(0)] public CreateIndexBufferCommand CreateIndexBufferCommand;
        [FieldOffset(0)] public SetVertexDataCommand SetVertexDataCommand;
        [FieldOffset(0)] public SetIndexDataCommand SetIndexDataCommand;

        [FieldOffset(0)] public CreateEffectCommand CreateEffectCommand;
        [FieldOffset(0)] public CreateBasicEffectCommand CreateBasicEffectCommand;


        [FieldOffset(0)] public CreateTexture2DCommand CreateTexture2DCommand;
        [FieldOffset(0)] public SetTexture2DDataCommand SetTexture2DDataCommand;


        [FieldOffset(0)] public IndexedPrimitiveDataCommand IndexedPrimitiveDataCommand;
        [FieldOffset(0)] public DestroyResourceCommand DestroyResourceCommand;


        public static readonly int SIZE = sizeof(BatchCommand);
    }
}