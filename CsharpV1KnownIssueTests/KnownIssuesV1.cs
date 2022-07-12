﻿// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.

using MsGraphSDKSnippetsCompiler.Models;
using NUnit.Framework;
using System.Collections.Generic;
using TestsCommon;

namespace CsharpV1KnownIssueTests;

[TestFixture]
public class KnownIssuesV1
{
    /// <summary>
    /// Gets TestCaseData for V1 known issues
    /// TestCaseData contains snippet file name, version and test case name
    /// </summary>
    public static IEnumerable<TestCaseData> TestDataV1 => TestDataGenerator.GetTestCaseData(
        new RunSettings
        {
            Version = Versions.V1,
            Language = Languages.CSharp,
            TestType = TestType.CompilationKnownIssues
        });

    /// <summary>
    /// Represents test runs generated from test case data
    /// </summary>
    /// <param name="testData">The Languages test data</param>
    [Test]
    [TestCaseSource(typeof(KnownIssuesV1), nameof(TestDataV1))]
    public void Test(LanguageTestData testData)
    {
        CSharpTestRunner.Compile(testData);
    }
}
