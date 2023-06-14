using ArtHoarderCore.DAL.Entities;
using ArtHoarderCore.Managers;
using Microsoft.EntityFrameworkCore;

namespace ArtHoarderCore.DAL;

public sealed class PHashDbContext : DbContext
{
    public DbSet<PHashInfo> PHashInfos { get; set; } = null!;

    private string DbPath { get; }

    public PHashDbContext(string workDirectory, string hashName)
    {
        DbPath = Path.Combine(workDirectory, Constants.PHashDbDir, hashName + ".db");
        Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options
            .UseSqlite($"Data Source={DbPath}");
}