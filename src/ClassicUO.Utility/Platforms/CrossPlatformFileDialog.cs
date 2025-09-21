using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ClassicUO.Utility.Platforms
{
    public static class CrossPlatformFileDialog
    {
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static bool IsOSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public static async Task<string> ShowOpenFileDialog(string title, string filter, string initialDirectory = null)
        {
            if (IsWindows)
            {
                return await ShowWindowsFileDialog(title, filter, initialDirectory, false);
            }
            else if (IsLinux)
            {
                return await ShowLinuxFileDialog(title, filter, initialDirectory, false);
            }
            else if (IsOSX)
            {
                return await ShowOSXFileDialog(title, filter, initialDirectory, false);
            }
            else
            {
                // Fallback to console input
                return ShowConsoleFileDialog(title, false);
            }
        }

        public static async Task<string> ShowSaveFileDialog(string title, string filter, string initialDirectory = null)
        {
            if (IsWindows)
            {
                return await ShowWindowsFileDialog(title, filter, initialDirectory, true);
            }
            else if (IsLinux)
            {
                return await ShowLinuxFileDialog(title, filter, initialDirectory, true);
            }
            else if (IsOSX)
            {
                return await ShowOSXFileDialog(title, filter, initialDirectory, true);
            }
            else
            {
                // Fallback to console input
                return ShowConsoleFileDialog(title, true);
            }
        }

        private static async Task<string> ShowWindowsFileDialog(string title, string filter, string initialDirectory, bool isSave)
        {
            try
            {
                // Try to use Windows native dialog via PowerShell
                var script = isSave ? 
                    $"Add-Type -AssemblyName System.Windows.Forms; $dialog = New-Object System.Windows.Forms.SaveFileDialog; $dialog.Title = '{title}'; $dialog.Filter = '{filter}'; if ('{initialDirectory}' -ne '') {{ $dialog.InitialDirectory = '{initialDirectory}' }}; if ($dialog.ShowDialog() -eq 'OK') {{ Write-Output $dialog.FileName }}" :
                    $"Add-Type -AssemblyName System.Windows.Forms; $dialog = New-Object System.Windows.Forms.OpenFileDialog; $dialog.Title = '{title}'; $dialog.Filter = '{filter}'; if ('{initialDirectory}' -ne '') {{ $dialog.InitialDirectory = '{initialDirectory}' }}; if ($dialog.ShowDialog() -eq 'OK') {{ Write-Output $dialog.FileName }}";
                
                var result = await RunProcessAsync("powershell", $"-Command \"{script}\"");
                return string.IsNullOrEmpty(result) ? null : result.Trim();
            }
            catch
            {
                return ShowConsoleFileDialog(title, isSave);
            }
        }

        private static async Task<string> ShowLinuxFileDialog(string title, string filter, string initialDirectory, bool isSave)
        {
            try
            {
                // Try to use zenity if available
                var zenityPath = FindExecutable("zenity");
                if (!string.IsNullOrEmpty(zenityPath))
                {
                    var args = isSave ? "--save" : "--file-selection";
                    args += $" --title=\"{title}\"";
                    if (!string.IsNullOrEmpty(initialDirectory))
                    {
                        args += $" --filename=\"{initialDirectory}\"";
                    }
                    
                    var result = await RunProcessAsync(zenityPath, args);
                    return string.IsNullOrEmpty(result) ? null : result.Trim();
                }
                
                // Try kdialog as fallback
                var kdialogPath = FindExecutable("kdialog");
                if (!string.IsNullOrEmpty(kdialogPath))
                {
                    var args = isSave ? "--getsavefilename" : "--getopenfilename";
                    args += $" \"{initialDirectory ?? "."}\"";
                    if (!string.IsNullOrEmpty(filter))
                    {
                        args += $" \"{filter}\"";
                    }
                    
                    var result = await RunProcessAsync(kdialogPath, args);
                    return string.IsNullOrEmpty(result) ? null : result.Trim();
                }
                
                return ShowConsoleFileDialog(title, isSave);
            }
            catch
            {
                return ShowConsoleFileDialog(title, isSave);
            }
        }

        private static async Task<string> ShowOSXFileDialog(string title, string filter, string initialDirectory, bool isSave)
        {
            try
            {
                // Use osascript to show a native macOS dialog
                var script = isSave ? 
                    $"choose file name with prompt \"{title}\"" :
                    $"choose file with prompt \"{title}\"";
                
                if (!string.IsNullOrEmpty(initialDirectory))
                {
                    script += $" default location (path to \"{initialDirectory}\")";
                }
                
                var result = await RunProcessAsync("osascript", $"-e '{script}'");
                return string.IsNullOrEmpty(result) ? null : result.Trim().Trim('"');
            }
            catch
            {
                return ShowConsoleFileDialog(title, isSave);
            }
        }

        private static string ShowConsoleFileDialog(string title, bool isSave)
        {
            Console.WriteLine($"\n{title}");
            Console.WriteLine($"Enter file path {(isSave ? "to save" : "to open")}:");
            return Console.ReadLine();
        }

        private static string FindExecutable(string executableName)
        {
            var path = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(path))
                return null;

            var paths = path.Split(Path.PathSeparator);
            foreach (var p in paths)
            {
                var fullPath = Path.Combine(p, executableName);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return null;
        }

        private static async Task<string> RunProcessAsync(string fileName, string arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                        return null;

                    process.WaitForExit();
                    
                    if (process.ExitCode == 0)
                    {
                        return process.StandardOutput.ReadToEnd();
                    }
                    else
                    {
                        var error = process.StandardError.ReadToEnd();
                        Console.WriteLine($"Error running {fileName}: {error}");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception running {fileName}: {ex.Message}");
                return null;
            }
        }
    }
}
