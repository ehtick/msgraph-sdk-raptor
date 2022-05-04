using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

using MsGraphSDKSnippetsCompiler.Models;
using TestsCommon;

namespace GoV1KnownFailureTests;

[TestFixture]
public class KnownFailuresV1
{
    private static IEnumerable<LanguageTestData> languageTestData => TestDataGenerator.GetLanguageTestCaseData(runSettings);
    private static RunSettings runSettings => new RunSettings(
        TestContext.Parameters,
        new RunSettings()
        {
            Version = Versions.V1,
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
    /// Gets TestCaseData for V1 known failures
    /// TestCaseData contains snippet file name, version, docsLink, and test case name
    /// </summary>
    public static IEnumerable<TestCaseData> TestDataV1 => TestDataGenerator.GetTestCaseData(languageTestData, runSettings);

    /// <summary>
    /// Represents test runs generated from test case data
    /// </summary>
    [Test]
    [TestCaseSource(typeof(KnownFailuresV1), nameof(TestDataV1))]
    public async Task Test(LanguageTestData testData)
    {
        await GoTestRunner.Compile(testData).ConfigureAwait(false);
    }
}
