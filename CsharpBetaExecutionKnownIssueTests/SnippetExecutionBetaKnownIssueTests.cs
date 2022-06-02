using System.Collections.Generic;
using System.Threading.Tasks;
using MsGraphSDKSnippetsCompiler.Models;

using NUnit.Framework;

using TestsCommon;

namespace CsharpBetaExecutionKnownIssueTests;

[TestFixture]
public class SnippetExecutionBetaKnownIssueTests
{
    /// <summary>
    /// Gets TestCaseData for Beta
    /// TestCaseData contains snippet file name, version and test case name
    /// </summary>
    public static IEnumerable<TestCaseData> TestDataBeta => TestDataGenerator.GetExecutionTestData(
        new RunSettings
        {
            Version = Versions.Beta,
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
    [RetryTestCaseSource(typeof(SnippetExecutionBetaKnownIssueTests), nameof(TestDataBeta), MaxTries = 3)]
    public async Task Test(LanguageTestData testData)
    {
        await CSharpTestRunner.Execute(testData).ConfigureAwait(false);
    }
}
