﻿using ArtHoarderArchiveService.Archive.Parsers;

namespace ArtHoarderArchiveService.Archive;

public static class ProjectAnalyzer
{
    public static string? AnalyzeProject()
    {
        // return UnsupportedParserTypes();
        return "";
    }

    #region Trubles

    // private static string? UnsupportedParserTypes()
    // {
    //     if (ParserFactory.UnsupportedTypes == null) return null;
    //
    //     var types = ParserFactory.UnsupportedTypes.Aggregate("", (current, type) => current + (type + ", ")); //TODO fix ", "
    //     return $"Unsupported parser types found: {types}. Check if the settings or version of the app is correct.";
    // }

    #endregion
}
