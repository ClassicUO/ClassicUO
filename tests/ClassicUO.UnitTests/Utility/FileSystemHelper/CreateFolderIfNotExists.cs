using System.IO;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Utility.FileSystemHelper
{
    public class CreateFolderIfNotExists
    {
        [Fact]
        public void When_ValidPath_Provided_Directories_Will_BeCreated_And_Valid_Path_Returned()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), nameof(CreateFolderIfNotExists));

            var createdPath = ClassicUO.Utility.FileSystemHelper.CreateFolderIfNotExists(tempPath, "Part1", "Part2");

            createdPath.Should().BeEquivalentTo(Path.Combine(tempPath, "Part1", "Part2"));

            Directory.Delete(tempPath, true);
        }
    }
}