﻿using System.Runtime.InteropServices;
using System.Diagnostics;
namespace TestsCommon;

public class JavaTestRunner
{
    private readonly string CompilationDirectory;
    public JavaTestRunner()
    {
        var javaNewGuid = "java" + Guid.NewGuid();
        CompilationDirectory = Path.Combine(
            Path.GetTempPath(),
            "raptor-java",
            javaNewGuid);
    }

    private const int TimeoutForJavacInSeconds = 10;
    private const string PreviewBuildFileContents = @"apply plugin: 'base'

repositories {
    mavenCentral()
}

configurations {
    toCopy
}

dependencies {
    --deps--
}

task download(type: Copy) {
    from configurations.toCopy
    into 'lib'
}
";

    private const string V1BuildFileContents = @"apply plugin: 'base'

repositories {
    mavenCentral()
}

configurations {
    toCopy
}

dependencies {
    --deps--
    toCopy 'com.microsoft.graph:microsoft-graph-core:--coreversion--'
    toCopy 'com.microsoft.graph:microsoft-graph:--libversion--'
}

task download(type: Copy) {
    from configurations.toCopy
    into 'lib'
}
";

    private const string BetaBuildFileContents = @"apply plugin: 'base'

repositories {
    mavenCentral()
    maven {
        url 'https://oss.sonatype.org/content/repositories/snapshots'
	}
}

configurations {
    toCopy
}

dependencies {
    --deps--
    toCopy 'com.microsoft.graph:microsoft-graph-core:--coreversion--'
    toCopy 'com.microsoft.graph:microsoft-graph-beta:--libversion--'
}

task download(type: Copy) {
    from configurations.toCopy
    into 'lib'
}
";

    private const string Deps = @"toCopy 'com.google.guava:guava:31.0.1-jre'
    toCopy 'com.google.code.gson:gson:2.8.9'
    toCopy 'com.squareup.okhttp3:okhttp:4.9.3'
    toCopy 'com.azure:azure-identity:1.4.3'";

    /// <summary>
    /// template to compile snippets in
    /// </summary>
    private const string SDKShellTemplate = @"import com.microsoft.graph.httpcore.*;
import com.microsoft.graph.requests.*;
import com.microsoft.graph.models.*;
import com.microsoft.graph.http.IHttpRequest;
import java.util.LinkedList;
import java.time.OffsetDateTime;
import java.io.InputStream;
import java.net.URL;
import java.util.UUID;
import java.util.Base64;
import java.util.EnumSet;
import javax.xml.datatype.DatatypeFactory;
import javax.xml.datatype.Duration;
import com.google.gson.JsonPrimitive;
import com.google.gson.JsonParser;
import com.google.gson.JsonElement;
import com.google.gson.JsonObject;
import java.util.concurrent.CompletableFuture;
import okhttp3.Request;
import com.microsoft.graph.core.*;
import com.microsoft.graph.options.*;
import com.microsoft.graph.serializer.*;
import com.microsoft.graph.authentication.*;
public class App
{
    public static void main(String[] args) throws Exception
    {
--auth--
        //insert-code-here
    }
}";
    private const string authProviderCurrent = @"        final IAuthenticationProvider authProvider = new IAuthenticationProvider() {
            @Override
            public CompletableFuture<String> getAuthorizationTokenAsync(final URL requestUrl) {
                return CompletableFuture.completedFuture("""");
            }
        };";
    /// <summary>
    /// matches csharp snippet from C# snippets markdown output
    /// </summary>
    private const string Pattern = @"```java(.*)```";

    /// <summary>
    /// compiled version of the C# markdown regular expression
    /// uses Singleline so that (.*) matches new line characters as well
    /// </summary>
    private static readonly Regex RegExp = new Regex(Pattern, RegexOptions.Singleline | RegexOptions.Compiled);


    /// <summary>
    /// 1. Fetches snippet from docs repo
    /// 2. Asserts that there is one and only one snippet in the file
    /// 3. Wraps snippet with compilable template
    /// 4. Attempts to compile and reports errors if there is any
    /// </summary>
    /// <param name="testData">Test data containing information such as snippet file name</param>
    public async Task Run(LanguageTestData testData)
    {
        if (testData == null)
        {
            throw new ArgumentNullException(nameof(testData));
        }

        var (stdout, stderr) = await ProcessSpawner.SpawnProcess
        (
            "javac",
            $"-cp lib/* -d bin {testData.JavaClassName}.java",
            CompilationDirectory,
            TimeoutForJavacInSeconds * 1000
        ).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(stderr))
        {
            var code = await File.ReadAllTextAsync(Path.Combine(CompilationDirectory, $"{testData.JavaClassName}.java")).ConfigureAwait(false);
            Assert.Fail($"{new CompilationOutputMessage(stderr, code, testData.DocsLink, testData.KnownIssueMessage, testData.IsCompilationKnownIssue, Languages.Java)}");
        }
        else
        {
            Assert.Pass();
        }
    }

    public async Task PrepareCompilationEnvironment(IEnumerable<LanguageTestData> languageTestData)
    {
        _ = languageTestData ?? throw new ArgumentNullException(nameof(languageTestData));
        
        var firstLanguageTestData = languageTestData.First();
        var isPreview = !string.IsNullOrEmpty(firstLanguageTestData.JavaPreviewLibPath);
        
        var libDirectory = Path.Combine(CompilationDirectory, "lib");
        Directory.CreateDirectory(libDirectory);

        var buildFile = Path.Combine(CompilationDirectory, "build.gradle");

        var buildFileContents = (isPreview, firstLanguageTestData.Version) switch
            {
                (false, Versions.V1) => V1BuildFileContents,
                (false, Versions.Beta) => BetaBuildFileContents,
                (true, Versions.V1) => PreviewBuildFileContents,
                _ => throw new ArgumentException("Unsupported version", nameof(languageTestData))
            };

        await TestContext.Out.WriteLineAsync("Writing build.gradle for downloading dependencies...").ConfigureAwait(false);
        await File.WriteAllTextAsync(buildFile, buildFileContents
            .Replace("--deps--", Deps)
            .Replace("--coreversion--", firstLanguageTestData.JavaCoreVersion)
            .Replace("--libversion--", firstLanguageTestData.JavaLibVersion)
            ).ConfigureAwait(false);

        await TestContext.Out.WriteLineAsync("Downloading dependencies dependencies...").ConfigureAwait(false);
        await DownloadDependencies(CompilationDirectory).ConfigureAwait(false);
        
        if (isPreview)
        {
            // copy preview files
            var previewLibFiles = new [] {
                Path.Combine(firstLanguageTestData.JavaPreviewLibPath, "msgraph-sdk-java/build/libs/msgraph-sdk-java.jar"),
                Path.Combine(firstLanguageTestData.JavaPreviewLibPath, "msgraph-sdk-java-core/build/libs/msgraph-sdk-java-core.jar")
            };

            foreach (var previewLibFile in previewLibFiles)
            {
                await TestContext.Out.WriteLineAsync($"Copying {previewLibFile} to {libDirectory}...").ConfigureAwait(false);
                File.Copy(previewLibFile, Path.Combine(libDirectory, Path.GetFileName(previewLibFile)));
            }
        }

        await TestContext.Out.WriteLineAsync("Creating all java files to be compiled...").ConfigureAwait(false);
        await DumpJavaFiles(CompilationDirectory, languageTestData).ConfigureAwait(false);
    }

    private static async Task DownloadDependencies(string compilationDirectory)
    {
        var gradleProcessName = "gradle";
        var gradleArguments = "download";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            gradleProcessName = "cmd";
            gradleArguments = $"/c \"gradle.bat {gradleArguments}\"";
        }

        using var gradleProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = gradleProcessName,
                Arguments = gradleArguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = compilationDirectory
            }
        };

        gradleProcess.Start();
        int dependencyDownloadTimeoutInSeconds = 120;
        var hasExited = gradleProcess.WaitForExit(dependencyDownloadTimeoutInSeconds * 1000);
        if (!hasExited)
        {
            gradleProcess.Kill(true);
            Assert.Fail($"Dependency download timed out after {dependencyDownloadTimeoutInSeconds} seconds");
        }

        var output = await gradleProcess.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        var error = await gradleProcess.StandardError.ReadToEndAsync().ConfigureAwait(false);

        if (!string.IsNullOrEmpty(error))
        {
            Assert.Fail($"Dependency download failed with error: {error}");
        }

        await TestContext.Out.WriteLineAsync("Dependency download output: " + output).ConfigureAwait(false);
    }

    private static async Task DumpJavaFiles(string compilationDirectory, IEnumerable<LanguageTestData> languageTestData)
    {
        foreach(var testData in languageTestData)
        {
            var codeToCompile = await GetCodeToCompile(testData).ConfigureAwait(false);
            codeToCompile = codeToCompile.Replace("public class App", "public class " + testData.JavaClassName);

            var filePath = Path.Combine(compilationDirectory, testData.JavaClassName + ".java");
            await File.WriteAllTextAsync(filePath, codeToCompile).ConfigureAwait(false);
        }
    }

    private async static Task<string> GetCodeToCompile(LanguageTestData testData)
    {
         var fullPath = Path.Join(GraphDocsDirectory.GetSnippetsDirectory(testData.Version, Languages.Java), testData.FileName);
        Assert.IsTrue(File.Exists(fullPath), "Snippet file referenced in documentation is not found!");

        var fileContent = await File.ReadAllTextAsync(fullPath).ConfigureAwait(false);
        var match = RegExp.Match(fileContent);
        Assert.IsTrue(match.Success, "Java snippet file is not in expected format!");

        var codeSnippetFormatted = match.Groups[1].Value
            .Replace("\r\n", "\r\n        ")            // add indentation to match with the template
            .Replace("\r\n        \r\n", "\r\n\r\n")    // remove indentation added to empty lines
            .Replace("\t", "    ")                      // do not use tabs
            .Replace("\r\n\r\n\r\n", "\r\n\r\n");       // do not have two consecutive empty lines
        var isCurrentSdk = string.IsNullOrEmpty(testData.JavaPreviewLibPath);
        var codeToCompile = BaseTestRunner.ConcatBaseTemplateWithSnippet(codeSnippetFormatted, SDKShellTemplate
                                                                        .Replace("--auth--", authProviderCurrent));

        return codeToCompile;
    }
}
