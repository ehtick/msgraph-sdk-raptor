using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

using MsGraphSDKSnippetsCompiler.Models;
using TestsCommon;

namespace GoBetaKnownIssueTests;

[TestFixture]
public class KnownIssuesBeta
{
    private static IEnumerable<LanguageTestData> languageTestData => TestDataGenerator.GetLanguageTestCaseData(runSettings);
    private static RunSettings runSettings => new RunSettings(
        TestContext.Parameters,
        new RunSettings()
        {
            Version = Versions.Beta,
            Language = Languages.Go,
            TestType = TestType.CompilationKnownIssues
        }
    );

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        await GoTestRunner.PrepareCompilationEnvironment(languageTestData).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets TestCaseData for Beta known issues
    /// TestCaseData contains snippet file name, version, docsLink, and test case name
    /// </summary>
    public static IEnumerable<TestCaseData> TestDataBeta => TestDataGenerator.GetTestCaseData(languageTestData, runSettings);

    /// <summary>
    /// Represents test runs generated from test case data
    /// </summary>
    [Test]
    [TestCaseSource(typeof(KnownIssuesBeta), nameof(TestDataBeta))]
    public async Task Test(LanguageTestData testData)
    {
        await GoTestRunner.Compile(testData).ConfigureAwait(false);
    }
}
