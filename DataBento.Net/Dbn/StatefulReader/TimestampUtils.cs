namespace DataBento.Net.Dbn.StatefulReader;

public static class TimestampUtils
{
    private static readonly long UnixEpochTicks = DateTimeOffset.UnixEpoch.Ticks;
    public static long DateTimeToUnixNano(DateTime dt)
    {
        if(dt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("DateTime must be in UTC");
        return (dt.Ticks - UnixEpochTicks) * 100L;
    }

    public static DateTime UnixNanoToDateTime(ulong unixNano)
    {
        var ticks = checked((long)(unixNano / 100UL)) + UnixEpochTicks;
        return new DateTime(ticks, DateTimeKind.Utc);
    }
    public static TimeSpan UnixNanoToTimeSpan(ulong unixNano)
    {
        var ticks = checked((long)(unixNano / 100UL));
        return new TimeSpan(ticks);
    }
}