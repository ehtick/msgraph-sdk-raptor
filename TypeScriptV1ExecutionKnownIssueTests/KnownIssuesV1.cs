using MsGraphSDKSnippetsCompiler.Models;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TestsCommon;
namespace TypeScriptV1KnownIssueTests
{
    [TestFixture]
    public class KnownIssuesV1
    {

        private static RunSettings runSettings => new RunSettings(
            TestContext.Parameters,
            new RunSettings()
            {
                Version = Versions.V1,
                Language = Languages.TypeScript,
                TestType = TestType.ExecutionKnownIssues
            }
        );

        private static IEnumerable<LanguageTestData> languageTestData => TestDataGenerator.GetExecutionLanguageTestData(runSettings);

        private TypeScriptTestRunner testRunner;

        /// <summary>
        /// Prepares the test directory if none exists
        /// NB: This function will require the pre-requisite packages to exist in the local file system
        /// </summary>
        [OneTimeSetUp]
        public async Task TestsSetUp()
        {
            testRunner = new TypeScriptTestRunner();
            await testRunner.PrepareEnvironment(languageTestData, true).ConfigureAwait(false);
        }


        /// <summary>
        /// Gets TestCaseData for V1
        /// TestCaseData contains snippet file name, version and test case name
        /// </summary>
        public static IEnumerable<TestCaseData> TestDataV1 => TestDataGenerator.GetLanguageTestCaseData(languageTestData);

        /// <summary>
        /// Represents test runs generated from test case data
        /// </summary>
        [Test]
        [TestCaseSource(typeof(KnownIssuesV1), nameof(TestDataV1))]
        public async Task Test(LanguageTestData testData)
        {
            await testRunner.RunExecutionTests(testData).ConfigureAwait(false);
        }
    }
}
