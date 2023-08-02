using ArtHoarderCore.DAL.Entities;

namespace ArtHoarderCore.Infrastructure;

public interface IDisplayViewItem
{
    string? Title { get; }

    string IconPath { get; }

    Property[] Properties { get; }
}