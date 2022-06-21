using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

using MsGraphSDKSnippetsCompiler.Models;
using TestsCommon;

namespace GoBetaTests;

[TestFixture]
public class SnippetCompileBetaTests
{
    private static RunSettings runSettings => new RunSettings(){
            Version = Versions.Beta,
            Language = Languages.Go,
            TestType = TestType.CompilationStable
        };
    private static IEnumerable<LanguageTestData> languageTestData => TestDataGenerator.GetLanguageTestCaseData(runSettings);

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        await GoTestRunner.PrepareCompilationEnvironment(languageTestData).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets TestCaseData for Beta
    /// TestCaseData contains snippet file name, version and test case name
    /// </summary>
    public static IEnumerable<TestCaseData> TestDataBeta => TestDataGenerator.GetTestCaseData(languageTestData, runSettings);

    /// <summary>
    /// Represents test runs generated from test case data
    /// </summary>
    [Test]
    [TestCaseSource(typeof(SnippetCompileBetaTests), nameof(TestDataBeta))]
    public async Task Test(LanguageTestData testData)
    {
        await GoTestRunner.Compile(testData).ConfigureAwait(false);
    }
}
