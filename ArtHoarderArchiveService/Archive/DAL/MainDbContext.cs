using ArtHoarderArchiveService.Archive.DAL.Entities;
using ArtHoarderArchiveService.Archive.Managers;
using Microsoft.EntityFrameworkCore;

namespace ArtHoarderArchiveService.Archive.DAL;

internal sealed class MainDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<GalleryProfile> GalleryProfiles { get; set; } = null!;
    public DbSet<Submission> Submissions { get; set; } = null!;
    public DbSet<FileMetaInfo> FilesMetaInfos { get; set; } = null!;
    public DbSet<SubmissionFileMetaInfo> SubmissionFileMetaInfos { get; set; } = null!;
    public DbSet<SubmissionComment> SubmissionComments { get; set; } = null!;
    public DbSet<ProfileComment> ProfileComments { get; set; } = null!;

    //Views
    public DbSet<ProfileInfo> DisplayedGalleries { get; set; } = null!;

    private string DbPath { get; }
    private readonly ChangesInterceptor _changesInterceptor;

    public MainDbContext(string workDirectory)
    {
        DbPath = Path.Combine(workDirectory, Constants.MainDbPath);
        _changesInterceptor = new ChangesInterceptor(Path.Combine(workDirectory, Constants.ChangesAuditDbPath));
        Database.EnsureCreated();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options
            .AddInterceptors(_changesInterceptor)
            .UseSqlite($"Data Source={DbPath}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SubmissionComment>().HasNoKey();
        modelBuilder.Entity<ProfileComment>().HasNoKey();

        modelBuilder.Entity<Submission>()
            .HasMany(s => s.FileMetaInfos)
            .WithMany(f => f.Submissions)
            .UsingEntity(b => b.ToTable("SubmissionsFileMetaInfos"));

        #region Views

        modelBuilder.Entity<ProfileInfo>()
            .HasNoKey()
            .ToView("View_DisplayedGalleries");

        #endregion
    }
}
