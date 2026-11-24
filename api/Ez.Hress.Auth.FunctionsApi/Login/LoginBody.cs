namespace Ez.Hress.Auth.FunctionsApi.Login;

public class LoginBody
{
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public class MagicCodeBody
{
    public string? Code { get; set; }
}
