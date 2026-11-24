namespace Ez.Hress.Auth.UseCases;

public interface IAuthenticationDataAccess
{
    /// <summary>Returns the user id and stored password hash for a username, or null when the user doesn't exist.</summary>
    Task<(int UserID, string? PasswordHash)?> GetUserByUsername(string username);

    /// <summary>Marks a valid, unexpired magic code as used and returns its user id. Returns 0 when the code is unknown, expired or already used.</summary>
    Task<int> RedeemMagicCode(string code);

    Task SaveLoginInformation(int userID, string ipAddress);
}
