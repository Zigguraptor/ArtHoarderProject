using ArtHoarderArchiveService.Archive.DAL.Entities;
using ArtHoarderArchiveService.Archive.Managers;
using Microsoft.EntityFrameworkCore;

namespace ArtHoarderArchiveService.Archive.DAL;

internal sealed class CacheDbContext : DbContext
{
    public DbSet<ScheduledGalleryUpdateInfo> ScheduledUpdateGalleries { get; set; } = null!;
    private string DbPath { get; }

    public CacheDbContext(string workDirectory)
    {
        DbPath = Path.Combine(workDirectory, Constants.Temp, "Cache"); //TODO literal
        Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options
            .UseSqlite($"Data Source={DbPath}");
}
