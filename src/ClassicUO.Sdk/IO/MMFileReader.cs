// SPDX-License-Identifier: BSD-2-Clause

using System.IO;
using System.IO.MemoryMappedFiles;

namespace ClassicUO.Sdk.IO
{
    public class MMFileReader : FileReader
    {
        private readonly MemoryMappedViewAccessor? _accessor;
        private readonly MemoryMappedFile? _mmf;
        private readonly BinaryReader _file;

        public MMFileReader(FileStream stream) : base(stream)
        {
            if (stream.Length <= 0)
            {
                _file = new BinaryReader(new MemoryStream());
                return;
            }

            _mmf = MemoryMappedFile.CreateFromFile
            (
                stream,
                null,
                0,
                MemoryMappedFileAccess.Read,
                HandleInheritability.None,
                false
            );

            _accessor = _mmf.CreateViewAccessor(0, stream.Length, MemoryMappedFileAccess.Read);

            unsafe
            {
                byte* ptr = null;
                _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                _file = new BinaryReader(new UnmanagedMemoryStream(ptr, stream.Length));
            }
        }


        public override void Dispose()
        {
            _accessor?.SafeMemoryMappedViewHandle.ReleasePointer();
            _accessor?.Dispose();
            _mmf?.Dispose();

            base.Dispose();
        }
    }
}
