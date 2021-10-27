# Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
Param(
    [string] $IdentifiersPath = (Join-Path $PSScriptRoot "../../../../msgraph-sdk-raptor-compiler-lib/identifiers.json" -Resolve)
)
$raptorUtils = Join-Path $PSScriptRoot "../../RaptorUtils.ps1" -Resolve
. $raptorUtils

$identifiers = Get-CurrentIdentifiers -IdentifiersPath $IdentifiersPath
$appSettings = Get-AppSettings

#Connect To Microsoft Graph Using Education ClientId, TenantId and Certificate in AppSettings
Connect-EduTenant -AppSettings $appSettings


$educationClass = Invoke-RequestHelper -Uri "education/classes" -Method GET |
    Where-Object { $_.displayName -eq "Physical Science" } |
    Select-Object -First 1
$educationClass.id
$identifiers.educationClass._value = $educationClass.id

$educationAssignment = Invoke-RequestHelper -Uri "education/classes/$($educationClass.id)/assignments?`$filter=displayName eq 'Midterm'" -Method GET |
    Select-Object -First 1
$educationAssignment.id
$identifiers.educationClass.educationAssignment._value = $educationAssignment.id

$educationSubmission = Invoke-RequestHelper -Uri "education/classes/$($educationClass.id)/assignments/$($educationAssignment.id)/submissions?`$filter=status eq 'submitted'" -Method GET |
    Select-Object -First 1
$educationSubmission.id
$identifiers.educationClass.educationAssignment.educationSubmission._value = $educationSubmission.id

$educationSubmissionResource = Invoke-RequestHelper -Uri "education/classes/$($educationClass.id)/assignments/$($educationAssignment.id)/submissions/$($educationSubmission.id)/resources?`$top=1" -Method GET |
    Select-Object -First 1
$educationSubmissionResource.id
$identifiers.educationClass.educationAssignment.educationSubmission.educationSubmissionResource._value = $educationSubmissionResource.id



$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
