﻿using System.Text.Json;
using ArtHoarderCore.Managers;
using ArtHoarderCore.Serializable;

namespace ArtHoarderCore;

public class ArchiveContext : IDisposable
{
    private readonly FileStream _mainFile;
    private readonly object _filesAccessSyncObj = new();
    private readonly string _workDirectory;
    private ArchiveMainFile _cachedArchiveMainFile;

    private string MainFilePath => Path.Combine(_workDirectory, Constants.ArchiveMainFilePath);

    public ArchiveContext(string workDirectory)
    {
        _workDirectory = workDirectory;
        _mainFile = File.Open(MainFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        _cachedArchiveMainFile = ReadArchiveFile();
    }

    private ArchiveMainFile ReadArchiveFile()
    {
        lock (_mainFile)
        {
            var archiveMainFile = JsonSerializer.Deserialize<ArchiveMainFile>(_mainFile);
            if (archiveMainFile == null)
                throw new Exception("Archive main file cannot be read");
            return archiveMainFile;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (_mainFile)
            {
                _mainFile.Dispose();
            }
        }
    }
}
