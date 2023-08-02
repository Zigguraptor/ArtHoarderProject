using ArtHoarderCore.DAL;
using ArtHoarderCore.DAL.Entities;

namespace ArtHoarderCore.Infrastructure;

public class UnregisteredFile : IDisplayViewItem
{
    public UnregisteredFile(string fullPath)
    {
        Title = Path.GetFileName(fullPath);
        IconPath = fullPath;
    }

    public string? Title { get; }
    public string IconPath { get; }

    [NotDisplay]
    public Property[] Properties
    {
        get
        {
            var type = GetType();
            var displayProps = type.GetProperties().Where(prop => !prop.IsDefined(typeof(NotDisplayAttribute), true))
                .ToArray();
            var result = new Property[displayProps.Length];
            for (var i = 0; i < displayProps.Length; i++)
            {
                result[i] = new Property(displayProps[i].Name, displayProps[i].GetValue(this)?.ToString());
            }

            return result;
        }
    }
}