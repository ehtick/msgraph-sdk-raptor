using static TestsCommon.JavaKnownIssues;
using static TestsCommon.CSharpKnownIssues;
using static TestsCommon.PowerShellKnownIssues;
using static TestsCommon.TypeScriptKnownIssues;

namespace TestsCommon;

public enum Category
{
    Documentation,
    MoreThanOnePermission,
    Raptor,
    SDK,
    HTTP,
    Metadata,
    MetadataPreprocessing,
    SnippetGeneration,
    TestGeneration,
    NeedsAnalysis,
    MissingData,
    MissingPermissionScope,
    MissingPermissionDescription,
    NoProgrammaticWay,
    ProtectedAPI,
    Service,
    EphemeralData
}

public static class KnownIssues
{
    #region SDK issues

    internal const string SearchHeaderIsNotSupported = "Search header is not supported by the SDK";
    internal const string SearchHeaderIsNotSupportedGithubIssue = "https://github.com/microsoftgraph/msgraph-sdk-dotnet/issues/178";
    internal const string CountIsNotSupported = "OData $count is not supported by the SDK at the moment.";
    internal const string CountIsNotSupportedGithubIssue = "https://github.com/microsoftgraph/MSGraph-SDK-Code-Generator/issues/402";
    internal const string MissingContentProperty = "IReportRootGetM365AppPlatformUserCountsRequestBuilder is missing Content property";
    internal const string MissingContentPropertyGithubIssue = "https://github.com/microsoftgraph/msgraph-beta-sdk-dotnet/pull/261";
    internal const string StreamRequestDoesNotSupportDelete = "Stream requests only support PUT and GET.";
    internal const string DeleteAsyncIsNotSupportedForReferences = "DeleteAsync is not supported for reference collections";
    internal const string DeleteAsyncIsNotSupportedForReferencesGithubIssue = "https://github.com/microsoftgraph/MSGraph-SDK-Code-Generator/issues/471";
    internal const string TypeCastIsNotSupported = "Type cast operation is not supported in SDK";
    internal const string TypeCastIsNotSupportedGithubIssue = "https://github.com/microsoftgraph/MSGraph-SDK-Code-Generator/issues/304";

    internal const string ComplexTypeNavigationProperties = "Complex Type navigation properties are not generated";
    internal const string ComplexTypeNavigationPropertiesGithubIssue = "https://github.com/microsoftgraph/msgraph-sdk-dotnet/issues/1003";
    #endregion

    #region HTTP Snippet Issues
    internal const string HttpSnippetWrong = "Http snippet should be fixed";
    internal const string RefNeeded = "URL needs to end with /$ref for reference types";
    internal const string RefShouldBeRemoved = "URL shouldn't end with /$ref";
    #endregion

    #region Metadata Issues
    internal const string MetadataWrong = "Metadata should be fixed";
    internal const string IdentityRiskEvents = "identityRiskEvents not defined in metadata.";
    internal const string IncorrectPowershellSnippetClassificationGithubIssue = "https://github.com/microsoftgraph/msgraph-metadata/issues/129";
    #endregion

    #region Metadata Preprocessing Issues
    internal const string EventActionsShouldNotBeReordered = "There is a reorder rule in XSLT. It should be removed";
    internal const string EventActionsShouldNotBeReorderedGithubIssue = "https://github.com/microsoftgraph/msgraph-metadata/pull/64";
    internal const string EducationAssignmentRubricContainsTargetPreprocessor = "EducationRubric containsTarget should be False to use $ref.";
    internal const string EducationAssignmentRubricContainsTargetPreprocessorGithubIssue = "https://github.com/microsoftgraph/msgraph-metadata/issues/81";
    #endregion

    #region Snipppet Generation Issues
    internal const string SnippetGenerationCreateAsyncSupport = "Snippet generation doesn't use CreateAsync";
    internal const string SnippetGenerationCreateAsyncSupportGithubIssue = "https://github.com/microsoftgraph/microsoft-graph-devx-api/issues/301";
    internal const string StructuralPropertiesAreNotHandled = "We don't generate request builders for URL navigation to structural properties." +
        " We should build a custom request with URL as this is not supported in SDK.";
    internal const string StructuralPropertiesAreNotHandledGithubIssue = "https://github.com/microsoftgraph/microsoft-graph-devx-api/issues/485";
    internal const string NamespaceOdataTypeAnnotationsWithoutHashSymbol = "We do not support namespacing when odata.type annotations are not prepended with hash symbol.";
    internal const string NamespaceOdataTypeAnnotationsWithoutHashSymbolGithubIssue = "https://github.com/microsoftgraph/microsoft-graph-devx-api/issues/580";
    internal const string DateTimeOffsetHandlingInUrls = "Dates supplied via GET request urls are not parsed to dates";
    internal const string DateTimeOffsetHandlingInUrlsGithubIssue = "https://github.com/microsoftgraph/microsoft-graph-devx-api/issues/612";
    internal const string IdentitySetAndIdentityShouldNestAdditionalDataGithubIssue = "https://github.com/microsoftgraph/microsoft-graph-devx-api/issues/613";
    #endregion

    internal const string NeedsAnalysisText = "This is a consistently failing test, the root cause is not yet identified";
    internal const string MissingDataText = "This test is missing data in identifiers.json file";
    internal const string MissingPermissionScopeText = "DevX API is not returning any delegated or application permissions for the URI.";
    internal const string MissingPermissionDescriptionText = "DevX Content repo doesn't have a description for the permission.";
    internal const string EphemeralDataInServiceText = "Ephemeral Service data that is deleted after an unknown period of time.";
    internal const string NoProgrammaticWayText = "There is no programmatic way to generate this data";

    #region HTTP methods
    internal const string DELETE = nameof(DELETE);
    internal const string PUT = nameof(PUT);
    internal const string POST = nameof(POST);
    internal const string GET = nameof(GET);
    internal const string PATCH = nameof(PATCH);
    #endregion

    internal static readonly KnownIssue ExcelItemAtDocumentationKnownIssue = new KnownIssue(Category.Documentation, "Excel documentation inconsistency between ID and itemAt. Could also be a service description issue.", "https://github.com/microsoftgraph/microsoft-graph-docs/issues/14853");
    internal static readonly KnownIssue TypeCastIsNotSupportedKnownIssue = new KnownIssue(Category.SDK, TypeCastIsNotSupported, TypeCastIsNotSupportedGithubIssue);
    internal static readonly KnownIssue SearchHeaderIsNotSupportedKnownIssue = new KnownIssue(Category.SDK, SearchHeaderIsNotSupported, SearchHeaderIsNotSupportedGithubIssue);
    internal static readonly KnownIssue MissingContentPropertyKnownIssue = new KnownIssue(Category.SDK, MissingContentProperty, MissingContentPropertyGithubIssue);
    internal static readonly KnownIssue DeleteAsyncIsNotSupportedForReferencesKnownIssue = new KnownIssue(Category.SDK, DeleteAsyncIsNotSupportedForReferences, DeleteAsyncIsNotSupportedForReferencesGithubIssue);
    internal static readonly KnownIssue EventActionsShouldNotBeReorderedKnownIssue = new KnownIssue(Category.MetadataPreprocessing, EventActionsShouldNotBeReordered, EventActionsShouldNotBeReorderedGithubIssue);
    internal static readonly KnownIssue StructuralPropertiesAreNotHandledKnownIssue = new KnownIssue(Category.SnippetGeneration, StructuralPropertiesAreNotHandled, StructuralPropertiesAreNotHandledGithubIssue);
    internal static readonly KnownIssue NamespaceOdataTypeAnnotationsWithoutHashSymbolKnownIssue = new KnownIssue(Category.SnippetGeneration, NamespaceOdataTypeAnnotationsWithoutHashSymbol, NamespaceOdataTypeAnnotationsWithoutHashSymbolGithubIssue);
    internal static readonly KnownIssue DateTimeOffsetHandlingInUrlsKnownIssue = new KnownIssue(Category.SnippetGeneration, DateTimeOffsetHandlingInUrls, DateTimeOffsetHandlingInUrlsGithubIssue);
    internal static readonly KnownIssue EducationAssignmentRubricContainsTargetPreprocessorKnownIssue = new KnownIssue(Category.Metadata, EducationAssignmentRubricContainsTargetPreprocessor, EducationAssignmentRubricContainsTargetPreprocessorGithubIssue);
    internal static readonly KnownIssue IdentitySetAndIdentityShouldNestAdditionalDataKnownIssue = new KnownIssue(Category.SnippetGeneration, GitHubIssue: IdentitySetAndIdentityShouldNestAdditionalDataGithubIssue);
    internal static readonly KnownIssue CountIsNotSupportedKnownIssue = new KnownIssue(Category.SDK, CountIsNotSupported, CountIsNotSupportedGithubIssue);
    internal static readonly KnownIssue SDKFunctionParameterKnownIssue = new KnownIssue(Category.SDK, GitHubIssue: "https://github.com/microsoftgraph/msgraph-sdk-dotnet/issues/1156");
    internal static readonly KnownIssue HTTPKnownIssue = new KnownIssue(Category.HTTP, CustomMessage: HttpSnippetWrong);
    internal static readonly KnownIssue NeedsAnalysisKnownIssue = new KnownIssue(Category.NeedsAnalysis, CustomMessage: NeedsAnalysisText);
    internal static readonly KnownIssue MissingDataKnownIssue = new KnownIssue(Category.MissingData, CustomMessage: MissingDataText);
    internal static readonly KnownIssue NoProgrammaticWayKnownIssue = new KnownIssue(Category.NoProgrammaticWay, CustomMessage: NoProgrammaticWayText);
    internal static readonly KnownIssue MissingPermissionScopeKnownIssue = new KnownIssue(Category.MissingPermissionScope, CustomMessage: MissingPermissionScopeText);
    internal static readonly KnownIssue MissingPermissionDescriptionKnownIssue = new KnownIssue(Category.MissingPermissionDescription, MissingPermissionDescriptionText, "https://github.com/microsoftgraph/microsoft-graph-devx-content/issues/178");

    internal static readonly KnownIssue EphemeralDataKnownIssue = new KnownIssue(Category.EphemeralData, CustomMessage: EphemeralDataInServiceText);
    internal static readonly KnownIssue EphemeralAlertDataKnownIssue = EphemeralDataKnownIssue with { GitHubIssue = "https://github.com/microsoftgraph/msgraph-sdk-raptor/issues/611" };
    internal static readonly KnownIssue EphemeralOperationDataKnownIssue = EphemeralAlertDataKnownIssue with  { GitHubIssue = "https://github.com/microsoftgraph/msgraph-sdk-raptor/issues/692" };
    internal static readonly KnownIssue EphemeralWorkbookOperationDataKnownIssue = EphemeralAlertDataKnownIssue with  { GitHubIssue = "https://github.com/microsoftgraph/msgraph-sdk-raptor/issues/524" };
    internal static readonly KnownIssue EphemeralOnenoteOperationDataKnownIssue = EphemeralAlertDataKnownIssue with  { GitHubIssue = "https://github.com/microsoftgraph/msgraph-sdk-raptor/issues/520" };
    internal static readonly KnownIssue EphemeralRiskDetectionKnownIssue = EphemeralDataKnownIssue with {GitHubIssue= "https://github.com/microsoftgraph/msgraph-sdk-raptor/issues/956"};

    internal static readonly KnownIssue ProtectedAPIKnownIssue = new KnownIssue(Category.ProtectedAPI, "Need resource specific consent", "https://github.com/microsoftgraph/msgraph-sdk-raptor/issues/506");

    internal static readonly KnownIssue ServiceTaskPrinterKnownIssue = new KnownIssue(Category.Service, "taskTrigger returns 404 even though it exists", "https://github.com/microsoftgraph/microsoft-graph-docs/issues/14774");
    internal static readonly KnownIssue PermissionsMoreThanOnePermissionKnownIssue = new KnownIssue(Category.MoreThanOnePermission, "More than one permission is required", "https://github.com/microsoftgraph/microsoft-graph-devx-api/issues/778");
    internal static readonly KnownIssue Add_MS_APP_ACTS_CustomheaderIssue = new KnownIssue(Category.Metadata, GitHubIssue: "https://github.com/microsoftgraph/msgraph-metadata/issues/109");
    internal static readonly KnownIssue IncorrectSnippetAPIClassificationKnownIssue = new KnownIssue(Category.Metadata, GitHubIssue: IncorrectPowershellSnippetClassificationGithubIssue);
    /// <summary>
    /// Constructs property not found message
    /// </summary>
    /// <param name="type">Type that need to define the property</param>
    /// <param name="property">Property that needs to be defined but missing in metadata</param>
    /// <returns>String representation of property not found message</returns>
    internal static string GetPropertyNotFoundMessage(string type, string property)
    {
        return HttpSnippetWrong + $": {type} does not contain definition of {property} in metadata";
    }

    /// <summary>
    /// Constructs metadata errors where a reference property has ContainsTarget=true
    /// </summary>
    /// <param name="type">Type in metadata</param>
    /// <param name="property">Property in metadata</param>
    /// <returns>String representation of metadata error</returns>
    internal static string GetContainsTargetRemoveMessage(string type, string property)
    {
        return MetadataWrong + $": {type}->{property} shouldn't have `ContainsTarget=true`";
    }

    /// <summary>
    /// Constructs metadata errors where a nav property should include ContainsTarget=true
    /// </summary>
    /// <param name="type">parent type in metadata</param>
    /// <param name="property">nav property which is missing ContainsTarget=true</param>
    /// <returns>String representation of metadata error</returns>
    internal static string MetadataAddContainsTargetMessage(string type, string property)
    {
        return MetadataWrong + $": {type}->{property} should have `ContainsTarget=true` for Nav property to expand into constituent object";
    }

    /// <summary>
    /// Returns a mapping of issues of which the source comes from service/documentation/metadata and are common accross langauges
    /// </summary>
    /// <param name="language">language to generate the exception from</param>
    /// <returns>mapping of issues of which the source comes from service/documentation/metadata and are common accross langauges</returns>
    public static Dictionary<string, KnownIssue> GetCompilationCommonIssues(Languages language, Versions versionEnum)
    {
        var version = versionEnum.ToString();
        var lng = language.AsString();
        return new Dictionary<string, KnownIssue>()
            {
                { $"call-updatemetadata-java-Beta-compiles", new KnownIssue(Category.Metadata, "updateMetadata doesn't exist in metadata") },
                { $"create-directoryobject-from-featurerolloutpolicy-java-{version}-compiles", new KnownIssue(Category.Metadata, GetContainsTargetRemoveMessage("featureRolloutPolicy", "appliesTo"))},
                { $"create-directoryobject-from-featurerolloutpolicy-csharp-V1-compiles", new KnownIssue(Category.Metadata, GetContainsTargetRemoveMessage("featureRolloutPolicy", "appliesTo"))},
                { $"create-directoryobject-from-featurerolloutpolicy-policies-{lng}-Beta-compiles", new KnownIssue(Category.Metadata, GetContainsTargetRemoveMessage("featureRolloutPolicy", "appliesTo"))},
                { $"create-educationrubric-from-educationassignment-{lng}-Beta-compiles", EducationAssignmentRubricContainsTargetPreprocessorKnownIssue},
                { $"create-externalsponsor-from-connectedorganization-{lng}-{version}-compiles", new KnownIssue(Category.Metadata, GetContainsTargetRemoveMessage("connectedOrganization", "externalSponsor")) },
                { $"create-internalsponsor-from-connectedorganization-{lng}-{version}-compiles", new KnownIssue(Category.Metadata, GetContainsTargetRemoveMessage("connectedOrganization", "internalSponsor")) },
                { $"delete-internalsponsor-from-connectedorganization-{lng}-V1-compiles", new KnownIssue(Category.Metadata, GetContainsTargetRemoveMessage("connectedOrganization", "internalSponsor")) },
                { $"delete-externalsponsor-from-connectedorganization-{lng}-V1-compiles", new KnownIssue(Category.Metadata, GetContainsTargetRemoveMessage("connectedOrganization", "externalSponsor")) },
                { $"list-directoryobject-{lng}-V1-compiles", new KnownIssue(Category.Metadata, MetadataAddContainsTargetMessage("accessPackageAssignment", "target"))},
                { $"get-analytics-{lng}-V1-compiles", new KnownIssue(Category.Metadata, MetadataAddContainsTargetMessage("driveItem", "analytics"))},
                { $"delete-directoryobject-from-featurerolloutpolicy-java-{version}-compiles", new KnownIssue(Category.Metadata, GetContainsTargetRemoveMessage("featureRolloutPolicy", "appliesTo")) },
                { $"delete-directoryobject-from-featurerolloutpolicy-csharp-V1-compiles", new KnownIssue(Category.Metadata, GetContainsTargetRemoveMessage("featureRolloutPolicy", "appliesTo")) },
                { $"delete-directoryobject-from-featurerolloutpolicy-policies-{lng}-Beta-compiles", new KnownIssue(Category.Metadata, GetContainsTargetRemoveMessage("featureRolloutPolicy", "appliesTo")) },
                { $"delete-educationrubric-from-educationassignment-{lng}-Beta-compiles", EducationAssignmentRubricContainsTargetPreprocessorKnownIssue},
                { $"delete-externalsponsor-from-connectedorganization-{lng}-Beta-compiles", new KnownIssue(Category.Metadata, GetContainsTargetRemoveMessage("connectedOrganization", "externalSponsor")) },
                { $"delete-internalsponsor-from-connectedorganization-{lng}-Beta-compiles", new KnownIssue(Category.Metadata, GetContainsTargetRemoveMessage("connectedOrganization", "internalSponsor")) },
                { $"directoryobject-delta-java-Beta-compiles", new KnownIssue(Category.Metadata, "Delta is not defined on directoryObject, but on user and group") },
                { $"remove-incompatiblegroup-from-accesspackage-{lng}-Beta-compiles", new KnownIssue(Category.Metadata, GetContainsTargetRemoveMessage("accessPackage", "incompatibleGroups"))},

                { $"create-onpremisesagentgroup-from-publishedresource-{lng}-Beta-compiles", new KnownIssue(Category.HTTP, RefShouldBeRemoved) },
                { $"create-reference-attachment-with-post-java-V1-compiles", new KnownIssue(Category.HTTP, GetPropertyNotFoundMessage("ReferenceAttachment", "SourceUrl, ProviderType, Permission and IsFolder")) },
                { $"create-directoryobject-from-orgcontact-java-Beta-compiles", new KnownIssue(Category.HTTP, RefNeeded) },
                { $"delete-publishedresource-{lng}-Beta-compiles", new KnownIssue(Category.HTTP, RefShouldBeRemoved) },
                { $"get-endpoint-java-V1-compiles", new KnownIssue(Category.HTTP, "This is only available in Beta") },
                { $"get-endpoints-java-V1-compiles", new KnownIssue(Category.HTTP, "This is only available in Beta") },
                { $"get-identityriskevent-java-Beta-compiles", new KnownIssue(Category.HTTP, IdentityRiskEvents) },
                { $"get-identityriskevents-java-Beta-compiles", new KnownIssue(Category.HTTP, IdentityRiskEvents) },

                { $"participant-configuremixer-{lng}-Beta-compiles", new KnownIssue(Category.Metadata, "ConfigureMixer doesn't exist in metadata") },
                { $"remove-group-from-rejectedsenderslist-of-group-{lng}-Beta-compiles", new KnownIssue(Category.Metadata, GetContainsTargetRemoveMessage("group", "rejectedSender")) },
                { $"remove-user-from-rejectedsenderslist-of-group-{lng}-Beta-compiles", new KnownIssue(Category.Metadata, GetContainsTargetRemoveMessage("group", "rejectedSender")) },
                { $"removeonpremisesagentfromanonpremisesagentgroup-{lng}-Beta-compiles", new KnownIssue(Category.HTTP, RefShouldBeRemoved) },
                { $"securescorecontrolprofiles-update-{lng}-Beta-compiles", new KnownIssue(Category.HTTP, HttpSnippetWrong + ": A list of SecureScoreControlStateUpdate objects should be provided instead of placeholder string.") },
                { $"shift-put-{lng}-{version}-compiles", IdentitySetAndIdentityShouldNestAdditionalDataKnownIssue },
                { $"update-openidconnectprovider-{lng}-Beta-compiles", new KnownIssue(Category.HTTP, "OpenIdConnectProvider should be specified") },
                { $"update-teamsapp-java-V1-compiles", new KnownIssue(Category.Metadata, $"teamsApp needs hasStream=true. In addition to that, we need these fixed: {Environment.NewLine}https://github.com/microsoftgraph/msgraph-sdk-dotnet-core/issues/160 {Environment.NewLine}https://github.com/microsoftgraph/microsoft-graph-devx-api/issues/336") },
                { $"create-connector-from-connectorgroup-{lng}-Beta-compiles", new KnownIssue(Category.SDK, "Missing method") },
            };
    }

    /// <summary>
    /// Gets known issues by language
    /// </summary>
    /// <param name="language">language to get the issues for</param>
    /// <param name="version">version to get the issues for</param>
    /// <returns>A mapping of test names into known issues</returns>
    public static Dictionary<string, KnownIssue> GetCompilationKnownIssues(Languages language, Versions version)
    {
        return (language switch
        {
            Languages.CSharp => GetCSharpCompilationKnownIssues(version),
            Languages.Java => GetJavaCompilationKnownIssues(version),
            Languages.TypeScript => GetTypescriptCompilationKnownIssues(version),
            _ => new Dictionary<string, KnownIssue>()
        }).Union(GetCompilationCommonIssues(language, version)).ToDictionary(x => x.Key, x => x.Value);
    }

    /// <summary>
    /// Gets known issues by language
    /// </summary>
    /// <param name="language">language to get the issues for</param>
    /// <param name="version">version to get the issues for</param>
    /// <returns>A mapping of test names into known issues</returns>
    public static Dictionary<string, KnownIssue> GetExecutionKnownIssues(Languages language, Versions version)
    {
        return language switch
        {
            Languages.CSharp => GetCSharpExecutionKnownIssues(version),
            Languages.PowerShell => GetPowerShellExecutionKnownIssues(version),
            _ => new Dictionary<string, KnownIssue>()
        };
    }
}
