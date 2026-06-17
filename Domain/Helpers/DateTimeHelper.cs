using System.Globalization;
using Domain.Constants;

namespace Domain.Helpers;

public static class DateTimeHelper
{
    /// <summary>
    ///     Get current date time
    /// </summary>
    /// <returns>DateTime</returns>
    public static DateTime GetDt()
    {
        return DateTime.Now;
    }

    /// <summary>
    ///     Get current date time
    /// </summary>
    /// <returns>DateTimeOffset</returns>
    public static DateTimeOffset GetDtOffset()
    {
        return DateTimeOffset.Now;
    }

    /// <summary>
    ///     Get current date time
    /// </summary>
    /// <returns>DateTime</returns>
    public static DateTime GetDtUtc()
    {
        return DateTime.UtcNow;
    }

    /// <summary>
    ///     Get current date time utc
    /// </summary>
    /// <returns>DateTimeOffset</returns>
    public static DateTimeOffset GetDtOffsetUtc()
    {
        return DateTimeOffset.UtcNow;
    }

    public static DateTime? ConvertToDateTime(this string? dateString, string format = DateTimeFormats.Date)
    {
        if (string.IsNullOrWhiteSpace(dateString)) return null;

        try
        {
            if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out var dateTime)) return dateTime;

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Convert date time to format
    /// </summary>
    /// <param name="source">Source</param>
    /// <param name="format">Format</param>
    /// <returns>Text of converted date time</returns>
    public static string ToFormat(this DateTime source, string format = DateTimeFormats.Date)
    {
        return source.ToString(format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Convert date time offset to format
    /// </summary>
    /// <param name="source">Source</param>
    /// <param name="format">Format</param>
    /// <returns>Text of converted date time offset</returns>
    public static string ToFormat(this DateTimeOffset source, string format = DateTimeFormats.Date)
    {
        return source.ToString(format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Convert time span to format
    /// </summary>
    /// <param name="source">Source</param>
    /// <param name="format">Format</param>
    /// <returns>Text of converted time span</returns>
    public static string ToFormat(this TimeSpan source, string format = DateTimeFormats.Time2)
    {
        return source.ToString(format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    ///     Parse a text to utc date with format
    /// </summary>
    /// <param name="source">Source</param>
    /// <param name="result">Parse result</param>
    /// <param name="format">Format</param>
    /// <returns>Parse success/fail</returns>
    public static bool TryParseUtc(this string source, out DateTimeOffset result, string format = DateTimeFormats.Date)
    {
        return DateTimeOffset.TryParseExact(
            source,
            format,
            null,
            DateTimeStyles.AssumeUniversal,
            out result);
    }

    /// <summary>
    ///     Parse a text to date with format
    /// </summary>
    /// <param name="source">Source</param>
    /// <param name="result">Parse result</param>
    /// <param name="format">Format</param>
    /// <returns>Parse success/fail</returns>
    public static bool TryParse(this string source, out DateTimeOffset result, string format = DateTimeFormats.Date)
    {
        return DateTimeOffset.TryParseExact(
            source,
            format,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out result);
    }

    /// <summary>
    ///     Parse a text to date with format
    /// </summary>
    /// <param name="source">Source</param>
    /// <param name="result">Parse result</param>
    /// <param name="format">Format</param>
    /// <returns>Parse success/fail</returns>
    public static bool TryParse(this string source, out DateTime result, string format = DateTimeFormats.Date)
    {
        return DateTime.TryParseExact(
            source,
            format,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out result);
    }

    /// <summary>
    ///     Get minutes between 2 date time offset
    /// </summary>
    /// <param name="from">From date time</param>
    /// <param name="to">To date time</param>
    /// <returns>Minutes</returns>
    public static double GetTotalMinutes(this DateTimeOffset from, DateTimeOffset to)
    {
        return (to - from).TotalMinutes;
    }

    /// <summary>
    ///     Convert to timezone
    /// </summary>
    /// <param name="source">Source</param>
    /// <param name="timezoneName">Timezone</param>
    /// <returns>Converted datetime</returns>
    public static DateTimeOffset ToTimezone(this DateTimeOffset source, string timezoneName)
    {
        var timezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneName);
        return TimeZoneInfo.ConvertTime(source, timezone);
    }

    /// <summary>
    ///     Convert the Unix timestamp to a DateTime
    /// </summary>
    /// <param name="timestampObj">TimeObj</param>
    /// <returns>Converted datetime</returns>
    public static DateTimeOffset ConvertUnixTimestampToDateTime(object timestampObj)
    {
        var timestamp = Convert.ToInt64(timestampObj);
        return DateTimeOffset.FromUnixTimeSeconds(timestamp);
    }

    public static DateTime GetNextDayOfWeek(this DateTime currentDate, DayOfWeek dow)
    {
        var currentDay = (int)currentDate.DayOfWeek;
        var gotoDay = (int)dow;
        return currentDate.AddDays(7).AddDays(gotoDay - currentDay);
    }

    public static List<(DayOfWeek dow, DateTime date)> GenerateDayOfWeek(DateTime startDate, DateTime endDate)
    {
        List<(DayOfWeek, DateTime)> weekdayDatePairs = [];
        var currentDate = startDate;
        while (currentDate <= endDate)
        {
            var dayOfWeek = currentDate.DayOfWeek;

            weekdayDatePairs.Add((dayOfWeek, currentDate));
            currentDate = currentDate.AddDays(1);
        }

        return weekdayDatePairs;
    }

    public static List<DateTime> GetDatesOfMonth(int year, int month)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        return Enumerable.Range(1, daysInMonth).Select(day => new DateTime(year, month, day)).ToList();
    }

    public static List<DateTime> GetDatesOfMonthUtc(int year, int month)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        return Enumerable.Range(1, daysInMonth)
            .Select(day => DateTime.SpecifyKind(new DateTime(year, month, day), DateTimeKind.Utc)).ToList();
    }

    public static TimeSpan GetDuration(DateTimeOffset time1, DateTimeOffset time2)
    {
        TimeSpan timeSpan = time1 - time2;

        return TimeSpan.FromTicks(Math.Abs(timeSpan.Ticks));
    }
}