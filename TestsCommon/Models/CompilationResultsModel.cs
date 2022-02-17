namespace MsGraphSDKSnippetsCompiler.Models;

public record CompilationResultsModel(bool IsSuccess, IEnumerable<Diagnostic> Diagnostics, string MarkdownFileName)
{
    public override string ToString()
    {
        if (Diagnostics == null)
        {
            return "No diagnostics from the compiler!";
        }

        var result = new StringBuilder("\r\n");
        foreach (var diagnostic in Diagnostics)
        {
            var lineSpan = diagnostic.Location.GetLineSpan();
            var line = lineSpan.StartLinePosition.Line + 1; // 0 indexed
            var column = lineSpan.StartLinePosition.Character;

            result.Append(CultureInfo.InvariantCulture, $"\r\n{diagnostic.Id}: (Line:{line}, Column:{column}) {diagnostic.GetMessage(CultureInfo.InvariantCulture)}");
        }

        return result.ToString();
    }
};
#pragma warning disable CA1801 // Review unused parameters (false positive, seems to be fixed in vNext of dotnet: https://github.com/dotnet/roslyn-analyzers/pull/4499/files)
public record ExecutionResultsModel(CompilationResultsModel CompilationResult, bool Success, string ExceptionMessage = null);
