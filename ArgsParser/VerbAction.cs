using ArgsParser.Attributes;

namespace ArgsParser;

public class VerbAction<T> : IVerbAction where T : new()
{
    private readonly Action<T> _verbAction;
    private readonly int _minArgs = 0;
    private readonly List<IArgAttribute> _commonGroupAttributes;
    private readonly Dictionary<string, List<IArgAttribute>>? _attributeGroups;

    public VerbAction(Action<T> verbAction)
    {
        _verbAction = verbAction;
        var argAttributes = typeof(T).GetCustomAttributes(typeof(IArgAttribute), true);
        _commonGroupAttributes = new List<IArgAttribute>(argAttributes.Length);
        _attributeGroups = new Dictionary<string, List<IArgAttribute>>();
        foreach (IArgAttribute attribute in argAttributes)
        {
            if (attribute.Required)
                _minArgs++;

            if (attribute.GroupName == null)
            {
                _commonGroupAttributes.Add(attribute);
            }
            else
            {
                if (_attributeGroups.TryGetValue(attribute.GroupName, out var group))
                {
                    group.Add(attribute);
                }
                else
                {
                    group = new List<IArgAttribute>();
                    _attributeGroups.Add(attribute.GroupName, group);
                }
            }
        }

        if (_attributeGroups.Count == 0)
            _attributeGroups = null;
    }

    public VerbAttribute Verb
    {
        get
        {
            var customAttribute = typeof(T).GetCustomAttributes(typeof(VerbAttribute), true)[0];
            if (customAttribute is VerbAttribute verbAttribute)
                return verbAttribute;

            throw new Exception(); //TODO
        }
    }

    public void Invoke(string[] args)
    {
        if (args.Length < _minArgs)
        {
            throw new Exception(""); //TODO error
        }

        var parsedOptions = new T();
    }
}
