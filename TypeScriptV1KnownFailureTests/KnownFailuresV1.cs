using MsGraphSDKSnippetsCompiler.Models;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using TestsCommon;

namespace TypeScriptV1KnownFailureTests
{
    [TestFixture]
    public class KnownFailuresV1
    {
        /// <summary>
        /// Holds a static reference of errors from test evaluation
        /// </summary>
        private static Dictionary<string, Collection<Dictionary<string, string>>> NpmResults;
        private static string testingPath;

        private static RunSettings TestRunSettings => new RunSettings(
            TestContext.Parameters,
            new RunSettings()
            {
                Version = Versions.V1,
                Language = Languages.TypeScript,
                TestType = TestType.CompilationKnownIssues
            }
        );

        /// <summary>
        /// Prepares the test directory if none exists
        /// NB: This function will require the pre-requisite packages to exist in the local file system
        /// </summary>
        [OneTimeSetUp]
        public static void TestsSetUp()
        {
            var data = TestDataGenerator.GetLanguageTestCaseData(TestRunSettings);

            testingPath = TypeScriptTestRunner.PrepareFolder();

            TypeScriptTestRunner.GenerateFiles(testingPath, data);

            NpmResults = TypeScriptTestRunner.RunAndParseNPMErrors(TestRunSettings.Version, testingPath);
        }


        /// <summary>
        /// Gets TestCaseData
        /// TestCaseData contains snippet file name, version and test case name
        /// </summary>
        public static IEnumerable<TestCaseData> TestDataV1 => TestDataGenerator.GetTestCaseData(TestRunSettings);

        /// <summary>
        /// Represents test runs generated from test case data
        /// </summary>
        /// <param name="fileName">snippet file name in docs repo</param>
        /// <param name="docsLink">documentation page where the snippet is shown</param>
        /// <param name="version">Docs version (e.g. V1, Beta)</param>
        [Test]
        [TestCaseSource(typeof(KnownFailuresV1), nameof(TestDataV1))]
        public void Test(LanguageTestData testData)
        {
            TypeScriptTestRunner.RunTest(testingPath,testData, NpmResults);
        }
    }
}
