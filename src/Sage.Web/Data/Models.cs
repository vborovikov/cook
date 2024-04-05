namespace Sage.Web.Data;

public static class PageModelExtensions
{
    public static string ToPrettyFormat(this TimeSpan timeSpan)
    {
        if (timeSpan.TotalMilliseconds < 1000d)
            return $"{Math.Ceiling(timeSpan.TotalMilliseconds)} ms";
        if (timeSpan.TotalSeconds < 60d)
            return $"{Math.Ceiling(timeSpan.TotalSeconds)} sec";
        if (timeSpan.TotalMinutes < 60d)
            return $"{timeSpan.Minutes} min {timeSpan.Seconds} sec";

        return "eternity";
    }
}