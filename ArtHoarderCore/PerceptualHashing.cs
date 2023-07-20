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
        throw new NotImplementedException();
    }

    private static void RegUnmanagedLib(string libPath)
    {
        throw new NotImplementedException();
    }
}
