// SPDX-License-Identifier: BSD-2-Clause
//
// `agent-desktop scenario --pid <id> --script <json> --out <dir> --label <name>`
//
// Drives the target game window through a sequence of input steps,
// capturing a screenshot after each interaction. Branch-agnostic: the
// same script + same coordinates produce comparable output across the
// ECS and main branches.
//
// Script schema (JSON):
//   { "steps": [
//       { "op": "sleep",         "ms": 1500 },
//       { "op": "shot",          "name": "login-before" },
//       { "op": "move",          "x": 100, "y": 200 },
//       { "op": "click",         "x": 100, "y": 200, "button": "left" },
//       { "op": "double-click",  "x": 100, "y": 200, "button": "left" },
//       { "op": "hold",          "x": 100, "y": 200, "button": "right" },
//       { "op": "release",       "x": 100, "y": 200, "button": "right" },
//       { "op": "type",          "text": "admin" }
//     ] }
//
// Screenshots: <out>/<label>__<NN>__<name>.png and .raw.bin. The NN
// counter increments on every `shot` step; `name` defaults to the
// previous op's name if omitted.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading;
using ClassicUO.Agent.Desktop.OsDriver;

namespace ClassicUO.Agent.Desktop.Commands;

[SupportedOSPlatform("windows5.0")]
internal static class ScenarioCommand
{
    public static Command Build()
    {
        var pid = new Option<int?>("--pid", "Target process id.");
        var title = new Option<string?>("--title", "Substring of target window title.");
        var script = new Option<string>("--script", "Path to JSON scenario script.") { IsRequired = true };
        var outDir = new Option<string>("--out", "Directory to write screenshots into.") { IsRequired = true };
        var label = new Option<string>("--label", "Label prefix for output files (e.g. 'main' or 'ecs').") { IsRequired = true };

        var cmd = new Command("scenario",
            "Run a JSON scripted input scenario against the target window, capturing screenshots at each `shot` step.");
        cmd.AddOption(pid);
        cmd.AddOption(title);
        cmd.AddOption(script);
        cmd.AddOption(outDir);
        cmd.AddOption(label);

        cmd.SetHandler((int? pidVal, string? titleVal, string scriptPath, string outVal, string labelVal) =>
        {
            var hwnd = TargetResolver.Resolve(pidVal, titleVal);
            if (hwnd == IntPtr.Zero)
            {
                CliJson.Error("could not locate target window");
                Environment.ExitCode = 1;
                return;
            }

            List<JsonElement> steps;
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(scriptPath));
                var arr = doc.RootElement.GetProperty("steps").EnumerateArray();
                steps = new List<JsonElement>();
                foreach (var e in arr) steps.Add(e.Clone());
            }
            catch (Exception ex)
            {
                CliJson.Error($"failed to parse script: {ex.Message}");
                Environment.ExitCode = 1;
                return;
            }

            Directory.CreateDirectory(outVal);
            var shotCounter = 0;

            foreach (var step in steps)
            {
                var op = step.GetProperty("op").GetString() ?? "";
                switch (op)
                {
                    case "sleep":
                        var ms = step.GetProperty("ms").GetInt32();
                        Thread.Sleep(ms);
                        CliJson.Status(new { step = op, ms });
                        break;

                    case "shot":
                        var shotName = step.TryGetProperty("name", out var n) ? n.GetString() ?? "shot" : "shot";
                        var pngPath = Path.Combine(outVal,
                            $"{labelVal}__{shotCounter:D2}__{shotName}.png");
                        var bgra = Capture.CaptureBgra(hwnd, out var w, out var h);
                        var bgraForRaw = (byte[])bgra.Clone();
                        File.WriteAllBytes(pngPath, Capture.PngEncode(bgra, w, h));
                        RawImage.Write(Path.ChangeExtension(pngPath, ".raw.bin"), bgraForRaw, w, h);
                        CliJson.Status(new { step = op, name = shotName, path = pngPath });
                        shotCounter++;
                        break;

                    case "move":
                        InputDriver.MouseMove(hwnd, step.GetProperty("x").GetInt32(), step.GetProperty("y").GetInt32());
                        CliJson.Status(new { step = op, x = step.GetProperty("x").GetInt32(), y = step.GetProperty("y").GetInt32() });
                        break;

                    case "click":
                        DispatchButton(step, (x, y, b) => InputDriver.Click(hwnd, x, y, b), op);
                        break;

                    case "double-click":
                        DispatchButton(step, (x, y, b) => InputDriver.DoubleClick(hwnd, x, y, b), op);
                        break;

                    case "hold":
                        DispatchButton(step, (x, y, b) => InputDriver.Hold(hwnd, x, y, b), op);
                        break;

                    case "release":
                        DispatchButton(step, (x, y, b) => InputDriver.Release(hwnd, x, y, b), op);
                        break;

                    case "type":
                        var text = step.GetProperty("text").GetString() ?? "";
                        InputDriver.TypeText(hwnd, text);
                        CliJson.Status(new { step = op, chars = text.Length });
                        break;

                    default:
                        CliJson.Error($"unknown step op: {op}");
                        Environment.ExitCode = 1;
                        return;
                }
            }

            CliJson.Status(new { status = "scenario-ok", label = labelVal, shots = shotCounter });
        }, pid, title, script, outDir, label);

        return cmd;
    }

    private static void DispatchButton(JsonElement step, Action<int, int, OsMouseButton> exec, string op)
    {
        var x = step.GetProperty("x").GetInt32();
        var y = step.GetProperty("y").GetInt32();
        var btnStr = step.TryGetProperty("button", out var b) ? b.GetString() ?? "left" : "left";
        var btn = btnStr.ToLowerInvariant() switch
        {
            "right" => OsMouseButton.Right,
            "middle" => OsMouseButton.Middle,
            _ => OsMouseButton.Left,
        };
        exec(x, y, btn);
        CliJson.Status(new { step = op, x, y, button = btn.ToString().ToLowerInvariant() });
    }
}
