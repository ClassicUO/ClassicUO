using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.Loader;
using ClassicUO.Utility.Logging;

namespace ClassicUO.PluginCompatibility
{
    public static class ClassicAssistCompatibility
    {
        private static readonly Dictionary<string, Assembly> _compatibilityAssemblies = 
            new Dictionary<string, Assembly>();

        public static bool TryLoadClassicAssist(string pluginPath)
        {
            try
            {
                Log.Trace("Attempting to load Classic Assist with compatibility mode...");

                // Verificar se é o Classic Assist
                if (!IsClassicAssist(pluginPath))
                {
                    return false;
                }

                // Criar configuração específica para Classic Assist
                CreateClassicAssistConfig(pluginPath);

                // Para .NET 8, usar abordagem simplificada
                Log.Trace("Using simplified loading approach for .NET 8 compatibility");
                return LoadDirectly(pluginPath);
            }
            catch (Exception ex)
            {
                Log.Error($"Classic Assist compatibility loading failed: {ex.Message}");
                return false;
            }
        }

        private static bool IsClassicAssist(string pluginPath)
        {
            var fileName = Path.GetFileName(pluginPath).ToLower();
            return fileName.Contains("classicassist") || fileName.Contains("assist");
        }

        private static void CreateClassicAssistConfig(string pluginPath)
        {
            var configPath = Path.ChangeExtension(pluginPath, ".config");
            
            if (File.Exists(configPath))
            {
                Log.Trace("Classic Assist config already exists, skipping creation");
                return;
            }

            var config = new System.Xml.XmlDocument();
            var declaration = config.CreateXmlDeclaration("1.0", "utf-8", null);
            config.AppendChild(declaration);

            var configuration = config.CreateElement("configuration");
            config.AppendChild(configuration);

            var runtime = config.CreateElement("runtime");
            configuration.AppendChild(runtime);

            var assemblyBinding = config.CreateElement("assemblyBinding");
            assemblyBinding.SetAttribute("xmlns", "urn:schemas-microsoft-com:asm.v1");
            runtime.AppendChild(assemblyBinding);

            // Binding redirects específicos para Classic Assist
            AddBindingRedirect(assemblyBinding, "System.Windows.Forms", "4.0.0.0", "4.0.0.0");
            AddBindingRedirect(assemblyBinding, "System.Drawing", "4.0.0.0", "4.0.0.0");
            AddBindingRedirect(assemblyBinding, "System", "4.0.0.0", "4.0.0.0");
            AddBindingRedirect(assemblyBinding, "System.Core", "4.0.0.0", "4.0.0.0");
            AddBindingRedirect(assemblyBinding, "System.Xml", "4.0.0.0", "4.0.0.0");
            AddBindingRedirect(assemblyBinding, "System.Configuration", "4.0.0.0", "4.0.0.0");
            AddBindingRedirect(assemblyBinding, "System.Data", "4.0.0.0", "4.0.0.0");
            AddBindingRedirect(assemblyBinding, "System.Web", "4.0.0.0", "4.0.0.0");
            AddBindingRedirect(assemblyBinding, "System.Web.Extensions", "4.0.0.0", "4.0.0.0");
            AddBindingRedirect(assemblyBinding, "System.Net.Http", "4.0.0.0", "4.0.0.0");
            AddBindingRedirect(assemblyBinding, "System.Numerics", "4.0.0.0", "4.0.0.0");
            AddBindingRedirect(assemblyBinding, "System.Numerics.Vectors", "4.0.0.0", "4.0.0.0");

            // Configurações específicas para Classic Assist
            var appSettings = config.CreateElement("appSettings");
            configuration.AppendChild(appSettings);

            AddAppSetting(appSettings, "ClassicAssistCompatibilityMode", "true");
            AddAppSetting(appSettings, "EnableLegacyUI", "true");
            AddAppSetting(appSettings, "DisableWPF", "true");
            AddAppSetting(appSettings, "UseWindowsForms", "true");

            config.Save(configPath);
            Log.Trace($"Created Classic Assist compatibility config: {configPath}");
        }

        private static void AddBindingRedirect(System.Xml.XmlElement parent, string assemblyName, string oldVersion, string newVersion)
        {
            var dependentAssembly = parent.OwnerDocument.CreateElement("dependentAssembly");
            parent.AppendChild(dependentAssembly);

            var assemblyIdentity = parent.OwnerDocument.CreateElement("assemblyIdentity");
            assemblyIdentity.SetAttribute("name", assemblyName);
            assemblyIdentity.SetAttribute("publicKeyToken", GetPublicKeyToken(assemblyName));
            assemblyIdentity.SetAttribute("culture", "neutral");
            dependentAssembly.AppendChild(assemblyIdentity);

            var bindingRedirect = parent.OwnerDocument.CreateElement("bindingRedirect");
            bindingRedirect.SetAttribute("oldVersion", oldVersion);
            bindingRedirect.SetAttribute("newVersion", newVersion);
            dependentAssembly.AppendChild(bindingRedirect);
        }

        private static void AddAppSetting(System.Xml.XmlElement parent, string key, string value)
        {
            var add = parent.OwnerDocument.CreateElement("add");
            add.SetAttribute("key", key);
            add.SetAttribute("value", value);
            parent.AppendChild(add);
        }

        private static string GetPublicKeyToken(string assemblyName)
        {
            switch (assemblyName)
            {
                case "System.Windows.Forms":
                case "System":
                case "System.Core":
                case "System.Xml":
                case "System.Data":
                case "System.Net.Http":
                case "System.Numerics":
                    return "b77a5c561934e089";
                case "System.Drawing":
                case "System.Configuration":
                case "System.Web":
                case "System.Numerics.Vectors":
                    return "b03f5f7f11d50a3a";
                case "System.Web.Extensions":
                case "System.Web.Extensions.Design":
                    return "31bf3856ad364e35";
                default:
                    return "b77a5c561934e089";
            }
        }

        private static bool LoadWithCompatibilityStrategies(string pluginPath)
        {
            // Estratégia 1: Carregamento com AppDomain isolado
            try
            {
                return LoadInIsolatedDomain(pluginPath);
            }
            catch (Exception ex1)
            {
                Log.Trace($"Isolated domain loading failed: {ex1.Message}");
            }

            // Estratégia 2: Carregamento com resolução de assembly personalizada
            try
            {
                return LoadWithCustomResolution(pluginPath);
            }
            catch (Exception ex2)
            {
                Log.Trace($"Custom resolution loading failed: {ex2.Message}");
            }

            // Estratégia 3: Carregamento direto com fallback
            try
            {
                return LoadDirectly(pluginPath);
            }
            catch (Exception ex3)
            {
                Log.Error($"All Classic Assist loading strategies failed: {ex3.Message}");
                return false;
            }
        }

        private static bool LoadInIsolatedDomain(string pluginPath)
        {
            Log.Trace("AppDomain isolation not available in .NET 8, using alternative approach...");
            
            // No .NET 8, usar AssemblyLoadContext para isolamento
            try
            {
                var context = new AssemblyLoadContext("ClassicAssistContext", isCollectible: true);
                var assembly = context.LoadFromAssemblyPath(pluginPath);
                
                if (assembly != null)
                {
                    Log.Trace("Classic Assist loaded successfully with AssemblyLoadContext");
                    _compatibilityAssemblies[pluginPath] = assembly;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Trace($"AssemblyLoadContext loading failed: {ex.Message}");
            }

            return false;
        }

        private static bool LoadWithCustomResolution(string pluginPath)
        {
            Log.Trace("Attempting to load Classic Assist with custom assembly resolution...");

            AppDomain.CurrentDomain.AssemblyResolve += OnClassicAssistAssemblyResolve;

            try
            {
                var assembly = Assembly.LoadFrom(pluginPath);
                
                if (assembly != null)
                {
                    Log.Trace("Classic Assist loaded successfully with custom resolution");
                    _compatibilityAssemblies[pluginPath] = assembly;
                    return true;
                }
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= OnClassicAssistAssemblyResolve;
            }

            return false;
        }

        private static bool LoadDirectly(string pluginPath)
        {
            Log.Trace("Attempting direct Classic Assist loading...");

            var assembly = Assembly.LoadFile(pluginPath);
            
            if (assembly != null)
            {
                Log.Trace("Classic Assist loaded successfully with direct loading");
                _compatibilityAssemblies[pluginPath] = assembly;
                return true;
            }

            return false;
        }

        private static Assembly OnClassicAssistAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);
            var pluginDir = Path.GetDirectoryName(args.RequestingAssembly?.Location);

            // Procurar na pasta do Classic Assist
            if (pluginDir != null)
            {
                var assemblyPath = Path.Combine(pluginDir, $"{assemblyName.Name}.dll");
                if (File.Exists(assemblyPath))
                {
                    return Assembly.LoadFrom(assemblyPath);
                }
            }

            // Procurar na pasta de plugins do ClassicUO
            var cuoPluginDir = Path.Combine(Environment.CurrentDirectory, "Data", "Plugins");
            if (Directory.Exists(cuoPluginDir))
            {
                var assemblyPath = Path.Combine(cuoPluginDir, $"{assemblyName.Name}.dll");
                if (File.Exists(assemblyPath))
                {
                    return Assembly.LoadFrom(assemblyPath);
                }
            }

            // Procurar assemblies do sistema
            try
            {
                return Assembly.Load(assemblyName);
            }
            catch
            {
                return null;
            }
        }

        public static void UnloadClassicAssist(string pluginPath)
        {
            if (_compatibilityAssemblies.TryGetValue(pluginPath, out var assembly))
            {
                _compatibilityAssemblies.Remove(pluginPath);
                Log.Trace($"Classic Assist unloaded: {Path.GetFileName(pluginPath)}");
            }
        }
    }
}
