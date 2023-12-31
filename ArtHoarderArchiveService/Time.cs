﻿using System.Globalization;

namespace ArtHoarderArchiveService;

public static class Time
{
    public static DateTime NowUtcDataTime()
    {
        // var client = new TcpClient("time.nist.gov", 13);
        // using var streamReader = new StreamReader(client.GetStream());
        // var response = streamReader.ReadToEnd();
        // if (response.Length < 1)
        //     return DateTime.UtcNow;
        //
        // var utcDateTimeString = response.Substring(7, 17);
        //
        // return DateTime.ParseExact(utcDateTimeString, "yy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        return DateTime.UtcNow;
    }

    public static DateOnly NowUtcDataOnly()
    {
        return DateOnly.FromDateTime(NowUtcDataTime());
    }

    public static TimeOnly NowCurrentTimeOnly()
    {
        return TimeOnly.FromDateTime(NowUtcDataTime());
    }

    public static string DataTimeToString(DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public static DateTime StringToDataTime(string time)
    {
        return DateTime.ParseExact(time, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }
}
