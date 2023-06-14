using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ArtHoarderCore.DAL.Entities;

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