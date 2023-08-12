using ArtHoarderArchiveService.Archive.DAL;
using ArtHoarderArchiveService.Archive.DAL.Entities;
using ArtHoarderArchiveService.Archive.HashAlgs;
using ArtHoarderArchiveService.Archive.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ArtHoarderArchiveService.Archive;

internal class PerceptualHashing
{
    private readonly string _workDirectory;
    private readonly List<IPerceptualHashAlgorithm> _enabledAlgorithms;

    internal PerceptualHashing(string workDirectory)
    {
        _workDirectory = workDirectory;
        _enabledAlgorithms = new List<IPerceptualHashAlgorithm>(Algorithms);
    }

    private static readonly IPerceptualHashAlgorithm[] Algorithms =
    {
        new FastDct()
    };

    public static string[] GetAvailableAlgorithms()
    {
        var algorithms = new string[Algorithms.Length];
        for (var i = 0; i < Algorithms.Length; i++)
        {
            algorithms[i] = Algorithms[i].HashName;
        }

        return algorithms;
    }

    public void SetEnabledAllAlgorithms()
    {
        if (Algorithms.Length == _enabledAlgorithms.Count) return;
        _enabledAlgorithms.Clear();
        _enabledAlgorithms.AddRange(Algorithms);
    }

    public void SetEnabledAlgorithms(List<string> algorithmsNames)
    {
        _enabledAlgorithms.Clear();
        foreach (var hashAlgorithm in Algorithms)
        {
            if (algorithmsNames.Contains(hashAlgorithm.HashName))
            {
                _enabledAlgorithms.Add(hashAlgorithm);
            }
        }
    }

    public void CalculateHashes(Guid guid, Stream stream)
    {
        var image = Image.Load<Rgb24>(stream);
        var lowImage = CompressImage(image);
        CalculateHashes(guid, lowImage);
    }

    private void CalculateHashes(Guid guid, double[,] lowImage)
    {
        foreach (var algorithm in _enabledAlgorithms)
        {
            SavePHash(algorithm.HashName, guid, algorithm.ComputeHash(lowImage));
        }
    }

    private double[,] CompressImage(Image<Rgb24> image)
    {
        image.Mutate(x => x.Resize(32, 32));

        var grayImageMatrix = new double[32, 32];
        for (var i = 0; i < 32; i++)
        {
            for (var j = 0; j < 32; j++)
            {
                var pixel = image[i, j];
                grayImageMatrix[i, j] = ((double)pixel.R + pixel.G + pixel.B) / 3d;
            }
        }

        return grayImageMatrix;
    }

    private void SavePHash(string hashName, Guid fileGuid, byte[] hash)
    {
        using var context = new PHashDbContext(_workDirectory, hashName);
        if (context.PHashInfos.Find(fileGuid) == null)
            context.PHashInfos.Add(new PHashInfo(fileGuid, hash));
        TrySaveChanges(context);
    }

    private static void TrySaveChanges(DbContext dbContext)
    {
        try
        {
            dbContext.SaveChanges();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}
