// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
namespace TestsCommon;

/// <summary>
/// Init-once access to snippets directory
/// </summary>
public static class GraphDocsDirectory
{
    /// <summary>
    /// Represents where the snippets are stored. Expected to refer to a single directory for each assembly.
    /// </summary>
    private static string SnippetsDirectory;

    /// <summary>
    /// Sets snippets directory only once and refers to the string if it is already set
    /// Assumes that default "git clone <remote-reference>" command is used, in other words,
    /// the repo is always in microsoft-graph-docs folder under RootDirectory defined above
    /// </summary>
    /// <param name="version">Docs version (e.g. V1 or Beta)</param>
    /// <param name="language">Language being compiled</param>
    /// <returns>
    /// C# snippets directory
    /// </returns>
    public static string GetSnippetsDirectory(Versions version, Languages language)
    {
        if (SnippetsDirectory is object)
        {
            return SnippetsDirectory;
        }

        var msGraphDocsRepoLocation = TestsSetup.Config.Value.SourcesDirectory;

        SnippetsDirectory = Path.Join(msGraphDocsRepoLocation, $@"microsoft-graph-docs{Path.DirectorySeparatorChar}api-reference{Path.DirectorySeparatorChar}{new VersionString(version)}{Path.DirectorySeparatorChar}includes{Path.DirectorySeparatorChar}snippets{Path.DirectorySeparatorChar}{language.AsString()}");

        return SnippetsDirectory;
    }

    /// <summary>
    /// Gets directory holding Microsoft Graph documentation in markdown format
    /// </summary>
    /// <param name="version">Docs version (e.g. V1 or Beta)</param>
    /// <returns>
    /// Directory holding Microsoft Graph documentation in markdown format
    /// </returns>
    public static string GetDocumentationDirectory(Versions version)
    {
        var msGraphDocsRepoLocation = TestsSetup.Config.Value.SourcesDirectory;
        return Path.Join(msGraphDocsRepoLocation, $@"microsoft-graph-docs{Path.DirectorySeparatorChar}api-reference{Path.DirectorySeparatorChar}{new VersionString(version)}{Path.DirectorySeparatorChar}api");
    }
}
