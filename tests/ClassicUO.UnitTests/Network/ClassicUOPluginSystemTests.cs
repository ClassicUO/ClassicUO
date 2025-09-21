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
    /// Testes específicos para o sistema de plugins do ClassicUO
    /// </summary>
    public class ClassicUOPluginSystemTests : IDisposable
    {
        private readonly string _testPluginPath;
        private readonly string _tempDirectory;

        public ClassicUOPluginSystemTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "ClassicUO_PluginSystemTests", Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(_tempDirectory);
            _testPluginPath = Path.Combine(_tempDirectory, "ClassicUOTestPlugin.dll");
        }

        [Fact]
        public void Plugin_LoadMethod_ShouldWork()
        {
            // Arrange
            CreateClassicUOTestPlugin();

            // Act
            var assembly = Assembly.LoadFrom(_testPluginPath);
            var pluginType = assembly.GetTypes().First(t => t.Name == "ClassicUOTestPlugin");
            var plugin = Activator.CreateInstance(pluginType);

            var loadMethod = pluginType.GetMethod("Load");
            var result = (bool)loadMethod.Invoke(plugin, null);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Plugin_UnloadMethod_ShouldWork()
        {
            // Arrange
            CreateClassicUOTestPlugin();
            var assembly = Assembly.LoadFrom(_testPluginPath);
            var pluginType = assembly.GetTypes().First(t => t.Name == "ClassicUOTestPlugin");
            var plugin = Activator.CreateInstance(pluginType);

            // Act
            var unloadMethod = pluginType.GetMethod("Unload");
            unloadMethod.Invoke(plugin, null);

            // Assert
            var isLoadedProperty = pluginType.GetProperty("IsLoaded");
            var isLoaded = (bool)isLoadedProperty.GetValue(plugin);
            isLoaded.Should().BeFalse();
        }

        [Fact]
        public void Plugin_GetCommands_ShouldReturnValidCommands()
        {
            // Arrange
            CreateClassicUOTestPlugin();
            var assembly = Assembly.LoadFrom(_testPluginPath);
            var pluginType = assembly.GetTypes().First(t => t.Name == "ClassicUOTestPlugin");
            var plugin = Activator.CreateInstance(pluginType);

            // Act
            var getCommandsMethod = pluginType.GetMethod("GetCommands");
            var commands = getCommandsMethod.Invoke(plugin, null) as System.Collections.Generic.List<string>;

            // Assert
            commands.Should().NotBeNull();
            commands.Should().Contain("test");
            commands.Should().Contain("help");
            commands.Should().Contain("status");
        }

        [Fact]
        public void Plugin_ExecuteCommand_ShouldWork()
        {
            // Arrange
            CreateClassicUOTestPlugin();
            var assembly = Assembly.LoadFrom(_testPluginPath);
            var pluginType = assembly.GetTypes().First(t => t.Name == "ClassicUOTestPlugin");
            var plugin = Activator.CreateInstance(pluginType);

            // Carregar o plugin primeiro
            var loadMethod = pluginType.GetMethod("Load");
            loadMethod.Invoke(plugin, null);

            // Act
            var executeCommandMethod = pluginType.GetMethod("ExecuteCommand");
            var result = (bool)executeCommandMethod.Invoke(plugin, new object[] { "test", new string[] { "arg1", "arg2" } });

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Plugin_ExecuteCommand_WithInvalidCommand_ShouldReturnFalse()
        {
            // Arrange
            CreateClassicUOTestPlugin();
            var assembly = Assembly.LoadFrom(_testPluginPath);
            var pluginType = assembly.GetTypes().First(t => t.Name == "ClassicUOTestPlugin");
            var plugin = Activator.CreateInstance(pluginType);

            var loadMethod = pluginType.GetMethod("Load");
            loadMethod.Invoke(plugin, null);

            // Act
            var executeCommandMethod = pluginType.GetMethod("ExecuteCommand");
            var result = (bool)executeCommandMethod.Invoke(plugin, new object[] { "invalidcommand", new string[] { } });

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Plugin_ExecuteCommand_WhenNotLoaded_ShouldReturnFalse()
        {
            // Arrange
            CreateClassicUOTestPlugin();
            var assembly = Assembly.LoadFrom(_testPluginPath);
            var pluginType = assembly.GetTypes().First(t => t.Name == "ClassicUOTestPlugin");
            var plugin = Activator.CreateInstance(pluginType);

            // Não carregar o plugin

            // Act
            var executeCommandMethod = pluginType.GetMethod("ExecuteCommand");
            var result = (bool)executeCommandMethod.Invoke(plugin, new object[] { "test", new string[] { } });

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Plugin_Properties_ShouldBeAccessible()
        {
            // Arrange
            CreateClassicUOTestPlugin();
            var assembly = Assembly.LoadFrom(_testPluginPath);
            var pluginType = assembly.GetTypes().First(t => t.Name == "ClassicUOTestPlugin");
            var plugin = Activator.CreateInstance(pluginType);

            // Act & Assert
            var nameProperty = pluginType.GetProperty("Name");
            var name = nameProperty.GetValue(plugin) as string;
            name.Should().Be("ClassicUOTestPlugin");

            var versionProperty = pluginType.GetProperty("Version");
            var version = versionProperty.GetValue(plugin) as string;
            version.Should().Be("1.0.0");

            var authorProperty = pluginType.GetProperty("Author");
            var author = authorProperty.GetValue(plugin) as string;
            author.Should().Be("Test Author");
        }

        [Fact]
        public void Plugin_IsLoadedProperty_ShouldReflectState()
        {
            // Arrange
            CreateClassicUOTestPlugin();
            var assembly = Assembly.LoadFrom(_testPluginPath);
            var pluginType = assembly.GetTypes().First(t => t.Name == "ClassicUOTestPlugin");
            var plugin = Activator.CreateInstance(pluginType);

            var isLoadedProperty = pluginType.GetProperty("IsLoaded");

            // Act & Assert - Inicialmente não carregado
            var initiallyLoaded = (bool)isLoadedProperty.GetValue(plugin);
            initiallyLoaded.Should().BeFalse();

            // Carregar o plugin
            var loadMethod = pluginType.GetMethod("Load");
            loadMethod.Invoke(plugin, null);

            var afterLoad = (bool)isLoadedProperty.GetValue(plugin);
            afterLoad.Should().BeTrue();

            // Descarregar o plugin
            var unloadMethod = pluginType.GetMethod("Unload");
            unloadMethod.Invoke(plugin, null);

            var afterUnload = (bool)isLoadedProperty.GetValue(plugin);
            afterUnload.Should().BeFalse();
        }

        [Fact]
        public void Plugin_AssemblyInfo_ShouldBeCorrect()
        {
            // Arrange
            CreateClassicUOTestPlugin();

            // Act
            var assembly = Assembly.LoadFrom(_testPluginPath);
            var assemblyName = assembly.GetName();

            // Assert
            assemblyName.Name.Should().Be("ClassicUOTestPlugin");
            assembly.Location.Should().Be(_testPluginPath);
        }

        [Fact]
        public void Plugin_Reflection_ShouldFindAllRequiredMembers()
        {
            // Arrange
            CreateClassicUOTestPlugin();
            var assembly = Assembly.LoadFrom(_testPluginPath);
            var pluginType = assembly.GetTypes().First(t => t.Name == "ClassicUOTestPlugin");

            // Act
            var methods = pluginType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            var properties = pluginType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var methodNames = methods.Select(m => m.Name).ToList();
            var propertyNames = properties.Select(p => p.Name).ToList();

            // Assert
            methodNames.Should().Contain("Load");
            methodNames.Should().Contain("Unload");
            methodNames.Should().Contain("GetCommands");
            methodNames.Should().Contain("ExecuteCommand");

            propertyNames.Should().Contain("Name");
            propertyNames.Should().Contain("Version");
            propertyNames.Should().Contain("Author");
            propertyNames.Should().Contain("IsLoaded");
        }

        [Fact]
        public void Plugin_ErrorHandling_ShouldWork()
        {
            // Arrange
            CreateClassicUOTestPlugin();
            var assembly = Assembly.LoadFrom(_testPluginPath);
            var pluginType = assembly.GetTypes().First(t => t.Name == "ClassicUOTestPlugin");
            var plugin = Activator.CreateInstance(pluginType);

            // Act
            var loadMethod = pluginType.GetMethod("Load");
            var result = (bool)loadMethod.Invoke(plugin, null);

            // Assert
            result.Should().BeTrue();

            // Testar comando que pode gerar erro
            var executeCommandMethod = pluginType.GetMethod("ExecuteCommand");
            var errorResult = (bool)executeCommandMethod.Invoke(plugin, new object[] { "error", new string[] { } });

            // O comando de erro deve retornar false
            errorResult.Should().BeFalse();
        }

        private void CreateClassicUOTestPlugin()
        {
            var pluginSource = @"
using System;
using System.Collections.Generic;
using System.Reflection;
using ClassicUO.Utility.Logging;

namespace ClassicUOTestPlugin
{
    public class ClassicUOTestPlugin
    {
        public string Name => ""ClassicUOTestPlugin"";
        public string Version => ""1.0.0"";
        public string Author => ""Test Author"";
        public bool IsLoaded { get; private set; }

        private readonly List<string> _commands = new List<string>();
        private readonly Dictionary<string, Action<string[]>> _commandHandlers = new Dictionary<string, Action<string[]>>();

        public ClassicUOTestPlugin()
        {
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            _commands.Add(""test"");
            _commands.Add(""help"");
            _commands.Add(""status"");
            _commands.Add(""error"");

            _commandHandlers[""test""] = HandleTestCommand;
            _commandHandlers[""help""] = HandleHelpCommand;
            _commandHandlers[""status""] = HandleStatusCommand;
            _commandHandlers[""error""] = HandleErrorCommand;
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
            if (!IsLoaded)
            {
                return false;
            }

            if (_commandHandlers.TryGetValue(command.ToLower(), out var handler))
            {
                try
                {
                    handler(args);
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }

            return false;
        }

        private void HandleTestCommand(string[] args)
        {
            // Comando de teste - sempre funciona
        }

        private void HandleHelpCommand(string[] args)
        {
            // Comando de ajuda
        }

        private void HandleStatusCommand(string[] args)
        {
            // Comando de status
        }

        private void HandleErrorCommand(string[] args)
        {
            // Comando que sempre gera erro para teste
            throw new InvalidOperationException(""Erro intencional para teste"");
        }
    }
}";

            var sourcePath = Path.Combine(_tempDirectory, "ClassicUOTestPlugin.cs");
            File.WriteAllText(sourcePath, pluginSource);

            // Compilar usando dotnet
            var projectPath = Path.Combine(_tempDirectory, "ClassicUOTestPlugin.csproj");
            var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>Library</OutputType>
    <AssemblyName>ClassicUOTestPlugin</AssemblyName>
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

            if (!File.Exists(_testPluginPath))
            {
                throw new InvalidOperationException("Plugin de teste não foi criado");
            }
        }

        public void Dispose()
        {
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
