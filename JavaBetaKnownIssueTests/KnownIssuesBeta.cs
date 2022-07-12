using MsGraphSDKSnippetsCompiler.Models;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestsCommon;

namespace JavaBetaKnownIssueTests;

[TestFixture]
public class KnownIssuesBeta
{
    private static IEnumerable<LanguageTestData> languageTestData => TestDataGenerator.GetLanguageTestCaseData(runSettings);
    private static RunSettings runSettings => new RunSettings(TestContext.Parameters, new RunSettings()
        {
            Version = Versions.Beta,
            Language = Languages.Java,
            TestType = TestType.CompilationKnownIssues
        });
    private JavaTestRunner javaTestRunner;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        javaTestRunner = new JavaTestRunner();
        await javaTestRunner.PrepareCompilationEnvironment(languageTestData).ConfigureAwait(false);
    }
    /// <summary>
    /// Gets TestCaseData for Beta known issues
    /// TestCaseData contains snippet file name, version and test case name
    /// </summary>
    public static IEnumerable<TestCaseData> TestDataBeta => TestDataGenerator.GetTestCaseData(languageTestData, runSettings);

    /// <summary>
    /// Represents test runs generated from test case data
    /// </summary>
    /// <param name="testData">The Languages test data</param>
    [Test]
    [TestCaseSource(typeof(KnownIssuesBeta), nameof(TestDataBeta))]
    public async Task Test(LanguageTestData testData)
    {
        await javaTestRunner.Run(testData).ConfigureAwait(false);
    }
}
