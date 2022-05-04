// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.

namespace TestsCommon;

/// <summary>
/// TestRunner for Go compilation tests
/// </summary>
public static class GoTestRunner
{
    private readonly static string CompilationDirectory = InitializeCompilationDirectory();

    private const int TimeoutForGoInSeconds = 120;


    // private const int TimeoutForProcessInSeconds = 10;

    /// <summary>
    /// template to compile snippets in
    /// </summary>
    private const string SDKShellTemplate = @"package main

import (
    ""log""

	msgraphsdk ""github.com/microsoftgraph/msgraph-sdk-go""
)

func //insert-testNameAsFunctionName-here() {
    //insert-code-here

    if err != nil {
		log.Fatal(err)
	}
}

";

    private const string MainFileShellTemplate = @"package main
    func main() {

        // insert-current-testName-here()
    }";
    private static string MainFileContentFormatted = FormatCodeSnippetSpaces(MainFileShellTemplate);

    /// <summary>
    /// matches Go snippet from Go snippets markdown output
    /// </summary>
    private const string GoSnippetPattern = @"```go(.*)```";

    /// <summary>
    /// compiled version of the Go markdown regular expression
    /// uses Singleline so that (.*) matches new line characters as well
    /// </summary>
    private static readonly Regex GoSnippetRegex = new Regex(GoSnippetPattern, RegexOptions.Singleline | RegexOptions.Compiled);

    static string InitializeCompilationDirectory()
    {
        var goNewGuid = "go" + Guid.NewGuid();
        Console.WriteLine($"Generated guid is {goNewGuid}");
        var raptorGoPath = Path.Combine(
            Path.GetTempPath(),
            "raptor-go",
            goNewGuid);
        Directory.CreateDirectory(raptorGoPath);
        return raptorGoPath;
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
        await DumpGoFiles(languageTestData).ConfigureAwait(false);
        await DownloadRequiredDependencies().ConfigureAwait(false);
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

    public static async Task DumpGoFiles(IEnumerable<LanguageTestData> languageTestData)
    {
        _ = languageTestData ?? throw new ArgumentNullException(nameof(languageTestData));

        foreach(var testData in languageTestData)
        {
            var (codeToCompile, _) = GetCodeToCompile(testData.FileContent);
            var testNameAsFunctionName = ReplaceHyphensWithUnderscores(testData.TestName);
            codeToCompile = codeToCompile
                .Replace("msgraphsdk.NewGraphServiceClient(requestAdapter)", "msgraphsdk.NewGraphServiceClient(nil)")
                .Replace("//insert-testNameAsFunctionName-here", testNameAsFunctionName )
                .Replace("result, err := graphClient.", "_, err := graphClient.");
            var filePath = Path.Combine(CompilationDirectory, testData.TestName + ".go");
            await File.WriteAllTextAsync(filePath, codeToCompile).ConfigureAwait(false);
        }
    }

    public static async Task DownloadRequiredDependencies()
    {
        //Download required packages
        var (stdout, stderr) = await ProcessSpawner.SpawnProcess(
            "go",
            "get RaptorGoTests",
            CompilationDirectory,
            TimeoutForGoInSeconds * 1000
        ).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(stderr))
        {
            Assert.Fail($"Failed to download required dependencies: {stderr}");
        }
    }

    private static async Task CompileSnippet(LanguageTestData testData)
    {
        // Copy Main.go file to build directory
        var mainFileDestination = Path.Combine(CompilationDirectory, "main.go");
        var testNameAsFunctionName = ReplaceHyphensWithUnderscores(testData.TestName);
        var mainFileContent = MainFileContentFormatted;
        mainFileContent = mainFileContent.Replace("// insert-current-testName-here", testNameAsFunctionName);

        await TestContext.Out.WriteLineAsync("Writing main function which will be entry point for run").ConfigureAwait(false);
        await File.WriteAllTextAsync(mainFileDestination, mainFileContent).ConfigureAwait(false);

        //Run the executable
        var (stdout, stderr) = await ProcessSpawner.SpawnProcess(
                "go",
                $"build main.go {testData.TestName}.go",
                CompilationDirectory,
                TimeoutForGoInSeconds * 1000
            ).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(stderr))
        {
            var code = await File.ReadAllTextAsync(Path.Combine(CompilationDirectory, $"{testData.TestName}.go")).ConfigureAwait(false);
            Assert.Fail($"{new CompilationOutputMessage(stderr, code, testData.DocsLink, testData.KnownIssueMessage, testData.IsCompilationKnownIssue, Languages.Java)}");
        }
        else
        {
            Assert.Pass();
        }
    }
}
