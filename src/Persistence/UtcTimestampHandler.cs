using System.Data;
using Dapper;
using Npgsql;
using NpgsqlTypes;

namespace Persistence;

internal sealed class UtcTimestampHandler : SqlMapper.TypeHandler<DateTime>
{
    public override void SetValue(IDbDataParameter parameter, DateTime value)
    {
        parameter.Value = Timestamps.Normalize(value);
        if (parameter is NpgsqlParameter npgsql)
        {
            npgsql.NpgsqlDbType = NpgsqlDbType.TimestampTz;
        }
    }

    public override DateTime Parse(object value)
    {
        return value switch
        {
            DateTime dateTime => Timestamps.Normalize(dateTime),
            DateTimeOffset offset => Timestamps.Normalize(offset.UtcDateTime),
            _ => throw new DataException($"Cannot read a timestamp from {value.GetType().Name}.")
        };
    }
}
