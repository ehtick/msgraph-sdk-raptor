using System.Collections.ObjectModel;

namespace MsGraphSDKSnippetsCompiler
{
    public class MicrosoftGraphTypeScriptCompiler : IMicrosoftGraphSnippetsCompiler
    {

        public CompilationResultsModel CompileSnippet(string codeSnippet, Versions version)
        {
            throw new NotImplementedException();
        }

        public Task<ExecutionResultsModel> ExecuteSnippet(string codeSnippet, Versions version)
        {
            throw new NotImplementedException();
        }

        public static IReadOnlyCollection<Diagnostic> GetDiagnostics(string fileName, Collection<Dictionary<string, string>> errorList)
        {
            ArgumentNullException.ThrowIfNull(fileName);
            ArgumentNullException.ThrowIfNull(errorList);

            var result = new List<Diagnostic>();

            foreach (var error in errorList)
            {
                var positions = error["errorPosition"].Replace("(", "", StringComparison.InvariantCulture).Replace(")", "", StringComparison.InvariantCulture).Split(",").Select(Int32.Parse).ToList(); ;
                result.Add(
                    Diagnostic.Create(
                        new DiagnosticDescriptor(error["errorCode"],
                                                        "Error during TypeScript compilation",
                                                        error["errorMessage"],
                                                        error["errorCode"],
                                                        DiagnosticSeverity.Error,
                                                        true),
                                            Location.Create(fileName,
                                                                            new TextSpan(0, 5),
                                                                            new LinePositionSpan(
                                                                                new LinePosition(positions[0], 0),
                                                                                new LinePosition(positions[0], positions[1]))))
                    );
            }

            return result;
        }
    }
}
