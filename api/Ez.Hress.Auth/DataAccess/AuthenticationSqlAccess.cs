using Ez.Hress.Auth.Entities;
using Ez.Hress.Auth.UseCases;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Ez.Hress.Auth.DataAccess;

public class AuthenticationSqlAccess : IAuthenticationDataAccess
{
    private const string _class = nameof(AuthenticationSqlAccess);
    private readonly string _connectionString;
    private readonly ILogger<AuthenticationSqlAccess> _log;

    public AuthenticationSqlAccess(DbConnectionInfo connectionInfo, ILogger<AuthenticationSqlAccess> log)
    {
        _connectionString = connectionInfo.ConnectionString;
        _log = log;
    }

    public async Task<(int UserID, string? PasswordHash)?> GetUserByUsername(string username)
    {
        _log.LogInformation("[{Class}.{Method}] Getting user '{Username}'", _class, nameof(GetUserByUsername), username);
        const string sql = "SELECT Id, ApiPassword FROM adm_User WHERE Username = @username";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@username", username);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return null;

        return (reader.GetInt32(0), reader.IsDBNull(1) ? null : reader.GetString(1));
    }

    public async Task<int> RedeemMagicCode(string code)
    {
        // Mark the code used and read its user in one statement, so a code can only be redeemed once
        // even with parallel requests. OUTPUT ... INTO a table variable works whether or not the table has triggers.
        const string sql = @"DECLARE @redeemed TABLE (UserID int);
                        UPDATE  adm_MagicCode
                        SET     DeletedBy = InsertedBy
                        OUTPUT  inserted.InsertedBy INTO @redeemed
                        WHERE   Code = @code
                            AND Deleted > GetDate()
                            AND DeletedBy IS NULL;
                        SELECT TOP 1 UserID FROM @redeemed;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@code", code);

        var result = await command.ExecuteScalarAsync();
        return result is null or DBNull ? 0 : Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task SaveLoginInformation(int userID, string ipAddress)
    {
        const string sql = @"UPDATE adm_User
                SET LastLoggedIn = GetDate(),
                    LastLoggedInBy = @ipAddress,
                    LoginCount = LoginCount + 1,
                    IsOnline = 1
            WHERE Id = @userID";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@userID", userID);
        command.Parameters.AddWithValue("@ipAddress", ipAddress);

        _ = await command.ExecuteNonQueryAsync();
    }
}
