using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Configuration;
using System.Xml;
using System.Runtime.Loader;
using ClassicUO.Utility.Logging;

namespace ClassicUO.PluginCompatibility
{
    public class PluginCompatibilityManager
    {
        private static readonly Dictionary<string, PluginCompatibilityInfo> _compatibilityCache = 
            new Dictionary<string, PluginCompatibilityInfo>();

        public static bool IsPluginCompatible(string pluginPath)
        {
            if (_compatibilityCache.TryGetValue(pluginPath, out var info))
            {
                return info.IsCompatible;
            }

            // Analisar o plugin para determinar compatibilidade
            info = AnalyzePluginCompatibility(pluginPath);
            _compatibilityCache[pluginPath] = info;

            return info.IsCompatible;
        }

        private static PluginCompatibilityInfo AnalyzePluginCompatibility(string pluginPath)
        {
            var info = new PluginCompatibilityInfo
            {
                PluginPath = pluginPath,
                IsCompatible = true,
                RequiredFramework = "Unknown",
                CompatibilityIssues = new List<string>()
            };

            try
            {
                // Verificar se é um assembly gerenciado
                if (Path.GetExtension(pluginPath).ToLower() == ".dll")
                {
                    // No .NET 8, usar AssemblyLoadContext para análise
                    var context = new AssemblyLoadContext("AnalysisContext", isCollectible: true);
                    var assembly = context.LoadFromAssemblyPath(pluginPath);
                    info.RequiredFramework = GetTargetFramework(assembly);
                    
                    // Verificar dependências problemáticas
                    var dependencies = assembly.GetReferencedAssemblies();
                    foreach (var dep in dependencies)
                    {
                        if (IsProblematicDependency(dep))
                        {
                            info.CompatibilityIssues.Add($"Problematic dependency: {dep.Name} {dep.Version}");
                        }
                    }

                    // Verificar se tem classes WPF/WinForms
                    if (HasWindowsSpecificTypes(assembly))
                    {
                        info.CompatibilityIssues.Add("Contains Windows-specific types (WPF/WinForms)");
                    }

                    // Determinar compatibilidade baseada nas verificações
                    info.IsCompatible = info.CompatibilityIssues.Count == 0;
                    
                    // Descarregar o contexto após análise
                    context.Unload();
                }
            }
            catch (Exception ex)
            {
                info.IsCompatible = false;
                info.CompatibilityIssues.Add($"Analysis failed: {ex.Message}");
            }

            return info;
        }

        private static string GetTargetFramework(Assembly assembly)
        {
            try
            {
                var attributes = assembly.GetCustomAttributesData();
                foreach (var attr in attributes)
                {
                    if (attr.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute")
                    {
                        return attr.ConstructorArguments[0].Value?.ToString() ?? "Unknown";
                    }
                }
            }
            catch
            {
                // Ignorar erros de reflexão
            }

            return "Unknown";
        }

        private static bool IsProblematicDependency(AssemblyName dependency)
        {
            var problematicAssemblies = new[]
            {
                "System.Windows.Forms",
                "System.Windows.Presentation",
                "System.Windows",
                "WindowsBase",
                "PresentationCore",
                "PresentationFramework"
            };

            foreach (var problematic in problematicAssemblies)
            {
                if (dependency.Name.StartsWith(problematic, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasWindowsSpecificTypes(Assembly assembly)
        {
            try
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (type.Namespace != null && 
                        (type.Namespace.StartsWith("System.Windows") || 
                         type.Namespace.StartsWith("Microsoft.Win32")))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Ignorar erros de reflexão
            }

            return false;
        }

        public static void CreateCompatibilityConfig(string pluginPath)
        {
            var configPath = Path.ChangeExtension(pluginPath, ".config");
            
            if (File.Exists(configPath))
            {
                return; // Já existe
            }

            var config = new XmlDocument();
            var declaration = config.CreateXmlDeclaration("1.0", "utf-8", null);
            config.AppendChild(declaration);

            var configuration = config.CreateElement("configuration");
            config.AppendChild(configuration);

            var runtime = config.CreateElement("runtime");
            configuration.AppendChild(runtime);

            var assemblyBinding = config.CreateElement("assemblyBinding");
            assemblyBinding.SetAttribute("xmlns", "urn:schemas-microsoft-com:asm.v1");
            runtime.AppendChild(assemblyBinding);

            // Adicionar binding redirects para assemblies comuns
            AddBindingRedirect(assemblyBinding, "System.Windows.Forms", "4.0.0.0", "4.0.0.0");
            AddBindingRedirect(assemblyBinding, "System.Drawing", "4.0.0.0", "4.0.0.0");
            AddBindingRedirect(assemblyBinding, "System", "4.0.0.0", "4.0.0.0");

            config.Save(configPath);
        }

        private static void AddBindingRedirect(XmlElement parent, string assemblyName, string oldVersion, string newVersion)
        {
            var dependentAssembly = parent.OwnerDocument.CreateElement("dependentAssembly");
            parent.AppendChild(dependentAssembly);

            var assemblyIdentity = parent.OwnerDocument.CreateElement("assemblyIdentity");
            assemblyIdentity.SetAttribute("name", assemblyName);
            assemblyIdentity.SetAttribute("publicKeyToken", "b77a5c561934e089");
            assemblyIdentity.SetAttribute("culture", "neutral");
            dependentAssembly.AppendChild(assemblyIdentity);

            var bindingRedirect = parent.OwnerDocument.CreateElement("bindingRedirect");
            bindingRedirect.SetAttribute("oldVersion", oldVersion);
            bindingRedirect.SetAttribute("newVersion", newVersion);
            dependentAssembly.AppendChild(bindingRedirect);
        }

        public static void LogCompatibilityInfo(string pluginPath)
        {
            if (_compatibilityCache.TryGetValue(pluginPath, out var info))
            {
                if (info.IsCompatible)
                {
                    Log.Trace($"Plugin '{Path.GetFileName(pluginPath)}' is compatible with .NET Framework 4.8");
                }
                else
                {
                    Log.Warn($"Plugin '{Path.GetFileName(pluginPath)}' has compatibility issues:");
                    foreach (var issue in info.CompatibilityIssues)
                    {
                        Log.Warn($"  - {issue}");
                    }
                }
            }
        }
    }

    public class PluginCompatibilityInfo
    {
        public string PluginPath { get; set; }
        public bool IsCompatible { get; set; }
        public string RequiredFramework { get; set; }
        public List<string> CompatibilityIssues { get; set; } = new List<string>();
    }
}
