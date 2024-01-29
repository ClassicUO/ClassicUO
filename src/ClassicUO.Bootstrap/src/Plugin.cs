using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using CUO_API;

interface IPluginHandler
{
    short GetPacketLen(Guid id, byte packetId);
    void CastSpell(Guid id, int index);
    bool SendToClient(Guid id, IntPtr data, ref int length);
    bool SendToServer(Guid id, IntPtr data, ref int length);
    bool SendToClient(Guid id, ref byte[] data, ref int length);
    bool SendToServer(Guid id, ref byte[] data, ref int length);
    void SetWindowTitle(Guid id, string title);
    string GetCliloc(Guid id, int cliloc, string args, bool capitalize);
    bool RequestMove(Guid id, int dir, bool run);
    bool GetPlayerPosition(Guid id, out int x, out int y, out int z);
}


sealed class Plugin
{
    public static List<Plugin> Plugins { get; } = new List<Plugin>();



    private readonly IPluginHandler _server;
    private readonly Dictionary<int, short> _packetLengthCache = new Dictionary<int, short>();

    public Plugin(IPluginHandler server, Guid clientID)
    {
        _server = server;
        ClientID = clientID;
    }


    public Guid ClientID { get; }
    public string AssetsPath { get; private set; }

    public unsafe void Load(IntPtr sdlWindow, string pluginPath, uint clientVersion, string assetsPath)
    {
        if (!File.Exists(pluginPath))
            return;

        AssetsPath = assetsPath;

        if (
            Environment.OSVersion.Platform != PlatformID.Unix
            && Environment.OSVersion.Platform != PlatformID.MacOSX
        )
        {
            UnblockPath(Path.GetDirectoryName(pluginPath));
        }

        var asm = Assembly.LoadFile(pluginPath);
        var type = asm.GetType("Assistant.Engine");

        if (type == null)
        {
            return;
        }

        var meth = type.GetMethod(
            "Install",
            BindingFlags.Public | BindingFlags.Static
        );

        if (meth == null)
        {
            return;
        }

        var hwnd_TODO = IntPtr.Zero;

        _recv = OnPluginRecv;
        _send = OnPluginSend;
        _recv_new = OnPluginRecv_new;
        _send_new = OnPluginSend_new;
        _getPacketLength = GetPacketLength;
        _getPlayerPosition = GetPlayerPosition;
        _castSpell = CastSpell;
        _getStaticImage = GetStaticImage;
        _getUoFilePath = GetUOFilePath;
        _requestMove = RequestMove;
        _setTitle = SetWindowTitle;
        _get_static_data = GetStaticData;
        _get_tile_data = GetTileData;
        _get_cliloc = GetCliloc;

        var header = new PluginHeader()
        {
            ClientVersion = (int)clientVersion,
            Recv = Marshal.GetFunctionPointerForDelegate(_recv),
            Send = Marshal.GetFunctionPointerForDelegate(_send),
            GetPacketLength = Marshal.GetFunctionPointerForDelegate(_getPacketLength),
            GetPlayerPosition = Marshal.GetFunctionPointerForDelegate(_getPlayerPosition),
            CastSpell = Marshal.GetFunctionPointerForDelegate(_castSpell),
            GetStaticImage = Marshal.GetFunctionPointerForDelegate(_getStaticImage),
            HWND = hwnd_TODO,
            GetUOFilePath = Marshal.GetFunctionPointerForDelegate(_getUoFilePath),
            RequestMove = Marshal.GetFunctionPointerForDelegate(_requestMove),
            SetTitle = Marshal.GetFunctionPointerForDelegate(_setTitle),
            Recv_new = Marshal.GetFunctionPointerForDelegate(_recv_new),
            Send_new = Marshal.GetFunctionPointerForDelegate(_send_new),
            SDL_Window = sdlWindow,
            GetStaticData = Marshal.GetFunctionPointerForDelegate(_get_static_data),
            GetTileData = Marshal.GetFunctionPointerForDelegate(_get_tile_data),
            GetCliloc = Marshal.GetFunctionPointerForDelegate(_get_cliloc)
        };

        meth.Invoke(null, new object[] { (IntPtr)(&header) });

        if (header.OnRecv != IntPtr.Zero)
        {
            _onRecv = Marshal.GetDelegateForFunctionPointer<OnPacketSendRecv>(header.OnRecv);
        }

        if (header.OnSend != IntPtr.Zero)
        {
            _onSend = Marshal.GetDelegateForFunctionPointer<OnPacketSendRecv>(header.OnSend);
        }

        if (header.OnHotkeyPressed != IntPtr.Zero)
        {
            _onHotkeyPressed = Marshal.GetDelegateForFunctionPointer<OnHotkey>(
                header.OnHotkeyPressed
            );
        }

        if (header.OnMouse != IntPtr.Zero)
        {
            _onMouse = Marshal.GetDelegateForFunctionPointer<OnMouse>(header.OnMouse);
        }

        if (header.OnPlayerPositionChanged != IntPtr.Zero)
        {
            _onUpdatePlayerPosition =
                Marshal.GetDelegateForFunctionPointer<OnUpdatePlayerPosition>(
                    header.OnPlayerPositionChanged
                );
        }

        if (header.OnClientClosing != IntPtr.Zero)
        {
            _onClientClose = Marshal.GetDelegateForFunctionPointer<OnClientClose>(
                header.OnClientClosing
            );
        }

        if (header.OnInitialize != IntPtr.Zero)
        {
            _onInitialize = Marshal.GetDelegateForFunctionPointer<OnInitialize>(
                header.OnInitialize
            );
        }

        if (header.OnConnected != IntPtr.Zero)
        {
            _onConnected = Marshal.GetDelegateForFunctionPointer<OnConnected>(
                header.OnConnected
            );
        }

        if (header.OnDisconnected != IntPtr.Zero)
        {
            _onDisconnected = Marshal.GetDelegateForFunctionPointer<OnDisconnected>(
                header.OnDisconnected
            );
        }

        if (header.OnFocusGained != IntPtr.Zero)
        {
            _onFocusGained = Marshal.GetDelegateForFunctionPointer<OnFocusGained>(
                header.OnFocusGained
            );
        }

        if (header.OnFocusLost != IntPtr.Zero)
        {
            _onFocusLost = Marshal.GetDelegateForFunctionPointer<OnFocusLost>(
                header.OnFocusLost
            );
        }

        if (header.Tick != IntPtr.Zero)
        {
            _tick = Marshal.GetDelegateForFunctionPointer<OnTick>(header.Tick);
        }

        if (header.OnRecv_new != IntPtr.Zero)
        {
            _onRecv_new = Marshal.GetDelegateForFunctionPointer<OnPacketSendRecv_new>(
                header.OnRecv_new
            );
        }

        if (header.OnSend_new != IntPtr.Zero)
        {
            _onSend_new = Marshal.GetDelegateForFunctionPointer<OnPacketSendRecv_new>(
                header.OnSend_new
            );
        }

        if (header.OnDrawCmdList != IntPtr.Zero)
        {
            _draw_cmd_list = Marshal.GetDelegateForFunctionPointer<OnDrawCmdList>(
                header.OnDrawCmdList
            );
        }

        if (header.OnWndProc != IntPtr.Zero)
        {
            _on_wnd_proc = Marshal.GetDelegateForFunctionPointer<OnWndProc>(header.OnWndProc);
        }

        _onInitialize?.Invoke();

        Plugins.Add(this);
    }



    //Code from https://stackoverflow.com/questions/6374673/unblock-file-from-within-net-4-c-sharp
    private static void UnblockPath(string path)
    {
        string[] files = Directory.GetFiles(path);
        string[] dirs = Directory.GetDirectories(path);

        foreach (string file in files)
        {
            if (file.EndsWith("dll") || file.EndsWith("exe"))
            {
                UnblockFile(file);
            }
        }

        foreach (string dir in dirs)
        {
            UnblockPath(dir);
        }
    }

    private static bool UnblockFile(string fileName)
    {
        return DeleteFile(fileName + ":Zone.Identifier");
    }

    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeleteFile(string name);

    public void Close()
    {
        OnClosing();
    }


    short GetPacketLength(int packetId)
    {
        if (_packetLengthCache.TryGetValue(packetId, out var len))
            return len;

        len = _server.GetPacketLen(ClientID, (byte)packetId);
        _packetLengthCache.Add(packetId, len);

        return len;
    }

    void CastSpell(int index)
    {
        _server.CastSpell(ClientID, index);
    }

    bool OnPluginRecv(ref byte[] data, ref int length)
    {
        return _server.SendToClient(ClientID, ref data, ref length);
    }

    bool OnPluginSend(ref byte[] data, ref int length)
    {
        return _server.SendToServer(ClientID, ref data, ref length);
    }

    bool OnPluginRecv_new(IntPtr buffer, ref int length)
    {
        return _server.SendToClient(ClientID, buffer, ref length);
    }

    bool OnPluginSend_new(IntPtr buffer, ref int length)
    {
        return _server.SendToServer(ClientID, buffer, ref length);
    }

    string GetUOFilePath()
    {
        // get from cuo
        return AssetsPath;
    }

    void SetWindowTitle(string str)
    {
        // get from cuo
        _server.SetWindowTitle(ClientID, str);
    }

    bool GetStaticData(
        int index,
        ref ulong flags,
        ref byte weight,
        ref byte layer,
        ref int count,
        ref ushort animid,
        ref ushort lightidx,
        ref byte height,
        ref string name
    )
    {
        // get from cuo

        return true;
    }

    bool GetTileData(
        int index,
        ref ulong flags,
        ref ushort textid,
        ref string name
    )
    {
        // get from cuo

        return true;
    }

    bool GetCliloc(int cliloc, string args, bool capitalize, out string buffer)
    {
        buffer = _server.GetCliloc(ClientID, cliloc, args, capitalize);

        return !string.IsNullOrEmpty(buffer);
    }

    void GetStaticImage(ushort g, ref CUO_API.ArtInfo info)
    {

    }

    bool RequestMove(int dir, bool run)
    {
        return _server.RequestMove(ClientID, dir, run);
    }

    bool GetPlayerPosition(out int x, out int y, out int z)
    {
        return _server.GetPlayerPosition(ClientID, out x, out y, out z);
    }

    public void Tick()
    {
        _tick?.Invoke();
    }

    public bool ProcessRecvPacket(ref byte[] data, ref int length)
    {
        var result = true;
        if (_onRecv_new != null)
        {
            result = _onRecv_new(data, ref length);
        }
        else if (_onRecv != null)
        {
            var tmp = data;
            result = _onRecv(ref data, ref length);

            if (!ReferenceEquals(tmp, data))
            {
                Array.Copy(data, tmp, length);
                data = tmp;
            }
        }
        
        // get from cuo
        
        return result;
    }

    public bool ProcessSendPacket(ref byte[] data, ref int length)
    {
        var result = true;
        if (_onSend_new != null)
        {
            result = _onSend_new(data, ref length);
        }
        else if (_onSend != null)
        {
            var tmp = data;
            result = _onSend(ref data, ref length);

            if (!ReferenceEquals(tmp, data))
            {
                Array.Copy(data, tmp, length);
                data = tmp;
            }
        }

        // get from cuo

        return result;
    }

    void OnClosing()
    {
        // get from cuo
        _onClientClose?.Invoke();
    }

    public void FocusGained()
    {
        _onFocusGained?.Invoke();
    }

    public void FocusLost()
    {
        _onFocusLost?.Invoke();
    }

    public void Connected()
    {
        _onConnected?.Invoke();
    }

    public void Disconnected()
    {
        _onDisconnected?.Invoke();
    }

    public bool ProcessHotkeys(int key, int mod, bool ispressed)
    {
        var result = _onHotkeyPressed?.Invoke(key, mod, ispressed) ?? true;

        return result;
    }

    public void ProcessMouse(int button, int wheel)
    {
        _onMouse?.Invoke(button, wheel);
    }

    public unsafe int ProcessWndProc(void* e)
    {
        var result = _on_wnd_proc?.Invoke(e) ?? 0;

        return result;
    }

    public void UpdatePlayerPosition(int x, int y, int z)
    {
        try
        {
            _onUpdatePlayerPosition?.Invoke(x, y, z);
        }
        catch
        {
        }
    }

    public void GetCommandList(out IntPtr listPtr, out int listLen)
    {
        listPtr = IntPtr.Zero;
        listLen = 0;
        _draw_cmd_list?.Invoke(out listPtr, ref listLen);
    }


    [MarshalAs(UnmanagedType.FunctionPtr)]
    private OnCastSpell _castSpell;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    private OnDrawCmdList _draw_cmd_list;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    private OnGetCliloc _get_cliloc;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    private OnGetStaticData _get_static_data;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    private OnGetTileData _get_tile_data;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    private OnGetPacketLength _getPacketLength;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    private OnGetPlayerPosition _getPlayerPosition;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    private OnGetStaticImage _getStaticImage;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    private OnGetUOFilePath _getUoFilePath;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    private OnWndProc _on_wnd_proc;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    private OnClientClose _onClientClose;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    private OnConnected _onConnected;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    private OnDisconnected _onDisconnected;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    private OnFocusGained _onFocusGained;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    private OnFocusLost _onFocusLost;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    private OnHotkey _onHotkeyPressed;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    private OnInitialize _onInitialize;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    private OnMouse _onMouse;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    private OnPacketSendRecv_new _onRecv_new,
        _onSend_new;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    private OnUpdatePlayerPosition _onUpdatePlayerPosition;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    private OnPacketSendRecv _recv,
        _send,
        _onRecv,
        _onSend;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    private OnPacketSendRecv_new_intptr _recv_new,
        _send_new;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    private RequestMove _requestMove;
    private readonly Dictionary<IntPtr, object> _resources =
        new Dictionary<IntPtr, object>();

    [MarshalAs(UnmanagedType.FunctionPtr)]
    private OnSetTitle _setTitle;

    [MarshalAs(UnmanagedType.FunctionPtr)]
    private OnTick _tick;





    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate void OnInstall(void* header);

    [return: MarshalAs(UnmanagedType.I1)]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate bool OnPacketSendRecv_new(byte[] data, ref int length);

    [return: MarshalAs(UnmanagedType.I1)]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate bool OnPacketSendRecv_new_intptr(IntPtr data, ref int length);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int OnDrawCmdList([Out] out IntPtr cmdlist, ref int size);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate int OnWndProc(void* ev);

    [return: MarshalAs(UnmanagedType.I1)]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate bool OnGetStaticData(
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
    private delegate bool OnGetTileData(
        int index,
        ref ulong flags,
        ref ushort textid,
        ref string name
    );

    [return: MarshalAs(UnmanagedType.I1)]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate bool OnGetCliloc(
        int cliloc,
        [MarshalAs(UnmanagedType.LPStr)] string args,
        bool capitalize,
        [Out][MarshalAs(UnmanagedType.LPStr)] out string buffer
    );
}

struct PluginHeader
{
    public int ClientVersion;
    public IntPtr HWND;
    public IntPtr OnRecv;
    public IntPtr OnSend;
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
    public IntPtr Recv;
    public IntPtr Send;
    public IntPtr GetPacketLength;
    public IntPtr GetPlayerPosition;
    public IntPtr CastSpell;
    public IntPtr GetStaticImage;
    public IntPtr Tick;
    public IntPtr RequestMove;
    public IntPtr SetTitle;

    public IntPtr OnRecv_new,
        OnSend_new,
        Recv_new,
        Send_new;

    public IntPtr OnDrawCmdList;
    public IntPtr SDL_Window;
    public IntPtr OnWndProc;
    public IntPtr GetStaticData;
    public IntPtr GetTileData;
    public IntPtr GetCliloc;
}

