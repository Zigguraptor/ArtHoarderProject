﻿using ArtHoarderCore.DAL;
using ArtHoarderCore.DAL.Entities;
using ArtHoarderCore.HashAlgs;
using ArtHoarderCore.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ArtHoarderCore;

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

    public void CalculateHashes(Guid guid, double[,] lowImage)
    {
        foreach (var algorithm in _enabledAlgorithms)
        {
            SavePHash(algorithm.HashName, guid, algorithm.ComputeHash(lowImage));
        }
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
