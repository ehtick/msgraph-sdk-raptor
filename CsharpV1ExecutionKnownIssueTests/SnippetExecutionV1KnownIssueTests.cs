using System.Collections.Generic;
using System.Threading.Tasks;
using MsGraphSDKSnippetsCompiler.Models;
using NUnit.Framework;
using TestsCommon;

namespace CsharpV1ExecutionKnownIssueTests;

[TestFixture]
public class SnippetExecutionV1KnownIssueTests
{
    /// <summary>
    /// Gets TestCaseData for V1
    /// TestCaseData contains snippet file name, version and test case name
    /// </summary>
    public static IEnumerable<TestCaseData> TestDataV1 => TestDataGenerator.GetExecutionTestData(
        new RunSettings
        {
            Version = Versions.V1,
            Language = Languages.CSharp,
            TestType = TestType.ExecutionKnownIssues
        });

    /// <summary>
    /// Represents test runs generated from test case data
    /// </summary>
    /// <param name="fileName">snippet file name in docs repo</param>
    /// <param name="docsLink">documentation page where the snippet is shown</param>
    /// <param name="version">Docs version (e.g. V1, Beta)</param>
    [Test]
    [RetryTestCaseSource(typeof(SnippetExecutionV1KnownIssueTests), nameof(TestDataV1), MaxTries = 3)]
    public async Task Test(LanguageTestData testData)
    {
        await CSharpTestRunner.Execute(testData).ConfigureAwait(false);
    }
}
