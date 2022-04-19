using System.Collections.ObjectModel;
using Microsoft.PowerShell;

namespace TestsCommon;

/// <summary>
///     Contains functionality for executing PowerShell scripts.
/// </summary>
internal static class HostedRunSpace
{
    private static InitialSessionState CreateDefaultState()
    {
        var currentSessionState = InitialSessionState.CreateDefault2();
        currentSessionState.ExecutionPolicy = ExecutionPolicy.Unrestricted;
        currentSessionState.LanguageMode = PSLanguageMode.FullLanguage;
        currentSessionState.ApartmentState = ApartmentState.STA;
        currentSessionState.ThreadOptions = PSThreadOptions.UseNewThread;
        currentSessionState.ImportPSModule("Microsoft.Graph.Authentication");
        return currentSessionState;
    }

    internal static PsExecutionResult FindMgGraphCommand(string command, string apiVersion, Action<string> output)
    {
        var findMgGraphCommand = new PsCommand("Find-MgGraphCommand",
            new Dictionary<string, object> {{"Command", command}, {"ApiVersion", apiVersion}});

        var findMgGraphCommandResult = RunScript(new List<PsCommand> {findMgGraphCommand}, output, string.Empty);
        return findMgGraphCommandResult;
    }

    internal static PsExecutionResult RunScript(IReadOnlyCollection<PsCommand> commands,
        Action<string> output,
        string scriptContents,
        Scope currentScope = default)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(output);

        var currentState = CreateDefaultState();
        using var ps = PowerShell.Create(currentState);
        foreach (var (command, dictionary) in commands)
        {
            ps.AddStatement()
                .AddCommand(command, true)
                .AddParameters(dictionary);
        }

        if (!string.IsNullOrWhiteSpace(scriptContents))
        {
            ps.AddStatement()
                .AddScript(scriptContents, true);
        }

        void OnErrorOnDataAdded(object sender, DataAddedEventArgs e)
        {
            if (sender is PSDataCollection<ErrorRecord> streamObjectsReceived)
            {
                var streamObjectsList = streamObjectsReceived.ToList();
                var currentStreamRecord = streamObjectsList[e.Index];
                output(
                    $@"ErrorStreamEvent: {currentStreamRecord.Exception.Message}  Current Scope: {currentScope?.value}");
            }
        }

        void OnWarningOnDataAdded(object sender, DataAddedEventArgs e)
        {
            if (sender is PSDataCollection<WarningRecord> streamObjectsReceived)
            {
                var streamObjectsList = streamObjectsReceived.ToList();
                var currentStreamRecord = streamObjectsList[e.Index];
                output($"WarningStreamEvent: {currentStreamRecord.Message}");
            }
        }

        void OnInformationOnDataAdded(object sender, DataAddedEventArgs e)
        {
            if (sender is PSDataCollection<InformationRecord> streamObjectsReceived)
            {
                var streamObjectsList = streamObjectsReceived.ToList();
                var currentStreamRecord = streamObjectsList?[e.Index];
                output($"InfoStreamEvent: {currentStreamRecord?.MessageData}");
            }
        }

        ps.Streams.Error.DataAdded += OnErrorOnDataAdded;
        ps.Streams.Warning.DataAdded += OnWarningOnDataAdded;
        ps.Streams.Information.DataAdded += OnInformationOnDataAdded;

        // execute the script and await the result.
        var pipelineObjects = ps.Invoke();
        var executionErrors = ps.Streams.Error.ToList();

        return new PsExecutionResult(ps.HadErrors, executionErrors, pipelineObjects);
    }

    internal readonly record struct PsCommand(string Command, Dictionary<string, object> Parameters);

    internal readonly record struct PsExecutionResult(bool HadErrors, IReadOnlyCollection<ErrorRecord> ErrorRecords,
        Collection<PSObject> Results);
}
