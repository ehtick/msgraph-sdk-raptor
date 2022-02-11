// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using MsGraphSDKSnippetsCompiler.Models;
using NUnit.Framework;
using TestsCommon;

namespace PowerShellV1ExecutionKnownFailureTests;

[TestFixture]
public class SnippetExecutionV1KnownFailureTests
{
    /// <summary>
    /// Gets TestCaseData for v1
    /// TestCaseData contains snippet file name, version and test case name
    /// </summary>
    public static IEnumerable<TestCaseData> TestDataV1 => TestDataGenerator.GetExecutionTestData(
        new RunSettings
        {
            Version = Versions.V1,
            Language = Languages.PowerShell,
            TestType = TestType.ExecutionKnownIssues
        });

    /// <summary>
    /// Represents test runs generated from test case data
    /// </summary>
    [Test]
    [RetryTestCaseSourceAttribute(typeof(SnippetExecutionV1KnownFailureTests), nameof(TestDataV1), MaxTries = 6)]
    public async Task Test(LanguageTestData testData)
    {
        await PowerShellTestRunner.Execute(testData).ConfigureAwait(false);
    }
}
