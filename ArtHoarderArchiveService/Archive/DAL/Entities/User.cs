﻿using System.ComponentModel.DataAnnotations;

namespace ArtHoarderArchiveService.Archive.DAL.Entities;

public class User
{
    [Key] public string Name { get; set; } = null!;

    [Required] public DateTime? FirstSaveTime { get; set; }
    [Required] public DateTime? LastUpdateTime { get; set; }
}
