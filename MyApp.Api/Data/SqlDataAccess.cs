using System.Data;
using Microsoft.Data.SqlClient;

namespace MyApp.Api.Data;

/// <summary>
/// Thin ADO.NET helper: every call opens its own connection, always calls a
/// stored procedure, and always disposes the connection/command. No EF, no
/// Dapper, no dynamic SQL — parameters only.
/// </summary>
public class SqlDataAccess
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SqlDataAccess(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<SqlConnection> OpenConnectionAsync(CancellationToken ct = default)
    {
        var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        return connection;
    }

    public static SqlCommand CreateCommand(SqlConnection connection, string storedProcedure, SqlTransaction? transaction = null)
    {
        var command = new SqlCommand(storedProcedure, connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 30
        };
        if (transaction is not null) command.Transaction = transaction;
        return command;
    }

    public static void AddParam(SqlCommand command, string name, object? value, SqlDbType? type = null)
    {
        var param = type is null
            ? command.Parameters.AddWithValue(name, value ?? DBNull.Value)
            : command.Parameters.Add(name, type.Value);

        if (type is not null) param.Value = value ?? DBNull.Value;
    }

    public static SqlParameter AddOutputParam(SqlCommand command, string name, SqlDbType type)
    {
        var param = command.Parameters.Add(name, type);
        param.Direction = ParameterDirection.Output;
        return param;
    }

    public async Task ExecuteNonQueryAsync(string storedProcedure, Action<SqlCommand> addParams, CancellationToken ct = default)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = CreateCommand(connection, storedProcedure);
        addParams(command);
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<T?> ExecuteScalarAsync<T>(string storedProcedure, Action<SqlCommand> addParams, CancellationToken ct = default)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = CreateCommand(connection, storedProcedure);
        addParams(command);
        var result = await command.ExecuteScalarAsync(ct);
        return result is null or DBNull ? default : (T)result;
    }

    public async Task<List<T>> ExecuteReaderAsync<T>(string storedProcedure, Action<SqlCommand> addParams,
        Func<SqlDataReader, T> map, CancellationToken ct = default)
    {
        var results = new List<T>();
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = CreateCommand(connection, storedProcedure);
        addParams(command);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(map(reader));
        }
        return results;
    }

    public async Task<T?> ExecuteReaderSingleAsync<T>(string storedProcedure, Action<SqlCommand> addParams,
        Func<SqlDataReader, T> map, CancellationToken ct = default)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = CreateCommand(connection, storedProcedure);
        addParams(command);
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? map(reader) : default;
    }
}
