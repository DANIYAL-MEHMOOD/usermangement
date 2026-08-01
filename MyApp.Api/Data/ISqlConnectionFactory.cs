using Microsoft.Data.SqlClient;

namespace MyApp.Api.Data;

public interface ISqlConnectionFactory
{
    SqlConnection CreateConnection();
}
