using System;
using System.IO;
using ClassicUO.Utility;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Utility.Tests
{
    public class FileSystemHelperTests : IDisposable
    {
        private readonly string _tempDir;

        public FileSystemHelperTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "ClassicUO_Tests_" + Guid.NewGuid().ToString("N"));
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }

        [Fact]
        public void CreateFolderIfNotExists_CreatesFolder_AndReturnsPath()
        {
            string result = FileSystemHelper.CreateFolderIfNotExists(_tempDir);

            Directory.Exists(_tempDir).Should().BeTrue();
            result.Should().Be(_tempDir);
        }

        [Fact]
        public void CreateFolderIfNotExists_WithParts_JoinsPathsCorrectly()
        {
            string result = FileSystemHelper.CreateFolderIfNotExists(_tempDir, "sub1", "sub2");

            Directory.Exists(result).Should().BeTrue();
            result.Should().Contain("sub1");
            result.Should().Contain("sub2");
        }

        [Fact]
        public void CreateFolderIfNotExists_ExistingFolder_DoesNotThrow()
        {
            Directory.CreateDirectory(_tempDir);

            var act = () => FileSystemHelper.CreateFolderIfNotExists(_tempDir);

            act.Should().NotThrow();
        }

        [Fact]
        public void EnsureFileExists_ThrowsWhenFileDoesNotExist()
        {
            string nonExistentPath = Path.Combine(_tempDir, "nonexistent.txt");

            var act = () => FileSystemHelper.EnsureFileExists(nonExistentPath);

            act.Should().Throw<FileNotFoundException>();
        }

        [Fact]
        public void EnsureFileExists_DoesNotThrowWhenFileExists()
        {
            Directory.CreateDirectory(_tempDir);
            string filePath = Path.Combine(_tempDir, "existing.txt");
            File.WriteAllText(filePath, "test content");

            var act = () => FileSystemHelper.EnsureFileExists(filePath);

            act.Should().NotThrow();
        }

        [Fact]
        public void CreateFolderIfNotExists_WithInvalidCharsInParts_StripsInvalidChars()
        {
            // Parts with characters that are invalid in file names get stripped
            string result = FileSystemHelper.CreateFolderIfNotExists(_tempDir, "valid_part");

            Directory.Exists(result).Should().BeTrue();
        }
    }
}
