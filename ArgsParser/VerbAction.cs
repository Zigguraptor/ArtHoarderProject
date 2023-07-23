namespace ArgsParser;

public class VerbAction<T> : IVerbAction
{
    private readonly Action<T> _verbAction;

    public VerbAction(Action<T> verbAction)
    {
        _verbAction = verbAction;
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
        throw new NotImplementedException();
    }
}
