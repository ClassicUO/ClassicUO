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