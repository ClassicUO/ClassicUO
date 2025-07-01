using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Clay_cs;
using Extism;
using MyPlugin;


static class Plugin
{
	private static void Main() {}



	[UnmanagedCallersOnly(EntryPoint = "on_init")]
	static void OnInitialize()
	{
		Pdk.Log(LogLevel.Info, "plugin initialized");

		HostWrapper.SendPacketToServer([0x73, 0xFF]);

		// _sender.Append(new PluginMessage.SetPacketHandler(0x73, nameof(Handler_0x73)));
	}

	[UnmanagedCallersOnly(EntryPoint = "on_update")]
	static void OnUpdate()
	{
		var json = Pdk.GetInputString();
		// Pdk.Log(LogLevel.Info, $"receive update: {json}");

		var time = json.FromJson<TimeProxy>();
		// Pdk.Log(LogLevel.Info, time.ToString());

		// _sender.Flush();
	}


	[UnmanagedCallersOnly(EntryPoint = "on_event")]
	static void OnEvent()
	{
		var json = Pdk.GetInputString();
		// Pdk.Log(LogLevel.Info, $"receive event: {json}");

		var evList = json.FromJson<HostMessages>();
		foreach (var ev in evList.Messages)
		{
			switch (ev)
			{
				case HostMessage.KeyPressed keyPressed:
					break;

				case HostMessage.KeyReleased keyReleased:
					switch (keyReleased.Key)
					{
						case Keys.A:
						{
							var compressedData = Zlib.Compress(Convert.FromBase64String(FileBytes.ImageFile));
							Pdk.Log(LogLevel.Info, $"compressed: {compressedData.Length}");
							HostWrapper.SetSprite(new SpriteDescription(
								AssetType.Gump,
								0x014E,
								497,
								376,
								Convert.ToBase64String(compressedData),
								CompressionType.Zlib
							));
							break;
						}
						case Keys.S:
						{
							var descOut = HostWrapper.GetSprite(new SpriteDescription(AssetType.Gump, 0x014E));
							break;
						}
						case Keys.D:
						{
							var playerSerial = HostWrapper.GetPlayerSerial();
							var graphic = HostWrapper.GetEntityGraphic(playerSerial);
							Pdk.Log(LogLevel.Info, $"found graphic: {graphic.Value:X2}");
							break;
						}
						case Keys.F:
						{
							var playerSerial = HostWrapper.GetPlayerSerial();
							HostWrapper.SetEntityGraphic(playerSerial, new Graphic() { Value = 0x12 });
							break;
						}
						case Keys.G:
							// CreateMenu();
							CreateLoginScreenMenu();
							break;
						case Keys.H:
							var response = HostWrapper.Query(new QueryRequest([(1, TermOp.Optional)]));
							foreach (var result in response.Results)
							{

							}
							break;
					}

					break;
			}
		}
	}


	private static readonly Dictionary<ulong, Action> _uiCallbacks = new();

	[UnmanagedCallersOnly(EntryPoint = "on_ui_mouse_event")]
	static void UIMouseCallback()
	{
		var json = Pdk.GetInputString();
		var ev = json.FromJson<UIMouseEvent>();

		if (ev.State == UIInteractionState.Released &&
		    _uiCallbacks.TryGetValue(ev.Id, out var fn))
		{
			fn();
		}
	}

	[UnmanagedCallersOnly(EntryPoint = "on_ui_keyboard_event")]
	static void UIKeyboardCallback()
	{

	}



	[UnmanagedCallersOnly(EntryPoint = nameof(Handler_0x73))]
	static void Handler_0x73()
	{
		Pdk.Log(LogLevel.Warn, "0x73 handler");
	}


	static void CreateLoginScreenMenu()
	{
		var root = new UINodeProxy(HostWrapper.SpawnEcsEntity(), new ClayElementDeclProxy()
		{
			BackgroundColor = new (0.2f, 0.2f, 0.2f, 1),
			Layout = new ()
			{
				sizing = {
					width = Clay_SizingAxis.Grow(),
					height = Clay_SizingAxis.Grow()
				},
				childAlignment = {
					x = Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
					y = Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER
				},
				layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM
			}
		});


		var mainMenu = new UINodeProxy(HostWrapper.SpawnEcsEntity(), new ClayElementDeclProxy()
		{
			BackgroundColor = new (0.2f, 0.2f, 0.2f, 1),
			Layout = new ()
			{
				sizing =
				{
					width = Clay_SizingAxis.Fit(0, 0),
					height = Clay_SizingAxis.Fit(0, 0)
				},
				layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM
			}
		});


		var builder = new GumpBuilder();

		var background = builder.AddGump(0x014E, Vector3.UnitZ);
		var quitButton = builder.AddButton((0x05CA, 0x05C9, 0x05C8), Vector3.UnitZ, new () { X = 25, Y = 240 });
		var creditButton = builder.AddButton((0x05D0, 0x05CF, 0x5CE), Vector3.UnitZ, new () { X = 530, Y = 125 });
		var arrowButton = builder.AddButton((0x5CD, 0x5CC, 0x5CB), Vector3.UnitZ, new () { X = 280, Y = 365 });
		var usernameBackground =
			builder.AddGumpNinePatch(0x0BB8, Vector3.UnitZ, new() { X = 218, Y = 283 }, new() { X = 210, Y = 30 });
		usernameBackground.TextConfig = new()
		{
			Value = "your username",
			TextConfig = new()
			{
				FontId = 0,
				FontSize = 24,
				TextColor = new (0.2f, 0.2f, 0.2f, 1)
			}
		};
		usernameBackground.AcceptInputs = true;
		usernameBackground.WidgetType = ClayWidgetType.TextInput;

		var passwordBackground =
			builder.AddGumpNinePatch(0x0BB8, Vector3.UnitZ, new() { X = 218, Y = 283 + 50}, new() { X = 210, Y = 30 });
		passwordBackground.TextConfig = new()
		{
			Value = "your password",
			ReplacedChar = '*',
			TextConfig = new()
			{
				FontId = 0,
				FontSize = 24,
				TextColor = new (1, 1, 1, 1)
			}
		};
		passwordBackground.AcceptInputs = true;
		passwordBackground.WidgetType = ClayWidgetType.TextInput;


		var relations = new Dictionary<ulong, ulong>
		{
			// child - parent
			{ mainMenu.Id, root.Id },

			{ background.Id, mainMenu.Id },
			{ quitButton.Id, mainMenu.Id },
			{ creditButton.Id, mainMenu.Id },
			{ arrowButton.Id, mainMenu.Id },
			{ usernameBackground.Id, mainMenu.Id },
			{ passwordBackground.Id, mainMenu.Id }
		};

		var nodes = new UINodes([root, mainMenu, background,
			quitButton, creditButton, arrowButton,
			usernameBackground, passwordBackground], relations);


		_uiCallbacks[quitButton.Id] = () =>
		{
			Pdk.Log(LogLevel.Info, "Quit button clicked");
		};

		_uiCallbacks[creditButton.Id] = () =>
		{
			Pdk.Log(LogLevel.Info, "Credit button clicked");
		};

		_uiCallbacks[arrowButton.Id] = () =>
		{
			Pdk.Log(LogLevel.Info, "Login button clicked");
		};

		HostWrapper.CreateUINodes(nodes);
	}

	static void CreateMenu()
	{
		var rootEnt = HostWrapper.SpawnEcsEntity();

		var root = new UINodeProxy(rootEnt, new ClayElementDeclProxy()
		{
			BackgroundColor = new(0.6f, 0.6f, 0.6f, 1),
			Layout = new()
			{
				sizing = {
					width = Clay_SizingAxis.Fixed(700),
					height = Clay_SizingAxis.Fixed(700),
				},
				layoutDirection = Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM,
				childAlignment = {
					x = Clay_LayoutAlignmentX.CLAY_ALIGN_X_CENTER,
					y = Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER
				},
			},
			Floating = new()
			{
				clipTo = Clay_FloatingClipToElement.CLAY_CLIP_TO_ATTACHED_PARENT,
				attachTo = Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT,
				offset = {
					x = 0,
					y = 0
				}
			}
		}, Movable: true, AcceptInputs: true);


		var childEnt = HostWrapper.SpawnEcsEntity();
		var child = new UINodeProxy(childEnt, new ClayElementDeclProxy()
		{
			BackgroundColor = new(1, 0, 0, 1),
			Layout = new()
			{
				sizing = {
					width = Clay_SizingAxis.Percent(0.5f),
					height = Clay_SizingAxis.Percent(0.5f)
				}
			}
		}, TextConfig: new()
		{
			Value = "ClassicUO is the best client ever made!",
			TextConfig = new (){
				FontId = 4,
				FontSize = 36,
				TextColor = new(0, 0, 1, 1),
				TextAlignment = Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER
			}
		});


		var childEnt2 = HostWrapper.SpawnEcsEntity();
		var child2 = new UINodeProxy(childEnt2, new ClayElementDeclProxy()
		{
			BackgroundColor = new(0, 1, 0, 1),
			Layout = new()
			{
				sizing = {
					width = Clay_SizingAxis.Percent(0.5f),
					height = Clay_SizingAxis.Percent(0.5f)
				}
			}
		}, TextConfig: new()
		{
			Value = "Hello from plugin!",
			TextConfig = new () {
				FontId = 2,
				FontSize = 36,
				TextColor = new(1, 0, 0, 1)
			},
		});


		var relations = new Dictionary<ulong, ulong>
		{
			// child - parent
			{ child.Id, root.Id },
			{ child2.Id, child.Id }
		};

		var nodes = new UINodes([root, child, child2], relations);

		HostWrapper.CreateUINodes(nodes);
	}
}



static partial class Host
{
	[LibraryImport("extism", EntryPoint = "send_events")]
	public static partial void SendEvents(ulong offset);

	[LibraryImport("extism", EntryPoint = "cuo_send_to_server")]
	public static partial void SendPacketToServer(ulong offset);

	[LibraryImport("extism", EntryPoint = "cuo_set_sprite")]
	public static partial void SetSprite(ulong offset);

	[LibraryImport("extism", EntryPoint = "cuo_get_sprite")]
	public static partial ulong GetSprite(ulong offset);


	[LibraryImport("extism", EntryPoint = "cuo_get_player_serial")]
	public static partial ulong GetPlayerSerial();

	[LibraryImport("extism", EntryPoint = "cuo_get_entity_graphic")]
	public static partial ulong GetEntityGraphic(ulong offset);

	[LibraryImport("extism", EntryPoint = "cuo_set_entity_graphic")]
	public static partial void SetEntityGraphic(ulong offset, ulong valueOffset);


	[LibraryImport("extism", EntryPoint = "cuo_ui_node")]
	public static partial void CreateUINode(ulong offset);



	[LibraryImport("extism", EntryPoint = "cuo_ecs_spawn_entity")]
	public static partial ulong SpawnEcsEntity();


	[LibraryImport("extism", EntryPoint = "cuo_ecs_delete_entity")]
	public static partial void DeleteEcsEntity(ulong id);


	[LibraryImport("extism", EntryPoint = "cuo_ecs_query")]
	public static partial ulong EcsQuery(ulong offset);


	[LibraryImport("extism", EntryPoint = "cuo_ecs_set_component")]
	public static partial void EcsSetComponent(ulong id, long offset);
}

static class HostWrapper
{
	public static void SendPacketToServer(ReadOnlySpan<byte> data)
	{
		using var mem = Pdk.Allocate(data);
		Host.SendPacketToServer(mem.Offset);
	}

	public static void SetSprite(SpriteDescription spriteDesc)
	{
		var json = spriteDesc.ToJson();
		using var mem = Pdk.Allocate(json);
		Host.SetSprite(mem.Offset);
	}

	public static SpriteDescription GetSprite(SpriteDescription spriteDesc)
	{
		var json = spriteDesc.ToJson();
		using var memIn = Pdk.Allocate(json);
		using var memOut = MemoryBlock.Find(Host.GetSprite(memIn.Offset));
		var jsonOut = memOut.ReadString();
		var spriteDescOut = jsonOut.FromJson<SpriteDescription>();
		Pdk.Log(LogLevel.Info, jsonOut);
		return spriteDescOut;
	}

	public static Graphic GetEntityGraphic(uint serial)
	{
		using var memIn = Pdk.Allocate(serial.AsBytes());
		using var memOut = MemoryBlock.Find(Host.GetEntityGraphic(memIn.Offset));
		var jsonOut = memOut.ReadString();
		var graphic = jsonOut.FromJson<Graphic>();
		Pdk.Log(LogLevel.Info, jsonOut);
		return graphic;
	}

	public static void SetEntityGraphic(uint serial, Graphic graphic)
	{
		using var memIn0 = Pdk.Allocate(serial.AsBytes());
		using var memIn1 = Pdk.Allocate(graphic.ToJson());
		Host.SetEntityGraphic(memIn0.Offset, memIn1.Offset);
	}

	public static uint GetPlayerSerial()
	{
		using var mem = MemoryBlock.Find(Host.GetPlayerSerial());
		return mem.ReadBytes().AsSpan().As<uint>();
	}




	public static void CreateUINodes(UINodes nodes)
	{
		var json = nodes.ToJson();
		using var memIn = Pdk.Allocate(json);
		Host.CreateUINode(memIn.Offset);
	}


	public static ulong SpawnEcsEntity() => Host.SpawnEcsEntity();

	public static void DeleteEcsEntity(ulong id) => Host.DeleteEcsEntity(id);


	public static QueryResponse Query(QueryRequest query)
	{
		using var memIn = Pdk.Allocate(query.ToJson());
		var offset = Host.EcsQuery(memIn.Offset);
		using var memOut = MemoryBlock.Find(offset);
		var jsonOut = memOut.ReadString();
		Pdk.Log(LogLevel.Info, jsonOut);
		var response = jsonOut.FromJson<QueryResponse>();
		return response;
	}
}





// sealed class MessageSender
// {
// 	private readonly List<PluginMessage> _messages = new();

// 	public void Append(PluginMessage msg) => _messages.Add(msg);

// 	public void Flush()
// 	{
// 		if (_messages.Count == 0)
// 			return;

// 		var jsonOut = new PluginMessages(_messages).ToJson();
// 		using var block = Pdk.Allocate(jsonOut);
// 		Host.SendEvents(block.Offset);
// 		_messages.Clear();
// 	}
// }




struct Graphic { public ushort Value; }
public struct Vector3
{
	public float X, Y, Z;


	public static readonly Vector3 UnitZ = new () { X = 0, Y = 0, Z = 1 };
}

public struct Vector2
{
	public float X, Y;
}


[JsonSourceGenerationOptions(IncludeFields = true, PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault)]
[JsonSerializable(typeof(HostMessages), GenerationMode = JsonSourceGenerationMode.Default)]
// [JsonSerializable(typeof(PluginMessages), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(TimeProxy), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(WasmPluginVersion), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(SpriteDescription), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(Graphic), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(UINodes), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(UIMouseEvent), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(QueryRequest), GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(QueryResponse), GenerationMode = JsonSourceGenerationMode.Default)]
internal partial class ModdingJsonContext : JsonSerializerContext { }

internal record WasmPluginVersion(uint Version = 1);
internal record struct TimeProxy(float Total, float Frame)
{
	public override string ToString()
	{
		return $"Total: {Total}, Frame: {Frame}";
	}

};

internal record struct ComponentInfoProxy(ulong Id, int Size, string Name);
internal record struct QueryRequest(List<(ulong Ids, TermOp Op)> Terms);
internal record struct ArchetypeProxy(IEnumerable<ComponentInfoProxy> Components, IEnumerable<ulong> Entities);
internal record struct QueryResponse(List<ArchetypeProxy> Results);
public enum TermOp : byte
{
	With,
	Without,
	Optional
}

internal record struct UIMouseEvent(ulong Id, int Button, float X, float Y, UIInteractionState State);

internal enum UIInteractionState : byte
{
	None,
	Over,
	Pressed,
	Released,
}


[DebuggerDisplay("ID: {ID}, Size: {Size}")]
public readonly struct ComponentInfo
{
	public readonly ulong ID;
	public readonly int Size;

	internal ComponentInfo(ulong id, int size)
	{
		ID = id;
		Size = size;
	}
}

internal record struct HostMessages(List<HostMessage> Messages);
// internal record struct PluginMessages(List<PluginMessage> Messages);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type", IgnoreUnrecognizedTypeDiscriminators = true)]
[JsonDerivedType(typeof(MouseMove), nameof(MouseMove))]
[JsonDerivedType(typeof(MouseWheel), nameof(MouseWheel))]
[JsonDerivedType(typeof(MousePressed), nameof(MousePressed))]
[JsonDerivedType(typeof(MouseReleased), nameof(MouseReleased))]
[JsonDerivedType(typeof(MouseDoubleClick), nameof(MouseDoubleClick))]
[JsonDerivedType(typeof(KeyPressed), nameof(KeyPressed))]
[JsonDerivedType(typeof(KeyReleased), nameof(KeyReleased))]
internal record HostMessage
{
	internal record MouseMove(float X, float Y) : HostMessage;
	internal record MouseWheel(float Delta) : HostMessage;
	internal record MousePressed(int Button, float X, float Y) : HostMessage;
	internal record MouseReleased(int Button, float X, float Y) : HostMessage;
	internal record MouseDoubleClick(int Button, float X, float Y) : HostMessage;
	internal record KeyPressed(Keys Key) : HostMessage;
	internal record KeyReleased(Keys Key) : HostMessage;
}

// [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
// [JsonDerivedType(typeof(SetPacketHandler), nameof(SetPacketHandler))]
// [JsonDerivedType(typeof(OverrideAsset), nameof(OverrideAsset))]
// internal interface PluginMessage
// {
// 	internal record struct SetPacketHandler(byte PacketId, string FuncName) : PluginMessage;

// 	internal record struct OverrideAsset(AssetType AssetType, uint Idx, string DataBase64, int Width, int Height) : PluginMessage;
// }

internal record struct SpriteDescription(AssetType AssetType, uint Idx, int Width = 0, int Height = 0, string Base64Data = "", CompressionType Compression = 0);

internal enum AssetType
{
	Gump,
	Arts,
	Animation,
}

enum CompressionType
{
	None,
	Zlib
}

internal record struct UINodes(List<UINodeProxy> Nodes, Dictionary<ulong, ulong> Relations);
internal record struct UINodeProxy(
	ulong Id,
	ClayElementDeclProxy Config,
	ClayUOCommandData? UOConfig = null,
	UITextProxy? TextConfig = null,
	UOButtonWidgetProxy? UOButton = null,
	ClayWidgetType WidgetType = ClayWidgetType.None,
	bool Movable = false,
	bool AcceptInputs = false
);

internal record struct UOButtonWidgetProxy(ushort Normal, ushort Pressed, ushort Over);


enum ClayUOCommandType : byte
{
	None,
	Text,
	Gump,
	GumpNinePatch,
	Art,
	Land,
	Animation,
}

enum ClayWidgetType
{
	None,
	Button,
	TextInput
}

[StructLayout(LayoutKind.Sequential)]
internal struct ClayUOCommandData
{
	public ClayUOCommandType Type;

	public uint Id;
	public Vector3 Hue;
}



internal record struct UITextProxy(string Value, char ReplacedChar = '\0', ClayTextProxy TextConfig = default);
internal record struct ClayTextProxy(
	Clay_Color TextColor,
	ushort FontId,
	ushort FontSize,
	ushort LetterSpacing,
	ushort LineHeight,
	Clay_TextElementConfigWrapMode WrapMode,
	Clay_TextAlignment TextAlignment
);
internal record struct ClayElementIdProxy(uint Id, uint Offset, uint BaseId, string StringId);
internal record struct ClayImageProxy(string Base64Data);
internal struct ClayElementDeclProxy
{
	public ClayElementIdProxy? Id;
	public Clay_LayoutConfig? Layout;
	public Clay_Color? BackgroundColor;
	public Clay_CornerRadius? CornerRadius;
	public ClayImageProxy? Image;
	public Clay_FloatingElementConfig? Floating;
	public Clay_ClipElementConfig? Clip;
	public Clay_BorderElementConfig? Border;
}

static class JsonEx2
{
	public static string ToJson<T>(this T obj)
	{
		return JsonSerializer.Serialize(obj, (JsonTypeInfo<T>)ModdingJsonContext.Default.GetTypeInfo(typeof(T)));
	}

	public static T FromJson<T>(this string json)
	{
		return JsonSerializer.Deserialize(json, (JsonTypeInfo<T>)ModdingJsonContext.Default.GetTypeInfo(typeof(T)));
	}
}

static class DataExt
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static T As<T>(this Span<byte> buf) where T : unmanaged
		=> As<byte, T>(buf);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe TTo As<TFrom, TTo>(this Span<TFrom> buf)
		where TFrom : unmanaged
		where TTo : unmanaged
	{
		ArgumentOutOfRangeException.ThrowIfNotEqual(sizeof(TFrom) * buf.Length, sizeof(TTo));

		return MemoryMarshal.Read<TTo>(MemoryMarshal.AsBytes(buf));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<byte> AsBytes<T>(this ref T val)
		where T : unmanaged
	{
		return MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref val, 1));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<byte> AsBytes<T>(this Span<T> val)
		where T : unmanaged
	{
		return MemoryMarshal.AsBytes(val);
	}
}

static class Zlib
{
	public static unsafe byte[] Uncompress(ReadOnlySpan<byte> data)
	{
		fixed (byte* dataPtr = data)
		{
			using var ms = new UnmanagedMemoryStream(dataPtr, data.Length);
			ms.Seek(2, SeekOrigin.Begin);
			using var deflateStream = new System.IO.Compression.DeflateStream(ms, System.IO.Compression.CompressionMode.Decompress);
			var buff = new byte[deflateStream.Length];
			deflateStream.ReadExactly(buff);
			return buff;
		}
	}

	public static byte[] Compress(ReadOnlySpan<byte> data)
	{
		using var ms = new MemoryStream();
		{
			using var deflateStream = new System.IO.Compression.DeflateStream(ms, System.IO.Compression.CompressionMode.Compress, leaveOpen: true);
			deflateStream.Write(data);
		}
		return ms.ToArray();
	}
}

internal sealed class GumpBuilder
{
    public UINodeProxy AddLabel(string text, Vector2? position = null, Vector2? size = null)
    {
	    var node = new UINodeProxy(HostWrapper.SpawnEcsEntity(), new ClayElementDeclProxy()
	    {
		    Layout = new () {
			    sizing = {
				    width = Clay_SizingAxis.Fixed(size?.X ?? 0),
				    height = Clay_SizingAxis.Fixed(size?.Y ?? 0),
			    }
		    },
		    Floating = new () {
			    clipTo = Clay_FloatingClipToElement.CLAY_CLIP_TO_ATTACHED_PARENT,
			    attachTo = position.HasValue ? Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT : Clay_FloatingAttachToElement.CLAY_ATTACH_TO_NONE,
			    offset = {
				    x = position?.X ?? 0,
				    y = position?.Y ?? 0
			    }
		    }
	    }, TextConfig: new UITextProxy(text, TextConfig: new ()
	    {
		    FontId = 0,
		    FontSize = 12,
		    TextColor = new (255, 255, 255, 255),
	    }));

        return node;
    }

    public UINodeProxy AddButton((ushort normal, ushort pressed, ushort over) ids, Vector3 hue, Vector2? position = null)
    {
	    var node = AddGump(ids.normal, hue, position, false, true);
	    node.WidgetType = ClayWidgetType.Button;
	    node.UOButton = new(ids.normal, ids.pressed, ids.over);
	    return node;
    }

    public UINodeProxy AddGump(ushort id, Vector3 hue, Vector2? position = null, bool movable = false, bool acceptInputs = false)
    {
        var spriteInfo = HostWrapper.GetSprite(new SpriteDescription(AssetType.Gump, id));

        var node = new UINodeProxy(HostWrapper.SpawnEcsEntity(), new ClayElementDeclProxy()
        {
	        Layout = new () {
		        sizing = {
			        width = Clay_SizingAxis.Fixed(spriteInfo.Width),
			        height = Clay_SizingAxis.Fixed(spriteInfo.Height),
		        }
	        },
	        Floating = new () {
		        clipTo = Clay_FloatingClipToElement.CLAY_CLIP_TO_ATTACHED_PARENT,
		        attachTo = position.HasValue ? Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT : Clay_FloatingAttachToElement.CLAY_ATTACH_TO_NONE,
		        offset = {
			        x = position?.X ?? 0,
			        y = position?.Y ?? 0
		        }
	        }
        }, UOConfig: new ()
        {
	        Type = ClayUOCommandType.Gump,
	        Id = id,
	        Hue = hue,
        }, Movable: movable, AcceptInputs: acceptInputs);

        return node;
    }

    public UINodeProxy AddGumpNinePatch(ushort id, Vector3 hue, Vector2? position = null, Vector2? size = null)
    {
	    var spriteInfo = HostWrapper.GetSprite(new SpriteDescription(AssetType.Gump, id));

        var node = new UINodeProxy(HostWrapper.SpawnEcsEntity(), new ClayElementDeclProxy()
        {
	        Layout = new () {
		        sizing = {
			        width = Clay_SizingAxis.Fixed(size.HasValue ? size.Value.X : spriteInfo.Width),
			        height = Clay_SizingAxis.Fixed(size.HasValue ? size.Value.Y : spriteInfo.Height),
		        }
	        },
	        Floating = new () {
		        clipTo = Clay_FloatingClipToElement.CLAY_CLIP_TO_ATTACHED_PARENT,
		        attachTo = position.HasValue ? Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT : Clay_FloatingAttachToElement.CLAY_ATTACH_TO_NONE,
		        offset = {
			        x = position?.X ?? 0,
			        y = position?.Y ?? 0
		        }
	        }
        }, UOConfig: new ()
        {
	        Type = ClayUOCommandType.GumpNinePatch,
	        Id = id,
	        Hue = hue,
        });

        return node;
    }
}
