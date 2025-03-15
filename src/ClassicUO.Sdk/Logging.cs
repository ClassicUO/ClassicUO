using System;

namespace ClassicUO.Sdk;

public static class Log
{
    public static void Trace(string msg) => Console.WriteLine(msg);
    public static void Info(string msg) => Console.WriteLine(msg);
    public static void Warn(string msg) => Console.WriteLine(msg);
    public static void Error(string msg) => Console.WriteLine(msg);
}