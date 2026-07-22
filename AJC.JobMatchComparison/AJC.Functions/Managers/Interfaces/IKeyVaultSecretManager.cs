namespace AJC.Functions.Managers.Interfaces;

public interface IKeyVaultSecretManager
{
    Task<string> GetSecretValueAsync(
        string secretName,
        CancellationToken cancellationToken = default);
}
