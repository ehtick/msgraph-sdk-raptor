// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.

namespace TestsCommon;

// Owner is used to categorize known test failures, so that we can redirect issues faster
public record KnownIssue(
    Category Category,
    string CustomMessage = null,
    string GitHubIssue = null)
{
    public string Message => string.Join(Environment.NewLine, new[] { CustomMessage, GitHubIssue }.Where(s => !string.IsNullOrEmpty(s)));
    public string Owner => Category.ToString();
    public string TestNamePrefix => "KnownIssue-" + Category.ToString() + "-";
};


/// <summary>
/// Generates TestCaseData for NUnit
/// </summary>
public static class TestDataGenerator
{
    /// <summary>
    /// Generates a dictionary mapping from snippet file name to documentation page listing the snippet.
    /// </summary>
    /// <param name="version">Docs version (e.g. V1, Beta)</param>
    /// <returns>Dictionary holding the mapping from snippet file name to documentation page listing the snippet.</returns>
    public static Dictionary<string, string> GetDocumentationLinks(Versions version, Languages language)
    {
        var documentationLinks = new Dictionary<string, string>();
        var documentationDirectory = GraphDocsDirectory.GetDocumentationDirectory(version);
        var files = Directory.GetFiles(documentationDirectory);
        var languageName = language.AsString();
        var SnippetLinkPattern = @$"includes\/snippets\/{languageName}\/(.*)\-{languageName}\-snippets\.md";
        var SnippetLinkRegex = new Regex(SnippetLinkPattern, RegexOptions.Compiled);
        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            var fileName = Path.GetFileNameWithoutExtension(file);
            var docsLink = $"https://docs.microsoft.com/en-us/graph/api/{fileName}?view=graph-rest-{new VersionString(version).DocsUrlSegment()}&tabs={languageName}";

            var match = SnippetLinkRegex.Match(content);
            while (match.Success)
            {
                documentationLinks[$"{match.Groups[1].Value}-{languageName}-snippets.md"] = docsLink;
                match = match.NextMatch();
            }
        }

        return documentationLinks;
    }

    /// <summary>
    /// For each snippet file creates a test case which takes the file name and version as reference
    /// Test case name is also set to to unique name based on file name
    /// </summary>
    /// <param name="runSettings">Test run settings</param>
    /// <returns>
    /// TestCaseData to be consumed by C# compilation tests
    /// </returns>
    public static IEnumerable<LanguageTestData> GetLanguageTestCaseData(RunSettings runSettings)
    {
        return from testData in GetLanguageTestData(runSettings)
               where !(testData.IsCompilationKnownIssue ^ runSettings.TestType == TestType.CompilationKnownIssues) // select known compilation issues iff requested
               select testData;
    }

    /// <summary>
    /// For each snippet file creates a test case which takes the file name and version as reference
    /// Test case name is also set to to unique name based on file name
    /// </summary>
    /// <param name="runSettings">Test run settings</param>
    /// <returns>
    /// TestCaseData to be consumed by C# compilation tests
    /// </returns>
    public static IEnumerable<TestCaseData> GetTestCaseData(RunSettings runSettings)
    {
        return from testData in GetLanguageTestData(runSettings)
               where !(testData.IsCompilationKnownIssue ^ runSettings.TestType == TestType.CompilationKnownIssues) // select known compilation issues iff requested
               select new TestCaseData(testData).SetName(testData.TestName).SetProperty("Owner", testData.Owner);
    }

    /// <summary>
    /// From a given list of testData it selects known issues if asked through run settings
    /// </summary>
    /// <param name="testData">List of test data</param>
    /// <param name="runSettings">Test run settings</param>
    /// <returns>
    /// TestCaseData to be consumed by compilation tests
    /// </returns>
    public static IEnumerable<TestCaseData> GetTestCaseData(IEnumerable<LanguageTestData> testData, RunSettings runSettings)
    {
        return testData
            .Where(td => !(td.IsCompilationKnownIssue ^ runSettings.TestType == TestType.CompilationKnownIssues))
            .Select(td => new TestCaseData(td).SetName(td.TestName).SetProperty("Owner", td.Owner));
    }

    /// <summary>
    /// For each snippet file creates a test case which takes the file name and version as reference
    /// Test case name is also set to to unique name based on file name
    /// </summary>
    /// <param name="runSettings">Test run settings</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <returns>
    /// TestCaseData to be consumed by C# execution tests
    /// </returns>
    public static IEnumerable<TestCaseData> GetExecutionTestData(RunSettings runSettings)
    {
        ArgumentNullException.ThrowIfNull(runSettings);

        bool CsharpFilter(LanguageTestData executionTestData) => executionTestData.FileContent.Contains("GetAsync()");
        bool PowerShellFilter(LanguageTestData executionTestData) => executionTestData.FileContent.Contains("Get-");
        Func<LanguageTestData, bool> languageFilter = runSettings.Language switch
        {
            Languages.CSharp => CsharpFilter,
            Languages.PowerShell => PowerShellFilter,
            _ => throw new ArgumentOutOfRangeException(nameof(runSettings))
        };
        return GetExecutionTestDataInternal(runSettings).Where(languageFilter)
            .Select(executionTestData => new TestCaseData(executionTestData).SetName(executionTestData.KnownIssueTestNamePrefix + executionTestData.TestName).SetProperty("Owner", executionTestData.Owner));
    }

    private static IEnumerable<LanguageTestData> GetExecutionTestDataInternal(RunSettings runSettings)
    {
        return from testData in GetLanguageTestData(runSettings)
               let executionTestData = testData with
               {
                   TestName = testData.TestName.Replace("-compiles", "-executes"),
               }
               where !executionTestData.IsCompilationKnownIssue // select compiling tests
                     && !(executionTestData.IsExecutionKnownIssue ^ runSettings.TestType == TestType.ExecutionKnownIssues) // select known execution issues iff requested
               select executionTestData;
    }

    private static IEnumerable<LanguageTestData> GetLanguageTestData(RunSettings runSettings)
    {
        ArgumentNullException.ThrowIfNull(runSettings);

        var language = runSettings.Language;
        var version = runSettings.Version;
        var documentationLinks = GetDocumentationLinks(version, language);
        var compilationKnownIssues = GetCompilationKnownIssues(language);
        var executionKnownIssues = GetExecutionKnownIssues(language);
        var snippetFileNames = documentationLinks.Keys.ToList();
        return from fileName in snippetFileNames                                            // e.g. application-addpassword-csharp-snippets.md
               let arbitraryDllPostfix = runSettings.DllPath == null || runSettings.Language != Languages.CSharp ? string.Empty : "arbitraryDll-"
               let testNamePostfix = arbitraryDllPostfix + version.ToString() + "-compiles" // e.g. Beta-compiles or arbitraryDll-Beta-compiles
               let testName = fileName.Replace("snippets.md", testNamePostfix)              // e.g. application-addpassword-csharp-Beta-compiles
               let docsLink = documentationLinks[fileName]
               let knownIssueLookupKey = testName.Replace("arbitraryDll-", string.Empty)
               let executionIssueLookupKey = testName.Replace("-compiles", "-executes")
               let isCompilationKnownIssue = compilationKnownIssues.ContainsKey(knownIssueLookupKey)
               let compilationKnownIssue = isCompilationKnownIssue ? compilationKnownIssues[knownIssueLookupKey] : null
               let isExecutionKnownIssue = executionKnownIssues.ContainsKey(executionIssueLookupKey)
               let executionKnownIssue = isExecutionKnownIssue ? executionKnownIssues[executionIssueLookupKey] : null
               let knownIssueMessage = compilationKnownIssue?.Message ?? executionKnownIssue?.Message ?? string.Empty
               let knownIssueTestNamePrefix = compilationKnownIssue?.TestNamePrefix ?? executionKnownIssue?.TestNamePrefix ?? string.Empty
               let owner = compilationKnownIssue?.Owner ?? executionKnownIssue?.Owner ?? string.Empty
               let fullPath = Path.Join(GraphDocsDirectory.GetSnippetsDirectory(version, runSettings.Language), fileName)
               let fileContent = File.ReadAllText(fullPath)
               select new LanguageTestData(
                       version,
                       isCompilationKnownIssue,
                       isExecutionKnownIssue,
                       knownIssueMessage,
                       knownIssueTestNamePrefix,
                       docsLink,
                       fileName,
                       runSettings.DllPath,
                       runSettings.JavaPreviewLibPath,
                       testName,
                       owner,
                       fileContent);
    }
}
