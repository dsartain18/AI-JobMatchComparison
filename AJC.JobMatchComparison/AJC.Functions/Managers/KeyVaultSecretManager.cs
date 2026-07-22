using AJC.Functions.Managers.Interfaces;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace AJC.Functions.Managers;

public sealed class KeyVaultSecretManager : IKeyVaultSecretManager
{
    private readonly Lazy<SecretClient> _secretClient;

    public KeyVaultSecretManager(
        IConfiguration configuration,
        TokenCredential credential)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(credential);

        _secretClient = new Lazy<SecretClient>(
            () => CreateSecretClient(configuration, credential),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public async Task<string> GetSecretValueAsync(
        string secretName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("A Key Vault secret name is required.", nameof(secretName));
        }

        var response = await _secretClient.Value.GetSecretAsync(
            secretName,
            cancellationToken: cancellationToken);
        var value = response.Value.Value;

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"The Key Vault secret '{secretName}' does not contain a value.");
        }

        return value;
    }

    private static SecretClient CreateSecretClient(
        IConfiguration configuration,
        TokenCredential credential)
    {
        var vaultUri = configuration["KeyVault:vaultUri"];

        if (string.IsNullOrWhiteSpace(vaultUri))
        {
            throw new InvalidOperationException(
                "KeyVault__vaultUri must be configured to retrieve provider credentials.");
        }

        return new SecretClient(new Uri(vaultUri), credential);
    }
}
