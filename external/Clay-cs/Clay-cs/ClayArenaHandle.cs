using System.Runtime.InteropServices;

namespace Clay_cs;

public struct ClayArenaHandle : IDisposable
{
	internal Clay_Arena Arena;
	internal nint Memory;

	public void Dispose()
	{
		Marshal.FreeHGlobal(Memory);
	}
}