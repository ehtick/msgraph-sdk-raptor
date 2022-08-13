using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

using MsGraphSDKSnippetsCompiler.Models;
using TestsCommon;

namespace GoV1Tests;

[TestFixture]
public class SnippetCompileV1Tests
{
    private static RunSettings runSettings => new RunSettings(){
            Version = Versions.V1,
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
        await GoTestRunner.Compile(testData).ConfigureAwait(false);
    }
}
