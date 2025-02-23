using System.Runtime.CompilerServices;

namespace Clay_cs;

public struct ClayId
{
	public Clay_String Text;
	public uint Offset;
	public uint Seed;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ClayId Global(string id) => Create(Clay.ClayStrings[id]);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ClayId Global(string id, int offset) => Create(Clay.ClayStrings[id], offset);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ClayId Global(Clay_String id) => Create(id);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ClayId Global(Clay_String id, int offset) => Create(id, offset);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ClayId Local(string id) => Create(Clay.ClayStrings[id], seed: Clay.GetParentElementId());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ClayId Local(string id, int offset) => Create(Clay.ClayStrings[id], offset, Clay.GetParentElementId());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ClayId Local(Clay_String id) => Create(id, seed: Clay.GetParentElementId());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ClayId Local(Clay_String id, int offset) => Create(id, offset, Clay.GetParentElementId());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator ClayId(Clay_String id) => Global(id);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator ClayId(string id) => Global(id);

	private static ClayId Create(Clay_String id, int offset = default, uint seed = default) => new()
	{
		Text = id,
		Offset = (uint)offset,
		Seed = seed
	};
}