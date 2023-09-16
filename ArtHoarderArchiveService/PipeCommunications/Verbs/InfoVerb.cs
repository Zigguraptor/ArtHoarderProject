using ArgsParser.Attributes;
using ArtHoarderArchiveService.Archive;

namespace ArtHoarderArchiveService.PipeCommunications.Verbs;

public class InfoVerb : BaseVerb
{
    [Option('f', "full", HelpText = "Print full archive info.")]
    public bool Full { get; set; }

    [Option('g', "galleries", HelpText = "Print galleries info.")]
    public bool Galleries { get; set; }

    public override bool Validate(out List<string>? errors)
    {
        throw new NotImplementedException();
    }

    public override void Invoke(IMessager messager, ArchiveContextFactory archiveContextFactory, string path,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private void PrintGalleriesInfo(IMessager messager, ArchiveContextFactory archiveContextFactory, string path)
    {
        var context = archiveContextFactory.CreateArchiveContext(messager, path, this);
        var users = context.GetUsers();

        archiveContextFactory.RealiseContext(path, this);
        if (users.Count > 100 && messager.Confirmation($"display {users.Count} lines?"))
        {
            var tempFilePath = Path.Combine(path, Constants.Temp, "temp_users.txt");
            var tempFileStream = CreateOrRecreateFile(tempFilePath);
            tempFileStream.Dispose();
            var streamWriter = File.AppendText(path);

            foreach (var user in users)
            {
                streamWriter.WriteLine(
                    $"{user.Name} FirstSaveTime:{user.FirstSaveTime} LastUpdateTime:{user.LastUpdateTime}");
                streamWriter.Flush();
            }

            streamWriter.Dispose();
            messager.WriteFile(tempFilePath);
        }
        else
        {
            foreach (var user in users)
            {
                messager.WriteMessage(
                    $"{user.Name} FirstSaveTime:{user.FirstSaveTime} LastUpdateTime:{user.LastUpdateTime}");
            }
        }
    }

    private FileStream CreateOrRecreateFile(string path)
    {
        if (File.Exists(path))
            File.Delete(path);

        return File.Create(path);
    }
}
