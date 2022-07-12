// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.

using MsGraphSDKSnippetsCompiler.Models;
using NUnit.Framework;
using System.Collections.Generic;
using TestsCommon;

namespace CSharpArbitraryDllTests;

[TestFixture]
public class SnippetCompileArbitraryDllTests
{
    /// <summary>
    /// Gets TestCaseData for version specified in Test.runsettings
    /// TestCaseData contains snippet file name, version and test case name
    /// </summary>
    public static IEnumerable<TestCaseData> ArbitraryDllTestData => TestDataGenerator.GetTestCaseData(new RunSettings(TestContext.Parameters));

    /// <summary>
    /// Represents test runs generated from test case data
    /// </summary>
    /// <param name="testData">The Languages test data</param>
    [Test]
    [TestCaseSource(typeof(SnippetCompileArbitraryDllTests), nameof(ArbitraryDllTestData))]
    public void Test(LanguageTestData testData)
    {
        CSharpTestRunner.Compile(testData);
    }
}
