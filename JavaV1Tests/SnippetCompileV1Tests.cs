using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

using MsGraphSDKSnippetsCompiler.Models;
using TestsCommon;

namespace JavaV1Tests;

[TestFixture]
public class SnippetCompileV1Tests
{
    private static IEnumerable<LanguageTestData> languageTestData => TestDataGenerator.GetLanguageTestCaseData(runSettings);
    private static RunSettings runSettings => new RunSettings(
        TestContext.Parameters,
        new RunSettings(){
            Version = Versions.V1,
            Language = Languages.Java,
            TestType = TestType.CompilationStable
        }
    );
    private JavaTestRunner javaTestRunner;
    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        javaTestRunner = new JavaTestRunner();
        await javaTestRunner.PrepareCompilationEnvironment(languageTestData).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets TestCaseData for V1
    /// TestCaseData contains snippet file name, version and test case name
    /// </summary>
    public static IEnumerable<TestCaseData> TestDataV1 => TestDataGenerator.GetTestCaseData(languageTestData, runSettings);

    /// <summary>
    /// Represents test runs generated from test case data
    /// </summary>
    [Test]
    [TestCaseSource(typeof(SnippetCompileV1Tests), nameof(TestDataV1))]
    public async Task Test(LanguageTestData testData)
    {
        await javaTestRunner.Run(testData).ConfigureAwait(false);
    }
}
