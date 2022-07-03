using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Reflection;

namespace TestsCommon
{
    public class TypeScriptTestRunner
    {
        private readonly string CompilationDirectory;
        public TypeScriptTestRunner()
        {
            var NewGuid = "ts -" + Guid.NewGuid();
            CompilationDirectory = Path.Combine(
                Path.GetTempPath(),
                "raptor-typescript",
                NewGuid);
        }

        /// <summary>
        /// compiled version of the TypeScript markdown regular expression
        /// uses Singleline so that (.*) matches new line characters as well
        /// </summary>
        private static readonly Regex RegExp = new Regex(@"```typescript(.*)```", RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// compiled version of the Typescript Compilation Error
        /// </summary>
        private static readonly Regex TSErrorRegExp = new Regex(@"(error )(TS\d{4}):(.+)", RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// compiled version of the Typescript Compuilation Error
        /// </summary>
        private static readonly Regex TSErrorPositions = new Regex(@"(ts\()(\d+),(\d+)(\):)(.+)", RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// compiled typescript bulk error
        /// </summary>
        private static readonly Regex TSBatchErrorRegExp = new Regex(@"(.+\.ts)\((\d+),(\d+).+(TS\d+): (.+)", RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// Holds a reference of errors from test evaluation
        /// </summary>
        private Dictionary<string, List<Diagnostic>> NpmResults = new Dictionary<string, List<Diagnostic>>();


        /// <summary>
        /// template to compile snippets in
        /// </summary>
        private const string SDKShellTemplate = @"import { FetchRequestAdapter } from '@microsoft/kiota-http-fetchlibrary';
import { GraphServiceClient } from '@microsoft/msgraph-sdk-javascript';
import { ClientSecretCredential } from '@azure/identity';
import { AzureIdentityAuthenticationProvider } from '@microsoft/kiota-authentication-azure';
//insert-imports-here

const authProvider = new AzureIdentityAuthenticationProvider(new ClientSecretCredential(""tenantId"", ""clientId"", ""clientSecret""));
//insert-code-here
";
        private const string DeclarationPattern = @"new (.+?)\(";
        private static readonly Regex RegExpDeclaration = new Regex(DeclarationPattern, RegexOptions.Singleline | RegexOptions.Compiled);

        private const string BUILD_DIR = "test_build";
        private const string BACKUP_DIR = "failed_tests";

        public async Task PrepareEnvironment(IEnumerable<LanguageTestData> languageTestData)
        {
            ArgumentNullException.ThrowIfNull(languageTestData);

            var buildDirectory = Path.Combine(CompilationDirectory, BUILD_DIR);
            Directory.CreateDirectory(buildDirectory);
            Directory.CreateDirectory(Path.Combine(CompilationDirectory, BACKUP_DIR));

            await TestContext.Out.WriteLineAsync("Setting up node directory").ConfigureAwait(false);
            await dumpFiles(buildDirectory, languageTestData).ConfigureAwait(false);
            await prepareNPM(buildDirectory).ConfigureAwait(false);
            await buildProject().ConfigureAwait(false);
        }

        private static async Task prepareNPM(string compilationDirectory)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TsDependecies"));
            FileInfo[] dependecies = directoryInfo.GetFiles("*.*");

            foreach (var fileName in dependecies)
            {
                File.Copy(fileName.FullName, Path.Combine(compilationDirectory, fileName.Name));
            }
            await executeProcess(compilationDirectory, "npm", "i", 600).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes a tsc process and returns a Tuple of output and error string
        /// </summary>
        /// <returns></returns>
        public async static Task<(string, string)> executeProcess(string WorkingDir, string NpmProcessName, string NpmProcessArgs, int TimeOutInSeconds = 300, bool failOnErrorExit = false)
        {

            var isWindowsPlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var ProcessName = isWindowsPlatform ? "powershell" : NpmProcessName;
            var ProcessArgs = isWindowsPlatform ? $"{NpmProcessName} {NpmProcessArgs}" : NpmProcessArgs;

            return await ProcessSpawner.SpawnProcess(ProcessName, ProcessArgs, WorkingDir, TimeOutInSeconds * 1000).ConfigureAwait(false);
        }

        private async static Task dumpFiles(string compilationDirectory, IEnumerable<LanguageTestData> languageTestData)
        {
            ArgumentNullException.ThrowIfNull(compilationDirectory);
            ArgumentNullException.ThrowIfNull(languageTestData);

            foreach (var testData in languageTestData)
            {
                var fullPath = Path.Join(GraphDocsDirectory.GetSnippetsDirectory(testData.Version, Languages.TypeScript), testData.FileName);
                Assert.IsTrue(File.Exists(fullPath), "Snippet file referenced in documentation is not found!");

                var fileContent = await File.ReadAllTextAsync(fullPath).ConfigureAwait(false);
                var match = RegExp.Match(fileContent);
                Assert.IsTrue(match.Success, "TypeScript snippet file is not in expected format!");

                var codeSnippetFormatted = match.Groups[1].Value;
                string[] lines = codeSnippetFormatted.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                var imports = new HashSet<string>();
                var excludedTypes = new List<string>() { "DateTimeTimeZone", "Date", "GraphServiceClient" };

                var declarationList = new HashSet<string>();

                foreach (string line in lines)
                {
                    var hasDeclaration = RegExpDeclaration.Match(line);
                    if (hasDeclaration.Success)
                    {
                        var className = hasDeclaration.Groups[1].Value;
                        if (!excludedTypes.Contains(className))
                            declarationList.Add(className);
                    }
                }

                if (declarationList.Count > 0)
                {
                    var template = @"import { className } from '@microsoft/msgraph-sdk-javascript/lib/models/microsoft/graph';";
                    imports.Add(template.Replace("className", String.Join(" , ", declarationList)));
                }

                var generatedImports = string.Join(Environment.NewLine, imports);
                var codeToCompile = SDKShellTemplate
                                        .Replace("//insert-code-here", codeSnippetFormatted)
                                        .Replace("\r\n", "\n").Replace("\n", "\r\n")
                                        .Replace("--imports--", generatedImports);


#pragma warning disable CA1308 // Normalize strings to uppercase
                await File.WriteAllTextAsync(Path.Combine(compilationDirectory, $"{testData.FileName.ToLowerInvariant().Replace(" ", "-")}.ts"), codeToCompile).ConfigureAwait(false);
#pragma warning restore CA1308 // Normalize strings to uppercase
            }
        }

        /// <summary>
        /// Executes tsc compilation process and returns a dictionary of errors in the data structure  Dictionary<'fileName', Collection<Dictionary<'errorCode','errorMessage'>>>
        ///
        /// </summary>
        /// <param name="version"></param>
        /// <param name="rootPath"></param>
        /// <returns></returns>
        private async Task buildProject()
        {
            var buildDir = Path.Combine(CompilationDirectory, BUILD_DIR);
            var backUpDir = Path.Combine(CompilationDirectory, BACKUP_DIR);

            int maxRetryCounts = 10;
            int retryCount = 0;
            while (retryCount < maxRetryCounts)
            {
                var (stdOut, stdErr) = await executeProcess(buildDir, "tsc", "-p tsconfig.json --outDir ./build", 600, false).ConfigureAwait(false);
                var errors = parseNPMErrors($"{stdOut}{stdErr}");

                // add errroes to global error dictionary
                foreach (var item in errors)
                {
                    var payload = (this.NpmResults.ContainsKey(item.Key)) ? errors[item.Key] : new List<Diagnostic>();

                    foreach (var keyItem in item.Value)
                        payload.Add(keyItem);

                    this.NpmResults[item.Key] = payload;

                    File.Move(Path.Combine(buildDir, item.Key), Path.Combine(backUpDir, item.Key));
                }

                if (errors.Count == 0) break;

                retryCount++;
            }
        }

        /// <summary>
        /// Returns a dictionary contains filename and listof diagnostic
        ///
        /// i.e Dictionary<'fileName', List<Diagnostic>>
        /// </summary>
        /// <param name="errorStrings"></param>
        /// <returns></returns>
        private static Dictionary<string, List<Diagnostic>> parseNPMErrors(string errorStrings)
        {
            ArgumentNullException.ThrowIfNull(errorStrings);

            var errors = new Dictionary<string, List<Diagnostic>>();
            string[] errorLines = errorStrings.Split("\r\n".ToCharArray(), StringSplitOptions.None);
            foreach (var err in errorLines)
            {
                var tsMatches = TSBatchErrorRegExp.Match(err);
                if (tsMatches.Success)
                {
                    var fileName = tsMatches.Groups[1].Value;
                    var errorStartPosition = int.Parse(tsMatches.Groups[2].Value, CultureInfo.InvariantCulture);
                    var errorEndPosition = int.Parse(tsMatches.Groups[3].Value, CultureInfo.InvariantCulture);
                    var errorCode = tsMatches.Groups[4].Value;
                    var errorMessage = tsMatches.Groups[5].Value;

                    var diagnostics = (errors.ContainsKey(fileName)) ? errors[fileName] : new List<Diagnostic>();
                    diagnostics.Add(
                        Diagnostic.Create(
                            new DiagnosticDescriptor(errorCode,
                                                            "Error during TypeScript compilation",
                                                            errorMessage,
                                                            errorCode,
                                                            DiagnosticSeverity.Error,
                                                            true),
                                                Location.Create(fileName,
                                                                                new TextSpan(0, 5),
                                                                                new LinePositionSpan(
                                                                                    new LinePosition(errorStartPosition, 0),
                                                                                    new LinePosition(errorStartPosition, errorEndPosition))))
                        );
                    errors[fileName] = diagnostics;
                }
            }

            return errors;
        }


        /// <summary>
        /// 1. Fetches snippet from docs repo
        /// 2. Asserts that there is one and only one snippet in the file
        /// 3. Wraps snippet with compilable template
        /// 4. Attempts to compile and reports errors if there is any
        /// </summary>
        /// <param name="testData">Test data containing information such as snippet file name</param>
        public async Task RunCompilationTests(LanguageTestData testData)
        {
            var backupDirectory = Path.Combine(CompilationDirectory, BACKUP_DIR);
            ArgumentNullException.ThrowIfNull(testData);

#pragma warning disable CA1308 // Normalize strings to uppercase
            var fileName = $"{testData.FileName.ToLowerInvariant().Replace(" ", "-")}.ts";
#pragma warning restore CA1308 // Normalize strings to uppercase


            if (!NpmResults.ContainsKey(fileName))
            {
                Assert.Pass();
            }
            else
            {
                var diagnostic = this.NpmResults[fileName];

                var compilationResultsModel = new CompilationResultsModel(false, diagnostic, testData.FileName);
                var fileContent = await File.ReadAllTextAsync(Path.Combine(backupDirectory, fileName)).ConfigureAwait(false);
                var compilationOutputMessage = new CompilationOutputMessage(compilationResultsModel.ToString(), fileContent, testData.DocsLink, testData.KnownIssueMessage, testData.IsCompilationKnownIssue, Languages.TypeScript);
                Assert.Fail($"{compilationOutputMessage}");
            }
        }

        /// <summary>
        /// 1. Fetches snippet from docs repo
        /// 2. Asserts that there is one and only one snippet in the file
        /// 3. Wraps snippet with compilable template
        /// 4. Attempts to compile and reports errors if there is any
        /// </summary>
        /// <param name="testData">Test data containing information such as snippet file name</param>
        public async Task RunExecutionTests(LanguageTestData testData)
        {
            var buildDirectory = Path.Combine(CompilationDirectory, BUILD_DIR, "build");

            ArgumentNullException.ThrowIfNull(testData);

#pragma warning disable CA1308 // Normalize strings to uppercase
            var fileName = $"{testData.FileName.ToLowerInvariant().Replace(" ", "-")}.js";
#pragma warning restore CA1308 // Normalize strings to uppercase

            var fileLocation = Path.Combine(buildDirectory, fileName);
            var (stdOut, stdErr) = await executeProcess(buildDirectory, "node", $"{fileName}", 600).ConfigureAwait(false);

            var buildOutPut = stdOut + stdErr;
            var hasErrorLine = TSErrorRegExp.Match(buildOutPut);

            if (!hasErrorLine.Success)
            {
                Assert.Pass();
            }
            else
            {

                var errorCode = hasErrorLine.Groups[2].Value;
                var errorMessage = hasErrorLine.Groups[3].Value.Trim();

                string[] lines = buildOutPut.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                var diagnostic = new List<Diagnostic>();
                foreach (string line in lines)
                {

                    var errorPositionsMatch = TSErrorPositions.Match(line);
                    if (errorPositionsMatch.Success)
                    {
                        var errorPositionStart = int.Parse(errorPositionsMatch.Groups[2].Value, CultureInfo.InvariantCulture);
                        var errorPositionEnd = int.Parse(errorPositionsMatch.Groups[3].Value, CultureInfo.InvariantCulture);

                        diagnostic.Add(
                            Diagnostic.Create(
                                new DiagnosticDescriptor(errorCode,
                                                                "Error during TypeScript compilation",
                                                                errorMessage,
                                                                errorCode,
                                                                DiagnosticSeverity.Error,
                                                                true),
                                                    Location.Create(fileName,
                                                                                    new TextSpan(0, 5),
                                                                                    new LinePositionSpan(
                                                                                        new LinePosition(errorPositionStart, 0),
                                                                                        new LinePosition(errorPositionStart, errorPositionEnd))))
                            );
                    }
                }

                var compilationResultsModel = new CompilationResultsModel(false, diagnostic, testData.FileName);
                var fileContent = await File.ReadAllTextAsync(fileLocation).ConfigureAwait(false);
                var compilationOutputMessage = new CompilationOutputMessage(compilationResultsModel.ToString(), fileContent, testData.DocsLink, testData.KnownIssueMessage, testData.IsCompilationKnownIssue, Languages.TypeScript);
                Assert.Fail($"{compilationOutputMessage}");
            }
        }
    }
}

