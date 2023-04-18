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

using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer.Batching;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using ClassicUO.Utility.Platforms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SDL2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ClassicUO.Plugins
{
    sealed unsafe class PluginApi
    {
        private delegate* unmanaged[Cdecl]<nint, nint, int> _onEvent;
        private readonly Dictionary<IntPtr, GraphicsResource> _resources = new Dictionary<IntPtr, GraphicsResource>();

        private PluginApi(FileInfo pluginFile, delegate* unmanaged[Cdecl]<nint, nint, int> onEvent)
        {
            File = pluginFile;
            _onEvent = onEvent;
        }

        public FileInfo File { get; }



        public int SendMouseEvent(MouseButtonType button, int x, int y, bool pressed)
        {
            var ev = new PluginEvent();
            ev.EventType = PluginEventType.Mouse;
            ev.Mouse.Button = button;
            ev.Mouse.X = x;
            ev.Mouse.Y = y;
            ev.Mouse.IsPressed = pressed;

            return SendEvent(&ev);
        }

        public int SendWheelEvent(int x, int y)
        {
            var ev = new PluginEvent();
            ev.EventType = PluginEventType.Wheel;
            ev.Wheel.X = x;
            ev.Wheel.Y = y;

            return SendEvent(&ev);
        }

        public int SendKeyboardEvent(int keycode, int mods, bool pressed)
        {
            var ev = new PluginEvent();
            ev.EventType = PluginEventType.Keyboard;
            ev.Keyboard.Keycode = keycode;
            ev.Keyboard.Mods = mods;
            ev.Keyboard.IsPressed = pressed;

            return SendEvent(&ev);
        }

        public int SendInputTextEvent(char c)
        {
            var ev = new PluginEvent();
            ev.EventType = PluginEventType.InputText;
            ev.InputText.InputChar = c;

            return SendEvent(&ev);
        }

        public int SendWindowEvent(PluginWindowEventType wndEvent)
        {
            var ev = new PluginEvent();
            ev.EventType = PluginEventType.Window;
            ev.Window.WindowEventType = wndEvent;

            return SendEvent(&ev);
        }

        public int SendQuitEvent()
        {
            var ev = new PluginEvent();
            ev.EventType = PluginEventType.Quit;

            return SendEvent(&ev);
        }

        public int SendConnectEvent(string server, ushort port)
        {
            var ev = new PluginEvent();
            ev.EventType = PluginEventType.Connect;

            var count = Encoding.UTF8.GetByteCount(server);

            fixed (char* ptr = server)
                Encoding.UTF8.GetBytes(ptr, server.Length, (byte*)ev.Connect.Server, count);

            ev.Connect.Port = port;

            return SendEvent(&ev);
        }

        public int SendDisconnectEvent()
        {
            var ev = new PluginEvent();
            ev.EventType = PluginEventType.Disconnect;

            return SendEvent(&ev);
        }
         
        public int SendFrameTick(uint ticks)
        {
            var ev = new PluginEvent();
            ev.EventType = PluginEventType.FrameTick;
            ev.FrameTick.Ticks = ticks;

            return SendEvent(&ev);
        }

        public int SendClientToServerPacketEvent(ReadOnlySpan<byte> message)
        {
            var ev = new PluginEvent();
            ev.EventType = PluginEventType.OnPacket;
            ev.OnPacket.ClientToServer = true;

            fixed (byte* ptr = message)
            {
                ev.OnPacket.PacketPtr = (nint)ptr;
                ev.OnPacket.Size = message.Length;

                return SendEvent(&ev);
            }
        }

        public int SendServerToClientPacketEvent(ReadOnlySpan<byte> message)
        {
            var ev = new PluginEvent();
            ev.EventType = PluginEventType.OnPacket;
            ev.OnPacket.ClientToServer = false;

            fixed (byte* ptr = message)
            {
                ev.OnPacket.PacketPtr = (nint)ptr;
                ev.OnPacket.Size = message.Length;

                return SendEvent(&ev);
            }
        }

        public int SendRenderCommandListEvent(GraphicsDevice device, uint ticks)
        {
            var ev = new PluginEvent();
            ev.EventType = PluginEventType.Render;
            ev.Render.CommandListPtr = null;
            ev.Render.CommandListSize = 0;
            ev.Render.FrameTick = ticks;

            var res = SendEvent(&ev);
            if (res != 1)
                return res;

            handleCommandList(device, (IntPtr)ev.Render.CommandListPtr, ev.Render.CommandListSize, _resources);

            static void handleCommandList(GraphicsDevice device, IntPtr ptr, int length, Dictionary<IntPtr, GraphicsResource> resources)
            {
                if (ptr == IntPtr.Zero || length <= 0)
                {
                    return;
                }

                const int CMD_VIEWPORT = 0;
                const int CMD_SCISSOR = 1;
                const int CMD_BLEND_STATE = 2;
                const int CMD_RASTERIZE_STATE = 3;
                const int CMD_STENCIL_STATE = 4;
                const int CMD_SAMPLER_STATE = 5;
                const int CMD_SET_VERTEX_BUFFER = 6;
                const int CMD_SET_INDEX_BUFFER = 7;
                const int CMD_CREATE_VERTEX_BUFFER = 8;
                const int CMD_CREATE_INDEX_BUFFER = 9;
                const int CMD_CREATE_EFFECT = 10;
                const int CMD_CREATE_TEXTURE_2D = 11;
                const int CMD_SET_TEXTURE_DATA_2D = 12;
                const int CMD_INDEXED_PRIMITIVE_DATA = 13;
                const int CMD_CREATE_BASIC_EFFECT = 14;
                const int CMD_SET_VERTEX_DATA = 15;
                const int CMD_SET_INDEX_DATA = 16;
                const int CMD_DESTROY_RESOURCE = 17;
                const int CMD_BLEND_FACTOR = 18;
                const int CMD_NEW_BLEND_STATE = 19;
                const int CMD_NEW_RASTERIZE_STATE = 20;
                const int CMD_NEW_STENCIL_STATE = 21;
                const int CMD_NEW_SAMPLER_STATE = 22;


                Effect current_effect = null;

                var lastViewport = device.Viewport;
                var lastScissorBox = device.ScissorRectangle;
                var lastBlendFactor = device.BlendFactor;
                var lastBlendState = device.BlendState;
                var lastRasterizeState = device.RasterizerState;
                var lastDepthStencilState = device.DepthStencilState;
                var lastsampler = device.SamplerStates[0];

                for (int i = 0; i < length; i++)
                {
                    var command = ((BatchCommand*)ptr)[i];

                    switch (command.type)
                    {
                        case CMD_VIEWPORT:
                            ref ViewportCommand viewportCommand = ref command.ViewportCommand;

                            device.Viewport = new Viewport(viewportCommand.X, viewportCommand.y, viewportCommand.w, viewportCommand.h);

                            break;

                        case CMD_SCISSOR:
                            ref ScissorCommand scissorCommand = ref command.ScissorCommand;

                            device.ScissorRectangle = new Rectangle(scissorCommand.x, scissorCommand.y, scissorCommand.w, scissorCommand.h);

                            break;

                        case CMD_BLEND_FACTOR:

                            ref BlendFactorCommand blendFactorCommand = ref command.NewBlendFactorCommand;

                            device.BlendFactor = blendFactorCommand.color;

                            break;

                        case CMD_NEW_BLEND_STATE:
                            ref CreateBlendStateCommand createBlend = ref command.NewCreateBlendStateCommand;

                            resources[createBlend.id] = new BlendState
                            {
                                AlphaBlendFunction = createBlend.AlphaBlendFunc,
                                AlphaDestinationBlend = createBlend.AlphaDestBlend,
                                AlphaSourceBlend = createBlend.AlphaSrcBlend,
                                ColorBlendFunction = createBlend.ColorBlendFunc,
                                ColorDestinationBlend = createBlend.ColorDestBlend,
                                ColorSourceBlend = createBlend.ColorSrcBlend,
                                ColorWriteChannels = createBlend.ColorWriteChannels0,
                                ColorWriteChannels1 = createBlend.ColorWriteChannels1,
                                ColorWriteChannels2 = createBlend.ColorWriteChannels2,
                                ColorWriteChannels3 = createBlend.ColorWriteChannels3,
                                BlendFactor = createBlend.BlendFactor,
                                MultiSampleMask = createBlend.MultipleSampleMask
                            };

                            break;

                        case CMD_NEW_RASTERIZE_STATE:

                            ref CreateRasterizerStateCommand rasterize = ref command.NewRasterizeStateCommand;

                            resources[rasterize.id] = new RasterizerState
                            {
                                CullMode = rasterize.CullMode,
                                DepthBias = rasterize.DepthBias,
                                FillMode = rasterize.FillMode,
                                MultiSampleAntiAlias = rasterize.MultiSample,
                                ScissorTestEnable = rasterize.ScissorTestEnabled,
                                SlopeScaleDepthBias = rasterize.SlopeScaleDepthBias
                            };

                            break;

                        case CMD_NEW_STENCIL_STATE:

                            ref CreateStencilStateCommand createStencil = ref command.NewCreateStencilStateCommand;

                            resources[createStencil.id] = new DepthStencilState
                            {
                                DepthBufferEnable = createStencil.DepthBufferEnabled,
                                DepthBufferWriteEnable = createStencil.DepthBufferWriteEnabled,
                                DepthBufferFunction = createStencil.DepthBufferFunc,
                                StencilEnable = createStencil.StencilEnabled,
                                StencilFunction = createStencil.StencilFunc,
                                StencilPass = createStencil.StencilPass,
                                StencilFail = createStencil.StencilFail,
                                StencilDepthBufferFail = createStencil.StencilDepthBufferFail,
                                TwoSidedStencilMode = createStencil.TwoSidedStencilMode,
                                CounterClockwiseStencilFunction = createStencil.CounterClockwiseStencilFunc,
                                CounterClockwiseStencilFail = createStencil.CounterClockwiseStencilFail,
                                CounterClockwiseStencilPass = createStencil.CounterClockwiseStencilPass,
                                CounterClockwiseStencilDepthBufferFail = createStencil.CounterClockwiseStencilDepthBufferFail,
                                StencilMask = createStencil.StencilMask,
                                StencilWriteMask = createStencil.StencilWriteMask,
                                ReferenceStencil = createStencil.ReferenceStencil
                            };


                            break;

                        case CMD_NEW_SAMPLER_STATE:

                            ref CreateSamplerStateCommand createSampler = ref command.NewCreateSamplerStateCommand;

                            resources[createSampler.id] = new SamplerState
                            {
                                AddressU = createSampler.AddressU,
                                AddressV = createSampler.AddressV,
                                AddressW = createSampler.AddressW,
                                Filter = createSampler.TextureFilter,
                                MaxAnisotropy = createSampler.MaxAnisotropy,
                                MaxMipLevel = createSampler.MaxMipLevel,
                                MipMapLevelOfDetailBias = createSampler.MipMapLevelOfDetailBias
                            };

                            break;

                        case CMD_BLEND_STATE:

                            device.BlendState = resources[command.SetBlendStateCommand.id] as BlendState;

                            break;

                        case CMD_RASTERIZE_STATE:

                            device.RasterizerState = resources[command.SetRasterizerStateCommand.id] as RasterizerState;

                            break;

                        case CMD_STENCIL_STATE:

                            device.DepthStencilState = resources[command.SetStencilStateCommand.id] as DepthStencilState;

                            break;

                        case CMD_SAMPLER_STATE:

                            device.SamplerStates[command.SetSamplerStateCommand.index] = resources[command.SetSamplerStateCommand.id] as SamplerState;

                            break;

                        case CMD_SET_VERTEX_DATA:

                            ref SetVertexDataCommand setVertexDataCommand = ref command.SetVertexDataCommand;

                            VertexBuffer vertex_buffer = resources[setVertexDataCommand.id] as VertexBuffer;

                            vertex_buffer?.SetDataPointerEXT(0, setVertexDataCommand.vertex_buffer_ptr, setVertexDataCommand.vertex_buffer_length, SetDataOptions.None);

                            break;

                        case CMD_SET_INDEX_DATA:

                            ref SetIndexDataCommand setIndexDataCommand = ref command.SetIndexDataCommand;

                            IndexBuffer index_buffer = resources[setIndexDataCommand.id] as IndexBuffer;

                            index_buffer?.SetDataPointerEXT(0, setIndexDataCommand.indices_buffer_ptr, setIndexDataCommand.indices_buffer_length, SetDataOptions.None);

                            break;

                        case CMD_CREATE_VERTEX_BUFFER:

                            ref CreateVertexBufferCommand createVertexBufferCommand = ref command.CreateVertexBufferCommand;

                            VertexElement[] elements = new VertexElement[createVertexBufferCommand.DeclarationCount];

                            for (int j = 0; j < elements.Length; j++)
                            {
                                elements[j] = ((VertexElement*)createVertexBufferCommand.Declarations)[j];
                            }

                            VertexBuffer vb = createVertexBufferCommand.IsDynamic ? new DynamicVertexBuffer(device, new VertexDeclaration(createVertexBufferCommand.Size, elements), createVertexBufferCommand.VertexElementsCount, createVertexBufferCommand.BufferUsage) : new VertexBuffer(device, new VertexDeclaration(createVertexBufferCommand.Size, elements), createVertexBufferCommand.VertexElementsCount, createVertexBufferCommand.BufferUsage);

                            resources[createVertexBufferCommand.id] = vb;

                            break;

                        case CMD_CREATE_INDEX_BUFFER:

                            ref CreateIndexBufferCommand createIndexBufferCommand = ref command.CreateIndexBufferCommand;

                            IndexBuffer ib = createIndexBufferCommand.IsDynamic ? new DynamicIndexBuffer(device, createIndexBufferCommand.IndexElementSize, createIndexBufferCommand.IndexCount, createIndexBufferCommand.BufferUsage) : new IndexBuffer(device, createIndexBufferCommand.IndexElementSize, createIndexBufferCommand.IndexCount, createIndexBufferCommand.BufferUsage);

                            resources[createIndexBufferCommand.id] = ib;

                            break;

                        case CMD_SET_VERTEX_BUFFER:

                            ref SetVertexBufferCommand setVertexBufferCommand = ref command.SetVertexBufferCommand;

                            vb = resources[setVertexBufferCommand.id] as VertexBuffer;

                            device.SetVertexBuffer(vb);

                            break;

                        case CMD_SET_INDEX_BUFFER:

                            ref SetIndexBufferCommand setIndexBufferCommand = ref command.SetIndexBufferCommand;

                            ib = resources[setIndexBufferCommand.id] as IndexBuffer;

                            device.Indices = ib;

                            break;

                        case CMD_CREATE_EFFECT:

                            ref CreateEffectCommand createEffectCommand = ref command.CreateEffectCommand;

                            break;

                        case CMD_CREATE_BASIC_EFFECT:

                            ref CreateBasicEffectCommand createBasicEffectCommand = ref command.CreateBasicEffectCommand;

                            if (!resources.TryGetValue(createBasicEffectCommand.id, out GraphicsResource res))
                            {
                                res = new BasicEffect(device);
                                resources[createBasicEffectCommand.id] = res;
                            }
                            else
                            {
                                BasicEffect be = res as BasicEffect;
                                be.World = createBasicEffectCommand.world;
                                be.View = createBasicEffectCommand.view;
                                be.Projection = createBasicEffectCommand.projection;
                                be.TextureEnabled = createBasicEffectCommand.texture_enabled;
                                be.Texture = resources[createBasicEffectCommand.texture_id] as Texture2D;
                                be.VertexColorEnabled = createBasicEffectCommand.vertex_color_enabled;

                                current_effect = be;
                            }

                            break;

                        case CMD_CREATE_TEXTURE_2D:

                            ref CreateTexture2DCommand createTexture2DCommand = ref command.CreateTexture2DCommand;

                            Texture2D texture;

                            if (createTexture2DCommand.IsRenderTarget)
                            {
                                texture = new RenderTarget2D
                                (
                                    device,
                                    createTexture2DCommand.Width,
                                    createTexture2DCommand.Height,
                                    false,
                                    createTexture2DCommand.Format,
                                    DepthFormat.Depth24Stencil8
                                );
                            }
                            else
                            {
                                texture = new Texture2D
                                (
                                    device,
                                    createTexture2DCommand.Width,
                                    createTexture2DCommand.Height,
                                    false,
                                    createTexture2DCommand.Format
                                );
                            }


                            resources[createTexture2DCommand.id] = texture;

                            break;

                        case CMD_SET_TEXTURE_DATA_2D:

                            ref SetTexture2DDataCommand setTexture2DDataCommand = ref command.SetTexture2DDataCommand;

                            texture = resources[setTexture2DDataCommand.id] as Texture2D;

                            texture?.SetDataPointerEXT(setTexture2DDataCommand.level, new Rectangle(setTexture2DDataCommand.x, setTexture2DDataCommand.y, setTexture2DDataCommand.width, setTexture2DDataCommand.height), setTexture2DDataCommand.data, setTexture2DDataCommand.data_length);

                            break;

                        case CMD_INDEXED_PRIMITIVE_DATA:

                            ref IndexedPrimitiveDataCommand indexedPrimitiveDataCommand = ref command.IndexedPrimitiveDataCommand;

                            //device.Textures[0] = resources[indexedPrimitiveDataCommand.texture_id] as Texture;

                            if (current_effect != null)
                            {
                                foreach (EffectPass pass in current_effect.CurrentTechnique.Passes)
                                {
                                    pass.Apply();

                                    device.DrawIndexedPrimitives
                                    (
                                        indexedPrimitiveDataCommand.PrimitiveType,
                                        indexedPrimitiveDataCommand.BaseVertex,
                                        indexedPrimitiveDataCommand.MinVertexIndex,
                                        indexedPrimitiveDataCommand.NumVertices,
                                        indexedPrimitiveDataCommand.StartIndex,
                                        indexedPrimitiveDataCommand.PrimitiveCount
                                    );
                                }
                            }
                            else
                            {
                                device.DrawIndexedPrimitives
                                (
                                    indexedPrimitiveDataCommand.PrimitiveType,
                                    indexedPrimitiveDataCommand.BaseVertex,
                                    indexedPrimitiveDataCommand.MinVertexIndex,
                                    indexedPrimitiveDataCommand.NumVertices,
                                    indexedPrimitiveDataCommand.StartIndex,
                                    indexedPrimitiveDataCommand.PrimitiveCount
                                );
                            }

                            break;

                        case CMD_DESTROY_RESOURCE:

                            ref DestroyResourceCommand destroyResourceCommand = ref command.DestroyResourceCommand;

                            resources[destroyResourceCommand.id]?.Dispose();

                            resources.Remove(destroyResourceCommand.id);

                            break;
                    }
                }


                device.Viewport = lastViewport;
                device.ScissorRectangle = lastScissorBox;
                device.BlendFactor = lastBlendFactor;
                device.BlendState = lastBlendState;
                device.RasterizerState = lastRasterizeState;
                device.DepthStencilState = lastDepthStencilState;
                device.SamplerStates[0] = lastsampler;
            }

            return res;
        }

        private int SendEvent(PluginEvent* ev)
            => _onEvent == null ? 1 : _onEvent((nint)ev, 0);


        public static PluginApi CreateFromLibrary
        (
            Microsoft.Xna.Framework.Game game,
            DirectoryInfo assetsPath,
            ClientVersion version,
            FileInfo pluginFile,
            string installFuncName
        )
        {
            Log.Trace($"creating plugin from library: {pluginFile.Name}");

            var libPtr = Native.LoadLibrary(pluginFile.FullName);
            if (libPtr == 0)
            {
                Log.Warn("plugin not loaded");
                return null;
            }

            var installPtr = (delegate* unmanaged[Cdecl]<PluginStruct*, nint>)Native.GetProcessAddress(libPtr, installFuncName);
            if (installPtr == null)
            {
                Log.Warn($"function '{installFuncName}' not found");
                return null;
            }

            return Create(game, assetsPath, version, pluginFile, installPtr);
        }

        public static PluginApi Create
        (
            Microsoft.Xna.Framework.Game game,
            DirectoryInfo assetsPath,
            ClientVersion version,
            FileInfo pluginFile,
            delegate* unmanaged[Cdecl]<PluginStruct*, nint> installFunc
        )
        {
            Log.Trace($"creating plugin: {pluginFile.Name}");

            var s = new PluginStruct();
            s.ApiVersion = 1;
            s.SdlWindow = game.Window.Handle;
            s.ClientVersion = (uint)version;
            s.AssetsPath = (nint)Unsafe.AsPointer(ref MemoryMarshal.AsRef<byte>(encodeToUtf8(assetsPath.FullName)));
            s.PluginPath = (nint)Unsafe.AsPointer(ref MemoryMarshal.AsRef<byte>(encodeToUtf8(Path.GetDirectoryName(pluginFile.FullName))));
            s.PluginToClientPacket = &pluginToClient;
            s.PluginToServerPacket = &pluginToServer;

            var onEvent = (delegate* unmanaged[Cdecl]<nint, nint, int>)installFunc(&s);
            if (onEvent == null)
            {
                Log.Warn("plugin didn't set the OnEvent function!");
            }

            return new PluginApi(game.GraphicsDevice, pluginFile, onEvent);


            [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvCdecl) })]
            static int pluginToClient(nint ptr, int size)
            {
                var span = new Span<byte>(ptr.ToPointer(), size);
                Log.Debug($"plugin to client -> {span[0]:X2} - {span.Length}");
                PacketHandlers.Handler.Append(span, true);
                return 1;
            }

            [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvCdecl) })]
            static int pluginToServer(nint ptr, int size)
            {
                var span = new Span<byte>(ptr.ToPointer(), size);
                Log.Debug($"plugin to server -> {span[0]:X2} - {span.Length}");
                NetClient.Socket.Send(span, true);
                return 1;
            }

            static Span<byte> encodeToUtf8(ReadOnlySpan<char> str)
            {
                var count = Encoding.UTF8.GetByteCount(str);
                Span<byte> span = new byte[count];
                fixed (char* ptr = str)
                fixed (byte* ptr2 = span)
                    Encoding.UTF8.GetBytes(ptr, str.Length, ptr2, count);
                return span;
            }
        }
    }


    enum PluginEventType
    {
        Mouse,
        Wheel,
        Keyboard,
        InputText,
        Window,
        Quit,
        Connect,
        Disconnect,
        FrameTick,
        OnPacket,
        Render
    }

    enum PluginWindowEventType
    {
        FocusGain,
        FocusLost,
        Maximize,
        Minimize,
        SizeChanged,
        PositionChanged
    }

    enum PluginAssetsType
    {
        Art,
        Gump,
        Cliloc,
        Tiledata
    }


    [StructLayout(LayoutKind.Explicit)]
    unsafe struct PluginEvent
    {
        [FieldOffset(0)] public PluginEventType EventType;

        [FieldOffset(0)] public PluginMouseEvent Mouse;
        [FieldOffset(0)] public PluginWheelEvent Wheel;
        [FieldOffset(0)] public PluginKeyboardEvent Keyboard;
        [FieldOffset(0)] public PluginInputTextEvent InputText;
        [FieldOffset(0)] public PluginWindowEvent Window;
        [FieldOffset(0)] public PluginQuitEvent Quit;
        [FieldOffset(0)] public PluginConnectEvent Connect;
        [FieldOffset(0)] public PluginDisconnectEvent Disconnect;
        [FieldOffset(0)] public PluginFrameTickEvent FrameTick;
        [FieldOffset(0)] public PluginOnPacketEvent OnPacket;
        [FieldOffset(0)] public PluginRenderEvent Render;
    }


    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PluginMouseEvent
    {
        private PluginEventType EventType;

        public MouseButtonType Button;
        public int X, Y;
        public bool IsPressed;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PluginWheelEvent
    {
        private PluginEventType EventType;

        public int X, Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PluginKeyboardEvent
    {
        private PluginEventType EventType;

        public int Keycode;
        public int Mods;
        public bool IsPressed;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PluginInputTextEvent
    {
        private PluginEventType EventType;

        public char InputChar;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PluginWindowEvent
    {
        private PluginEventType EventType;

        public PluginWindowEventType WindowEventType;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PluginQuitEvent
    {
        private PluginEventType EventType;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PluginConnectEvent
    {
        private PluginEventType EventType;
        public fixed char Server[64];
        public ushort Port;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PluginDisconnectEvent
    {
        private PluginEventType EventType;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PluginFrameTickEvent
    {
        private PluginEventType EventType;
        public uint Ticks;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PluginOnPacketEvent
    {
        private PluginEventType EventType;
        public nint PacketPtr;
        public int Size;
        public bool ClientToServer;
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PluginRenderEvent
    {
        private PluginEventType EventType;
        public BatchCommand* CommandListPtr;
        public int CommandListSize;
        public uint FrameTick;
    }

    

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct PluginStruct
    {
        public int ApiVersion;
        public nint SdlWindow;
        public uint ClientVersion;
        public nint AssetsPath;
        public nint PluginPath;
        public delegate* unmanaged[Cdecl]<nint, int, int> PluginToClientPacket, PluginToServerPacket;
    }
}
