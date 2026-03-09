using System;
using System.Collections.Generic;
using System.Reflection;
using ClassicUO.Utility.Logging;

namespace TestPlugin
{
    /// <summary>
    /// Plugin de teste para verificar funcionalidades do sistema de plugins
    /// </summary>
    public class TestPlugin
    {
        public string Name => "TestPlugin";
        public string Version => "1.0.0";
        public string Author => "Test Author";
        public bool IsLoaded { get; private set; }

        private readonly List<string> _commands = new List<string>();
        private readonly Dictionary<string, Action<string[]>> _commandHandlers = new Dictionary<string, Action<string[]>>();

        public TestPlugin()
        {
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            _commands.Add("test");
            _commands.Add("echo");
            _commands.Add("version");
            _commands.Add("info");

            _commandHandlers["test"] = HandleTestCommand;
            _commandHandlers["echo"] = HandleEchoCommand;
            _commandHandlers["version"] = HandleVersionCommand;
            _commandHandlers["info"] = HandleInfoCommand;
        }

        public bool Load()
        {
            try
            {
                Log.Info($"[{Name}] Plugin carregado com sucesso!");
                IsLoaded = true;
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"[{Name}] Erro ao carregar plugin: {ex.Message}");
                return false;
            }
        }

        public void Unload()
        {
            try
            {
                Log.Info($"[{Name}] Plugin descarregado com sucesso!");
                IsLoaded = false;
            }
            catch (Exception ex)
            {
                Log.Error($"[{Name}] Erro ao descarregar plugin: {ex.Message}");
            }
        }

        public List<string> GetCommands()
        {
            return new List<string>(_commands);
        }

        public bool ExecuteCommand(string command, string[] args)
        {
            if (!IsLoaded)
            {
                Log.Warn($"[{Name}] Plugin não está carregado!");
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
                    Log.Error($"[{Name}] Erro ao executar comando '{command}': {ex.Message}");
                    return false;
                }
            }

            Log.Warn($"[{Name}] Comando '{command}' não encontrado!");
            return false;
        }

        private void HandleTestCommand(string[] args)
        {
            Log.Info($"[{Name}] Comando 'test' executado com {args?.Length ?? 0} argumentos");
            if (args != null && args.Length > 0)
            {
                Log.Info($"[{Name}] Argumentos: {string.Join(", ", args)}");
            }
        }

        private void HandleEchoCommand(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                Log.Info($"[{Name}] Echo: {string.Join(" ", args)}");
            }
            else
            {
                Log.Info($"[{Name}] Echo: (sem argumentos)");
            }
        }

        private void HandleVersionCommand(string[] args)
        {
            Log.Info($"[{Name}] Versão: {Version}");
        }

        private void HandleInfoCommand(string[] args)
        {
            Log.Info($"[{Name}] Informações do Plugin:");
            Log.Info($"[{Name}] - Nome: {Name}");
            Log.Info($"[{Name}] - Versão: {Version}");
            Log.Info($"[{Name}] - Autor: {Author}");
            Log.Info($"[{Name}] - Carregado: {IsLoaded}");
            Log.Info($"[{Name}] - Comandos disponíveis: {string.Join(", ", _commands)}");
        }

        // Métodos para testes de reflection
        public string GetPluginInfo()
        {
            return $"{Name} v{Version} by {Author}";
        }

        public int AddNumbers(int a, int b)
        {
            return a + b;
        }

        public string ProcessText(string input)
        {
            return input?.ToUpper() ?? "NULL";
        }

        public bool ValidateInput(string input)
        {
            return !string.IsNullOrWhiteSpace(input);
        }

        // Propriedades para testes
        public int TestProperty { get; set; } = 42;
        public string TestString { get; set; } = "Hello World";
        public bool TestBoolean { get; set; } = true;
    }
}
