using Microsoft.Data.SqlClient;

namespace MyApp.Infrastructure.Data;

public interface ISqlConnectionFactory
{
    SqlConnection CreateConnection();
}
