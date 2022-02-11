namespace MsGraphSDKSnippetsCompiler.Models;

public static class TestsSetup
{
    public static readonly Lazy<RaptorConfig> Config = new(() => RaptorConfig.Create(AppSettings.Config()));
    public static readonly Lazy<Task<PermissionManager>> RegularTenantPermissionManager
        = new(GetPermissionManagerApplication());
    public static readonly Lazy<Task<PermissionManager>> EducationTenantPermissionManager
        = new(GetPermissionManagerApplication(true));

    /// <summary>
    /// Initializes permission manager application with the tenant and application specified in the config
    /// Fetches delegated tokens and caches them
    /// </summary>
    /// <returns>Permission manager application to access auth providers and tokens</returns>
    public static async Task<PermissionManager> GetPermissionManagerApplication(bool isEducation = false)
    {
        var permissionManagerApplication = new PermissionManager(isEducation);
        await permissionManagerApplication.CreateDelegatedAuthProviders().ConfigureAwait(false);
        return permissionManagerApplication;
    }
}
