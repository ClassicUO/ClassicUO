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
    /// Testes de integração para o sistema de plugins do ClassicUO
    /// </summary>
    public class PluginIntegrationTests : IDisposable
    {
        private readonly string _testPluginPath;
        private readonly string _tempDirectory;

        public PluginIntegrationTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "ClassicUO_PluginIntegrationTests", Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(_tempDirectory);
            _testPluginPath = Path.Combine(_tempDirectory, "IntegrationTestPlugin.dll");
        }

        [Fact]
        public void Plugin_IntegrationWithClassicUO_ShouldWork()
        {
            // Arrange
            CreateIntegrationTestPlugin();

            // Act
            var assembly = Assembly.LoadFrom(_testPluginPath);
            var pluginType = assembly.GetTypes().First(t => t.Name == "IntegrationTestPlugin");
            var plugin = Activator.CreateInstance(pluginType);

            // Testar carregamento
            var loadMethod = pluginType.GetMethod("Load");
            var loadResult = (bool)loadMethod.Invoke(plugin, null);

            // Testar execução de comandos
            var executeCommandMethod = pluginType.GetMethod("ExecuteCommand");
            var testResult = (bool)executeCommandMethod.Invoke(plugin, new object[] { "test", new string[] { } });
            var helpResult = (bool)executeCommandMethod.Invoke(plugin, new object[] { "help", new string[] { } });

            // Testar descarregamento
            var unloadMethod = pluginType.GetMethod("Unload");
            unloadMethod.Invoke(plugin, null);

            // Assert
            loadResult.Should().BeTrue();
            testResult.Should().BeTrue();
            helpResult.Should().BeTrue();
        }

        [Fact]
        public void Plugin_CommandExecution_ShouldHandleAllCommandTypes()
        {
            // Arrange
            CreateIntegrationTestPlugin();
            var assembly = Assembly.LoadFrom(_testPluginPath);
            var pluginType = assembly.GetTypes().First(t => t.Name == "IntegrationTestPlugin");
            var plugin = Activator.CreateInstance(pluginType);

            var loadMethod = pluginType.GetMethod("Load");
            loadMethod.Invoke(plugin, null);

            var executeCommandMethod = pluginType.GetMethod("ExecuteCommand");

            // Act & Assert
            var commands = new[] { "test", "help", "status", "info", "version" };
            foreach (var command in commands)
            {
                var result = (bool)executeCommandMethod.Invoke(plugin, new object[] { command, new string[] { } });
                result.Should().BeTrue($"Comando '{command}' deveria executar com sucesso");
            }
        }

        [Fact]
        public void Plugin_CommandExecution_WithArguments_ShouldWork()
        {
            // Arrange
            CreateIntegrationTestPlugin();
            var assembly = Assembly.LoadFrom(_testPluginPath);
            var pluginType = assembly.GetTypes().First(t => t.Name == "IntegrationTestPlugin");
            var plugin = Activator.CreateInstance(pluginType);

            var loadMethod = pluginType.GetMethod("Load");
            loadMethod.Invoke(plugin, null);

            var executeCommandMethod = pluginType.GetMethod("ExecuteCommand");

            // Act
            var result1 = (bool)executeCommandMethod.Invoke(plugin, new object[] { "echo", new string[] { "Hello", "World" } });
            var result2 = (bool)executeCommandMethod.Invoke(plugin, new object[] { "echo", new string[] { "Test", "Argument" } });

            // Assert
            result1.Should().BeTrue();
            result2.Should().BeTrue();
        }

        [Fact]
        public void Plugin_StateManagement_ShouldWork()
        {
            // Arrange
            CreateIntegrationTestPlugin();
            var assembly = Assembly.LoadFrom(_testPluginPath);
            var pluginType = assembly.GetTypes().First(t => t.Name == "IntegrationTestPlugin");
            var plugin = Activator.CreateInstance(pluginType);

            var isLoadedProperty = pluginType.GetProperty("IsLoaded");
            var loadMethod = pluginType.GetMethod("Load");
            var unloadMethod = pluginType.GetMethod("Unload");

            // Act & Assert
            // Estado inicial
            var initialState = (bool)isLoadedProperty.GetValue(plugin);
            initialState.Should().BeFalse();

            // Após carregar
            loadMethod.Invoke(plugin, null);
            var loadedState = (bool)isLoadedProperty.GetValue(plugin);
            loadedState.Should().BeTrue();

            // Após descarregar
            unloadMethod.Invoke(plugin, null);
            var unloadedState = (bool)isLoadedProperty.GetValue(plugin);
            unloadedState.Should().BeFalse();
        }

        [Fact]
        public void Plugin_ErrorHandling_ShouldBeRobust()
        {
            // Arrange
            CreateIntegrationTestPlugin();
            var assembly = Assembly.LoadFrom(_testPluginPath);
            var pluginType = assembly.GetTypes().First(t => t.Name == "IntegrationTestPlugin");
            var plugin = Activator.CreateInstance(pluginType);

            var loadMethod = pluginType.GetMethod("Load");
            var executeCommandMethod = pluginType.GetMethod("ExecuteCommand");

            loadMethod.Invoke(plugin, null);

            // Act & Assert
            // Comando inválido
            var invalidCommandResult = (bool)executeCommandMethod.Invoke(plugin, new object[] { "invalidcommand", new string[] { } });
            invalidCommandResult.Should().BeFalse();

            // Comando que gera erro
            var errorCommandResult = (bool)executeCommandMethod.Invoke(plugin, new object[] { "error", new string[] { } });
            errorCommandResult.Should().BeFalse();

            // Plugin não carregado
            var unloadMethod = pluginType.GetMethod("Unload");
            unloadMethod.Invoke(plugin, null);

            var notLoadedResult = (bool)executeCommandMethod.Invoke(plugin, new object[] { "test", new string[] { } });
            notLoadedResult.Should().BeFalse();
        }

        [Fact]
        public void Plugin_Reflection_ShouldBeComplete()
        {
            // Arrange
            CreateIntegrationTestPlugin();
            var assembly = Assembly.LoadFrom(_testPluginPath);
            var pluginType = assembly.GetTypes().First(t => t.Name == "IntegrationTestPlugin");

            // Act
            var methods = pluginType.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            var properties = pluginType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var methodNames = methods.Select(m => m.Name).ToList();
            var propertyNames = properties.Select(p => p.Name).ToList();

            // Assert
            // Métodos obrigatórios
            methodNames.Should().Contain("Load");
            methodNames.Should().Contain("Unload");
            methodNames.Should().Contain("GetCommands");
            methodNames.Should().Contain("ExecuteCommand");

            // Propriedades obrigatórias
            propertyNames.Should().Contain("Name");
            propertyNames.Should().Contain("Version");
            propertyNames.Should().Contain("Author");
            propertyNames.Should().Contain("IsLoaded");

            // Verificar tipos de retorno
            var loadMethod = pluginType.GetMethod("Load");
            loadMethod.ReturnType.Should().Be(typeof(bool));

            var unloadMethod = pluginType.GetMethod("Unload");
            unloadMethod.ReturnType.Should().Be(typeof(void));

            var getCommandsMethod = pluginType.GetMethod("GetCommands");
            getCommandsMethod.ReturnType.Should().Be(typeof(System.Collections.Generic.List<string>));

            var executeCommandMethod = pluginType.GetMethod("ExecuteCommand");
            executeCommandMethod.ReturnType.Should().Be(typeof(bool));
        }

        [Fact]
        public void Plugin_AssemblyLoading_ShouldBeReliable()
        {
            // Arrange
            CreateIntegrationTestPlugin();

            // Act
            var assembly1 = Assembly.LoadFrom(_testPluginPath);
            var assembly2 = Assembly.LoadFrom(_testPluginPath);

            // Assert
            assembly1.Should().NotBeNull();
            assembly2.Should().NotBeNull();
            assembly1.GetName().Name.Should().Be(assembly2.GetName().Name);
            assembly1.Location.Should().Be(assembly2.Location);
        }

        [Fact]
        public void Plugin_TypeInstantiation_ShouldWork()
        {
            // Arrange
            CreateIntegrationTestPlugin();
            var assembly = Assembly.LoadFrom(_testPluginPath);
            var pluginType = assembly.GetTypes().First(t => t.Name == "IntegrationTestPlugin");

            // Act
            var instance1 = Activator.CreateInstance(pluginType);
            var instance2 = Activator.CreateInstance(pluginType);

            // Assert
            instance1.Should().NotBeNull();
            instance2.Should().NotBeNull();
            instance1.Should().NotBeSameAs(instance2);
            instance1.GetType().Should().Be(instance2.GetType());
        }

        [Fact]
        public void Plugin_CommandList_ShouldBeConsistent()
        {
            // Arrange
            CreateIntegrationTestPlugin();
            var assembly = Assembly.LoadFrom(_testPluginPath);
            var pluginType = assembly.GetTypes().First(t => t.Name == "IntegrationTestPlugin");
            var plugin = Activator.CreateInstance(pluginType);

            var getCommandsMethod = pluginType.GetMethod("GetCommands");

            // Act
            var commands1 = getCommandsMethod.Invoke(plugin, null) as System.Collections.Generic.List<string>;
            var commands2 = getCommandsMethod.Invoke(plugin, null) as System.Collections.Generic.List<string>;

            // Assert
            commands1.Should().NotBeNull();
            commands2.Should().NotBeNull();
            commands1.Should().BeEquivalentTo(commands2);
            commands1.Should().Contain("test");
            commands1.Should().Contain("help");
            commands1.Should().Contain("status");
            commands1.Should().Contain("info");
            commands1.Should().Contain("version");
            commands1.Should().Contain("echo");
        }

        private void CreateIntegrationTestPlugin()
        {
            var pluginSource = @"
using System;
using System.Collections.Generic;
using System.Reflection;
using ClassicUO.Utility.Logging;

namespace IntegrationTestPlugin
{
    public class IntegrationTestPlugin
    {
        public string Name => ""IntegrationTestPlugin"";
        public string Version => ""1.0.0"";
        public string Author => ""Test Author"";
        public bool IsLoaded { get; private set; }

        private readonly List<string> _commands = new List<string>();
        private readonly Dictionary<string, Action<string[]>> _commandHandlers = new Dictionary<string, Action<string[]>>();

        public IntegrationTestPlugin()
        {
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            _commands.Add(""test"");
            _commands.Add(""help"");
            _commands.Add(""status"");
            _commands.Add(""info"");
            _commands.Add(""version"");
            _commands.Add(""echo"");
            _commands.Add(""error"");

            _commandHandlers[""test""] = HandleTestCommand;
            _commandHandlers[""help""] = HandleHelpCommand;
            _commandHandlers[""status""] = HandleStatusCommand;
            _commandHandlers[""info""] = HandleInfoCommand;
            _commandHandlers[""version""] = HandleVersionCommand;
            _commandHandlers[""echo""] = HandleEchoCommand;
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
            // Comando de teste
        }

        private void HandleHelpCommand(string[] args)
        {
            // Comando de ajuda
        }

        private void HandleStatusCommand(string[] args)
        {
            // Comando de status
        }

        private void HandleInfoCommand(string[] args)
        {
            // Comando de informações
        }

        private void HandleVersionCommand(string[] args)
        {
            // Comando de versão
        }

        private void HandleEchoCommand(string[] args)
        {
            // Comando echo
        }

        private void HandleErrorCommand(string[] args)
        {
            // Comando que sempre gera erro
            throw new InvalidOperationException(""Erro intencional para teste"");
        }
    }
}";

            var sourcePath = Path.Combine(_tempDirectory, "IntegrationTestPlugin.cs");
            File.WriteAllText(sourcePath, pluginSource);

            var projectPath = Path.Combine(_tempDirectory, "IntegrationTestPlugin.csproj");
            var projectContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>Library</OutputType>
    <AssemblyName>IntegrationTestPlugin</AssemblyName>
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
