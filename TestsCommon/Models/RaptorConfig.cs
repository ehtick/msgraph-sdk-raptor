using System.Security.Cryptography.X509Certificates;
using Azure;
using Azure.Security.KeyVault.Certificates;

namespace MsGraphSDKSnippetsCompiler.Models;

public sealed class RaptorConfig
{
    public static RaptorConfig Create(IConfigurationRoot config)
    {
        var raptorConfig = new RaptorConfig
        {
            Authority = config.GetNonEmptyValue(nameof(Authority)), //Delegated Permissions
            Username = config.GetNonEmptyValue(nameof(Username)),
            Password = config.GetNonEmptyValue(nameof(Password)), // Application permissions
            EducationUsername = config.GetNonEmptyValue(nameof(EducationUsername)),
            EducationPassword = config.GetNonEmptyValue(nameof(EducationPassword)), // Application permissions
            TenantID = config.GetNonEmptyValue(nameof(TenantID)),
            ClientID = config.GetNonEmptyValue(nameof(ClientID)),
            ClientSecret = config.GetNonEmptyValue(nameof(ClientSecret)),
            CertificateName = config.GetNonEmptyValue(nameof(CertificateName)),
            EducationTenantID = config.GetNonEmptyValue(nameof(EducationTenantID)),
            EducationClientID = config.GetNonEmptyValue(nameof(EducationClientID)),
            EducationClientSecret = config.GetNonEmptyValue(nameof(EducationClientSecret)),
            SourcesDirectory = config.GetNonEmptyValue("BUILD_SOURCESDIRECTORY"),
            RaptorStorageConnectionString = config.GetNonEmptyValue(nameof(RaptorStorageConnectionString)),
            IsLocalRun = bool.Parse(config.GetNonEmptyValue(nameof(IsLocalRun))),
            TypeScriptFolder = Path.Join(config.GetNonEmptyValue("BUILD_SOURCESDIRECTORY"), "typescript-tests"),
            AzureKeyVaultUri = new Uri(config.GetNonEmptyValue(nameof(AzureKeyVaultUri))),
            AzureApplicationID = config.GetNonEmptyValue(nameof(AzureApplicationID)),
            AzureClientSecret = config.GetNonEmptyValue(nameof(AzureClientSecret)),
            AzureKeyVaultName = config.GetNonEmptyValue(nameof(AzureKeyVaultName)),
            AzureTenantID = config.GetNonEmptyValue(nameof(AzureTenantID))
        };
        if (!Directory.Exists(Path.Join(raptorConfig.SourcesDirectory, "microsoft-graph-docs")))
        {
            throw new FileNotFoundException("If you are running this locally, please set environment" +
                " variable BUILD_SOURCESDIRECTORY to the docs repo checkout location.");
        }

        return raptorConfig;
    }

    private static X509Certificate2 GetCertificate(RaptorConfig raptorConfig)
    {
        try
        {
            var clientSecretCredential = new ClientSecretCredential(raptorConfig.AzureTenantID, raptorConfig.AzureApplicationID, raptorConfig.AzureClientSecret);
            var client = new CertificateClient(raptorConfig.AzureKeyVaultUri, clientSecretCredential);
            var certificate = client.DownloadCertificate(certificateName: raptorConfig.CertificateName);
            return certificate;
        }
        catch (Exception ex)
        {
            Assert.Fail("Could Not Get Auth Certificate:{0}", ex.Message);
            throw;
        }
    }

    public string AzureTenantID
    {
        get;
        set;
    }

    public string AzureApplicationID
    {
        get;
        set;
    }
    public string AzureKeyVaultName
    {
        get;
        set;
    }

    public string AzureClientSecret
    {
        get;
        set;
    }

    private X509Certificate2 _certificate;
    public Lazy<X509Certificate2> Certificate => new(() => _certificate ??= GetCertificate(this));

    public string CertificateName
    {
        get;
        set;
    }

    public Uri AzureKeyVaultUri
    {
        get;
        set;
    }

    public string ClientID
    {
        get;
        init;
    }

    public string EducationClientID
    {
        get;
        init;
    }

    public string Authority
    {
        get;
        init;
    }

    public string Username
    {
        get;
        init;
    }

    public string Password
    {
        get;
        init;
    }


    public string EducationUsername
    {
        get;
        init;
    }

    public string EducationPassword
    {
        get;
        init;
    }

    public string TenantID
    {
        get;
        init;
    }

    public string ClientSecret
    {
        get;
        init;
    }
    public string EducationTenantID
    {
        get;
        init;
    }

    public string EducationClientSecret
    {
        get;
        init;
    }

    public string CertificateThumbprint
    {
        get;
        init;
    }

    public string SourcesDirectory
    {
        get;
        init;
    }

    public string RaptorStorageConnectionString
    {
        get;
        init;
    }

    public bool IsLocalRun
    {
        get;
        init;
    }

    //TODO Move typescript code generation to use a temporary folder
    public string TypeScriptFolder
    {
        get;
        init;
    }
}
