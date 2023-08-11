using System.Reflection;
using ArgsParser.Attributes;
using ArtHoarderArchiveService.Archive.Parsers;
using ArtHoarderArchiveService.Archive.Parsers.Settings;

namespace ArtHoarderArchiveService.PipeCommunications.Verbs;

[Verb("parser", HelpText = "TODO")] //TODO help text
public class ParserVerb : BaseVerb
{
    public ParserVerb()
    {
        IsParallel = true;
    }

    [Option('r', "reload")] public bool Reload { get; set; }
    [Option('L', "list-types")] public bool SupportedTypes { get; set; }
    [Option('l', "list")] public bool List { get; set; }
    [Option('c', "create", 1)] public string? CreateMode { get; set; }
    [Option("export")] public List<string>? Export { get; set; }
    [Option("import")] public List<string>? Import { get; set; }

    public override bool Validate(out List<string>? errors)
    {
        errors = null;
        return true;
    }

    public override void Invoke(IMessageWriter messageWriter, string path, CancellationToken cancellationToken)
    {
        if (Reload)
            ParserFactory.ReloadParsesSettings(messageWriter);
        if (List)
            WriteLoadedParserConfigs(messageWriter);
        if (SupportedTypes)
            WriteSupportedTypes(messageWriter);
        if (CreateMode != null)
            CreateParserSettings(messageWriter);
        if (Import != null)
        {
            foreach (var cfgPath in Import)
                ParserFactory.ImportParserConfig(messageWriter, cfgPath);
        }

        if (Export != null)
        {
            throw new NotImplementedException();
        }
    }

    private void CreateParserSettings(IMessageWriter messageWriter)
    {
        if (!ParserFactory.SupportedTypes.TryGetValue(CreateMode!, out var cfgType)) return;
        var cfgInstance = Activator.CreateInstance(cfgType);
        cfgType.GetProperty("ParserType")!.SetValue(cfgInstance, CreateMode);
        cfgType.GetProperty("Version")!.SetValue(cfgInstance, Time.NowUtcDataOnly());

        foreach (var propertyInfo in cfgType.GetProperties())
        {
            if (propertyInfo.GetCustomAttribute(typeof(AutoSetAttribute)) != null) continue;
            messageWriter.Write(propertyInfo.Name + ": ");
            var line = messageWriter.ReadLine();
            if (line == null) return;
            if (propertyInfo.PropertyType == typeof(int))
            {
                if (int.TryParse(line, out var number))
                    propertyInfo.SetValue(cfgInstance, number);
                else
                {
                    messageWriter.WriteLine("\nCan not convert to int");
                    return;
                }
            }
            else if (propertyInfo.PropertyType == typeof(string))
            {
                propertyInfo.SetValue(cfgInstance, line);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        ParserFactory.ImportParserConfig(messageWriter, (ParserSettings)cfgInstance!);
    }

    private void WriteLoadedParserConfigs(IMessageWriter messageWriter)
    {
        var msg = "";
        foreach (var parserSettings in ParserFactory.ParsersSettingsList)
            msg += $"[Type: {parserSettings.ParserType}] {parserSettings.Host} [{parserSettings.Version}]\n";

        if (ParserFactory.UnsupportedTypesReport != null)
        {
            msg += "Configs found for unsupported types: ";
            foreach (var unsupportedType in ParserFactory.UnsupportedTypesReport)
                msg += unsupportedType + ' ';
            msg += '\n';
        }

        messageWriter.WriteLine(msg);
    }

    private void WriteSupportedTypes(IMessageWriter messageWriter)
    {
        var msg = "Supported parser types: ";
        foreach (var (key, _) in ParserFactory.SupportedTypes)
            msg += key + ", ";
        msg = msg.TrimEnd(' ').TrimEnd(',') + '.';
        messageWriter.WriteLine(msg);
    }
}
