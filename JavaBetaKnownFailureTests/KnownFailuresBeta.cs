﻿using MsGraphSDKSnippetsCompiler.Models;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestsCommon;

namespace JavaBetaKnownFailureTests;

[TestFixture]
public class KnownFailuresBeta
{
    private static IEnumerable<LanguageTestData> languageTestData => TestDataGenerator.GetLanguageTestCaseData(runSettings);
    private static RunSettings runSettings => new RunSettings(TestContext.Parameters)
        {
            Version = Versions.Beta,
            Language = Languages.Java,
            TestType = TestType.CompilationKnownIssues
        };
    private JavaTestRunner javaTestRunner;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        javaTestRunner = new JavaTestRunner();
        await javaTestRunner.PrepareCompilationEnvironment(languageTestData).ConfigureAwait(false);
    }
    /// <summary>
    /// Gets TestCaseData for Beta known failures
    /// TestCaseData contains snippet file name, version and test case name
    /// </summary>
    public static IEnumerable<TestCaseData> TestDataBeta => TestDataGenerator.GetTestCaseData(languageTestData, runSettings);

    /// <summary>
    /// Represents test runs generated from test case data
    /// </summary>
    /// <param name="fileName">snippet file name in docs repo</param>
    /// <param name="docsLink">documentation page where the snippet is shown</param>
    /// <param name="version">Docs version (e.g. V1, Beta)</param>
    [Test]
    [TestCaseSource(typeof(KnownFailuresBeta), nameof(TestDataBeta))]
    public async Task Test(LanguageTestData testData)
    {
        await javaTestRunner.Run(testData).ConfigureAwait(false);
    }
}
