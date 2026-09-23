// SPDX-License-Identifier: BSD-2-Clause

using System.Globalization;

namespace ClassicUO.BootstrapHost;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            try
            {
                var dir = Path.Combine(AppContext.BaseDirectory, "Logs");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_bootstraphost_crash.txt");
                File.WriteAllText(path,
                    $"BootstrapHost crash {DateTime.Now:o}{Environment.NewLine}" +
                    $"Thread: {Thread.CurrentThread.Name}{Environment.NewLine}" +
                    $"OS: {Environment.OSVersion}{Environment.NewLine}{Environment.NewLine}" +
                    e.ExceptionObject);
            }
            catch { }
        };

        var bridge = new HostBridge();
        return bridge.Run(args);
    }
}
