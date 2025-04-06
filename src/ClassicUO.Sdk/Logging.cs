using System;

namespace ClassicUO.Sdk;

public static class Log
{
    public static void Trace(string msg) => ComposeLog(msg, LogMessageType.Trace);
    public static void Info(string msg) => ComposeLog(msg, LogMessageType.Info);
    public static void Warn(string msg) => ComposeLog(msg, LogMessageType.Warn);
    public static void Error(string msg) => ComposeLog(msg, LogMessageType.Error);


    private enum LogMessageType : byte
    {
        Trace,
        Info,
        Warn,
        Error
    }

    private static readonly string[] _prefix = [
        "{0} | \u001b[90mTRACE\u001b[0m | {1}", // Gray
        "{0} | \u001b[97m INFO\u001b[0m | {1}", // White
        "{0} | \u001b[33m WARN\u001b[0m | {1}", // Yellow
        "{0} | \u001b[31mERROR\u001b[0m | {1}", // Red
    ];

    private static void ComposeLog(string msg, LogMessageType logMessageType)
    {
        var prefix = _prefix[(int)logMessageType];

        Console.WriteLine(prefix, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff"), msg);
    }
}