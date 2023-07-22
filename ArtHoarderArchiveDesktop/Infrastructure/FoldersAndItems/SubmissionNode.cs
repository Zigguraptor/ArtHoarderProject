using System.Collections.Generic;
using System.Linq;
using ArtHoarderCore.DAL.Entities;
using ArtHoarderCore.Infrastructure;
using ArtHoarderCore.Managers;

namespace ArtHoarderClient.Infrastructure.FoldersAndItems;

public class SubmissionNode : BaseNode
{
    private readonly FullSubmissionInfo _submissionInfo;
    public override string Title => _submissionInfo.Title ?? string.Empty;

    public override string IconPath => _submissionInfo.SubmissionFiles.FirstOrDefault()?.IconPath ??
                                       MyConstants.UnknownIconLocalPath;

    public override Property[] Properties => _submissionInfo.Properties;

    public override bool IsContainer => _submissionInfo.SubmissionFiles.Count > 1;

    public SubmissionNode(FullSubmissionInfo fullSubmissionInfo, FolderNode? parent = null) : base("",
        parent)
    {
        _submissionInfo = fullSubmissionInfo;
    }

    public override List<IDisplayViewItem> GetViewItems(Archive archive)
    {
        var result = new List<IDisplayViewItem>(_submissionInfo.SubmissionFiles.Count);
        for (int i = 0; i < result.Count; i++)
            result[i] = new FileNode(_submissionInfo.SubmissionFiles[i]);

        return result;
    }
}