using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ArtHoarderArchiveService.Archive.DAL.Entities;

[Index(nameof(Hash))]
public class PHashInfo
{
    public PHashInfo(Guid fileGuid, byte[] hash)
    {
        FileGuid = fileGuid;
        Hash = hash;
    }

    [Key] public Guid FileGuid { get; set; }
    public Byte[] Hash { get; set; }
}
