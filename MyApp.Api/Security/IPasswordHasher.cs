namespace MyApp.Api.Repository.Interfaces;

public interface IPasswordHasher
{
    (byte[] Hash, byte[] Salt) Hash(string password);
    bool Verify(string password, byte[] hash, byte[] salt);
}
