// SPDX-License-Identifier: BSD-2-Clause
//
// agent.login: bypass the GUI login screen and drive LoginScene.Connect
// directly. Auto-progress through ServerSelection + CharacterSelection
// runs from AgentBootstrap.AdvanceAutoLogin (called each frame after
// DrainInbox) so the script doesn't have to click through the menus.
//
// Params: { username, password, address?, port?, autoSelect?, serverIndex?, characterIndex? }
//   - username (string, required): plaintext account name.
//   - password (string, required): plaintext password — LoginScene.Connect
//     handles encryption + storage internally.
//   - address  (string, optional): override Settings.GlobalSettings.IP.
//   - port     (number, optional): override Settings.GlobalSettings.Port.
//   - autoSelect (bool, default true): if true, auto-advance through
//     ServerSelection and CharacterSelection. If false, caller drives.
//   - serverIndex (int, default 0): index into LoginScene.Servers.
//   - characterIndex (int, default 0): index into LoginScene.Characters.
//
// Returns: { dispatched: true } once Connect is called. The TCP connect
// is asynchronous; callers should poll lifecycle.inWorld.

#if AGENT_BUILD
#nullable enable

using System.Text.Json;
using System.Text.Json.Nodes;
using ClassicUO.Agent.Contracts;
using ClassicUO.Agent.Host;
using ClassicUO.Configuration;
using ClassicUO.Game.Scenes;

namespace ClassicUO.Agent.Agent.Handlers;

internal static class AgentLoginHandlers
{
    public static void Register(AgentDispatcher<GameController> d)
    {
        d.Register(RpcVerbs.AgentLogin, Login);
    }

    public static JsonRpcResponse Login(JsonRpcRequest req, in AgentRpcContext<GameController> ctx)
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

        if (TryGetString(p, "address", out var overrideAddress) && !string.IsNullOrWhiteSpace(overrideAddress))
        {
            Settings.GlobalSettings.IP = overrideAddress!;
        }

        if (p.TryGetProperty("port", out var portElement))
        {
            if (portElement.ValueKind == JsonValueKind.Number && portElement.TryGetUInt16(out var parsedPort))
            {
                Settings.GlobalSettings.Port = parsedPort;
            }
            else if (portElement.ValueKind == JsonValueKind.String
                && ushort.TryParse(portElement.GetString(), out var parsedFromString))
            {
                Settings.GlobalSettings.Port = parsedFromString;
            }
            else if (portElement.ValueKind != JsonValueKind.Null && portElement.ValueKind != JsonValueKind.Undefined)
            {
                return AgentServer.ErrorResponse(
                    req.Id,
                    JsonRpcErrorCodes.InvalidParams,
                    "agent.login: 'port' must be a uint16 number or numeric string");
            }
        }

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

        // LoginScene.Connect expects PLAINTEXT password — it handles
        // Crypter.Encrypt + Settings.Save internally when SaveAccount is on.
        var loginScene = ctx.Runtime.GetScene<LoginScene>();
        if (loginScene is null)
        {
            return AgentServer.ErrorResponse(
                req.Id,
                JsonRpcErrorCodes.InternalError,
                "agent.login: current scene is not LoginScene (already logged in?)");
        }

        loginScene.Connect(username!, password!);

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
