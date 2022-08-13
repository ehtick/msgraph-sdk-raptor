using MsGraphSDKSnippetsCompiler.Models;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TestsCommon;

namespace TypeScriptV1Tests
{
    [TestFixture]
    public class SnippetCompileV1Tests
    {
        private static RunSettings runSettings => new RunSettings(
            TestContext.Parameters,
            new RunSettings()
            {
                Version = Versions.V1,
                Language = Languages.TypeScript,
                TestType = TestType.CompilationStable
            }
        );

        private static IEnumerable<LanguageTestData> languageTestData => TestDataGenerator.GetLanguageTestCaseData(runSettings);

        private TypeScriptTestRunner testRunner;

        /// <summary>
        /// Prepares the test directory if none exists
        /// NB: This function will require the pre-requisite packages to exist in the local file system
        /// </summary>
        [OneTimeSetUp]
        public async Task TestsSetUp()
        {
            testRunner = new TypeScriptTestRunner();
            await testRunner.PrepareEnvironment(languageTestData).ConfigureAwait(false);
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
        [TestCaseSource(typeof(SnippetCompileV1Tests), nameof(TestDataV1))]
        public async Task Test(LanguageTestData testData)
        {
            await testRunner.RunCompilationTests(testData).ConfigureAwait(false);
        }
    }
}
