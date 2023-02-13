using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using Step3_WebApi_Jwt_AzureKV.Models;
using Step3_WebApi_Jwt_AzureKV.JwtAuthorization;


namespace Step3_WebApi_Jwt_AzureKV
{
    public sealed class AppConfig
    {
        private static readonly object instanceLock = new();

        private static AppConfig _instance = null;
        private static IConfigurationRoot _configuration;

#if DEBUG
        private string _appsettingfile = "appsettings.Development.json";
#else
        private string _appsettingfile = "appsettings.json";
#endif
        private AppConfig()
        {
#if DEBUG
            //Lets get the credentials access Azure KV and set them as Environment variables
            //During Development this will come from User Secrets,
            //After Deployment it will come from appsettings.json
            var _azureConf = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile(_appsettingfile, optional: true, reloadOnChange: true)
                                .AddUserSecrets("3d2b8454-7957-4457-9167-d64aaaedb8d3")
                                .Build();

            // Access the Azure KeyVault
            Environment.SetEnvironmentVariable("AZURE_KeyVaultUri", _azureConf["AZURE_KeyVaultUri"]);
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", _azureConf["AZURE_CLIENT_SECRET"]);
            Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", _azureConf["AZURE_CLIENT_ID"]);
            Environment.SetEnvironmentVariable("AZURE_TENANT_ID", _azureConf["AZURE_TENANT_ID"]);
#endif
            var _kvuri = Environment.GetEnvironmentVariable("AZURE_KeyVaultUri");
            //Get the user-secrets from Azure Key Vault
            var client = new SecretClient(new Uri(_kvuri), new DefaultAzureCredential(
                new DefaultAzureCredentialOptions { AdditionallyAllowedTenants = { "*" } }));

            //Get user-secrets from AKV and flatten it into a Dictionary<string, string>
            var secret = client.GetSecret("user-secrets");
            var message = secret.Value.Value;
            var userSecretsAKV = JsonFlatToDictionary(message);

            //Create final ConfigurationRoot which includes also AzureKeyVault
            var builder = new ConfigurationBuilder()

                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile(_appsettingfile, optional: true, reloadOnChange: true)
#if DEBUG
                                //Shared on one developer machine
                                //.AddUserSecrets("3d2b8454-7957-4457-9167-d64aaaedb8d3")
#endif
                                //super secrets managed by Azure Key Vault
                                .AddInMemoryCollection(userSecretsAKV);
            
            _configuration = builder.Build();
        }

        private static IConfigurationRoot ConfigurationRoot
        {
            get
            {
                lock (instanceLock)
                {
                    if (_instance == null)
                    {
                        _instance = new AppConfig();
                    }
                    return _configuration;
                }
            }
        }

        private static Dictionary<string, string> JsonFlatToDictionary(string json)
        {
            IEnumerable<(string Path, JsonProperty P)> GetLeaves(string path, JsonProperty p)
                => p.Value.ValueKind != JsonValueKind.Object
                    ? new[] { (Path: path == null ? p.Name : path + ":" + p.Name, p) }
                    : p.Value.EnumerateObject().SelectMany(child => GetLeaves(path == null ? p.Name : path + ":" + p.Name, child));

            using (JsonDocument document = JsonDocument.Parse(json)) // Optional JsonDocumentOptions options
                return document.RootElement.EnumerateObject()
                    .SelectMany(p => GetLeaves(null, p))
                    .ToDictionary(k => k.Path, v => v.P.Value.Clone().ToString()); //Clone so that we can use the values outside of using
        }


        public static string CurrentDbType => ConfigurationRoot.GetValue<string>("CurrentDbType");
        public static string CurrentDbConnection => ConfigurationRoot.GetValue<string>("CurrentDbConnection");
        public static string CurrentDbConnectionString => ConfigurationRoot.GetConnectionString(CurrentDbConnection);

        public static string SecretMessage => ConfigurationRoot.GetValue<string>("SecretMessage");
        public static string SecretMessageAzureKV => ConfigurationRoot.GetValue<string>("SecretMessageAzureKeyVault");

        public static List<User> Users
        {
            get
            {
                var _users = new List<User>();
                ConfigurationRoot.Bind("Users", _users);
                return _users;
            }
        }
        public static JwtConfig JwtSetting
        {
            get
            {
                var _jwt_conf = new JwtConfig();
                ConfigurationRoot.Bind("JsonWebTokenKeys", _jwt_conf);
                return _jwt_conf;
            }
        }
    }
}
