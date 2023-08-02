using System.Runtime.InteropServices;
using ArtHoarderCore.Infrastructure;

namespace ArtHoarderCore.HashAlgs;

public class FastDct : IPerceptualHashAlgorithm
{
    [DllImport("PerceptualHashing.dll")]
    static extern void calculate_dct_hash(double[,] matrix, byte[] output);

    public string HashName { get; } = "FastDCT v1.0";

    public byte[] ComputeHash(double[,] image)
    {
        var result = new byte[8];
        calculate_dct_hash(image, result);
        return result;
    }
}
