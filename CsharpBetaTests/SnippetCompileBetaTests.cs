// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.

using MsGraphSDKSnippetsCompiler.Models;
using NUnit.Framework;
using System.Collections.Generic;
using TestsCommon;

namespace CsharpBetaTests;

[TestFixture]
public class SnippetCompileBetaTests
{
    /// <summary>
    /// Gets TestCaseData for Beta
    /// TestCaseData contains snippet file name, version and test case name
    /// </summary>
    public static IEnumerable<TestCaseData> TestDataBeta => TestDataGenerator.GetTestCaseData(
        new RunSettings
        {
            Version = Versions.Beta,
            Language = Languages.CSharp,
            TestType = TestType.CompilationStable
        });

    /// <summary>
    /// Represents test runs generated from test case data
    /// </summary>
    /// <param name="testData">The Languages test data</param>
    [Test]
    [TestCaseSource(typeof(SnippetCompileBetaTests), nameof(TestDataBeta))]
    public void Test(LanguageTestData testData)
    {
        CSharpTestRunner.Compile(testData);
    }
}
