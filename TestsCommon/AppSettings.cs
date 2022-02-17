using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace MsGraphSDKSnippetsCompiler;

public static class AppSettings
{
    public static IConfigurationRoot Config()
    {
        const string RaptorConnectionString = "RAPTOR_CONFIGCONNECTIONSTRING";
        const string RaptorConfigEndpoint = "RAPTOR_CONFIGENDPOINT";

        // environment variables expected to be set only in Azure DevOps runs.
        const string BuildReason = "BUILD_REASON";
        const string BuildBuildId = "BUILD_BUILDID";

        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(System.IO.Directory.GetCurrentDirectory())
            .AddEnvironmentVariables();

        var config = configBuilder.Build();

        var isAzureDevOpsEnvironment = !string.IsNullOrEmpty(config[BuildReason]) && !string.IsNullOrEmpty(config[BuildBuildId]);
        var isLocalRun = !isAzureDevOpsEnvironment;

        configBuilder.AddInMemoryCollection(new Dictionary<string, string>()
            {
                { "IsLocalRun", isLocalRun.ToString() }
            });

        configBuilder.AddAzureAppConfiguration(options =>
        {
            var raptorConfigConnectionString = config[RaptorConnectionString];
            if (IsRaptorConfigAddressCorrect(raptorConfigConnectionString))
            {
                options.Connect(raptorConfigConnectionString);
            }
            else
            {
                var raptorEndpoint = config[RaptorConfigEndpoint];
                if (IsRaptorConfigAddressCorrect(raptorEndpoint))
                {
                    var credentials = new DefaultAzureCredential(includeInteractiveCredentials: true);
                    options.Connect(new Uri(raptorEndpoint), credentials);
                }
                else
                {
                    Assert.Fail($"Incorrect Raptor Config. Please Set {RaptorConnectionString} or {RaptorConfigEndpoint}");
                }
            }

            var runEnvironmentLabel = GetAppConfigLabel(config, isLocalRun);
            options.Select(keyFilter: KeyFilter.Any, runEnvironmentLabel)
                .UseFeatureFlags(flagOptions =>
                {
                    flagOptions.Label = runEnvironmentLabel;
                });
        });

        return configBuilder.Build();
    }

    /// <summary>
    /// determines which label to pick in Azure App Config
    /// </summary>
    /// <param name="config">config containing environment variables</param>
    /// <param name="isLocalRun">whether the context is local run or not</param>
    /// <returns>
    /// - custom label if specified in environment variable RAPTOR_CONFIGLABEL
    /// - "Development" if running locally
    /// - "CI" if running in Azure DevOps
    /// </returns>
    public static string GetAppConfigLabel(IConfigurationRoot config, bool isLocalRun)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        const string RaptorConfigLabel = "RAPTOR_CONFIGLABEL";
        var customLabel = config[RaptorConfigLabel];
        if (!string.IsNullOrEmpty(customLabel))
        {
            return customLabel;
        }

        return isLocalRun ? "Development" : "CI";
    }

    /// <summary>
    ///     Validate Raptor Configuration Address (Endpoint or Connection String) is not null, empty or placeholder text.
    /// </summary>
    /// <param name="raptorConfigAddress"></param>
    /// <returns></returns>
    private static bool IsRaptorConfigAddressCorrect(string raptorConfigAddress)
    {
        const string variablePlaceHolder = "[ENTER_VALUE]";
        return !string.IsNullOrWhiteSpace(raptorConfigAddress) && !string.Equals(raptorConfigAddress, variablePlaceHolder, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Extracts the configuration value, throws if empty string
    /// </summary>
    /// <param name="config">configuration</param>
    /// <param name="key">lookup key</param>
    /// <returns>non-empty configuration value if found</returns>
    public static string GetNonEmptyValue(this IConfigurationRoot config, string key)
    {
        var value = config?.GetSection(key)?.Value;
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidDataException($"Value for {key} is not found in appsettings.json");
        }

        return value;
    }
}
