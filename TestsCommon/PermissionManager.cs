using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

using Azure.Core;

using static NUnit.Framework.TestContext;

namespace MsGraphSDKSnippetsCompiler;

/// <summary>
/// A Graph client to get information about all of the Azure Applications that have the permissions used in the test cases.
/// </summary>
public class PermissionManager
{
    /// <summary>
    /// The Graph resource ID. Constant across tenants.
    /// </summary>

    /// <summary>
    /// Graph service client with application permissions
    /// </summary>
    private readonly GraphServiceClient _client;

    private readonly RaptorConfig _config;

    private readonly bool _isEducation;

    /// <summary>
    /// Delegated auth providers
    /// key: scopeName
    /// value: token credential auth provider instance
    /// </summary>
    private readonly ConcurrentDictionary<string, TokenCredentialAuthProvider> _authProviders;
    private readonly ConcurrentDictionary<string, TokenCredential> _tokenCredentials;

    public ClientCertificateCredential ClientCertificateCredential
    {
        get;
    }

    /// <summary>
    /// Auth provider to initialize GraphServiceClients within the snippets
    /// when application permissions are needed
    /// </summary>
    public IAuthenticationProvider AuthProvider
    {
        get; init;
    }

    public PermissionManager(bool isEducation = false)
    {
        _isEducation = isEducation;
        _config = TestsSetup.Config.Value;
        var clientSecretCredential = _isEducation
            ? new ClientSecretCredential(_config.EducationTenantID, _config.EducationClientID, _config.EducationClientSecret)
            : new ClientSecretCredential(_config.TenantID, _config.ClientID, _config.ClientSecret);

        AuthProvider = new TokenCredentialAuthProvider(
            clientSecretCredential,
            new List<string> { ResourceConstants.DefaultAuthScope });
        _client = new GraphServiceClient(AuthProvider);

        _authProviders = new ConcurrentDictionary<string, TokenCredentialAuthProvider>();
        _tokenCredentials = new ConcurrentDictionary<string, TokenCredential>();
        ClientCertificateCredential = InitializeCerticateCredential();
    }

    private ClientCertificateCredential InitializeCerticateCredential()
    {
        (string tenantId, string clientId, Lazy<X509Certificate2> certificate) = _isEducation
            ? (_config.EducationTenantID, _config.EducationClientID, _config.Certificate)
            : (_config.TenantID, _config.ClientID, _config.Certificate);
        ClientCertificateCredential clientCertificateCredential = new ClientCertificateCredential(tenantId, clientId, certificate.Value);
        return clientCertificateCredential;
    }
    /// <summary>
    /// Gets service principal id for the permission manager
    /// </summary>
    public async Task<string> GetPermissionManagerServicePrincipalId()
    {
        var servicePrincipal = await _client.ServicePrincipals
            .Request()
            .Filter($"displayName eq '{ResourceConstants.PermissionManagerAppName}'")
            .GetAsync()
            .ConfigureAwait(false);

        return servicePrincipal.FirstOrDefault()?.Id;
    }

    /// <summary>
    /// Gets permission descriptions from DevX API
    /// </summary>
    /// <returns>permission descriptions</returns>
    public static async Task<PermissionDescriptions> GetPermissionDescriptions()
    {
        using var httpClient = new HttpClient();

        using var scopesRequest = new HttpRequestMessage(HttpMethod.Get, "https://raw.githubusercontent.com/microsoftgraph/microsoft-graph-devx-content/dev/permissions/permissions-descriptions.json");

        using var response = await httpClient.SendAsync(scopesRequest).ConfigureAwait(false);
        var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JsonSerializer.Deserialize<PermissionDescriptions>(responseString);
    }

    /// <summary>
    /// Gets existing applications from the tenant with the given prefix in app display name
    /// </summary>
    /// <param name="prefix">prefix for the application name. e.g. DelegatedApp for querying DelegatedApp User.Read, DelegatedApp Group.Read etc.</param>
    /// <returns>application names matching the query</returns>
    public async Task<Dictionary<string, string>> GetExistingApplicationsWithPrefix(string prefix = "DelegatedApp ")
    {
        var query = $"startswith(displayName, '{prefix}')";
        var result = new Dictionary<string, string>();
        var applications = await _client.Applications
            .Request()
            .Filter(query)
            .Select("appId,displayName")
            .GetAsync().ConfigureAwait(false);

        var pageIterator = PageIterator<Application>
            .CreatePageIterator(
                _client,
                applications,
                (application) =>
                {
                    result.Add(application.AppId, application.DisplayName);
                    return true;
                });
        await pageIterator.IterateAsync().ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Gets existing application permissions from the Permission Manager application
    /// </summary>
    /// <returns>application permissions</returns>
    public async Task<HashSet<string>> GetExistingApplicationPermissions()
    {
        var result = new HashSet<string>();

        var application = await GetPermissionManagerApplication().ConfigureAwait(false);

        var requiredResourceAccess = application.RequiredResourceAccess
            .SingleOrDefault(x => x.ResourceAppId == ResourceConstants.GraphResourceId);
        if (requiredResourceAccess == null)
        {
            throw new InvalidDataException("Application PermissionManager has no required resource access");
        }

        var resourceAccess = requiredResourceAccess.ResourceAccess;
        foreach (var resource in resourceAccess)
        {
            if (resource.Type == "Role") // application permission
            {
                result.Add(resource.Id.ToString());
            }
        }

        return result;
    }

    /// <summary>
    /// Gets id of the service principal representing Microsoft Graph Service in the tenant.
    /// </summary>
    /// <returns>Microsoft Graph Service Principal id</returns>
    public async Task<string> GetMicrosoftGraphServicePrincipalId()
    {
        var servicePrincipals = await _client.ServicePrincipals
            .Request()
            .Filter("servicePrincipalNames/any(n:n eq 'https://graph.microsoft.com')")
            .GetAsync().ConfigureAwait(false);

        return servicePrincipals?.FirstOrDefault()?.Id;
    }

    /// <summary>
    /// Creates an application with desired Microsoft Graph delegated scopes
    /// </summary>
    /// <param name="applicationName">application name</param>
    /// <param name="scopeGuid">Guid representing the delegated scope</param>
    /// <returns>created application</returns>
    public async Task<Application> CreateApplication(string applicationName, string scopeGuid)
    {
        ArgumentNullException.ThrowIfNull(applicationName);
        ArgumentNullException.ThrowIfNull(scopeGuid);

        var resourceAccess = new ResourceAccess
        {
            Type = "Scope",
            Id = new Guid(scopeGuid)
        };

        var requiredResourceAccess = new RequiredResourceAccess
        {
            ResourceAccess = new List<ResourceAccess> { resourceAccess },
            ResourceAppId = ResourceConstants.GraphResourceId
        };

        var application = new Application
        {
            DisplayName = applicationName,
            PublicClient = new PublicClientApplication
            {
                RedirectUris = new[] { "http://localhost" }
            },
            RequiredResourceAccess = new List<RequiredResourceAccess> { requiredResourceAccess },
            SignInAudience = "AzureADMyOrg",
            IsFallbackPublicClient = true // allowPublicClient: true in application manifest
        };

        var createdApplication = await _client.Applications
            .Request()
            .AddAsync(application).ConfigureAwait(false);

        return createdApplication;
    }

    /// <summary>
    /// Updates PermissionManager application with desired Microsoft Graph application scopes
    /// </summary>
    /// <param name="scopeGuids">guids of each application permission</param>
    /// <returns>updated application</returns>
    public async Task<Application> UpdateApplication(IEnumerable<string> scopeGuids)
    {
        ArgumentNullException.ThrowIfNull(scopeGuids);

        var listOfResourceAccesses = new List<ResourceAccess>();
        foreach (var scopeGuid in scopeGuids)
        {
            listOfResourceAccesses.Add(new ResourceAccess
            {
                Type = "Role", // application permission
                Id = new Guid(scopeGuid)
            });
        }

        var requiredResourceAccess = new RequiredResourceAccess()
        {
            ResourceAccess = listOfResourceAccesses,
            ResourceAppId = ResourceConstants.GraphResourceId
        };

        var application = new Application
        {
            RequiredResourceAccess = new List<RequiredResourceAccess> { requiredResourceAccess },
        };

        var permissionManagerApplication = await GetPermissionManagerApplication().ConfigureAwait(false);

        var updatedApplication = await _client.Applications[permissionManagerApplication.Id]
            .Request()
            .UpdateAsync(application).ConfigureAwait(false);

        return updatedApplication;
    }

    /// <summary>
    /// Assigns application permissions to the service principal of the application (equivalent of clicking "Grant admin consent" in Azure Portal)
    /// </summary>
    /// <param name="principalId">service principal id of the application</param>
    /// <param name="resourceId">Microsoft Graph Service service principal id</param>
    /// <param name="appRoleId">application permission guid</param>
    /// <returns></returns>
    public async Task AssignAppRole(string principalId, string resourceId, string appRoleId)
    {
        _ = await _client.ServicePrincipals[principalId].AppRoleAssignments
            .Request()
            .AddAsync(new AppRoleAssignment
            {
                PrincipalId = new Guid(principalId),
                ResourceId = new Guid(resourceId),
                AppRoleId = new Guid(appRoleId)
            })
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Creates an oauth2 permission grant
    /// </summary>
    /// <param name="clientId">object id of the service principal representing the application</param>
    /// <param name="resourceId">Microsoft Graph Resource id</param>
    /// <param name="scope">desired scope</param>
    /// <returns>created permission grant object</returns>
    public async Task<OAuth2PermissionGrant> CreateOAuthPermission(string clientId, string resourceId, string scope)
    {
        ArgumentNullException.ThrowIfNull(clientId);
        ArgumentNullException.ThrowIfNull(resourceId);
        ArgumentNullException.ThrowIfNull(scope);

        var oauthPermission = new OAuth2PermissionGrant
        {
            ClientId = clientId,
            ConsentType = "AllPrincipals",
            PrincipalId = null,
            ResourceId = resourceId,
            Scope = scope
        };

        var createdOauthPermission = await _client.Oauth2PermissionGrants
            .Request()
            .AddAsync(oauthPermission).ConfigureAwait(false);

        return createdOauthPermission;
    }

    /// <summary>
    /// Creates a service principal to represent an application
    /// </summary>
    /// <param name="appId">appId of the application</param>
    /// <returns>created service principal</returns>
    public async Task<ServicePrincipal> CreateServicePrincipal(string appId)
    {
        ArgumentNullException.ThrowIfNull(appId);

        var servicePrincipal = new ServicePrincipal
        {
            AppId = appId
        };

        return await _client.ServicePrincipals
            .Request()
            .AddAsync(servicePrincipal).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the preconfigured app corresponding to the scope using app's display name
    /// e.g. for User.Read -> DelegatedApp User.Read
    /// </summary>
    /// <param name="scope">delegated permission scope, e.g. User.Read</param>
    /// <returns>Application to acquire a token in given scope</returns>
    internal async Task<Application> GetApplication(Scope scope)
    {
        var AppDisplayNamePrefix = "DelegatedApp ";

        var appDisplayName = AppDisplayNamePrefix + scope.value;

        var collectionPage = await _client.Applications
                          .Request()
                          .Filter($"displayName eq '{ appDisplayName }'")
                          .GetAsync().ConfigureAwait(false);
        return collectionPage?.FirstOrDefault();
    }

    /// <summary>
    /// Gets delegated auth provider
    /// </summary>
    /// <param name="delegatedScope">delegated scope</param>
    /// <exception cref="KeyNotFoundException">throws key not found if there is no app in the tenant representing the given scope</exception>
    /// <returns>token credential provider for the delegated scope</returns>
    public TokenCredentialAuthProvider GetDelegatedAuthProvider(Scope delegatedScope)
    {
        return _authProviders[delegatedScope?.value];
    }

    internal async Task CreateDelegatedAuthProviders()
    {
        var permissionDescriptions = await GetPermissionDescriptions().ConfigureAwait(false);
        (string username, string password, string tenantId) = _isEducation
            ? (_config.EducationUsername, _config.EducationPassword, _config.EducationTenantID)
            : (_config.Username, _config.Password, _config.TenantID);
        var apps = await GetExistingApplicationsWithPrefix("DelegatedApp").ConfigureAwait(false);
        foreach (var delegatedPermissionScope in permissionDescriptions.delegatedScopesList)
        {
            var scopeName = delegatedPermissionScope.value;
            try
            {
                var (clientId, _) = apps.FirstOrDefault(c => c.Value == $"DelegatedApp {scopeName}");
                if (!string.IsNullOrWhiteSpace(clientId))
                {
                    var usernamePasswordCredential = new UsernamePasswordCredential(username, password,
                        tenantId, clientId, new UsernamePasswordCredentialOptions
                        {
                            TokenCachePersistenceOptions = new TokenCachePersistenceOptions
                            {
                                Name = username + delegatedPermissionScope.value,
                                // there is no default linux implementation for safe storage. This project is run in 2 environments:
                                // 1. local development
                                // 2. disposable Azure DevOps machines
                                // So we are OK to use unencrypted storage for Raptor tokens at the moment.
                                UnsafeAllowUnencryptedStorage = true
                            }
                        });
                    _authProviders[delegatedPermissionScope.value] =
                        new TokenCredentialAuthProvider(usernamePasswordCredential, new List<string> { scopeName });
                    _tokenCredentials[delegatedPermissionScope.value] = usernamePasswordCredential;
                }
            }
            catch
            {
                await Out.WriteLineAsync($"Couldn't create an auth provider for scope: {scopeName}").ConfigureAwait(false);
            }
        }
    }

    public TokenCredential GetTokenCredential(Scope delegatedScope)
    {
        return _tokenCredentials[delegatedScope?.value];
    }

    /// <summary>
    /// Gets PermissionManager Application
    /// </summary>
    /// <returns>PermissionManager Application</returns>
    private async Task<Application> GetPermissionManagerApplication()
    {
        var collectionPage = await _client.Applications
                          .Request()
                          .Filter($"displayName eq '{ ResourceConstants.PermissionManagerAppName }'")
                          .GetAsync().ConfigureAwait(false);
        return collectionPage?.FirstOrDefault();
    }
}
