namespace Persistence;

internal static class Timestamps
{
    public static DateTime Normalize(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    public static DateTime? Normalize(DateTime? value)
    {
        if (value is not DateTime dateTime)
        {
            return null;
        }

        return Normalize(dateTime);
    }
}
