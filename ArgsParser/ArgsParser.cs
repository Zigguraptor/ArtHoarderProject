namespace ArgsParser;

public class ArgsParser
{
    private readonly ArgsParserSettings _argsParserSettings;
    private readonly Dictionary<string, IVerbAction> _verbActionsByName;

    public ArgsParser(ArgsParserSettings argsParserSettings, List<IVerbAction> verbActions)
    {
        _argsParserSettings = argsParserSettings;
        _verbActionsByName = new Dictionary<string, IVerbAction>();
        foreach (var verbAction in verbActions)
        {
            var verb = verbAction.Verb;
            _verbActionsByName.Add(verb.Name, verbAction);
        }
    }
}
