using System;
using System.Runtime.InteropServices;

static class Program
{
    [DllImport("ClassicUO", EntryPoint = "Initialize")]
    static extern void Initialize(string[] args, int count);

    [STAThread]
    static void Main(string[] args)
        => Initialize(args, args.Length);
}