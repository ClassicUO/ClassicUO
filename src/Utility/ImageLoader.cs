using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework;

using SDL2;

namespace ObjCRuntime
{
    [AttributeUsage(AttributeTargets.Method)]
    class MonoPInvokeCallbackAttribute : Attribute
    {
        public MonoPInvokeCallbackAttribute(Type t)
        {

        }
    }
}

namespace ClassicUO.Utility
{
    
    internal static class ImageLoader
    {
        private static unsafe IntPtr INTERNAL_convertSurfaceFormat(IntPtr surface)
        {
            IntPtr result = surface;
            unsafe
            {
                SDL.SDL_Surface* surPtr = (SDL.SDL_Surface*) surface;
                SDL.SDL_PixelFormat* pixelFormatPtr = (SDL.SDL_PixelFormat*) surPtr->format;

                // SurfaceFormat.Color is SDL_PIXELFORMAT_ABGR8888
                if (pixelFormatPtr->format != SDL.SDL_PIXELFORMAT_ABGR8888)
                {
                    // Create a properly formatted copy, free the old surface
                    result = SDL.SDL_ConvertSurfaceFormat(surface, SDL.SDL_PIXELFORMAT_ABGR8888, 0);
                    SDL.SDL_FreeSurface(surface);
                }
            }
            return result;
        }

        private static class FakeRWops
        {
            private static readonly Dictionary<IntPtr, Stream> streamMap =
                new Dictionary<IntPtr, Stream>();

            // Based on PNG_ZBUF_SIZE default
            private static byte[] temp = new byte[8192];

            private static readonly SDL.SDLRWopsSizeCallback sizeFunc = size;
            private static readonly SDL.SDLRWopsSeekCallback seekFunc = seek;
            private static readonly SDL.SDLRWopsReadCallback readFunc = read;
            private static readonly SDL.SDLRWopsWriteCallback writeFunc = write;
            private static readonly SDL.SDLRWopsCloseCallback closeFunc = close;
            private static readonly IntPtr sizePtr =
                Marshal.GetFunctionPointerForDelegate(sizeFunc);
            private static readonly IntPtr seekPtr =
                Marshal.GetFunctionPointerForDelegate(seekFunc);
            private static readonly IntPtr readPtr =
                Marshal.GetFunctionPointerForDelegate(readFunc);
            private static readonly IntPtr writePtr =
                Marshal.GetFunctionPointerForDelegate(writeFunc);
            private static readonly IntPtr closePtr =
                Marshal.GetFunctionPointerForDelegate(closeFunc);

            public static IntPtr Alloc(Stream stream)
            {
                IntPtr rwops = SDL.SDL_AllocRW();
                unsafe
                {
                    SDL.SDL_RWops* p = (SDL.SDL_RWops*) rwops;
                    p->size = sizePtr;
                    p->seek = seekPtr;
                    p->read = readPtr;
                    p->write = writePtr;
                    p->close = closePtr;
                }
                lock (streamMap)
                {
                    streamMap.Add(rwops, stream);
                }
                return rwops;
            }

            private static byte[] GetTemp(int len)
            {
                if (len > temp.Length)
                {
                    temp = new byte[len];
                }
                return temp;
            }

            [ObjCRuntime.MonoPInvokeCallback(typeof(SDL.SDLRWopsSizeCallback))]
            private static long size(IntPtr context)
            {
                Stream stream;
                lock (streamMap)
                {
                    stream = streamMap[context];
                }
                return stream.Length;
            }

            [ObjCRuntime.MonoPInvokeCallback(typeof(SDL.SDLRWopsSeekCallback))]
            private static long seek(IntPtr context, long offset, int whence)
            {
                Stream stream;
                lock (streamMap)
                {
                    stream = streamMap[context];
                }
                stream.Seek(offset, (SeekOrigin) whence);
                return stream.Position;
            }

            [ObjCRuntime.MonoPInvokeCallback(typeof(SDL.SDLRWopsReadCallback))]
            private static IntPtr read(
                IntPtr context,
                IntPtr ptr,
                IntPtr size,
                IntPtr maxnum
            )
            {
                Stream stream;
                int len = size.ToInt32() * maxnum.ToInt32();
                lock (streamMap)
                {
                    stream = streamMap[context];

                    // Other streams may contend for temp!
                    len = stream.Read(
                        GetTemp(len),
                        0,
                        len
                    );
                    Marshal.Copy(temp, 0, ptr, len);
                }
                return (IntPtr) len;
            }

            [ObjCRuntime.MonoPInvokeCallback(typeof(SDL.SDLRWopsWriteCallback))]
            private static IntPtr write(
                IntPtr context,
                IntPtr ptr,
                IntPtr size,
                IntPtr num
            )
            {
                Stream stream;
                int len = size.ToInt32() * num.ToInt32();
                lock (streamMap)
                {
                    stream = streamMap[context];

                    // Other streams may contend for temp!
                    Marshal.Copy(
                        ptr,
                        GetTemp(len),
                        0,
                        len
                    );
                    stream.Write(temp, 0, len);
                }
                return (IntPtr) len;
            }

            [ObjCRuntime.MonoPInvokeCallback(typeof(SDL.SDLRWopsCloseCallback))]
            public static int close(IntPtr context)
            {
                lock (streamMap)
                {
                    streamMap.Remove(context);
                }
                SDL.SDL_FreeRW(context);
                return 0;
            }
        }
        private static IntPtr TextureDataFromStreamInternal(
            Stream stream,
            int reqWidth,
            int reqHeight,
            bool zoom
        )
        {
            // Load the SDL_Surface* from RWops, get the image data
            IntPtr surface = SDL_image.IMG_Load_RW(
                FakeRWops.Alloc(stream),
                1
            );
            if (surface == IntPtr.Zero)
            {
                // File not found, supported, etc.
                FNALoggerEXT.LogError(
                    "TextureDataFromStream: " +
                    SDL.SDL_GetError()
                );
                return IntPtr.Zero;
            }
            surface = INTERNAL_convertSurfaceFormat(surface);

            // Image scaling, if applicable
            if (reqWidth != -1 && reqHeight != -1)
            {
                // Get the file surface dimensions now...
                int rw;
                int rh;
                unsafe
                {
                    SDL.SDL_Surface* surPtr = (SDL.SDL_Surface*) surface;
                    rw = surPtr->w;
                    rh = surPtr->h;
                }

                // Calculate the image scale factor
                bool scaleWidth;
                if (zoom)
                {
                    scaleWidth = rw < rh;
                }
                else
                {
                    scaleWidth = rw > rh;
                }
                float scale;
                if (scaleWidth)
                {
                    scale = reqWidth / (float) rw;
                }
                else
                {
                    scale = reqHeight / (float) rh;
                }

                // Calculate the scaled image size, crop if zoomed
                int resultWidth;
                int resultHeight;
                SDL.SDL_Rect crop = new SDL.SDL_Rect();
                if (zoom)
                {
                    resultWidth = reqWidth;
                    resultHeight = reqHeight;
                    if (scaleWidth)
                    {
                        crop.x = 0;
                        crop.w = rw;
                        crop.y = (int) (rh / 2 - (reqHeight / scale) / 2);
                        crop.h = (int) (reqHeight / scale);
                    }
                    else
                    {
                        crop.y = 0;
                        crop.h = rh;
                        crop.x = (int) (rw / 2 - (reqWidth / scale) / 2);
                        crop.w = (int) (reqWidth / scale);
                    }
                }
                else
                {
                    resultWidth = (int) (rw * scale);
                    resultHeight = (int) (rh * scale);
                }

                // Alloc surface, blit!
                IntPtr newSurface = SDL.SDL_CreateRGBSurface(
                    0,
                    resultWidth,
                    resultHeight,
                    32,
                    0x000000FF,
                    0x0000FF00,
                    0x00FF0000,
                    0xFF000000
                );
                SDL.SDL_SetSurfaceBlendMode(
                    surface,
                    SDL.SDL_BlendMode.SDL_BLENDMODE_NONE
                );
                if (zoom)
                {
                    SDL.SDL_BlitScaled(
                        surface,
                        ref crop,
                        newSurface,
                        IntPtr.Zero
                    );
                }
                else
                {
                    SDL.SDL_BlitScaled(
                        surface,
                        IntPtr.Zero,
                        newSurface,
                        IntPtr.Zero
                    );
                }
                SDL.SDL_FreeSurface(surface);
                surface = newSurface;
            }

            return surface;
        }
        private static unsafe void TextureDataClearAlpha(
            byte* pixels,
            int len
        )
        {
            /* Ensure that the alpha pixels are... well, actual alpha.
			 * You think this looks stupid, but be assured: Your paint program is
			 * almost certainly even stupider.
			 * -flibit
			 */
            for (int i = 0; i < len; i += 4, pixels += 4)
            {
                if (pixels[3] == 0)
                {
                    pixels[0] = 0;
                    pixels[1] = 0;
                    pixels[2] = 0;
                }
            }
        }
        public static void TextureDataFromStreamPtr(
            Stream stream,
            out int width,
            out int height,
            out IntPtr pixels,
            out int len,
            int reqWidth = -1,
            int reqHeight = -1,
            bool zoom = false
        )
        {
            IntPtr surface = TextureDataFromStreamInternal(
                                                           stream,
                                                           reqWidth,
                                                           reqHeight,
                                                           zoom
                                                          );
            if (surface == IntPtr.Zero)
            {
                width = 0;
                height = 0;
                pixels = IntPtr.Zero;
                len = 0;
                return;
            }

            // Copy surface data to output managed byte array
            unsafe
            {
                SDL.SDL_Surface* surPtr = (SDL.SDL_Surface*) surface;
                width = surPtr->w;
                height = surPtr->h;
                len = width * height * 4;
                pixels = Marshal.AllocHGlobal(len); // MUST be SurfaceFormat.Color!

                SDL.SDL_memcpy(pixels, surPtr->pixels, (IntPtr) len);
                TextureDataClearAlpha((byte*) pixels, len);
            }
            SDL.SDL_FreeSurface(surface);
        }
    }
}
