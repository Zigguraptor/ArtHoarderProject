namespace ArgsParser;

public class ArgsParserBuilder
{
    private readonly ArgsParserSettings _argsParserSettings;
    private readonly List<IVerbAction> _verbActions;

    public ArgsParserBuilder() : this(new ArgsParserSettings())
    {
    }

    public ArgsParserBuilder(ArgsParserSettings settings)
    {
        _argsParserSettings = settings;
        _verbActions = new List<IVerbAction>();
    }

    public ArgsParserBuilder AddVerb<T>(Action<T> verbAction) where T : new()
    {
        _verbActions.Add(new VerbAction<T>(verbAction));
        return this;
    }

    public ArgsParser Build()
    {
        return new ArgsParser(_argsParserSettings, _verbActions);
    }
}
