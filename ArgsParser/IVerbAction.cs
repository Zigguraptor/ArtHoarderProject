namespace ArgsParser;

public interface IVerbAction
{
    public VerbAttribute Verb { get; }
    public void Invoke(string[] args);
}
