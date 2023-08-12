using System.Text;

namespace ArtHoarderArchive;

public static class CommandCreator
{
    public const char Separator = ' ';
    public const char Insulator = '\"';

    public static string Create(string[] args)
    {
        var stringBuilder = new StringBuilder(args.Length);
        var path = Directory.GetCurrentDirectory();

        stringBuilder.Append(Insulator);
        stringBuilder.Append(Escape(path));
        stringBuilder.Append(Insulator);
        if (args.Length > 0)
            stringBuilder.Append(Separator);

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            stringBuilder.Append(Insulator);
            stringBuilder.Append(Escape(arg));
            stringBuilder.Append(Insulator);
            if (i + 1 < args.Length)
                stringBuilder.Append(Separator);
        }

        return stringBuilder.ToString();
    }

    private static string Escape(string s)
    {
        s = s.Replace("\\", "\\\\");
        return s.Replace("\"", "\\\"");
    }
}
