using Microsoft.Data.SqlClient;

namespace MyApp.Api.Data;

public static class SqlDataReaderExtensions
{
    /// <summary>Lets mapping code stay resilient when a stored procedure's result set
    /// legitimately varies by caller (e.g. sp_Login returns more columns than sp_GetUsers).</summary>
    public static bool HasColumn(this SqlDataReader reader, string columnName)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
