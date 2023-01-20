using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

//Used to remove complexity for getting secrets from Azure Key Vaults
//Use Interactive for user browser login, use managed for serverless apps
namespace AASPWaynesLibrary
{
    public class VaultHandler
    {
        //just needs the keyvault name and current secret. all environments can use the same secret name but not the same keyvault name.
        public static async Task<string> GetSecretInteractive(string keyvaultname, string secret)
        {
            var kvUri = "https://" + keyvaultname + ".vault.azure.net";

            SecretClientOptions options = new();
            options.Retry.MaxRetries = 0;
            TokenHandler.tokenCredential ??= new InteractiveBrowserCredential();
            var client = new SecretClient(new Uri(kvUri), TokenHandler.tokenCredential, options);

            KeyVaultSecret Secret = await client.GetSecretAsync(secret);

            return Secret.Value;
        }
        //just needs the keyvault name and current secret. all environments can use the same secret name but not the same keyvault name.
        public static async Task<string> GetSecretManaged(string keyvaultname, string secret)
        {
            var kvUri = "https://" + keyvaultname + ".vault.azure.net";

            SecretClientOptions options = new();
            options.Retry.MaxRetries = 0;
            TokenHandler.managedTokenCredential ??= new ManagedIdentityCredential();
            var client = new SecretClient(new Uri(kvUri), TokenHandler.managedTokenCredential, options);

            KeyVaultSecret Secret = await client.GetSecretAsync(secret);

            return Secret.Value;
        }
    }
}
