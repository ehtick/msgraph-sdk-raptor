using System.Collections.ObjectModel;
using System.Diagnostics;

namespace TestsCommon
{
    public static class TypeScriptTestRunner
    {
        /// <summary>
        /// template to compile snippets in
        /// </summary>
        private const string SDKShellTemplate = @"import { FetchRequestAdapter } from '@microsoft/kiota-http-fetchlibrary';
import { GraphServiceClient } from '@microsoft/msgraph-sdk-typescript';
import { ClientSecretCredential } from '@azure/identity';
import { AzureIdentityAuthenticationProvider } from '@microsoft/kiota-authentication-azure';
--imports--

--auth--
//insert-code-here";

        private const string authProviderCurrent = @"const authProvider = new AzureIdentityAuthenticationProvider(new ClientSecretCredential(""tenantId"", ""clientId"", ""clientSecret""));
const requestAdapter = new FetchRequestAdapter(authProvider);  ";

        /// <summary>
        /// matches typescript snippet from TypeScript snippets markdown output
        /// </summary>
        private const string Pattern = @"```typescript(.*)```";

        /// <summary>
        /// compiled version of the C# markdown regular expression
        /// uses Singleline so that (.*) matches new line characters as well
        /// </summary>
        private static readonly Regex RegExp = new Regex(Pattern, RegexOptions.Singleline | RegexOptions.Compiled);


        private const string DeclarationPattern = @"new (.+?)\(";
        private static readonly Regex RegExpDeclaration = new Regex(DeclarationPattern, RegexOptions.Singleline | RegexOptions.Compiled);

        public static string ToLowerFirstChar(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return char.ToLower(input[0], CultureInfo.InvariantCulture) + input.Substring(1);
        }


        /// <summary>
        /// 1. Fetches snippet from docs repo
        /// 2. Asserts that there is one and only one snippet in the file
        /// 3. Wraps snippet with compilable template
        /// 4. Attempts to compile and reports errors if there is any
        /// </summary>
        /// <param name="testData">Test data containing information such as snippet file name</param>
        public static void RunTest(LanguageTestData testData, Dictionary<string, Collection<Dictionary<string, string>>> npmResults)
        {
            if (npmResults == null)
                throw new ArgumentNullException(nameof(npmResults));

            if (testData == null)
                throw new ArgumentNullException(nameof(testData));

#pragma warning disable CA1308 // Normalize strings to uppercase
            var fileName = $"{testData.Version.ToString().ToLowerInvariant()}-{testData.FileName.ToLowerInvariant().Replace(" ", "-")}.ts";
#pragma warning restore CA1308 // Normalize strings to uppercase

            if (!npmResults.ContainsKey(fileName))
            {
                Assert.Pass();
            }
            else
            {
                var result = MicrosoftGraphTypeScriptCompiler.GetDiagnostics(testData.FileName, npmResults[fileName]);

                var compilationResultsModel = new CompilationResultsModel(false, result, testData.FileName);
                var compilationOutputMessage = new CompilationOutputMessage(compilationResultsModel, File.ReadAllText(Path.Combine(TestsSetup.Config.Value.TypeScriptFolder, fileName)), testData.DocsLink, testData.KnownIssueMessage, testData.IsCompilationKnownIssue, Languages.TypeScript);
                Assert.Fail($"{compilationOutputMessage}");
            }
        }

        /// <summary>
        ///  Generates a file in the typescript test folder using the test language model
        /// </summary>
        /// <param name="testData"></param>
        public static void GenerateFiles(LanguageTestData testData)
        {
            if(testData == null)
                throw new ArgumentNullException(nameof(testData));

            var fullPath = Path.Join(GraphDocsDirectory.GetSnippetsDirectory(testData.Version, Languages.TypeScript), testData.FileName);
            Assert.IsTrue(File.Exists(fullPath), "Snippet file referenced in documentation is not found!");

            var fileContent = File.ReadAllText(fullPath);
            var match = RegExp.Match(fileContent);
            Assert.IsTrue(match.Success, "TypeScript snippet file is not in expected format!");


            var codeSnippetFormatted = match.Groups[1].Value;

            string[] lines = codeSnippetFormatted.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            var imports = new HashSet<string>();
            var excludedTypes = new List<string>() { "Map", "DateTimeTimeZone", "Date", "GraphServiceClient" };

            foreach (string line in lines)
            {
                var hasDeclaration = RegExpDeclaration.Match(line);
                if (hasDeclaration.Success)
                {
                    var className = hasDeclaration.Groups[1].Value;
                    if (!excludedTypes.Contains(className))
                    {
                        var template = @"import { className } from '@microsoft/msgraph-sdk-typescript/src/models/microsoft/graph/fileName';";
                        imports.Add(
                            template
                            .Replace("className", className)
                            .Replace("fileName", className.ToLowerFirstChar())
                            );
                    }
                }
            }

            var generatedImports = string.Join(Environment.NewLine, imports);
            var codeToCompile = SDKShellTemplate
                       .Replace("//insert-code-here", codeSnippetFormatted)
                       .Replace("\r\n", "\n").Replace("\n", "\r\n")
                    .Replace("--imports--", generatedImports)
                    .Replace("--auth--", authProviderCurrent);


#pragma warning disable CA1308 // Normalize strings to uppercase
            File.WriteAllText(Path.Combine(TestsSetup.Config.Value.TypeScriptFolder, $"{testData.Version.ToString().ToLowerInvariant()}-{testData.FileName.ToLowerInvariant().Replace(" ", "-")}.ts"), codeToCompile);
#pragma warning restore CA1308 // Normalize strings to uppercase
        }


        /// <summary>
        /// Executes tsc compile and return a string pair of the output and error message from the tsc statement
        /// </summary>
        /// <returns></returns>
        public static (string, string) CompileTypescriptFiles()
        {            // start info should change for windows vs linux
            using var tscProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "tsc",
                    Arguments = "-p tsconfig.json --outDir ./build",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = TestsSetup.Config.Value.TypeScriptFolder,
                },
            };

            var stdOuputSB = new StringBuilder();
            var stdErrSB = new StringBuilder();
            using var outputWaitHandle = new AutoResetEvent(false);
            using var errorWaitHandle = new AutoResetEvent(false);
            tscProcess.OutputDataReceived += (sender, e) => {
                if (string.IsNullOrEmpty(e.Data))
                {
                    outputWaitHandle.Set();
                }
                else
                {
                    stdOuputSB.Append(e.Data);
                }
            };
            tscProcess.ErrorDataReceived += (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.Data))
                {
                    errorWaitHandle.Set();
                }
                else
                {
                    stdErrSB.Append(e.Data);
                }
            };
            tscProcess.Start();
            tscProcess.BeginOutputReadLine();
            tscProcess.BeginErrorReadLine();
            var hasExited = tscProcess.WaitForExit(240000);
            if (!hasExited)
                tscProcess.Kill(true);
            var stdOutput = stdOuputSB.ToString();
            var stdErr = stdErrSB.ToString();

            return (stdOutput, stdErr);
        }

        /// <summary>
        /// Parses the results of the tsc process. A dictionary is returned containing the file name as the string and all the constinuent erros
        /// </summary>
        /// <param name="errors"></param>
        /// <returns></returns>
        public static Dictionary<string, Collection<Dictionary<string,string>>> ParseNPMErrors(Versions version, (string, string) errorStrings)
        {
            var errorMessage = String.IsNullOrEmpty(errorStrings.Item1) ? errorStrings.Item2 : errorStrings.Item1;

            // break the string
            string[] errors = errorMessage.Split(new[] { $"{version.ToString().ToLowerFirstChar()}-" }, StringSplitOptions.None);

            var result = new Dictionary<string, Collection<Dictionary<string, string>>>();

            foreach (var err in errors)
            {
                if (!String.IsNullOrEmpty(err) && err.Contains(".md.ts") && err.Contains('(') && err.Contains(')') && err.Contains(':'))
                {
                    var fileName = string.Concat($"{version.ToString().ToLowerFirstChar()}-", err.AsSpan(0, err.IndexOf(".md.ts", StringComparison.InvariantCulture)), ".md.ts");
                    var errorPosition = err.Substring(err.IndexOf("(", StringComparison.InvariantCulture) + 1, err.IndexOf(")", StringComparison.InvariantCulture) - err.IndexOf("(", StringComparison.InvariantCulture) - 1);

                    int startIndex = err.IndexOf(':');
                    int secondIndex = err.IndexOf(':', startIndex + 1);
                    var errorCode = err.Substring(startIndex + 1, (secondIndex - startIndex - 1)).Trim();


                    var message = err.Substring(secondIndex + 1, err.Length - secondIndex - 2).Trim();


                    var payload =  (result.ContainsKey(fileName)) ? result[fileName] :  new Collection<Dictionary<string, string>>();

                    payload.Add(
                        new Dictionary<string, string> {
                    {"errorPosition", errorPosition},
                    {"errorCode", errorCode},
                    {"errorMessage", message}
                });

                    result[fileName] = payload;
                }
            }

            return result;
        }

    }
}
