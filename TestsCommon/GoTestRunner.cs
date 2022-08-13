// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.

namespace TestsCommon;

/// <summary>
/// TestRunner for Go compilation tests
/// </summary>
public static class GoTestRunner
{
    private readonly static string CompilationDirectory = InitializeCompilationDirectory();

    private const int TimeoutForGoInSeconds = 10 * 60;  // 10 min

    private const int TimeoutForCacheWarmupInSeconds = 12 * 60;

    /// <summary>
    /// template to compile snippets in
    /// </summary>
    private const string SDKShellTemplate = @"package snippets

import (
    //insert-optional-log-here
    //insert-optional-time-here

    msgraphsdk ""github.com/microsoftgraph/<msgraph-sdk-version>""
    //insert-optional-models-import
    //insert-optional-config-import
)

func //Insert-capitalized-testNameAsFunctionName-here() {
    //insert-code-here

    //insert-optional-error-here
}

";


    private const string ErrorLog = @"
        if err != nil {
            log.Fatal(err)
        }
    ";

    /// <summary>
    /// matches Go snippet from Go snippets markdown output
    /// </summary>
    private const string GoSnippetPattern = @"```go(.*)```";

    /// <summary>
    /// compiled version of the Go markdown regular expression
    /// uses Singleline so that (.*) matches new line characters as well
    /// </summary>
    private static readonly Regex GoSnippetRegex = new Regex(GoSnippetPattern, RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex ConfigParentRegex =  new Regex("graphClient\\.(.)*", RegexOptions.Compiled);  // Don't match newline

    private static readonly Dictionary<string, string> SourceRepository = new Dictionary<string, string>(){
        {"V1", "msgraph-sdk-go"},
        {"Beta", "msgraph-beta-sdk-go"}
    };

    static string InitializeCompilationDirectory()
    {
        var goNewGuid = "go" + Guid.NewGuid();
        Console.WriteLine($"Generated guid is {goNewGuid}");
        var raptorGoBasePath = Path.Combine(
            Path.GetTempPath(),
            "raptor-go",
            goNewGuid);
        Directory.CreateDirectory(raptorGoBasePath);
        var raptorSrcPath = Path.Combine(raptorGoBasePath, "src");
        Directory.CreateDirectory(raptorSrcPath);
        return raptorGoBasePath;
    }

    /// <summary>
    /// 1. Fetches snippet from docs repo
    /// 2. Asserts that there is one and only one snippet in the file
    /// 3. Wraps snippet with compilable template
    /// 4. Attempts to compile and reports errors if there is any
    /// </summary>
    /// <param name="testData">Test data containing information such as snippet file name</param>
    public static async Task Compile(LanguageTestData testData)
    {

        _ = testData ?? throw new ArgumentNullException(nameof(testData));
        // Compile Code
        await CompileSnippet(testData).ConfigureAwait(false);

    }
    public static async Task PrepareCompilationEnvironment(IEnumerable<LanguageTestData> languageTestData)
    {
        var version = languageTestData.First().Version;
        await CopyDependenciesFileIntoCompilationDirectory(version).ConfigureAwait(false);
        await DumpGoFiles(languageTestData, version).ConfigureAwait(false);
        await DownloadRequiredDependencies().ConfigureAwait(false);
        await WarmUpCache(languageTestData.First()).ConfigureAwait(false);
    }

    private static string FormatCodeSnippetSpaces(string codeSnippetString)
    {
        var codeSnippetFormatted = codeSnippetString
            .Replace("\r\n", "\r\n        ")            // add indentation to match with the template
            .Replace("\r\n        \r\n", "\r\n\r\n")    // remove indentation added to empty lines
            .Replace("\t", "    ");                     // do not use tabs

        while (codeSnippetFormatted.Contains("\r\n\r\n"))
        {
            codeSnippetFormatted = codeSnippetFormatted.Replace("\r\n\r\n", "\r\n"); // do not have empty lines for shorter error messages
        }

        return codeSnippetFormatted;
    }

    /// <summary>
    /// Gets code to be compiled
    /// </summary>
    /// <param name="fileContent">snippet file content</param>
    /// <returns>code to be compiled</returns>
    private static (string, string) GetCodeToCompile(string fileContent)
    {
        var match = GoSnippetRegex.Match(fileContent);
        Assert.IsTrue(match.Success, "Go snippet file is not in expected format!");

        var codeSnippetFormatted = FormatCodeSnippetSpaces(match.Groups[1].Value);

        var codeToCompile = BaseTestRunner.ConcatBaseTemplateWithSnippet(codeSnippetFormatted, SDKShellTemplate);

        return (codeToCompile, codeSnippetFormatted);
    }

    public static async Task CopyDependenciesFileIntoCompilationDirectory(Versions version)
    {
        //Copy go.mod file in place for reference in downloads
        var buildFileDestination = Path.Combine(CompilationDirectory, "go.mod");
        var goModuleSourceFile = Path.Combine(
            TestsSetup.Config.Value.SourcesDirectory,
            "msgraph-sdk-raptor",
            $"Go{version}Tests",
            "goDependencies",
            "go.mod");
        await TestContext.Out.WriteLineAsync("Writing go.mod for downloading dependencies...").ConfigureAwait(false);
        File.Copy(goModuleSourceFile, buildFileDestination, true);
    }



    private static string ReplaceHyphensWithUnderscores(string stringContent)
    {
        _ = stringContent?? throw new ArgumentNullException(nameof(stringContent));
        return stringContent.Replace("-", "_");
    }

    private static string ParseCodeStringForRequiredConfig(this string codeContentString){
        var configParentStringMatch = ConfigParentRegex.Match(codeContentString);
        var configParentString = "";
        if (configParentStringMatch.Success)
        {
            configParentString = configParentStringMatch.Groups[0].Value;
        }
        else
        {
            Assert.Fail("Regex {0}, against code {1} Failed", ConfigParentRegex, codeContentString);
        }
        var configSegments = configParentString.Split(".");
        var configSegmentsRequired = configSegments[1..^1];
        var requiredConfigPath = string.Join("/",configSegmentsRequired.Select(segment => segment.Split("(").First()))
            .Replace("ById", "/Item")
            .Replace("$", "")
            .ToLower(CultureInfo.CurrentCulture);
        var basepath = "github.com/microsoftgraph/<msgraph-sdk-version>/";
        var requiredConfig = basepath + requiredConfigPath;
        return requiredConfig;
    }

    public static async Task DumpGoFiles(IEnumerable<LanguageTestData> languageTestData, Versions version)
    {
        _ = languageTestData ?? throw new ArgumentNullException(nameof(languageTestData));

        foreach(var testData in languageTestData)
        {
            var (codeToCompile, _) = GetCodeToCompile(testData.FileContent);
            var testNameAsFunctionName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(
                ReplaceHyphensWithUnderscores(testData.TestName).ToLower(CultureInfo.CurrentCulture)
            );
            var sdkSource = SourceRepository[version.ToString()];
            if(codeToCompile.Contains("graphconfig"))
            {
                string requiredConfig = codeToCompile.ParseCodeStringForRequiredConfig();
                codeToCompile = codeToCompile.Replace("//insert-optional-config-import", $"graphconfig \"{requiredConfig}\"");
            }
            codeToCompile = codeToCompile
                .Replace("<msgraph-sdk-version>", sdkSource)
                .Replace("msgraphsdk.NewGraphServiceClient(requestAdapter)", "msgraphsdk.NewGraphServiceClient(nil)")
                .Replace("//Insert-capitalized-testNameAsFunctionName-here", testNameAsFunctionName )
                .Replace("result, err := graphClient.", "_, err := graphClient.")
                .ReplaceOrRemove(codeToCompile.Contains("time.Parse"), "//insert-optional-time-here", "\"time\"")
                .ReplaceOrRemove(codeToCompile.Contains("err := graphClient"), "//insert-optional-log-here", "\"log\"")
                .ReplaceOrRemove(codeToCompile.Contains("err := graphClient"), "//insert-optional-error-here", ErrorLog)
                .ReplaceOrRemove(codeToCompile.Contains("graphmodels"), "//insert-optional-models-import","graphmodels \"github.com/microsoftgraph/msgraph-sdk-go/models\"");
            var filePath = Path.Combine(CompilationDirectory, "src", testData.TestName + ".go");
            await File.WriteAllTextAsync(filePath, codeToCompile).ConfigureAwait(false);
        }
    }

    public static async Task DownloadRequiredDependencies()
    {
        //Download required packages
        var (stdout, stderr) = await ProcessSpawner.SpawnProcess(
            "go",
            "get RaptorGoTests/src",
            CompilationDirectory,
            TimeoutForGoInSeconds * 1000
        ).ConfigureAwait(false);

        if (string.IsNullOrEmpty(stderr))
        {
            return;
        }

        // special case empty lines or lines starting with "go: downloading" since they are not errors
        // Workaround to be removed once go sdk fixes this.
        if (stderr.Split(Environment.NewLine)
            .Any(line => !string.IsNullOrEmpty(line.Trim()) && !line.StartsWith("go: ", StringComparison.InvariantCulture)))
        {
            Assert.Fail($"Failed to download required dependencies: {stderr}");
        }
    }

    private static async Task WarmUpCache(LanguageTestData testData)
    {
        // Compile a single file to create a build cache
        var compilePath = Path.Combine(CompilationDirectory, "src", testData.TestName + ".go");
        _ = await ProcessSpawner.SpawnProcess(
                "go",
                $"build {compilePath}",
                CompilationDirectory,
                TimeoutForCacheWarmupInSeconds * 1000
            ).ConfigureAwait(false);
    }

    private static async Task CompileSnippet(LanguageTestData testData)
    {
        //Compile file
        var compilePath = Path.Combine(CompilationDirectory, "src", testData.TestName + ".go");
        var (stdout, stderr) = await ProcessSpawner.SpawnProcess(
                "go",
                $"build {compilePath}",
                CompilationDirectory,
                TimeoutForGoInSeconds * 1000
            ).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(stderr))
        {
            var code = await File.ReadAllTextAsync(compilePath).ConfigureAwait(false);

            Assert.Fail($"{new CompilationOutputMessage(stderr, code, testData.DocsLink, testData.KnownIssueMessage, testData.IsCompilationKnownIssue, Languages.Go)}");
        }
        else
        {
            Assert.Pass();
        }
    }
}
