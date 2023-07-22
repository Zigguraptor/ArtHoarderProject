using ArtHoarderCore.Parsers;

namespace ArtHoarderCore;

//В этом классе должны быть только те методы, которые не влияют на архив. Что логично.
public static class GalleryAnalyzer
{
    static GalleryAnalyzer()
    {
        //Нельзя вызывать методы этого парсера влияющие на архив.
        //Пустой обработчик не будет ничего записывать, он выкинет исключение!
        UniversalParser = new UniversalParser(new EmptyParsingHandler());
    }

    private static readonly UniversalParser UniversalParser;


    public static Task<List<Uri>>? TryGetSubscriptionsAsync(Uri uri, CancellationToken cancellationToken)
    {
        return UniversalParser.GetSubscriptions(uri, cancellationToken);
    }
    
    public static string? TryGetUserName(Uri uri)
    {
        return UniversalParser.TryGetUserName(uri);
    }
}
