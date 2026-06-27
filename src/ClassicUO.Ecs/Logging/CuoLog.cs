// SPDX-License-Identifier: BSD-2-Clause

#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ClassicUO.Ecs.Logging;

// NativeAOT-safe logging. We adopt Microsoft.Extensions.Logging's ILogger
// abstraction but DELIBERATELY skip the LoggerFactory + DI + config-binding
// stack the article wires up (https://alexandrehtrb.github.io/posts/2025/04/
// structured-logging-in-dotnet-with-nativeaot/): we only ever have one provider
// live at a time, so a single ILogger instance with a baked-in minimum level is
// all we need — no reflection, no source-gen JsonSerializerContext. The JSON
// sink writes via Utf8JsonWriter (fully trim/AOT-safe), switching on the value's
// runtime type, which sidesteps the article's whole JsonSerializerContext dance.
//
// The created ILogger is registered as an ECS resource (app.AddResource<ILogger>)
// and consumed via Res<ILogger> in systems / property-injected into non-ECS infra.
// There is no static logger holder — see ECS rule "no static shared state".
public enum LogOutput
{
    Console,
    Json
}

public static class CuoLog
{
    // Build the single app-wide ILogger. Output defaults from CUO_LOG_FORMAT
    // (json|console), console otherwise. Register the result as a resource.
    public static ILogger Create(LogOutput? output = null, LogLevel minLevel = LogLevel.Trace)
    {
        var fmt = output ?? (string.Equals(
            Environment.GetEnvironmentVariable("CUO_LOG_FORMAT"), "json",
            StringComparison.OrdinalIgnoreCase)
            ? LogOutput.Json
            : LogOutput.Console);

        return fmt == LogOutput.Json
            ? new CuoJsonLogger(minLevel)
            : new CuoConsoleLogger(minLevel);
    }
}

file sealed class NoopScope : IDisposable
{
    public static readonly NoopScope Instance = new();
    public void Dispose() { }
}

file sealed class CuoConsoleLogger(LogLevel minLevel) : ILogger
{
    private static readonly object _gate = new();

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoopScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && logLevel >= minLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var text = formatter(state, exception);
        var (color, label) = Style(logLevel);

        lock (_gate)
        {
            Console.Write(DateTime.UtcNow);
            Console.Write(" | ");
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(label);
            Console.ForegroundColor = prev;
            Console.Write(" | ");
            Console.WriteLine(text);
            if (exception != null)
                Console.WriteLine(exception);
        }
    }

    private static (ConsoleColor, string) Style(LogLevel level) => level switch
    {
        LogLevel.Trace => (ConsoleColor.Green, "  Trace   "),
        LogLevel.Debug => (ConsoleColor.DarkGreen, "  Debug   "),
        LogLevel.Information => (ConsoleColor.Green, "  Info    "),
        LogLevel.Warning => (ConsoleColor.Yellow, "  Warning "),
        LogLevel.Error => (ConsoleColor.Red, "  Error   "),
        LogLevel.Critical => (ConsoleColor.Red, "  Critical"),
        _ => (ConsoleColor.White, "          "),
    };
}

// CLEF-ish one-JSON-object-per-line. @t timestamp, @l level, @mt message
// template, @m rendered message, @x exception; structured properties from the
// log state get their own top-level keys.
file sealed class CuoJsonLogger(LogLevel minLevel) : ILogger
{
    private static readonly object _gate = new();

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoopScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None && logLevel >= minLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var buffer = new ArrayBufferWriter<byte>(256);
        using (var w = new Utf8JsonWriter(buffer))
        {
            w.WriteStartObject();
            w.WriteString("@t", DateTime.UtcNow);
            w.WriteString("@l", logLevel.ToString());

            string? template = null;
            if (state is IReadOnlyList<KeyValuePair<string, object?>> props)
            {
                for (var i = 0; i < props.Count; i++)
                {
                    var kv = props[i];
                    if (kv.Key == "{OriginalFormat}")
                    {
                        template = kv.Value as string;
                        continue;
                    }
                    WriteValue(w, kv.Key, kv.Value);
                }
            }

            w.WriteString("@m", formatter(state, exception));
            if (template != null)
                w.WriteString("@mt", template);
            if (exception != null)
                w.WriteString("@x", exception.ToString());

            w.WriteEndObject();
        }

        lock (_gate)
            Console.Out.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    private static void WriteValue(Utf8JsonWriter w, string key, object? v)
    {
        switch (v)
        {
            case null: w.WriteNull(key); break;
            case string s: w.WriteString(key, s); break;
            case bool b: w.WriteBoolean(key, b); break;
            case int i: w.WriteNumber(key, i); break;
            case long l: w.WriteNumber(key, l); break;
            case uint u: w.WriteNumber(key, u); break;
            case double d: w.WriteNumber(key, d); break;
            case float f: w.WriteNumber(key, f); break;
            default: w.WriteString(key, v.ToString()); break;
        }
    }
}
