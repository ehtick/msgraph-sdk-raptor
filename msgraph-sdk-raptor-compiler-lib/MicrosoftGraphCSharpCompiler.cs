extern alias beta;

using Azure.Core;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Linq.Expressions;
using System.Runtime.Loader;
using System.Reflection;
using Platform = Microsoft.CodeAnalysis.Platform;

namespace MsGraphSDKSnippetsCompiler;

/// <summary>
///     Microsoft Graph SDK CSharp snippets compiler class
/// </summary>
public class MicrosoftGraphCSharpCompiler : IMicrosoftGraphSnippetsCompiler
{
    const string GlobalUsings = @"global using System;
global using System.Collections.Generic;
global using System.Net.Http;
global using System.Text;
global using System.Text.Json;
global using System.Threading.Tasks;

global using Microsoft.Graph;
global using MsGraphSDKSnippetsCompiler;

// Disambiguate colliding namespaces
global using DayOfWeek = Microsoft.Graph.DayOfWeek;
global using TimeOfDay = Microsoft.Graph.TimeOfDay;
global using KeyValuePair = Microsoft.Graph.KeyValuePair;
";
    const string SourceCodePath = "generated.cs";
    const string GlobalUsingsSourceCodePath = "GlobalUsings.generated.cs";
    private readonly LanguageTestData TestData;
    private bool _isEducation;
    private static readonly Lazy<SourceText> GlobalUsingsSourceText = new(() =>
    {
        var buffer = Encoding.UTF8.GetBytes(GlobalUsings);
        return SourceText.From(buffer, buffer.Length, Encoding.UTF8, canBeEmbedded: true);
    });

    private static readonly Lazy<SyntaxTree> GlobalUsingsSyntaxTree = new(() =>
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(GlobalUsings, new CSharpParseOptions(), GlobalUsingsSourceCodePath);
        var syntaxRootNode = syntaxTree.GetRoot() as CSharpSyntaxNode;
        return CSharpSyntaxTree.Create(syntaxRootNode, null, SourceCodePath, Encoding.UTF8);
    });

    private async Task<PermissionManager> GetPermissionManager()
    {
        return _isEducation
            ? await TestsSetup.EducationTenantPermissionManager.Value.ConfigureAwait(false)
            : await TestsSetup.RegularTenantPermissionManager.Value.ConfigureAwait(false);
    }

    /// for hiding bearer token
    private const string AuthHeaderPattern = "Authorization: Bearer .*";
    private const string AuthHeaderReplacement = "Authorization: Bearer <token>";
    private static readonly Regex AuthHeaderRegex = new(AuthHeaderPattern, RegexOptions.Compiled);

    public MicrosoftGraphCSharpCompiler(LanguageTestData testData)
    {
        TestData = testData ?? throw new ArgumentNullException(nameof(testData));
    }

    /// <summary>
    ///     Returns CompilationResultsModel which has the results status and the compilation diagnostics.
    /// </summary>
    /// <param name="codeSnippet">The code snippet to be compiled.</param>
    /// <param name="version"></param>
    /// <returns>CompilationResultsModel</returns>
    private (CompilationResultsModel, Assembly) CompileSnippetAndGetAssembly(string codeSnippet, Versions version)
    {
        var buffer = Encoding.UTF8.GetBytes(codeSnippet);
        // Mark Original Source as Embeddable
        var sourceText = SourceText.From(buffer, buffer.Length, Encoding.UTF8, canBeEmbedded: true);
        // Embed Original Source Code in Syntax Tree
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(codeSnippet, new CSharpParseOptions(), SourceCodePath);
        var syntaxRootNode = syntaxTree.GetRoot() as CSharpSyntaxNode;
        var encoded = CSharpSyntaxTree.Create(syntaxRootNode, null, SourceCodePath, Encoding.UTF8);

        string assemblyName = Path.GetRandomFileName();
        string commonAssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
        string graphAssemblyPathV1 = Path.GetDirectoryName(typeof(GraphServiceClient).Assembly.Location);
        string graphAssemblyPathBeta = Path.GetDirectoryName(typeof(beta.Microsoft.Graph.GraphServiceClient).Assembly.Location);

        List<MetadataReference> metadataReferences = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(Path.Combine(commonAssemblyPath, "System.Private.CoreLib.dll")),
                MetadataReference.CreateFromFile(Path.Combine(commonAssemblyPath, "System.Console.dll")),
                MetadataReference.CreateFromFile(Path.Combine(commonAssemblyPath, "System.Runtime.dll")),
                MetadataReference.CreateFromFile(Path.Combine(commonAssemblyPath, "System.Text.Json.dll")),
                MetadataReference.CreateFromFile(Path.Combine(commonAssemblyPath, "System.Memory.dll")),
                MetadataReference.CreateFromFile(Path.Combine(commonAssemblyPath, "netstandard.dll")),
                MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(IAuthenticationProvider).Assembly.Location), "Microsoft.Graph.Core.dll")),
                MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(TokenCredential).Assembly.Location), "Azure.Core.dll")),
                MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(MicrosoftGraphCSharpCompiler).Assembly.Location), "msgraph-sdk-raptor-compiler-lib.dll")),
                MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(Task).Assembly.Location), "System.Threading.Tasks.dll")),
                MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(HttpClient).Assembly.Location), "System.Net.Http.dll")),
                MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(Expression).Assembly.Location), "System.Linq.Expressions.dll")),
                MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(Uri).Assembly.Location), "System.Private.Uri.dll"))
            };

        //Use the right Microsoft Graph Version
        if (!string.IsNullOrEmpty(TestData.DllPath))
        {
            if (!System.IO.File.Exists(TestData.DllPath))
            {
                throw new ArgumentException($"Provided dll path {TestData.DllPath} doesn't exist!");
            }

            metadataReferences.Add(MetadataReference.CreateFromFile(TestData.DllPath));
        }
        else if (version == Versions.V1)
        {
            metadataReferences.Add(MetadataReference.CreateFromFile(Path.Combine(graphAssemblyPathV1, "Microsoft.Graph.dll")));
        }
        else
        {
            metadataReferences.Add(MetadataReference.CreateFromFile(Path.Combine(graphAssemblyPathBeta, "Microsoft.Graph.Beta.dll")));
        }

        var compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: new[] { encoded, GlobalUsingsSyntaxTree.Value },
            references: metadataReferences,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, deterministic: true, platform: Platform.AnyCpu)
                .WithConcurrentBuild(true)
                .WithOptimizationLevel(OptimizationLevel.Release));

        var (emitResult, assembly) = GetEmitResult(compilation, sourceText);
        CompilationResultsModel results = GetCompilationResults(emitResult);

        return (results, assembly);
    }

    public CompilationResultsModel CompileSnippet(string codeSnippet, Versions version)
    {
        var (results, _) = CompileSnippetAndGetAssembly(codeSnippet, version);
        return results;
    }

    /// <summary>
    ///     Returns ExecutionResultsModel which has the results status and the compilation diagnostics.
    /// </summary>
    /// <param name="codeSnippet">The code snippet to be compiled and executed.</param>
    /// <param name="version">Graph API version</param>
    /// <returns>ExecutionResultsModel</returns>
    public async Task<ExecutionResultsModel> ExecuteSnippet(string codeSnippet, Versions version)
    {
        var (compilationResult, assembly) = CompileSnippetAndGetAssembly(codeSnippet, version);

        string exceptionMessage = null;
        bool success = false;
        if (compilationResult.IsSuccess)
        {
            try
            {
                dynamic instance = assembly.CreateInstance("GraphSDKTest");

                using HttpRequestMessage httpRequestMessage = instance.GetRequestMessage(null);
                if (httpRequestMessage.RequestUri.PathAndQuery[5..].Contains("/education", StringComparison.OrdinalIgnoreCase))
                {
                    _isEducation = true;
                }

                var delegatedScopes = await GetScopes(httpRequestMessage).ConfigureAwait(false);

                if (delegatedScopes != null)
                {
                    // succeeding for one scope is OK for now.
                    success = await ExecuteWithDelegatedPermissions(instance, delegatedScopes);
                }

                if (!success)
                {
                    success = await ExecuteWithApplicationPermissions(instance);
                }
            }
            catch (Exception e)
            {
                var innerExceptionMessage = e.InnerException?.Message ?? string.Empty;
                exceptionMessage = e.Message + Environment.NewLine + innerExceptionMessage;
                if (!TestsSetup.Config.Value.IsLocalRun)
                {
                    exceptionMessage = AuthHeaderRegex.Replace(exceptionMessage, AuthHeaderReplacement);
                }
            }
        }

        return new ExecutionResultsModel(compilationResult, success, exceptionMessage);
    }

    /// <summary>
    /// Executes the compiled snippet with tokens from given scope.
    /// </summary>
    /// <param name="instance">snippet instance</param>
    /// <param name="scopes">delegated permission scopes</param>
    /// <returns>
    /// true if a token from one of the scopes executes successfully
    /// false if none of them.
    /// </returns>
    private async Task<bool> ExecuteWithDelegatedPermissions(dynamic instance, Scope[] scopes)
    {
        var permissionManager = await GetPermissionManager().ConfigureAwait(false);
        foreach (var scope in scopes)
        {
            try
            {
                var authProvider = permissionManager.GetDelegatedAuthProvider(scope);

                // Pass custom http provider to provide interception and logging
                using var customHttpProvider = new CustomHttpProvider();
                await ((instance.Main(authProvider, customHttpProvider) as Task).ConfigureAwait(false));
                return true;
            }
            catch (Exception e)
            {
                await TestContext.Out.WriteLineAsync($"Failed for delegated scope: {scope}").ConfigureAwait(false);
                await TestContext.Out.WriteLineAsync(e.Message).ConfigureAwait(false);
            }
        }

        await TestContext.Out.WriteLineAsync($"None of the delegated permissions work!").ConfigureAwait(false);
        return false;
    }

    /// <summary>
    /// Executes the compiled snippet with an auth provider that that has all the application permissions
    /// </summary>
    /// <param name="instance">snippet instance</param>
    /// <returns>true if the call succeeds</returns>
    private async Task<bool> ExecuteWithApplicationPermissions(dynamic instance)
    {
        var permissionManager = await GetPermissionManager().ConfigureAwait(false);
        // Pass custom http provider to provide interception and logging
        using var customHttpProvider = new CustomHttpProvider();
        await ((instance.Main(permissionManager.AuthProvider, customHttpProvider) as Task).ConfigureAwait(false));
        return true;
    }

    /// <summary>
    /// Calls DevX Api to get required permissions
    /// </summary>
    /// <param name="httpRequestMessage">HttpRequestMessage that the C# SDK built for the snippet</param>
    /// <returns>
    /// 1. delegated scopes if found
    /// 2. null only if application scopes are found (we don't care about the specific application permission as our app has all of them)
    /// </returns>
    /// <exception cref="AggregateException">
    /// If DevX API fails to return scopes for both application and delegation permissions,
    /// throws an AggregateException containing the last exception from the service
    /// </exception>
    private async Task<Scope[]> GetScopes(HttpRequestMessage httpRequestMessage)
    {
        return await PermissionScopes.GetScopes(testData: TestData, message: httpRequestMessage).ConfigureAwait(false);
    }

    /// <summary>
    ///     Gets the result of the Compilation.Emit method.
    /// </summary>
    /// <param name="compilation">Immutable representation of a single invocation of the compiler</param>
    /// <param name="sourceText">Original Source Representation of the Snippet</param>
    private static (EmitResult, Assembly) GetEmitResult(CSharpCompilation compilation, SourceText sourceText)
    {
        Assembly assembly = null;

        using MemoryStream assemblyStream = new MemoryStream();
        var emitOptions = new EmitOptions(false, DebugInformationFormat.Embedded);
        var embeddedTexts = new List<EmbeddedText> {
            EmbeddedText.FromSource(GlobalUsingsSourceCodePath, GlobalUsingsSourceText.Value),
            EmbeddedText.FromSource(SourceCodePath, sourceText)
        };

        EmitResult emitResult = compilation.Emit(assemblyStream, embeddedTexts: embeddedTexts, options: emitOptions);

        if (emitResult.Success)
        {
            assemblyStream.Seek(0, SeekOrigin.Begin);
            assembly = AssemblyLoadContext.Default.LoadFromStream(assemblyStream);
        }
        return (emitResult, assembly);
    }

    /// <summary>
    ///     Checks whether the EmitResult is successful and returns an instance of CompilationResultsModel.
    /// </summary>
    /// <param name="emitResult">The result of the Compilation.Emit method.</param>
    private CompilationResultsModel GetCompilationResults(EmitResult emitResult)
    {
        // We are only interested with warnings and errors hence the diagnostics filter
        var failures = emitResult.Success
            ? null
            : emitResult.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

        return new CompilationResultsModel(emitResult.Success, failures, TestData.FileName);
    }
}
