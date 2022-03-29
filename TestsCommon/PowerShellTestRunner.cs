using System.Collections.Concurrent;
using System.Management.Automation;
using Azure.Core;
using static MsGraphSDKSnippetsCompiler.PermissionScopes;
using static TestsCommon.HostedRunSpace;

namespace TestsCommon;

public static class PowerShellTestRunner
{
    internal record struct PowerShellExecutionResultsModel(bool Success, string Snippet, Scope Scope,
        string ErrorMessage);
    internal record struct GraphCommandDetails(string Uri, string Method, string CurrentVersion, bool IsEducation);

    private const string SelectProfileScript = "Select-MgProfile";
    private const string ConnectMgGraphScript = "Connect-MgGraph";

    /// <summary>
    ///     matches powershell snippet from Powershell snippets markdown output
    /// </summary>
    private const string Pattern = @"```powershell(.*)```";

    /// <summary>
    ///     Regex to extract current cmdlet
    /// </summary>
    private const string CmdletExtractor = @"(Get-Mg[a-zA-Z0-9\.]+)";

    /// <summary>
    ///     compiled version of the powershell markdown regular expression
    ///     uses Singleline so that (.*) matches new line characters as well
    /// </summary>
    private static readonly Regex PowerShellSnippetRegex =
        new(Pattern, RegexOptions.Singleline | RegexOptions.Compiled);


    private static readonly Regex CmdletExtractorRegex =
        new(CmdletExtractor, RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly ConcurrentDictionary<string, string> DelegatedAccessTokenCache = new();
    private static readonly ConcurrentDictionary<bool, string> ApplicationAccessTokenCache = new();

    public static async Task Execute(LanguageTestData testData)
    {
        if (testData == null)
        {
            throw new ArgumentNullException(nameof(testData));
        }
        var snippet = GetSnippetToExecute(testData.FileContent);
        var graphCommandDetails = GetGraphCommandDetails(testData, snippet);
        var delegatedScopes = await GetScopes(testData, graphCommandDetails.Uri, graphCommandDetails.Method)
            .ConfigureAwait(false);
        PowerShellExecutionResultsModel psExecutionResult = default;
        if (delegatedScopes != null && delegatedScopes.Any())
        {
            psExecutionResult =
                await ExecuteSnippetWithDelegatedPermissions(delegatedScopes, snippet, graphCommandDetails.IsEducation, graphCommandDetails.CurrentVersion)
                    .ConfigureAwait(false);
            if (psExecutionResult.Success)
            {
                Assert.Pass($"Snippet Executed Successfully with {psExecutionResult.Scope.value}{Environment.NewLine}{psExecutionResult.Snippet}");
            }
        }

        if (!psExecutionResult.Success)
        {
            psExecutionResult =
                await ExecuteWithApplicationPermissions(snippet, graphCommandDetails.IsEducation, graphCommandDetails.CurrentVersion)
                    .ConfigureAwait(false);
            Assert.True(psExecutionResult.Success, $"Snippet Execution Failed with Application Permissions {Environment.NewLine}{psExecutionResult.ErrorMessage}{Environment.NewLine}{psExecutionResult.Snippet}");
            Assert.Pass($"Snippet Executed Successfully with Application Permissions {Environment.NewLine}{psExecutionResult.Snippet}");
        }
        Assert.Fail($"Snippet Execution Failed with {psExecutionResult.ErrorMessage}{Environment.NewLine}{psExecutionResult.Snippet}");
    }
#pragma warning disable CA1031 // Do not catch general exception types
    private static async Task<PowerShellExecutionResultsModel> ExecuteSnippetWithDelegatedPermissions(
        IEnumerable<Scope> delegatedScopes,
        string snippet,
        bool isEducation,
        string graphVersion)
    {
        var psExecutionResults = new List<PsExecutionResult>();
        foreach (var scope in delegatedScopes)
        {
            try
            {
                var token = await GetDelegatedAccessToken(scope, isEducation)
                    .ConfigureAwait(false);

                var selectProfileCommand = new PsCommand(SelectProfileScript,
                    new Dictionary<string, object> { { "Name", graphVersion } });
                var connectMgGraphCommand = new PsCommand(ConnectMgGraphScript,
                    new Dictionary<string, object> { { "AccessToken", token } });
                var commands = new List<PsCommand> { selectProfileCommand, connectMgGraphCommand };

                var psExecutionResult = RunScript(commands, TestContext.Out.WriteLine, snippet, scope);
                if (psExecutionResult.HadErrors)
                {
                    psExecutionResults.Add(psExecutionResult);
                    continue;
                }

                return new PowerShellExecutionResultsModel(true, snippet, scope, string.Empty);
            }
            catch (Exception ex)
            {
                await TestContext.Out.WriteLineAsync(ex.Message).ConfigureAwait(false);
            }
        }

        var errorRecords = GetPsErrors(psExecutionResults.SelectMany(c => c.ErrorRecords).ToList());
        return new PowerShellExecutionResultsModel(false, snippet, null,
            $"None of the delegated permissions work!{Environment.NewLine}{errorRecords}");
    }
#pragma warning restore CA1031 // Do not catch general exception types

    private static async Task<PowerShellExecutionResultsModel> ExecuteWithApplicationPermissions(string codeToExecute,
        bool isEducation,
        string graphVersion)
    {
#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            var token = await GetApplicationAccessToken(isEducation)
                .ConfigureAwait(false);
            var selectProfileCommand = new PsCommand(SelectProfileScript,
                new Dictionary<string, object> { { "Name", graphVersion } });
            var connectMgGraphCommand = new PsCommand(ConnectMgGraphScript,
                new Dictionary<string, object> { { "AccessToken", token } });
            var commands = new List<PsCommand> { selectProfileCommand, connectMgGraphCommand };
            var psExecutionResult = RunScript(commands, TestContext.Out.WriteLine, codeToExecute);
            if (psExecutionResult.HadErrors)
            {
                var errorMessage = GetPsErrors(psExecutionResult.ErrorRecords);
                return new PowerShellExecutionResultsModel(false, codeToExecute, null, errorMessage);
            }

            return new PowerShellExecutionResultsModel(true, codeToExecute, null, string.Empty);
        }
        catch (Exception ex)
        {
            return new PowerShellExecutionResultsModel(false, codeToExecute, null,
                $"Failed for Application Permissions{Environment.NewLine}{ex.Message}");
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }

    private static async Task<string> GetApplicationAccessToken(bool isEducation)
    {
        var currentToken = ApplicationAccessTokenCache.TryGetValue(isEducation, out var tokenValue);
        if (currentToken == false && string.IsNullOrWhiteSpace(tokenValue))
        {
            var permissionManager = await GetPermissionManager(isEducation).ConfigureAwait(false);
            var tokenContext = new TokenRequestContext(new[] { ResourceConstants.DefaultAuthScope });
            try
            {
                var token = await permissionManager.ClientCertificateCredential.GetTokenAsync(tokenContext)
                    .ConfigureAwait(false);
                tokenValue = token.Token;
                ApplicationAccessTokenCache[isEducation] = tokenValue;
            }
            catch (Exception ex)
            {
                await TestContext.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
                throw;
            }
        }
        return tokenValue;
    }

    private static GraphCommandDetails GetGraphCommandDetails(LanguageTestData testData, string snippet)
    {
        var currentVersion = GetVersion(testData.Version);
        var cmdlet = CmdletExtractorRegex.Match(snippet);
        Assert.True(cmdlet.Success, $"Could not Extract Cmdlet from Snippet:{snippet} using {CmdletExtractorRegex}");

        var findCommandResult = FindMgGraphCommand(cmdlet.Value, currentVersion, TestContext.Out.WriteLine);
        Assert.True(!findCommandResult.HadErrors, $"Find-MgGraphCommand Failure for {cmdlet.Value}{Environment.NewLine}{GetPsErrors(findCommandResult.ErrorRecords)}");

        var commandDetails = GetObjectValues(findCommandResult.Results[0].BaseObject);
        var method = commandDetails["Method"].ToString();
        var uriString = commandDetails["URI"].ToString();
        var isEducation = uriString != null && uriString.Contains("/education", StringComparison.OrdinalIgnoreCase);

        var graphCommandDetails = new GraphCommandDetails(uriString, method, currentVersion, isEducation);
        return graphCommandDetails;
    }

    private static string GetVersion(Versions version)
    {
        var stringVersion = version switch
        {
            Versions.Beta => "beta",
            Versions.V1 => "v1.0",
            _ => throw new ArgumentOutOfRangeException(nameof(version))
        };
        return stringVersion;
    }

    private static async Task<PermissionManager> GetPermissionManager(bool isEducation)
    {
        var currentPermissionManager = isEducation switch
        {
            true => await TestsSetup.EducationTenantPermissionManager.Value.ConfigureAwait(false),
            _ => await TestsSetup.RegularTenantPermissionManager.Value.ConfigureAwait(false)
        };

        return currentPermissionManager;
    }

    private static Dictionary<string, object> GetObjectValues(object obj)
    {
        var properties = obj.GetType().GetProperties();
        var permissions = new Dictionary<string, object>();
        foreach (var propertyInfo in properties)
        {
            permissions[propertyInfo.Name] = propertyInfo.GetValue(obj);
        }

        return permissions;
    }

    private static string GetPsErrors(IEnumerable<ErrorRecord> errorRecords)
    {
        var stringBuilder = new StringBuilder();
        var handler = new StringBuilder.AppendInterpolatedStringHandler(1, 1, stringBuilder);
        foreach (var errorRecord in errorRecords)
        {
            handler.AppendLiteral(errorRecord.Exception.Message);
            handler.AppendLiteral(Environment.NewLine);
        }

        return stringBuilder.ToString();
    }

    private static async Task<string> GetDelegatedAccessToken(Scope scope, bool isEducation)
    {
        var key = $@"{scope.value}:{isEducation}";
        var currentToken = DelegatedAccessTokenCache.TryGetValue(key, out var tokenValue);
        if (currentToken == false && string.IsNullOrWhiteSpace(tokenValue))
        {
            var permissionManager = await GetPermissionManager(isEducation).ConfigureAwait(false);
            var tokenCredential = permissionManager.GetTokenCredential(scope);
            try
            {
                var token = await tokenCredential.GetTokenAsync(new TokenRequestContext(new[] { scope.value }), default)
                    .ConfigureAwait(false);
                tokenValue = token.Token;
                DelegatedAccessTokenCache[key] = token.Token;
            }
            catch (Exception ex)
            {
                await TestContext.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
                throw;
            }
        }
        return tokenValue;
    }

    private static string GetSnippetToExecute(string fileContent)
    {
        var snippet = ExtractPowerShellSnippet(fileContent);
        var formattedSnippet = FormatPowerShellSnippet(snippet);
        try
        {
            var snippetWithIdentifiers = ReplaceIdentifiers(formattedSnippet);
            return snippetWithIdentifiers;
        }
        catch (Exception ex)
        {
            Assert.Fail($"{formattedSnippet}{Environment.NewLine}{ex.Message}");
            throw;
        }
    }

    private static Match ExtractPowerShellSnippet(string fileContent)
    {
        var match = PowerShellSnippetRegex.Match(fileContent);
        return match;
    }

    private static string FormatPowerShellSnippet(Match match)
    {
        Assert.IsTrue(match.Success, "Powershell snippet file is not in expected format!");
        var codeSnippetFormatted = match.Groups[1].Value
            .Replace("\r\n", "\r\n        ") // add indentation to match with the template
            .Replace("\r\n        \r\n", "\r\n\r\n") // remove indentation added to empty lines
            .Replace("\t", "    "); // do not use tabs

        while (codeSnippetFormatted.Contains("\r\n\r\n"))
        {
            codeSnippetFormatted =
                codeSnippetFormatted.Replace("\r\n\r\n", "\r\n"); // do not have empty lines for shorter error messages
        }

        return codeSnippetFormatted;
    }

    private static string ReplaceIdentifiers(string codeSnippet)
    {
        var psIdentifierReplacer = PsIdentifiersReplacer.Instance;
        codeSnippet = psIdentifierReplacer.ReplaceIds(codeSnippet,Languages.PowerShell);
        return codeSnippet;
    }
}
