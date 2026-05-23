// SPDX-License-Identifier: BSD-2-Clause
//
// agent.login: bypass the GUI login screen and emit OnLoginRequest
// directly so the network plugin can connect. The handler mirrors the
// LoginScreenPlugin path — encrypts the password via Crypter.Encrypt,
// writes Username/Password back into Settings, then enqueues the event
// for HandleLoginRequests to consume next tick.
//
// Params: { username, password, address?, port? }
//   - username (string, required): plaintext account name.
//   - password (string, required): plaintext password, encrypted here.
//   - address  (string, optional): override Settings.IP.
//   - port     (number, optional): override Settings.Port.
//
// Returns: { dispatched: true } once enqueued. The actual TCP connect
// is asynchronous; callers should poll lifecycle.inWorld to determine
// when the player has reached the in-game state.

#if AGENT_BUILD
#nullable enable

using System.Text.Json;
using System.Text.Json.Nodes;
using ClassicUO.Agent.Contracts;
using ClassicUO.Ecs;
using ClassicUO.Utility;
using TinyEcs.Bevy;

namespace ClassicUO.Agent.Agent.Handlers;

internal static class AgentLoginHandlers
{
    public static JsonRpcResponse Login(JsonRpcRequest req, in AgentRpcContext ctx)
    {
        if (req.Params is not JsonElement p || p.ValueKind != JsonValueKind.Object)
        {
            return AgentServer.ErrorResponse(
                req.Id,
                JsonRpcErrorCodes.InvalidParams,
                "agent.login expects an object with 'username' and 'password'");
        }

        if (!TryGetString(p, "username", out var username) || string.IsNullOrWhiteSpace(username))
        {
            return AgentServer.ErrorResponse(
                req.Id,
                JsonRpcErrorCodes.InvalidParams,
                "agent.login: 'username' is required and must be a non-empty string");
        }

        if (!TryGetString(p, "password", out var password) || string.IsNullOrWhiteSpace(password))
        {
            return AgentServer.ErrorResponse(
                req.Id,
                JsonRpcErrorCodes.InvalidParams,
                "agent.login: 'password' is required and must be a non-empty string");
        }

        var settings = ctx.Settings.Value!;

        var address = settings.IP;
        if (TryGetString(p, "address", out var overrideAddress) && !string.IsNullOrWhiteSpace(overrideAddress))
        {
            address = overrideAddress;
        }

        var port = settings.Port;
        if (p.TryGetProperty("port", out var portElement))
        {
            if (portElement.ValueKind == JsonValueKind.Number && portElement.TryGetUInt16(out var parsedPort))
            {
                port = parsedPort;
            }
            else if (portElement.ValueKind == JsonValueKind.String
                && ushort.TryParse(portElement.GetString(), out var parsedFromString))
            {
                port = parsedFromString;
            }
            else if (portElement.ValueKind != JsonValueKind.Null && portElement.ValueKind != JsonValueKind.Undefined)
            {
                return AgentServer.ErrorResponse(
                    req.Id,
                    JsonRpcErrorCodes.InvalidParams,
                    "agent.login: 'port' must be a uint16 number or numeric string");
            }
        }

        // Match LoginScreenPlugin.Login: stash credentials on Settings so
        // downstream packets (e.g. account list re-request) can reuse them.
        settings.Username = username!;
        settings.Password = Crypter.Encrypt(password!);

        // EmitTrigger goes through Commands; TinyEcs.Bevy flushes the
        // command queue after the DrainInbox system completes, then fires
        // observers synchronously. HandleLoginRequests therefore runs this
        // same frame — no inbox→event→reader ordering hazard.
        ctx.Commands.EmitTrigger(new OnLoginRequest
        {
            Username = settings.Username,
            Password = settings.Password,
            Address = address,
            Port = port,
        });

        return new JsonRpcResponse
        {
            Id = req.Id,
            Result = new JsonObject
            {
                ["dispatched"] = true,
            },
        };
    }

    private static bool TryGetString(JsonElement obj, string name, out string? value)
    {
        if (obj.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String)
        {
            value = element.GetString();
            return value is not null;
        }

        value = null;
        return false;
    }
}

#endif
