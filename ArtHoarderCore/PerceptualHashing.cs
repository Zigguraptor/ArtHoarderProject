using ArtHoarderCore.HashAlgs;
using ArtHoarderCore.Infrastructure;

namespace ArtHoarderCore;

internal static class PerceptualHashing
{
    private static IPerceptualHashAlgorithm[] _algorithms =
    {
        new FastDct()
    };
}
