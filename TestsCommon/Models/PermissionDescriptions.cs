namespace MsGraphSDKSnippetsCompiler.Models;

// modeling JSON object here: https://github.com/microsoftgraph/microsoft-graph-devx-content/blob/dev/permissions/permissions-descriptions.json
#pragma warning disable CA1819 // Properties should not return arrays: used only to deserialized a JSON model
public record PermissionDescriptions(Scope[] delegatedScopesList, Scope[] applicationScopesList);
