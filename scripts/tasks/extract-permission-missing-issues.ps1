# Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.

Param(
    [string]$trxPath = "$env:BUILD_SOURCESDIRECTORY/msgraph-sdk-raptor/TestResults/RaptorTestResults.trx"
)

if (!(Test-Path $trxPath))
{
    Write-Error "please provide a path to a trx file"
    exit
}

[xml]$results = Get-Content $trxPath
return $results.TestRun.Results.UnitTestResult |
    Where-Object { $_.Output?.StdOut?.Contains("Can't get scopes for both") } |
    Select-Object -ExpandProperty Output |
    Select-Object -ExpandProperty StdOut |
    ForEach-Object {
        # last two lines of the StdOut are of the form:
        # url=<url>
        # docslink=<docslink>
        $lines = $_.Split([System.Environment]::NewLine)
        return @{
            url = $lines[-2].Split("url=")[-1];
            docslink = $lines[-1].Split("docslink=")[-1]
        }
    } |
    # convert array of objects into a flattened table with url and docs link columns
    ForEach-Object { [PSCustomObject]$_ }
