using ArtHoarderCore.Managers;
using Mono.Cecil;

namespace ArtHoarderCore;

internal static class PerceptualHashing
{
    private static List<Action<double[,], byte[]>> _algorithms = new();
    private static Object _syncObj = new();

    static PerceptualHashing()
    {
        ScanLibsDirectory();
    }

    private static string _libsDirPath = Constants.PerceptualHashingLibs;

    public static void ScanLibsDirectory()
    {
        var libPaths = Directory.GetFiles(_libsDirPath, "*.dll", SearchOption.TopDirectoryOnly);
        lock (_syncObj)
        {
            foreach (var libPath in libPaths)
            {
                TargetRuntime? runtimeVer = null;
                try
                {
                    runtimeVer = ModuleDefinition.ReadModule(libPath).Runtime;
                    if (runtimeVer == TargetRuntime.Net_4_0)
                    {
                        RegManagedLib(libPath);
                        continue;
                    }
                }
                catch
                {
                    // ignored
                }

                RegUnmanagedLib(libPath);
            }
        }
    }

    private static void RegManagedLib(string libPath)
    {
        try
        {
            var moduleDefinition = ModuleDefinition.ReadModule(libPath);
            var type = moduleDefinition.Types.SingleOrDefault(t => t.Name == "PerceptualHash");
            if (type != null && type.IsClass)
            {
                var field = type.Fields.SingleOrDefault(f => f.Name == "HashName");
                if (field != null && field.IsPublic && field.IsStatic && field.IsLiteral &&
                    field.FieldType.FullName == "System.String")
                {
                    var method = type.Methods.SingleOrDefault(m => m.Name == "ComputeHash");
                    if (method != null)
                    {
                        //TODO
                    }
                }
            }
        }
        catch (BadImageFormatException e)
        {
            Console.WriteLine(e);
        }
    }

    private static void RegUnmanagedLib(string libPath)
    {
        throw new NotImplementedException();
    }
}
