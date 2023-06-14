namespace ArtHoarderCore.Infrastructure;

public interface IImageHasher
{
    public string HashName { get; }

    public Byte[] ComputeHash(double[,] image);
}