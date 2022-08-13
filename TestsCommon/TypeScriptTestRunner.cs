using System.Runtime.InteropServices;
using System.Reflection;
using System.Collections.Concurrent;
using Azure.Core;

namespace TestsCommon
{
    public class TypeScriptTestRunner
    {
        private readonly string CompilationDirectory;
        private readonly string BuildDirectory;
        private readonly string BackUpDirectory;
        public TypeScriptTestRunner()
        {
            var NewGuid = "ts-" + Guid.NewGuid();
            CompilationDirectory = Path.Combine(Path.GetTempPath(), "raptor-typescript", NewGuid);

            BuildDirectory = Path.Combine(CompilationDirectory, "test_build");
            BackUpDirectory = Path.Combine(CompilationDirectory, "failed_tests");
            _config = TestsSetup.Config.Value;
        }

        /// <summary>
        /// Holds a reference of errors from test evaluation
        /// </summary>
        private Dictionary<string, List<Diagnostic>> NpmResults = new Dictionary<string, List<Diagnostic>>();

        /// <summary>
        /// compiled version of the TypeScript markdown regular expression
        /// uses Singleline so that (.*) matches new line characters as well
        /// </summary>
        private static readonly Regex RegExp = new Regex(@"```typescript(.*)```", RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// compiled typescript bulk error
        /// </summary>
        private static readonly Regex TSBatchErrorRegExp = new Regex(@"(.+\.ts)\((\d+),(\d+).+(TS\d+): (.+)", RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// compiled version of the Typescript Execution Error Message RegEx
        /// </summary>
        private static readonly Regex TSExecutionMessageRegExp = new Regex(@"_message.+'(.+)'", RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>
        /// compiled version of the Typescript Execution Error Code RegEx
        /// </summary>
        private static readonly Regex TSExecutionCodeRegExp = new Regex(@"_code.+'(.+)'", RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>
        /// compiled Regex to extract all declarations
        /// </summary>
        private static readonly Regex RegExpDeclaration = new Regex(@"new (.+?)\(", RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// compiled Regex to extract executed url from Typescript Script
        /// </summary>
        private static readonly Regex urlRegex = new Regex(@"url: (.+)", RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>
        /// compiled Regex to extract http Method from Typescript Script
        /// </summary>
        private static readonly Regex methodRegex = new Regex(@"httpMethod: (.+)", RegexOptions.Multiline | RegexOptions.Compiled);

        private readonly RaptorConfig _config;

        /// <summary>
        /// template to compile snippets in
        /// </summary>
        private const string SDKShellTemplate = @"import { GraphServiceClient } from '@microsoft/msgraph-sdk-javascript';
import { ClientSecretCredential, UsernamePasswordCredential } from '@azure/identity';
import { AzureIdentityAuthenticationProvider } from '@microsoft/kiota-authentication-azure';
//insert-imports-here

// set the boolean value in generation logic
const clientCredFlow : boolean =  true;

const defaultProvider = new AzureIdentityAuthenticationProvider(new ClientSecretCredential(""//insert-tenantid-here"", ""//insert-clientid-here"", ""//insert-clientsecret-here""));

const tokenCredentials = new UsernamePasswordCredential(""//insert-tenantid-here"", ""//insert-clientid-here"", ""//insert-username-here"", ""//insert-password-here"");
const scopes = [""//insert-scopes-here""];
const tokenProvider = new AzureIdentityAuthenticationProvider(tokenCredentials, scopes);

const authProvider = clientCredFlow ? defaultProvider : tokenProvider;
//insert-code-here
//insert-console-here
";

        private const string LogResults = @"result().then(resp => {
    console.log(resp);
});
";

        private const string BUILD_REQ_PREFIX = "build-req-";

        public async Task PrepareEnvironment(IEnumerable<LanguageTestData> languageTestData, bool executionTests = false)
        {
            ArgumentNullException.ThrowIfNull(languageTestData);

            Directory.CreateDirectory(BuildDirectory);
            Directory.CreateDirectory(BackUpDirectory);

            await TestContext.Out.WriteLineAsync("Generating TS Files").ConfigureAwait(false);
            await dumpFiles(BuildDirectory, languageTestData).ConfigureAwait(false);
            await TestContext.Out.WriteLineAsync("Setting up node directory").ConfigureAwait(false);
            await prepareNPM(BuildDirectory).ConfigureAwait(false);
            await TestContext.Out.WriteLineAsync("Compiling TS Files").ConfigureAwait(false);
            await buildProject().ConfigureAwait(false);
            if (executionTests)
            {
                await TestContext.Out.WriteLineAsync("Generating Scope Metadata").ConfigureAwait(false);
                await generateRequestInformationFiles(languageTestData).ConfigureAwait(false);
            }
        }

        private async Task generateRequestInformationFiles(IEnumerable<LanguageTestData> languageTestData)
        {
            Regex regexStatement = new Regex(@"await(.+)\.(\w+)\((.+)?\);", RegexOptions.Multiline | RegexOptions.Compiled);
            Regex regexTail = new Regex("(.+)(const.result.+=.+)", RegexOptions.Singleline | RegexOptions.Compiled);
            var RequestDetailsSnippetTemplate = @"const req = //request-statement
const url = req.URL
const method = req.httpMethod;
console.log(""url: "" + url);
console.log(""httpMethod: "" + method);";

            foreach (var testData in languageTestData)
            {
                var fileName = $"{testData.FormattedFileName}.ts";
                var jsFileName = $"{testData.FormattedFileName}.js";

                if (NpmResults.ContainsKey(fileName)) continue;
                var newFileName = $"{BUILD_REQ_PREFIX}{jsFileName}";

                var fileContent = await File.ReadAllTextAsync(Path.Combine(BuildDirectory, jsFileName)).ConfigureAwait(false);

                // generate
                var match = regexStatement.Match(fileContent);

                var requestPath = match.Groups[1].Value;
                var requestCommand = match.Groups[2].Value;
                var requestStatement = requestPath + ".create###RequestInformation()".Replace("###", requestCommand.ToFirstCharacterUpperCase());
                var result = RequestDetailsSnippetTemplate.Replace("//request-statement", requestStatement);

                // write new files
                var newSnippet = regexTail.Match(fileContent).Groups[1].Value + result;
                await File.WriteAllTextAsync(Path.Combine(BuildDirectory, newFileName), newSnippet).ConfigureAwait(false);
            }
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
                                        .Replace("//insert-imports-here", generatedImports)
                                        .ReplaceOrRemove(codeSnippetFormatted.Contains("const result = async () => {"), "//insert-console-here", LogResults);


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
            int maxRetryCounts = 10;
            int retryCount = 0;
            while (retryCount < maxRetryCounts)
            {
                var (stdOut, stdErr) = await executeProcess(BuildDirectory, "tsc", "-p tsconfig.json", 600, false).ConfigureAwait(false);
                var errors = parseNPMErrors($"{stdOut}{stdErr}");

                // add errroes to global error dictionary
                foreach (var item in errors)
                {
                    var payload = (this.NpmResults.ContainsKey(item.Key)) ? errors[item.Key] : new List<Diagnostic>();

                    foreach (var keyItem in item.Value)
                        payload.Add(keyItem);

                    this.NpmResults[item.Key] = payload;

                    File.Move(Path.Combine(BuildDirectory, item.Key), Path.Combine(BackUpDirectory, item.Key));
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
            ArgumentNullException.ThrowIfNull(testData);

            var fileName = $"{testData.FormattedFileName}.ts";

            if (!NpmResults.ContainsKey(fileName))
            {
                Assert.Pass();
            }
            else
            {
                var diagnostic = this.NpmResults[fileName];

                var compilationResultsModel = new CompilationResultsModel(false, diagnostic, testData.FileName);
                var fileContent = await File.ReadAllTextAsync(Path.Combine(BackUpDirectory, fileName)).ConfigureAwait(false);
                var compilationOutputMessage = new CompilationOutputMessage(compilationResultsModel.ToString(), fileContent, testData.DocsLink, testData.KnownIssueMessage, testData.IsCompilationKnownIssue, Languages.TypeScript);
                Assert.Fail($"{compilationOutputMessage}");
            }
        }

        /// <summary>
        /// Returns a pair holding (url , httpMethod) extracted from a typecsript call
        /// </summary>
        /// <returns></returns>
        private async Task<(string, string)> readSnippetMethodAndUrl(LanguageTestData testData)
        {
            var fileName = $"{BUILD_REQ_PREFIX}{testData.FormattedFileName}.js";
            var (stdOut, stdErr) = await executeProcess(BuildDirectory, "node", $"{fileName}", 600).ConfigureAwait(false);
            var npmResult = stdOut + stdErr;

            var method = methodRegex.Match(npmResult).Groups[1].Value;
            var url = urlRegex.Match(npmResult).Groups[1].Value;

            return (url, method);
        }

        private async Task<string> saveFileAndExecute(string genFileName, string jsFileName, Scope[] scopes = null)
        {
            var genFileLocation = Path.Combine(BuildDirectory, genFileName);
            var jsFileLocation = Path.Combine(BuildDirectory, jsFileName);

            var fileContent = await File.ReadAllTextAsync(jsFileLocation).ConfigureAwait(false);
            var isEducation = fileContent.Contains("education");

            var tenantId = isEducation ? _config.EducationTenantID : _config.TenantID;
            var clientId = isEducation ? _config.EducationClientID : _config.ClientID;
            var clientSecret = isEducation ? _config.EducationClientSecret : _config.ClientSecret;

            string scopVal = scopes == null ? "" : String.Join("\",\"", scopes.Select(x => x.value).ToArray());
            var formatedContent = fileContent
                .Replace("//insert-tenantid-here", tenantId)
                .Replace("//insert-clientid-here", clientId)
                .Replace("//insert-clientsecret-here", clientSecret)
                .Replace("//insert-username-here", _config.Username)
                .Replace("//insert-password-here", _config.Password)
                .Replace("//insert-scopes-here", scopVal);

            if (scopes != null)
            {
                // scopes are only provided in delegated authetication
                formatedContent = formatedContent.Replace("const clientCredFlow : boolean =  true;", "const clientCredFlow : boolean =  false;");
                await TestContext.Out.WriteLineAsync("Executing " + genFileName + " with tokens " + scopVal).ConfigureAwait(false);
            }

            var contentToExecute = TypeScriptIdentifiersReplacer.Instance.ReplaceIds(formatedContent);

            await File.WriteAllTextAsync(genFileLocation, contentToExecute).ConfigureAwait(false);
            var (stdOut, stdErr) = await executeProcess(BuildDirectory, "node", $"{genFileName}", 600).ConfigureAwait(false);
            return stdOut + stdErr;
        }

        /// <summary>
        /// Injects a token and replaces all ids with valid ids for execution
        /// </summary>
        private async Task<string> executeWithApplicationToken(LanguageTestData testData, string jsFileName)
        {
            var genFileName = $"app-token-{jsFileName}";
            return await saveFileAndExecute(genFileName, jsFileName).ConfigureAwait(false);
        }

        private async Task<string> executeWithDelegatedToken(LanguageTestData testData, string jsFileName)
        {
            var (graphUrl, graphMethod) = await readSnippetMethodAndUrl(testData).ConfigureAwait(false);
            var delegatedScopes = await PermissionScopes.GetScopes(testData, graphUrl, graphMethod).ConfigureAwait(false);
            var genFileName = $"scope-token-{jsFileName}";
            return await saveFileAndExecute(genFileName, jsFileName, delegatedScopes).ConfigureAwait(false);
        }

        private static bool isAuthError(string errorMsg) => errorMsg.ContainsAny("AuthenticationRequiredError", "403", "Forbidden", "ErrorAccessDenied", "authentication");

        /// <summary>
        /// Returns the (generated filename , build output)
        /// </summary>
        private async Task<string> executeScript(LanguageTestData testData, string jsFileName)
        {
            var result = await executeWithApplicationToken(testData, jsFileName).ConfigureAwait(false);
            if (isAuthError(result))
            {
                try
                {
                    result = await executeWithDelegatedToken(testData, jsFileName).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await TestContext.Out.WriteLineAsync(ex.Message).ConfigureAwait(false);
                }
            }
            return result;
        }

        /// <summary>
        /// Executes a precompiled TS file , using the js output, using Nods
        /// </summary>
        /// <param name="testData">Test data containing information such as snippet file name</param>
        public async Task RunExecutionTests(LanguageTestData testData)
        {
            ArgumentNullException.ThrowIfNull(testData);

            var jsFileName = $"{testData.FormattedFileName}.js";
            var fileName = $"{testData.FormattedFileName}.ts";

            // file failed compilation, dont try execution

            if (NpmResults.ContainsKey(fileName))
            {

                var diagnostic = this.NpmResults[fileName];

                var compilationResultsModel = new CompilationResultsModel(false, diagnostic, testData.FileName);
                var fileContent = await File.ReadAllTextAsync(Path.Combine(BackUpDirectory, fileName)).ConfigureAwait(false);
                var compilationOutputMessage = new CompilationOutputMessage(compilationResultsModel.ToString(), fileContent, testData.DocsLink, testData.KnownIssueMessage, testData.IsCompilationKnownIssue, Languages.TypeScript);
                Assert.Fail($"{compilationOutputMessage}");
            }

            // update file with token
            var buildOutPut = await executeScript(testData, jsFileName).ConfigureAwait(false);

            if (!buildOutPut.Contains("Error"))
            {
                Assert.Pass();
            }
            else
            {
                // file execution failed
                var hasErrorCode = TSExecutionCodeRegExp.Match(buildOutPut);
                var hasErrorMessage = TSExecutionMessageRegExp.Match(buildOutPut);

                var errorCode = hasErrorCode.Success ? hasErrorCode.Groups[1].Value.Trim() : "Unknown Error";
                var errorMessage = hasErrorMessage.Success ? hasErrorMessage.Groups[1].Value.Trim() : buildOutPut;


                var diagnostic = new List<Diagnostic>{
                    Diagnostic.Create(
                        new DiagnosticDescriptor(errorCode,"Error during TypeScript execution Test",errorMessage,errorCode,DiagnosticSeverity.Error,true),
                        Location.Create(fileName,new TextSpan(0, 5),new LinePositionSpan( new LinePosition(11, 0), new LinePosition(18, 20)))
                    )
                };

                var compilationResultsModel = new CompilationResultsModel(false, diagnostic, testData.FileName);
                var fileContent = await File.ReadAllTextAsync(Path.Combine(BuildDirectory, fileName)).ConfigureAwait(false);
                var compilationOutputMessage = new CompilationOutputMessage(compilationResultsModel.ToString(), fileContent, testData.DocsLink, testData.KnownIssueMessage, testData.IsCompilationKnownIssue, Languages.TypeScript);
                Assert.Fail($"{compilationOutputMessage}");
            }
        }
    }
}

