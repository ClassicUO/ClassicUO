using System;
using System.IO;
using System.Reflection;
using System.Linq;
using FluentAssertions;
using Xunit;
using ClassicUO.Network;
using ClassicUO.Utility.Logging;

namespace ClassicUO.UnitTests.Network
{
    /// <summary>
    /// Testes unitários para o sistema de plugins do ClassicUO
    /// </summary>
    public class PluginTests : IDisposable
    {
        private readonly string _testPluginPath;
        private readonly string _tempDirectory;

        public PluginTests()
        {
            // Criar diretório temporário para testes
            _tempDirectory = Path.Combine(Path.GetTempPath(), "ClassicUO_PluginTests", Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(_tempDirectory);

            // Caminho para o plugin de teste
            _testPluginPath = Path.Combine(_tempDirectory, "TestPlugin.dll");
        }

        [Fact]
        public void LoadPlugin_WithValidPlugin_ShouldSucceed()
        {
            // Arrange
            CreateTestPlugin();

            // Act
            var assembly = LoadPlugin(_testPluginPath);

            // Assert
            assembly.Should().NotBeNull();
            assembly.GetName().Name.Should().Be("TestPlugin");
        }

        [Fact]
        public void LoadPlugin_WithInvalidPath_ShouldReturnNull()
        {
            // Arrange
            var invalidPath = Path.Combine(_tempDirectory, "NonExistentPlugin.dll");

            // Act
            var assembly = LoadPlugin(invalidPath);

            // Assert
            assembly.Should().BeNull();
        }

        [Fact]
        public void LoadPlugin_WithCorruptedFile_ShouldReturnNull()
        {
            // Arrange
            var corruptedPath = Path.Combine(_tempDirectory, "CorruptedPlugin.dll");
            File.WriteAllText(corruptedPath, "This is not a valid DLL");

            // Act
            var assembly = LoadPlugin(corruptedPath);

            // Assert
            assembly.Should().BeNull();
        }

        [Fact]
        public void PluginReflection_ShouldFindCorrectTypes()
        {
            // Arrange
            CreateTestPlugin();
            var assembly = LoadPlugin(_testPluginPath);
            assembly.Should().NotBeNull();

            // Act
            var types = assembly.GetTypes();
            var testPluginType = types.FirstOrDefault(t => t.Name == "TestPlugin");

            // Assert
            testPluginType.Should().NotBeNull();
            testPluginType.Name.Should().Be("TestPlugin");
            testPluginType.Namespace.Should().Be("TestPlugin");
        }

        [Fact]
        public void PluginReflection_ShouldFindCorrectMethods()
        {
            // Arrange
            CreateTestPlugin();
            var assembly = LoadPlugin(_testPluginPath);
            assembly.Should().NotBeNull();

            var testPluginType = assembly.GetTypes().First(t => t.Name == "TestPlugin");

            // Act
            var methods = testPluginType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            var methodNames = methods.Select(m => m.Name).ToList();

            // Assert
            methodNames.Should().Contain("Load");
            methodNames.Should().Contain("Unload");
            methodNames.Should().Contain("GetCommands");
            methodNames.Should().Contain("ExecuteCommand");
            methodNames.Should().Contain("GetPluginInfo");
            methodNames.Should().Contain("AddNumbers");
            methodNames.Should().Contain("ProcessText");
            methodNames.Should().Contain("ValidateInput");
        }

        [Fact]
        public void PluginReflection_ShouldFindCorrectProperties()
        {
            // Arrange
            CreateTestPlugin();
            var assembly = LoadPlugin(_testPluginPath);
            assembly.Should().NotBeNull();

            var testPluginType = assembly.GetTypes().First(t => t.Name == "TestPlugin");

            // Act
            var properties = testPluginType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var propertyNames = properties.Select(p => p.Name).ToList();

            // Assert
            propertyNames.Should().Contain("Name");
            propertyNames.Should().Contain("Version");
            propertyNames.Should().Contain("Author");
            propertyNames.Should().Contain("IsLoaded");
            propertyNames.Should().Contain("TestProperty");
            propertyNames.Should().Contain("TestString");
            propertyNames.Should().Contain("TestBoolean");
        }

        [Fact]
        public void PluginInstantiation_ShouldWork()
        {
            // Arrange
            CreateTestPlugin();
            var assembly = LoadPlugin(_testPluginPath);
            assembly.Should().NotBeNull();

            var testPluginType = assembly.GetTypes().First(t => t.Name == "TestPlugin");

            // Act
            var instance = Activator.CreateInstance(testPluginType);

            // Assert
            instance.Should().NotBeNull();
            instance.GetType().Name.Should().Be("TestPlugin");
        }

        [Fact]
        public void PluginMethodInvocation_ShouldWork()
        {
            // Arrange
            CreateTestPlugin();
            var assembly = LoadPlugin(_testPluginPath);
            assembly.Should().NotBeNull();

            var testPluginType = assembly.GetTypes().First(t => t.Name == "TestPlugin");
            var instance = Activator.CreateInstance(testPluginType);

            // Act
            var getPluginInfoMethod = testPluginType.GetMethod("GetPluginInfo");
            var result = getPluginInfoMethod.Invoke(instance, null) as string;

            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("TestPlugin");
        }

        [Fact]
        public void PluginMethodInvocation_WithParameters_ShouldWork()
        {
            // Arrange
            CreateTestPlugin();
            var assembly = LoadPlugin(_testPluginPath);
            assembly.Should().NotBeNull();

            var testPluginType = assembly.GetTypes().First(t => t.Name == "TestPlugin");
            var instance = Activator.CreateInstance(testPluginType);

            // Act
            var addNumbersMethod = testPluginType.GetMethod("AddNumbers");
            var result = (int)addNumbersMethod.Invoke(instance, new object[] { 5, 3 });

            // Assert
            result.Should().Be(8);
        }

        [Fact]
        public void PluginPropertyAccess_ShouldWork()
        {
            // Arrange
            CreateTestPlugin();
            var assembly = LoadPlugin(_testPluginPath);
            assembly.Should().NotBeNull();

            var testPluginType = assembly.GetTypes().First(t => t.Name == "TestPlugin");
            var instance = Activator.CreateInstance(testPluginType);

            // Act
            var nameProperty = testPluginType.GetProperty("Name");
            var name = nameProperty.GetValue(instance) as string;

            var versionProperty = testPluginType.GetProperty("Version");
            var version = versionProperty.GetValue(instance) as string;

            // Assert
            name.Should().Be("TestPlugin");
            version.Should().Be("1.0.0");
        }

        [Fact]
        public void PluginPropertySetting_ShouldWork()
        {
            // Arrange
            CreateTestPlugin();
            var assembly = LoadPlugin(_testPluginPath);
            assembly.Should().NotBeNull();

            var testPluginType = assembly.GetTypes().First(t => t.Name == "TestPlugin");
            var instance = Activator.CreateInstance(testPluginType);

            // Act
            var testProperty = testPluginType.GetProperty("TestProperty");
            testProperty.SetValue(instance, 100);
            var value = (int)testProperty.GetValue(instance);

            // Assert
            value.Should().Be(100);
        }

        [Fact]
        public void PluginCommandExecution_ShouldWork()
        {
            // Arrange
            CreateTestPlugin();
            var assembly = LoadPlugin(_testPluginPath);
            assembly.Should().NotBeNull();

            var testPluginType = assembly.GetTypes().First(t => t.Name == "TestPlugin");
            var instance = Activator.CreateInstance(testPluginType);

            // Act
            var loadMethod = testPluginType.GetMethod("Load");
            var loadResult = (bool)loadMethod.Invoke(instance, null);

            var executeCommandMethod = testPluginType.GetMethod("ExecuteCommand");
            var executeResult = (bool)executeCommandMethod.Invoke(instance, new object[] { "test", new string[] { "arg1", "arg2" } });

            // Assert
            loadResult.Should().BeTrue();
            executeResult.Should().BeTrue();
        }

        [Fact]
        public void PluginCommandList_ShouldWork()
        {
            // Arrange
            CreateTestPlugin();
            var assembly = LoadPlugin(_testPluginPath);
            assembly.Should().NotBeNull();

            var testPluginType = assembly.GetTypes().First(t => t.Name == "TestPlugin");
            var instance = Activator.CreateInstance(testPluginType);

            // Act
            var getCommandsMethod = testPluginType.GetMethod("GetCommands");
            var commands = getCommandsMethod.Invoke(instance, null) as System.Collections.Generic.List<string>;

            // Assert
            commands.Should().NotBeNull();
            commands.Should().Contain("test");
            commands.Should().Contain("echo");
            commands.Should().Contain("version");
            commands.Should().Contain("info");
        }

        [Fact]
        public void PluginAssemblyInfo_ShouldBeCorrect()
        {
            // Arrange
            CreateTestPlugin();
            var assembly = LoadPlugin(_testPluginPath);
            assembly.Should().NotBeNull();

            // Act
            var assemblyName = assembly.GetName();
            var assemblyLocation = assembly.Location;

            // Assert
            assemblyName.Name.Should().Be("TestPlugin");
            assemblyLocation.Should().Be(_testPluginPath);
        }

        [Fact]
        public void PluginDependencies_ShouldBeResolved()
        {
            // Arrange
            CreateTestPlugin();
            var assembly = LoadPlugin(_testPluginPath);
            assembly.Should().NotBeNull();

            // Act
            var referencedAssemblies = assembly.GetReferencedAssemblies();

            // Assert
            referencedAssemblies.Should().NotBeNull();
            // Verificar se as dependências principais estão presentes
            var assemblyNames = referencedAssemblies.Select(a => a.Name).ToList();
            assemblyNames.Should().Contain("System.Runtime");
            assemblyNames.Should().Contain("System.Core");
        }

        private void CreateTestPlugin()
        {
            // Compilar o plugin de teste
            var testPluginSource = @"
using System;
using System.Collections.Generic;
using System.Reflection;
using ClassicUO.Utility.Logging;

namespace TestPlugin
{
    public class TestPlugin
    {
        public string Name => ""TestPlugin"";
        public string Version => ""1.0.0"";
        public string Author => ""Test Author"";
        public bool IsLoaded { get; private set; }

        private readonly List<string> _commands = new List<string>();
        private readonly Dictionary<string, Action<string[]>> _commandHandlers = new Dictionary<string, Action<string[]>>();

        public TestPlugin()
        {
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            _commands.Add(""test"");
            _commands.Add(""echo"");
            _commands.Add(""version"");
            _commands.Add(""info"");

            _commandHandlers[""test""] = HandleTestCommand;
            _commandHandlers[""echo""] = HandleEchoCommand;
            _commandHandlers[""version""] = HandleVersionCommand;
            _commandHandlers[""info""] = HandleInfoCommand;
        }

        public bool Load()
        {
            try
            {
                IsLoaded = true;
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public void Unload()
        {
            IsLoaded = false;
        }

        public List<string> GetCommands()
        {
            return new List<string>(_commands);
        }

        public bool ExecuteCommand(string command, string[] args)
        {
            if (!IsLoaded) return false;

            if (_commandHandlers.TryGetValue(command.ToLower(), out var handler))
            {
                try
                {
                    handler(args);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        private void HandleTestCommand(string[] args) { }
        private void HandleEchoCommand(string[] args) { }
        private void HandleVersionCommand(string[] args) { }
        private void HandleInfoCommand(string[] args) { }

        public string GetPluginInfo()
        {
            return $""{Name} v{Version} by {Author}"";
        }

        public int AddNumbers(int a, int b)
        {
            return a + b;
        }

        public string ProcessText(string input)
        {
            return input?.ToUpper() ?? ""NULL"";
        }

        public bool ValidateInput(string input)
        {
            return !string.IsNullOrWhiteSpace(input);
        }

        public int TestProperty { get; set; } = 42;
        public string TestString { get; set; } = ""Hello World"";
        public bool TestBoolean { get; set; } = true;
    }
}";

            // Salvar o código fonte
            var sourcePath = Path.Combine(_tempDirectory, "TestPlugin.cs");
            File.WriteAllText(sourcePath, testPluginSource);

            // Compilar usando o compilador C#
            var cscPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), 
                "Microsoft Visual Studio", "2022", "Community", "MSBuild", "Current", "Bin", "Roslyn", "csc.exe");
            
            if (!File.Exists(cscPath))
            {
                // Tentar caminho alternativo
                cscPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), 
                    "Microsoft Visual Studio", "2022", "BuildTools", "MSBuild", "Current", "Bin", "Roslyn", "csc.exe");
            }

            if (!File.Exists(cscPath))
            {
                // Usar dotnet para compilar
                var projectPath = Path.Combine(_tempDirectory, "TestPlugin.csproj");
                var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>Library</OutputType>
    <AssemblyName>TestPlugin</AssemblyName>
  </PropertyGroup>
</Project>";
                File.WriteAllText(projectPath, projectContent);

                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build \"{projectPath}\" -o \"{_tempDirectory}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = System.Diagnostics.Process.Start(startInfo);
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Falha ao compilar plugin de teste: {process.StandardError.ReadToEnd()}");
                }
            }
            else
            {
                // Usar csc diretamente
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = cscPath,
                    Arguments = $"/target:library /out:\"{_testPluginPath}\" \"{sourcePath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = System.Diagnostics.Process.Start(startInfo);
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Falha ao compilar plugin de teste: {process.StandardError.ReadToEnd()}");
                }
            }

            // Verificar se o arquivo foi criado
            if (!File.Exists(_testPluginPath))
            {
                throw new InvalidOperationException("Plugin de teste não foi criado");
            }
        }

        private Assembly LoadPlugin(string pluginPath)
        {
            try
            {
                return Assembly.LoadFrom(pluginPath);
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            // Limpar arquivos temporários
            try
            {
                if (Directory.Exists(_tempDirectory))
                {
                    Directory.Delete(_tempDirectory, true);
                }
            }
            catch
            {
                // Ignorar erros de limpeza
            }
        }
    }
}
