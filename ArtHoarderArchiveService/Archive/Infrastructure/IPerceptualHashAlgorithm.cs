namespace ArtHoarderCore.Infrastructure;

public interface IPerceptualHashAlgorithm
{
    public string HashName { get; }

    public Byte[] ComputeHash(double[,] image);
}