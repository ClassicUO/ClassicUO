using ClassicUO.Renderer.Batching;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Network.Plugins
{
    static unsafe partial class PluginManager
    {
        public static int Initialize()
        {
            ClientFunctions clientFunc = new ClientFunctions();
            clientFunc.SendPacketToClient = _sendPacketToClientPtr;
            clientFunc.SendPacketToServer = _sendPacketToServerPtr;
            clientFunc.MovePlayer = _movePlayerPtr;
            clientFunc.SetGameWindowTitle = _setGameWindowTitlePtr;
            clientFunc.GetCliloc = _getClilocPtr;
            clientFunc.GetUltimaOnlinePath = _getUltimaOnlinePathPtr;
            clientFunc.GetPacketLength = _getPacketLengthPtr;

            return SendCommand(PluginEventType.Initialize, (IntPtr)Unsafe.AsPointer(ref clientFunc), out _);
        }

        public static int Close()
        {
            return SendCommand(PluginEventType.Close, IntPtr.Zero, out _);
        }

        public static int OnConnect()
        {
            return SendCommand(PluginEventType.Connect, IntPtr.Zero, out _);
        }

        public static int OnDisconnect()
        {
            return SendCommand(PluginEventType.Disconnect, IntPtr.Zero, out _);
        }

        public static int OnPacketSend(IntPtr buffer, ref int size, out bool filtered)
        {
            BufferDescriptor packetInfo = new BufferDescriptor();
            packetInfo.Buffer = buffer;
            packetInfo.LengthPtr = (IntPtr)Unsafe.AsPointer(ref size);

            return SendCommand(PluginEventType.PacketSend, (IntPtr)Unsafe.AsPointer(ref packetInfo), out filtered);
        }

        public static int OnPacketRecv(IntPtr buffer, ref int size, out bool filtered)
        {
            BufferDescriptor packetInfo = new BufferDescriptor();
            packetInfo.Buffer = buffer;
            packetInfo.LengthPtr = (IntPtr)Unsafe.AsPointer(ref size);

            return SendCommand(PluginEventType.PacketRecv, (IntPtr)Unsafe.AsPointer(ref packetInfo), out filtered);
        }

        public static int OnTick(uint ticks)
        {
            return SendCommand(PluginEventType.Tick, (IntPtr)Unsafe.AsPointer(ref ticks), out _);
        }

        public static int OnDraw(GraphicsDevice device)
        {
            BufferDescriptor packetInfo = new BufferDescriptor();
            packetInfo.Buffer = IntPtr.Zero;
            packetInfo.LengthPtr = IntPtr.Zero;

            int res = SendCommand(PluginEventType.Draw, (IntPtr)Unsafe.AsPointer(ref packetInfo), out _);

            if (res > 0)
            {
                if (packetInfo.Buffer != IntPtr.Zero && packetInfo.LengthPtr != IntPtr.Zero)
                {
                    HandleCmdList(device, packetInfo.Buffer, *(int*)packetInfo.LengthPtr, _resources);
                }
            }
            else
            {
                Console.WriteLine("draw returns {0}", res);
            }

            return res;
        }

        public static int OnPlayerPosition(int x, int y, int z)
        {
            PlayerPosition playerPosition = new PlayerPosition();
            playerPosition.X = x;
            playerPosition.Y = y;
            playerPosition.Z = z;

            return SendCommand(PluginEventType.SendPlayerPosition, (IntPtr)Unsafe.AsPointer(ref playerPosition), out _);
        }



        private static void HandleCmdList(GraphicsDevice device, IntPtr ptr, int length, IDictionary<IntPtr, GraphicsResource> resources)
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


            Viewport lastViewport = device.Viewport;
            Rectangle lastScissorBox = device.ScissorRectangle;

            Color lastBlendFactor = device.BlendFactor;
            BlendState lastBlendState = device.BlendState;
            RasterizerState lastRasterizeState = device.RasterizerState;
            DepthStencilState lastDepthStencilState = device.DepthStencilState;
            SamplerState lastsampler = device.SamplerStates[0];

            for (int i = 0; i < length; i++)
            {
                BatchCommand command = ((BatchCommand*)ptr)[i];

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

    }
}
