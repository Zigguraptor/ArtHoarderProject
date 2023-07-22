using System.Collections.Generic;
using System.Linq;

namespace ArtHoarderClient.Infrastructure.FoldersAndItems;

public class ViewItemsTree
{
    public ViewItemsTree(BaseNode current)
    {
        Current = current;
    }

    public BaseNode Current { get; private set; }

    public bool TryGoBack()
    {
        if (Current.Parent == null) return false;
        Current = Current.Parent;
        return true;
    }

    public bool TryEnterTo(BaseNode node)
    {
        if (node.IsContainer == false || !Current.Children.Contains(node)) return false;
        Current = node;
        return true;
    }

    public BaseNode GoToRoot()
    {
        while (Current.Parent != null)
            Current = Current.Parent;

        return Current;
    }

    public List<string> FullPath
    {
        get
        {
            var path = new List<string>();
            var folderTemplate = Current;
            while (folderTemplate.Parent != null)
            {
                path.Add(folderTemplate.Title);
                folderTemplate = folderTemplate.Parent;
            }

            path.Reverse();
            return path;
        }
    }

    public bool TryGoBackTo(FolderNode node)
    {
        if (Current.GetBreadcrumbs().All(item => item != node)) return false;
        Current = node;
        return true;
    }
}