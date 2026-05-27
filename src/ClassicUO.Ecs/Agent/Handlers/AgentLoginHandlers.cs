// SPDX-License-Identifier: BSD-2-Clause
//
// agent.login: bypass the GUI login screen and emit OnLoginRequest
// directly so the network plugin can connect. The handler mirrors the
// LoginScreenPlugin path — encrypts the password via Crypter.Encrypt,
// writes Username/Password back into Settings, then emits the trigger
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
using ClassicUO.Agent.Host;
using ClassicUO.Configuration;
using ClassicUO.Ecs;
using ClassicUO.Utility;
using TinyEcs.Bevy;

namespace ClassicUO.Agent.Agent.Handlers;

internal static class AgentLoginHandlers
{
    public static void Register(AgentDispatcher<App> d)
    {
        d.Register(RpcVerbs.AgentLogin, Login);
    }

    public static JsonRpcResponse Login(JsonRpcRequest req, in AgentRpcContext<App> ctx)
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

        var settings = ctx.Runtime.GetResource<Settings>();

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

        // Auto-progress through ServerSelection + CharacterSelection by
        // default so the script can skip the UI clicks. Caller can disable
        // via "autoSelect": false to drive the menus manually.
        var autoSelect = true;
        if (p.TryGetProperty("autoSelect", out var asEl) && asEl.ValueKind == JsonValueKind.False)
            autoSelect = false;
        ctx.State.AutoLoginActive = autoSelect;
        ctx.State.AutoServerIndex =
            p.TryGetProperty("serverIndex", out var srvEl)
            && srvEl.ValueKind == JsonValueKind.Number
            && srvEl.TryGetInt32(out var sIdx) ? sIdx : 0;
        ctx.State.AutoCharacterIndex =
            p.TryGetProperty("characterIndex", out var chrEl)
            && chrEl.ValueKind == JsonValueKind.Number
            && chrEl.TryGetInt32(out var cIdx) ? cIdx : 0;

        // Fire the OnLoginRequest trigger synchronously against the world.
        // The original LoginScreenPlugin path used Commands.EmitTrigger to
        // queue a deferred trigger; we don't have a Commands handle inside
        // the dispatcher (it's not on App), so we emit straight through
        // world.EmitTrigger with EntityId=0 → only the global observer
        // (HandleLoginRequests) fires. Same observable behaviour, just
        // synchronous instead of deferred.
        ctx.Runtime.GetWorld().EmitTrigger(0UL, new OnLoginRequest
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
