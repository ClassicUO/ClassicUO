using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ClassicUO.Network
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    unsafe partial class Plugin
    {
        public struct PluginHeader
        {
            public int ClientVersion;
            public IntPtr HWND;
            [Obsolete] public IntPtr OnRecv_OBSOLETE_DO_NOT_USE;
            [Obsolete] public IntPtr OnSend_OBSOLETE_DO_NOT_USE;
            public IntPtr OnHotkeyPressed;
            public IntPtr OnMouse;
            public IntPtr OnPlayerPositionChanged;
            public IntPtr OnClientClosing;
            public IntPtr OnInitialize;
            public IntPtr OnConnected;
            public IntPtr OnDisconnected;
            public IntPtr OnFocusGained;
            public IntPtr OnFocusLost;
            public IntPtr GetUOFilePath;
            [Obsolete] public IntPtr Recv_OBSOLETE_DO_NOT_USE;
            [Obsolete] public IntPtr Send_OBSOLETE_DO_NOT_USE;
            public IntPtr GetPacketLength;
            public IntPtr GetPlayerPosition;
            public IntPtr CastSpell;
            public IntPtr GetStaticImage;
            public IntPtr Tick;
            public IntPtr RequestMove;
            public IntPtr SetTitle;

            public IntPtr OnRecv_new, OnSend_new, Recv_new, Send_new;

            public IntPtr OnDrawCmdList;
            public IntPtr SDL_Window;
            public IntPtr OnWndProc;
            public IntPtr GetStaticData;
            public IntPtr GetTileData;
            public IntPtr GetCliloc;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void dOnInstall(void* header);

        [return: MarshalAs(UnmanagedType.I1)]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool dOnPacketSendRecv_new(byte[] data, ref int length);

        [return: MarshalAs(UnmanagedType.I1)]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool dOnPacketSendRecv_new_intptr(IntPtr data, ref int length);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int dOnDrawCmdList([Out] out IntPtr cmdlist, ref int size);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int dOnWndProc(void* ev);

        [return: MarshalAs(UnmanagedType.I1)]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool dOnGetStaticData
        (
            int index,
            ref ulong flags,
            ref byte weight,
            ref byte layer,
            ref int count,
            ref ushort animid,
            ref ushort lightidx,
            ref byte height,
            ref string name
        );

        [return: MarshalAs(UnmanagedType.I1)]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool dOnGetTileData(int index, ref ulong flags, ref ushort textid, ref string name);

        [return: MarshalAs(UnmanagedType.I1)]
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool dOnGetCliloc(int cliloc, [MarshalAs(UnmanagedType.LPStr)] string args, bool capitalize, [Out][MarshalAs(UnmanagedType.LPStr)] out string buffer);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate string dOnGetUOFilePath();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate short dOnGetPacketLength(int id);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool dOnGetPlayerPosition(out int x, out int y, out int z);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void dOnCastSpell(int idx);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void dOnGetStaticImage(ushort g, ref ArtInfo art);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ArtInfo
        {
            public long Address;
            public long Size;
            public long CompressedSize;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void dOnTick();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool dRequestMove(int dir, bool run);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void dOnSetTitle(string title);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate bool dOnHotkey(int key, int mod, bool pressed);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void dOnMouse(int button, int wheel);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void dOnUpdatePlayerPosition(int x, int y, int z);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void dOnClientClose();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void dOnInitialize();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void dOnConnected();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void dOnDisconnected();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void dOnFocusGained();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void dOnFocusLost();
    }
}
