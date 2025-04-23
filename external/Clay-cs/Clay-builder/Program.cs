using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using static Bullseye.Targets;
using static SimpleExec.Command;
using System.Runtime.InteropServices;

string GetFilePath([CallerFilePath] string path = default!) => path;

string workingDirectory = Path.GetDirectoryName(GetFilePath());
string clayh = Path.Combine(workingDirectory, "src/clay.h");
string claycs = Path.Combine(workingDirectory, "../Clay-cs/Interop/ClayInterop_2.cs");

// Platform detection
bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
bool isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

// Platform-specific library extensions and paths
string libraryExtension = isWindows ? ".dll" : (isLinux ? ".so" : ".dylib");
string libraryName = $"Clay{libraryExtension}";
string zigoutlib = Path.Combine(workingDirectory, $"./zig-out/bin/{libraryName}");
string libpath = Path.Combine(workingDirectory, $"../Clay-cs/{libraryName}");

Target("Interop", () =>
{
    var interopArgs = string.Join(' ', [
        string.Join(' ', [
            "--config",
            "generate-file-scoped-namespaces",
            "generate-disable-runtime-marshalling",
            "strip-enum-member-type-name",
            "log-exclusions",
            "log-potential-typedef-remappings",
            "exclude-anonymous-field-helpers",
            "unix-types",
            "exclude-fnptr-codegen"
        ]),
        "--with-access-specifier ClayInterop=Internal",
        string.Join(' ', [
            "--exclude",
            "Clay__Clay_StringWrapper",
            "Clay__Clay__StringArrayWrapper",
            "Clay__Clay_ArenaWrapper",
            "Clay__Clay_DimensionsWrapper",
            "Clay__Clay_Vector2Wrapper",
            "Clay__Clay_ColorWrapper",
            "Clay__Clay_BoundingBoxWrapper",
            "Clay__Clay_ElementIdWrapper",
            "Clay__Clay_CornerRadiusWrapper",
            "Clay__Clay__ElementConfigTypeWrapper",
            "Clay__Clay_LayoutDirectionWrapper",
            "Clay__Clay_LayoutAlignmentXWrapper",
            "Clay__Clay_LayoutAlignmentYWrapper",
            "Clay__Clay__SizingTypeWrapper",
            "Clay__Clay_ChildAlignmentWrapper",
            "Clay__Clay_SizingMinMaxWrapper",
            "Clay__Clay_SizingAxisWrapper",
            "Clay__Clay_SizingWrapper",
            "Clay__Clay_PaddingWrapper",
            "Clay__Clay_LayoutConfigWrapper",
            "Clay__Clay_RectangleElementConfigWrapper",
            "Clay__Clay_TextElementConfigWrapModeWrapper",
            "Clay__Clay_TextElementConfigWrapper",
            "Clay__Clay_ImageElementConfigWrapper",
            "Clay__Clay_FloatingAttachPointTypeWrapper",
            "Clay__Clay_FloatingAttachPointsWrapper",
            "Clay__Clay_PointerCaptureModeWrapper",
            "Clay__Clay_FloatingElementConfigWrapper",
            "Clay__Clay_CustomElementConfigWrapper",
            "Clay__Clay_ScrollElementConfigWrapper",
            "Clay__Clay_BorderWrapper",
            "Clay__Clay_BorderElementConfigWrapper",
            "Clay__Clay_ElementConfigUnionWrapper",
            "Clay__Clay_ElementConfigWrapper",
            "Clay__Clay_ScrollContainerDataWrapper",
            "Clay__Clay_ElementDataWrapper",
            "Clay__Clay_RenderCommandTypeWrapper",
            "Clay__Clay_RenderCommandWrapper",
            "Clay__Clay_RenderCommandArrayWrapper",
            "Clay__Clay_PointerDataInteractionStateWrapper",
            "Clay__Clay_PointerDataWrapper",
            "Clay__Clay_ErrorTypeWrapper",
            "Clay__Clay_ErrorDataWrapper",
            "Clay__Clay_ErrorHandlerWrapper",
            "Clay__StringArray",
        ]),

        "--namespace Clay_cs",
        "--methodClassName ClayInterop",
        "--libraryPath Clay",
        $"--file {clayh}",
        $"--output {claycs}",
    ]);
    Run("ClangSharpPInvokeGenerator", interopArgs, workingDirectory);

    // ClangSharpPInvokeGenerator is adding a trailing '}' that breaks compilation
    var text = File.ReadAllText(claycs);
    var idx = text.LastIndexOf('}');
    text = text.Substring(0, idx);

    // fix naming
    text = text.Replace("_size_e__Union", "ClaySizingUnion");

    File.WriteAllText(claycs, text);
});

Target("Dll", async () =>
{
    var ZigToolsetPath = Environment.GetEnvironmentVariable("ZigToolsetPath");
    var ZigExePath = Environment.GetEnvironmentVariable("ZigExePath");
    var ZigLibPath = Environment.GetEnvironmentVariable("ZigLibPath");
    var ZigDocPath = Environment.GetEnvironmentVariable("ZigDocPath");

    // clean build
    var oudDir = Path.GetDirectoryName(zigoutlib);
    if (Directory.Exists(oudDir))
    {
        Directory.Delete(oudDir, true);
    }

    await RunAsync("zig", "build", workingDirectory);

    // Uncomment to copy the library to the Clay-cs directory
    // Directory.CreateDirectory(Path.GetDirectoryName(libpath));
    // File.Delete(libpath);
    // File.Copy(zigoutlib, libpath, true);
});

Target("default", DependsOn("Dll", "Interop"));

await RunTargetsAndExitAsync(args, ex => ex is SimpleExec.ExitCodeException);
