using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Collections.Generic;
using ClassicUO.Utility.Logging;

namespace ClassicUO.PluginCompatibility
{
    public class EnhancedPluginLoader
    {
        private static readonly Dictionary<string, Assembly> _loadedAssemblies = 
            new Dictionary<string, Assembly>();

        public static Assembly LoadPluginWithCompatibility(string pluginPath)
        {
            try
            {
                // Verificar compatibilidade primeiro
                if (!PluginCompatibilityManager.IsPluginCompatible(pluginPath))
                {
                    Log.Warn($"Plugin '{Path.GetFileName(pluginPath)}' may have compatibility issues, attempting enhanced loading...");
                }

                // Criar configuração de compatibilidade se necessário
                PluginCompatibilityManager.CreateCompatibilityConfig(pluginPath);

                // Tentar carregar com diferentes estratégias
                return LoadWithFallbackStrategies(pluginPath);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to load plugin '{Path.GetFileName(pluginPath)}': {ex.Message}");
                return null;
            }
        }

        private static Assembly LoadWithFallbackStrategies(string pluginPath)
        {
            // Estratégia 1: Carregamento direto
            try
            {
                var assembly = Assembly.LoadFrom(pluginPath);
                _loadedAssemblies[pluginPath] = assembly;
                Log.Trace($"Plugin loaded successfully using LoadFrom: {Path.GetFileName(pluginPath)}");
                return assembly;
            }
            catch (Exception ex1)
            {
                Log.Trace($"LoadFrom failed for {Path.GetFileName(pluginPath)}: {ex1.Message}");
            }

            // Estratégia 2: Carregamento com contexto personalizado
            try
            {
                var context = new PluginLoadContext(pluginPath);
                var assembly = context.LoadFromAssemblyPath(pluginPath);
                _loadedAssemblies[pluginPath] = assembly;
                Log.Trace($"Plugin loaded successfully using custom context: {Path.GetFileName(pluginPath)}");
                return assembly;
            }
            catch (Exception ex2)
            {
                Log.Trace($"Custom context failed for {Path.GetFileName(pluginPath)}: {ex2.Message}");
            }

            // Estratégia 3: Carregamento com resolução de assembly personalizada
            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
                var assembly = Assembly.LoadFile(pluginPath);
                _loadedAssemblies[pluginPath] = assembly;
                Log.Trace($"Plugin loaded successfully using LoadFile with custom resolution: {Path.GetFileName(pluginPath)}");
                return assembly;
            }
            catch (Exception ex3)
            {
                Log.Trace($"LoadFile with custom resolution failed for {Path.GetFileName(pluginPath)}: {ex3.Message}");
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
            }

            throw new InvalidOperationException($"All loading strategies failed for plugin: {Path.GetFileName(pluginPath)}");
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);
            var pluginDir = Path.GetDirectoryName(args.RequestingAssembly?.Location);

            if (pluginDir != null)
            {
                // Procurar na pasta do plugin
                var assemblyPath = Path.Combine(pluginDir, $"{assemblyName.Name}.dll");
                if (File.Exists(assemblyPath))
                {
                    return Assembly.LoadFrom(assemblyPath);
                }

                // Procurar em subpastas
                var subDirs = Directory.GetDirectories(pluginDir);
                foreach (var subDir in subDirs)
                {
                    assemblyPath = Path.Combine(subDir, $"{assemblyName.Name}.dll");
                    if (File.Exists(assemblyPath))
                    {
                        return Assembly.LoadFrom(assemblyPath);
                    }
                }
            }

            // Procurar na pasta de plugins do ClassicUO
            var cuoPluginDir = Path.Combine(CUOEnviroment.ExecutablePath, "Data", "Plugins");
            if (Directory.Exists(cuoPluginDir))
            {
                var assemblyPath = Path.Combine(cuoPluginDir, $"{assemblyName.Name}.dll");
                if (File.Exists(assemblyPath))
                {
                    return Assembly.LoadFrom(assemblyPath);
                }
            }

            return null;
        }

        public static void UnloadPlugin(string pluginPath)
        {
            if (_loadedAssemblies.TryGetValue(pluginPath, out var assembly))
            {
                _loadedAssemblies.Remove(pluginPath);
                Log.Trace($"Plugin unloaded: {Path.GetFileName(pluginPath)}");
            }
        }

        public static void UnloadAllPlugins()
        {
            _loadedAssemblies.Clear();
            Log.Trace("All plugins unloaded");
        }
    }

    // Contexto de carregamento personalizado para plugins
    public class PluginLoadContext : AssemblyLoadContext
    {
        private readonly string _pluginPath;
        private readonly AssemblyDependencyResolver _resolver;

        public PluginLoadContext(string pluginPath)
        {
            _pluginPath = pluginPath;
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            // Tentar resolver usando o resolver de dependências
            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            // Fallback para assemblies do sistema
            return AssemblyLoadContext.Default.LoadFromAssemblyName(assemblyName);
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }
    }
}
