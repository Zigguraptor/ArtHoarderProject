using ArgsParser.Attributes;
using ArtHoarderArchiveService.Archive;

namespace ArtHoarderArchiveService.PipeCommunications.Verbs;

[Verb("add", HelpText = "Add local user name or gallery profile to archive")]
public class AddVerb : BaseVerb
{
    [Group("target", true)]
    [Option('u', "user", 1, HelpText = "Add user")]
    public List<string>? UserNames { get; set; }

    [Group("target", true)]
    [Option('g', "gallery", 1, HelpText = "Add gallery")]
    public List<string>? Gallery { get; set; }

    [Option('a', "auto-names", HelpText = "Automatically assign names (if you add galleries)")]
    public bool AutoNames { get; set; }

    public override bool Validate(out List<string>? errors)
    {
        errors = new List<string>();
        if (Gallery == null)
        {
            if (UserNames != null) return true;
            //TODO
            errors.Add("");
            return false;
        }

        if (AutoNames)
        {
            if (Gallery.All(link => Uri.TryCreate(link, UriKind.Absolute, out _)))
                return true;

            //TODO
            errors.Add("");
            return false;
        }

        if (Gallery.Count % 2 != 0)
        {
            //TODO
            errors.Add("");
            return false;
        }

        for (var i = 1; i < Gallery.Count; i += 2)
        {
            if (!Uri.TryCreate(Gallery[i], UriKind.Absolute, out _)) continue;

            //TODO
            errors.Add("");
            return false;
        }

        return true;
    }

    public override void Invoke(IMessageWriter messageWriter, string path, CancellationToken cancellationToken)
    {
        if (UserNames != null)
            AddUsers(messageWriter, path);
        else
            AddGalleries(messageWriter, path);
    }

    private void AddUsers(IMessageWriter statusWriter, string path)
    {
        if (UserNames == null)
            throw new Exception();

        using var context = new ArchiveContext(path);

        context.TryAddNewUsers(statusWriter, UserNames);
    }

    private void AddGalleries(IMessageWriter statusWriter, string path)
    {
        if (!AutoNames)
            AddGalleriesNoAutoNames(statusWriter, path);
        else
            AddGalleriesAutoNames(statusWriter, path);
    }

    private void AddGalleriesNoAutoNames(IMessageWriter statusWriter, string path)
    {
        if (Gallery == null)
            throw new Exception();

        var uris = new List<Uri>();

        if (Gallery.Count % 2 != 0)
        {
            //TODO
            statusWriter.Write("Args error");
            return;
        }

        for (var i = 1; i < Gallery.Count; i += 2)
        {
            if (Uri.TryCreate(Gallery[i], UriKind.Absolute, out var uri))
            {
                uris.Add(uri);
            }
            else
            {
                statusWriter.Write($"\"{Gallery[i]}\" is not uri.");
            }
        }

        var names = new List<string>(uris.Count);
        for (var i = 0; i < Gallery.Count; i += 2)
        {
            names.Add(Gallery[i]);
        }

        using var context = new ArchiveContext(path);
        context.TryAddNewGalleries(statusWriter, uris, names); //TODO
    }

    private void AddGalleriesAutoNames(IMessageWriter statusWriter, string path)
    {
        if (Gallery == null)
            throw new Exception();

        var uris = new List<Uri>();
        foreach (var s in Gallery)
        {
            if (Uri.TryCreate(s, UriKind.Absolute, out var uri))
            {
                uris.Add(uri);
            }
            else
            {
                statusWriter.Write($"\"{s}\" is not uri.");
            }
        }

        using var context = new ArchiveContext(path);
        context.TryAddNewGalleries(statusWriter, uris); //TODO
    }
}
