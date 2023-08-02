﻿using ArtHoarderCore.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArtHoarderCore.DAL;

internal sealed class ChangesAuditContext : DbContext
{
    public DbSet<ChangeInfo> Changes { get; set; } = null!;
    private string DbPath { get; }

    public ChangesAuditContext(string dbPath)
    {
        DbPath = dbPath;
        Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options
            .UseSqlite($"Data Source={DbPath}");
}