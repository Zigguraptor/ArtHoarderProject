namespace ArtHoarderArchiveService.Archive;

public struct ActionResult
{
    public bool IsOk = false;
    public List<string> Errors = new();

    public ActionResult()
    {
    }

    public void AddError(string message) => Errors.Add(message);

    public static ActionResult operator +(ActionResult m1, ActionResult m2)
    {
        var errors = new List<string>(m1.Errors);
        errors.AddRange(m2.Errors);

        return new ActionResult
        {
            IsOk = m1.IsOk || m2.IsOk,
            Errors = errors
        };
    }
}
