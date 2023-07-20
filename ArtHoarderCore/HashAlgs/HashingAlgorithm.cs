namespace ArtHoarderCore.HashAlgs;

internal class HashingAlgorithm
{
    public string HashName { get; }
    public Action<double[,], byte[]> ComputeHash;

    public HashingAlgorithm(string hashName, Action<double[,], byte[]> computeHash)
    {
        HashName = hashName;
        ComputeHash = computeHash;
    }
}
