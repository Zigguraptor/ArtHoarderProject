namespace ArgsParser;

public class ArgsParser
{
    private readonly ArgsParserSettings _argsParserSettings;
    private readonly Dictionary<string, IVerbAction> _verbActionsByName;
    private readonly Action<string>? _error;

    public ArgsParser(ArgsParserSettings argsParserSettings, List<IVerbAction> verbActions,
        Action<string>? error = null)
    {
        _argsParserSettings = argsParserSettings;
        _error = error;
        _verbActionsByName = new Dictionary<string, IVerbAction>();
        foreach (var verbAction in verbActions)
        {
            var verb = verbAction.Verb;
            _verbActionsByName.Add(verb.Name, verbAction);
        }
    }

    public void ParseArgs(string[] args)
    {
        if (args.Length == 0)
        {
            _error?.Invoke("No Args");
            return;
        }

        if (_verbActionsByName.TryGetValue(args[0], out var verbAction))
            verbAction.Invoke(args);
    }
}
