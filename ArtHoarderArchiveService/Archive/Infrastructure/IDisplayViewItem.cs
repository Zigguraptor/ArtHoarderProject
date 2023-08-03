using ArtHoarderArchiveService.Archive.DAL.Entities;

namespace ArtHoarderArchiveService.Archive.Infrastructure;

public interface IDisplayViewItem
{
    string? Title { get; }

    string IconPath { get; }

    Property[] Properties { get; }
}