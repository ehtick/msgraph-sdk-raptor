namespace MsGraphSDKSnippetsCompiler.Models;

/// <summary>
/// Model from https://github.com/microsoftgraph/microsoft-graph-devx-api/blob/dev/GraphExplorerPermissionsService/Models/ScopeInformation.cs
/// </summary>
public record Scope(string value, bool isAdmin, string id)
{
    /// <summary>
    /// Creates a delegated app name from scope
    /// </summary>
    /// <param name="prefix">app display name prefix</param>
    /// <returns>e.g. 'DelegatedApp User.Read' for prefix: 'DelegatedApp ' and Scope: 'User.Read'</returns>
    public string DelegatedAppName(string prefix)
    {
        return prefix + value;
    }
};
