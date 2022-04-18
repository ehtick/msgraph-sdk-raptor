using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace TestsCommon
{
    public static class TypeScriptTestRunner
    {
        /// <summary>
        /// template to compile snippets in
        /// </summary>
        private const string SDKShellTemplate = @"import { FetchRequestAdapter } from '@microsoft/kiota-http-fetchlibrary';
import { GraphServiceClient } from '@microsoft/msgraph-sdk-javascript';
import { ClientSecretCredential } from '@azure/identity';
import { AzureIdentityAuthenticationProvider } from '@microsoft/kiota-authentication-azure';
--imports--

--auth--
//insert-code-here";

        private const string authProviderCurrent = @"const authProvider = new AzureIdentityAuthenticationProvider(new ClientSecretCredential(""tenantId"", ""clientId"", ""clientSecret"")); ";

        private const string TsConfigContent = @"{
	""compilerOptions"": {
		""module"": ""CommonJS"",
		""esModuleInterop"": true,
		""target"": ""ES2018"",
        ""moduleResolution"": ""node"",
        ""downlevelIteration"": true
	},
	""include"": ["".""],
	""exclude"": [""node_modules""],
	""lib"": [""es2015"", ""es2016"", ""es2017"", ""es2018"", ""es2019"", ""es2020""]
}
";

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

        private const string BUILD_DIR = "build";
        private const string BACKUP_DIR = "backup";
        private const string FILE_PREFIX = "generated-snippet";

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
        public static void RunTest(string path, LanguageTestData testData, Dictionary<string, Collection<Dictionary<string, string>>> npmResults)
        {
            ArgumentNullException.ThrowIfNull(npmResults);
            ArgumentNullException.ThrowIfNull(testData);

#pragma warning disable CA1308 // Normalize strings to uppercase
            var fileName = $"{FILE_PREFIX}-{testData.FileName.ToLowerInvariant().Replace(" ", "-")}.ts";
#pragma warning restore CA1308 // Normalize strings to uppercase

            if (!npmResults.ContainsKey(fileName))
            {
                Assert.Pass();
            }
            else
            {
                var result = MicrosoftGraphTypeScriptCompiler.GetDiagnostics(testData.FileName, npmResults[fileName]);

                var compilationResultsModel = new CompilationResultsModel(false, result, testData.FileName);
                var compilationOutputMessage = new CompilationOutputMessage(compilationResultsModel.ToString(), File.ReadAllText(Path.Combine(path, BACKUP_DIR, fileName)), testData.DocsLink, testData.KnownIssueMessage, testData.IsCompilationKnownIssue, Languages.TypeScript);
                Assert.Fail($"{compilationOutputMessage}");
            }
        }


        public static void GenerateFiles(string rootPath, IEnumerable<LanguageTestData> data)
        {
            ArgumentNullException.ThrowIfNull(rootPath);
            ArgumentNullException.ThrowIfNull(data);

            foreach (var testData in data) GenerateFiles(rootPath, testData);
        }

        /// <summary>
        ///  Generates a file in the typescript test folder using the test language model
        /// </summary>
        /// <param name="testData"></param>
        private static void GenerateFiles(string rootPath, LanguageTestData testData)
        {
            ArgumentNullException.ThrowIfNull(rootPath);
            ArgumentNullException.ThrowIfNull(testData);

            var buildPath = Path.Combine(rootPath, BUILD_DIR);

            var fullPath = Path.Join(GraphDocsDirectory.GetSnippetsDirectory(testData.Version, Languages.TypeScript), testData.FileName);
            Assert.IsTrue(File.Exists(fullPath), "Snippet file referenced in documentation is not found!");

            var fileContent = File.ReadAllText(fullPath);
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
                                    .Replace("--imports--", generatedImports)
                                    .Replace("--auth--", authProviderCurrent);


#pragma warning disable CA1308 // Normalize strings to uppercase
            File.WriteAllText(Path.Combine(buildPath, $"{FILE_PREFIX}-{testData.FileName.ToLowerInvariant().Replace(" ", "-")}.ts"), codeToCompile);
#pragma warning restore CA1308 // Normalize strings to uppercase
        }


        public static string SetUpFolder()
        {
            var folderPath = Path.Combine(Path.GetTempPath(), "typescript", "ts-" + Guid.NewGuid());

            Directory.CreateDirectory(folderPath);
            Directory.CreateDirectory(Path.Combine(folderPath, BUILD_DIR));
            Directory.CreateDirectory(Path.Combine(folderPath, BACKUP_DIR));

            return folderPath;
        }

        public async static Task RunNPMSetUp(string RootPath)
        {

            var BuildPath = Path.Combine(RootPath, BUILD_DIR);

            // create tsconfig file
            File.WriteAllText(Path.Combine(BuildPath, "tsconfig.json"), TsConfigContent);

            // install npm dependecies
#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
            string[,] NpmCommands = {
                {"npm" ,"i -D typescript ts-node" },
                {"npx" ,"tsc --init" },
                {"npm" ,"i -D @types/node" },
                {"npm" ,"i @microsoft/kiota-abstractions" },
                {"npm" ,"i @microsoft/kiota-authentication-azure" },
                {"npm" ,"i @microsoft/kiota-http-fetchlibrary" },
                {"npm" ,"i @microsoft/kiota-serialization-json" },
                {"npm" ,"i @microsoft/kiota-serialization-text" },
                {"npm" ,"i @azure/identity" },
                {"npm" ,"i node-fetch" },
                {"npm" ,"i @microsoft/msgraph-sdk-javascript" }
            };
#pragma warning restore CA1814 // Prefer jagged arrays over multidimensional

            for (int x = 0; x < NpmCommands.GetLength(0); x++)
                await ExecuteNPMProcess(BuildPath, NpmCommands[x, 0], NpmCommands[x, 1], 600).ConfigureAwait(false);
        }

        /// Creates a temporary folder and initializes the typescript folder
        public static async Task<string> PrepareFolder()
        {
            var RootFolder = SetUpFolder();
            await RunNPMSetUp(RootFolder).ConfigureAwait(false);

            return RootFolder;
        }


        /// <summary>
        /// Executes a tsc process and returns a Tuple of output and error string
        /// </summary>
        /// <returns></returns>
        public async static Task<(string, string)> ExecuteNPMProcess(string WorkingDir, string NpmProcessName, string NpmProcessArgs, int TimeOutInSeconds = 300, bool failOnErrorExit= false)
        {

            var isWindowsPlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var ProcessName = isWindowsPlatform ? "powershell" : NpmProcessName;
            var ProcessArgs = isWindowsPlatform ? $"{NpmProcessName} {NpmProcessArgs}" : NpmProcessArgs;

            return await ProcessSpawner.SpawnProcess(ProcessName, ProcessArgs, WorkingDir, TimeOutInSeconds * 1000).ConfigureAwait(false);
        }


        /// <summary>
        /// Executes tsc compile and return a string pair of the output and error message from the tsc statement
        /// </summary>
        /// <returns></returns>
        private async static Task<(string, string)> CompileTypescriptFiles(string RootPath)
        {
            var BuildPath = Path.Combine(RootPath, "build");
            return await ExecuteNPMProcess(BuildPath, "tsc", "-p tsconfig.json --outDir ./build", 600, false).ConfigureAwait(false);
        }



        /// <summary>
        /// Copies all the content of dest to src dictionary with the strcuture Dictionary<string, Collection<Dictionary<string, string>>>
        /// All the missing Keys in src will be added to dest with the values. If a key exists in both then the Collection value will be merged to dest
        ///
        /// </summary>
        /// <param name="dest">Final data structure that will contain the merged data</param>
        /// <param name="src">Holds Data that is intended to be merged to the desc variable</param>
        /// <returns></returns>
        private static void Merge(Dictionary<string, Collection<Dictionary<string, string>>> dest, Dictionary<string, Collection<Dictionary<string, string>>> src)
        {
            foreach (var item in src)
            {
                var payload = (dest.ContainsKey(item.Key)) ? src[item.Key] : new Collection<Dictionary<string, string>>();

                foreach (var keyItem in item.Value)
                    payload.Add(keyItem);

                dest[item.Key] = payload;
            }
        }


        /// <summary>
        /// Executes tsc compilation process and returns a dictionary of errors in the data structure  Dictionary<'fileName', Collection<Dictionary<'errorCode','errorMessage'>>>
        ///
        /// </summary>
        /// <param name="version"></param>
        /// <param name="rootPath"></param>
        /// <returns></returns>
        public async static Task<Dictionary<string, Collection<Dictionary<string, string>>>> RunAndParseNPMErrors(Versions version, string rootPath)
        {
            var result = new Dictionary<string, Collection<Dictionary<string, string>>>();

            int maxRetryCounts = 10;
            int retryCount = 0;
            while (retryCount < maxRetryCounts)
            {
                var errors = ParseNPMErrors(version, await CompileTypescriptFiles(rootPath).ConfigureAwait(false));
                Merge(result, errors);

                foreach (var item in errors)
                    File.Move(Path.Combine(rootPath, BUILD_DIR, item.Key), Path.Combine(rootPath, BACKUP_DIR, item.Key));

                if (errors.Count == 0) break;

                retryCount++;
            }

            return result;
        }

        /// <summary>
        /// Returns a dictionary of errors by parsing the NPM process strrings.
        ///
        /// The key is the fileName, and the Value is a collection of errors with an error code and a string
        ///
        /// i.e Dictionary<'fileName', Collection<Dictionary<'errorCode','errorMessage'>>>
        /// </summary>
        /// <param name="version"></param>
        /// <param name="errorStrings"></param>
        /// <returns></returns>
        public static Dictionary<string, Collection<Dictionary<string,string>>> ParseNPMErrors(Versions version, (string, string) errorStrings)
        {
            var errorMessage = String.IsNullOrEmpty(errorStrings.Item1) ? errorStrings.Item2 : errorStrings.Item1;

            // break the string
            string[] errors = errorMessage.Split(new[] { FILE_PREFIX }, StringSplitOptions.None);

            var result = new Dictionary<string, Collection<Dictionary<string, string>>>();

            foreach (var err in errors)
            {
                if (!String.IsNullOrEmpty(err) && err.Contains(".md.ts") && err.Contains('(') && err.Contains(')') && err.Contains(':'))
                {
                    var fileName = string.Concat(FILE_PREFIX, err.AsSpan(0, err.IndexOf(".md.ts", StringComparison.InvariantCulture)), ".md.ts");
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
                        }
                    );

                    result[fileName] = payload;
                }
            }

            return result;
        }

    }
}
