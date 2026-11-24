using Ez.Hress.Auth.UseCases;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Text;

namespace Ez.Hress.Auth.UnitTest.UseCases;

public class PasswordHasherTests
{
    /// <summary>The hash Hress.Org computed (AuthenticationInteractor.HashPassword). Must stay byte-identical, or existing passwords stop working.</summary>
    private static string LegacyHash(string password, string salt) =>
        Convert.ToBase64String(KeyDerivation.Pbkdf2(password, Encoding.UTF32.GetBytes(salt), KeyDerivationPrf.HMACSHA512, 10000, 256 / 8));

    [Theory]
    [InlineData("password", "salt")]
    [InlineData("Lykilorð með íslenskum stöfum þæö", "some-long-salt-value-1234567890")]
    [InlineData("p", "s")]
    [InlineData("emoji 🍺 password", "salt with spaces")]
    public void Hash_MatchesHressOrgLegacyHash(string password, string salt)
    {
        Assert.Equal(LegacyHash(password, salt), PasswordHasher.Hash(password, salt));
    }

    [Fact]
    public void Matches_SameHash_True()
    {
        var hash = PasswordHasher.Hash("password", "salt");
        Assert.True(PasswordHasher.Matches(hash, hash));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-the-hash")]
    public void Matches_DifferentOrMissing_False(string? stored)
    {
        Assert.False(PasswordHasher.Matches(PasswordHasher.Hash("password", "salt"), stored));
    }
}
