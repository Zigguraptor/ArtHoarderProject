using System.Collections.Generic;
using ArtHoarderClient.Infrastructure;
using ArtHoarderCore.DAL.Entities;

namespace ArtHoarderClient.Models;

internal class PanelSubmissionInfo : IPanelData
{
    public string PanelTitle { get; } = "Submission Info";
}