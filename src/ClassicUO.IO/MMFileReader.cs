using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace ClassicUO.IO
{
    public class MMFileReader : FileReader
    {
        private readonly MemoryMappedViewAccessor _accessor;
        private readonly MemoryMappedFile _mmf;
        private readonly BinaryReader _file;

        public MMFileReader(FileStream stream) : base(stream)
        {
            if (Length <= 0)
                return;

            _mmf = MemoryMappedFile.CreateFromFile
            (
                stream,
                null,
                0,
                MemoryMappedFileAccess.Read,
                HandleInheritability.None,
                false
            );

            _accessor = _mmf.CreateViewAccessor(0, Length, MemoryMappedFileAccess.Read);

            try
            {
                unsafe
                {
                    byte* ptr = null;
                    _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                    _file = new BinaryReader(new UnmanagedMemoryStream(ptr, Length));
                }
            }
            catch
            {
                _accessor.SafeMemoryMappedViewHandle.ReleasePointer();

                throw new Exception("Something went wrong...");
            }
        }

        public override BinaryReader Reader => _file;

        public override void Dispose()
        {
            _accessor?.SafeMemoryMappedViewHandle.ReleasePointer();
            _accessor?.Dispose();
            _mmf?.Dispose();

            base.Dispose();
        }
    }
}
