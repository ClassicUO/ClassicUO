// SPDX-License-Identifier: BSD-2-Clause
//
// JSON-RPC 2.0 envelope types and the source-generated JsonSerializerContext
// used by both ends of the wire. The wire format is newline-framed JSON,
// loopback only, one connection at a time.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ClassicUO.Agent.Contracts;

public sealed class JsonRpcRequest
{
    [JsonPropertyName("jsonrpc")] public string Version { get; set; } = "2.0";
    [JsonPropertyName("id")] public long? Id { get; set; }
    [JsonPropertyName("method")] public string Method { get; set; } = string.Empty;
    [JsonPropertyName("params")] public JsonElement? Params { get; set; }
}

public sealed class JsonRpcResponse
{
    [JsonPropertyName("jsonrpc")] public string Version { get; set; } = "2.0";
    [JsonPropertyName("id")] public long? Id { get; set; }
    [JsonPropertyName("result")] public JsonNode? Result { get; set; }
    [JsonPropertyName("error")] public JsonRpcError? Error { get; set; }
}

public sealed class JsonRpcError
{
    [JsonPropertyName("code")] public int Code { get; set; }
    [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
    [JsonPropertyName("data")] public JsonNode? Data { get; set; }
}

public sealed class JsonRpcEvent
{
    [JsonPropertyName("jsonrpc")] public string Version { get; set; } = "2.0";
    [JsonPropertyName("method")] public string Method { get; set; } = "event";
    [JsonPropertyName("params")] public JsonRpcEventParams? Params { get; set; }
}

public sealed class JsonRpcEventParams
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("data")] public JsonNode? Data { get; set; }
}

public static class JsonRpcErrorCodes
{
    public const int ParseError = -32700;
    public const int InvalidRequest = -32600;
    public const int MethodNotFound = -32601;
    public const int InvalidParams = -32602;
    public const int InternalError = -32603;

    // Application-defined codes (above -32000 per JSON-RPC spec).
    public const int NotImplemented = 1000;
    public const int NotInWorld = 1001;
    public const int CaptureUnavailable = 1002;
    public const int AlreadyRecording = 1003;
}
