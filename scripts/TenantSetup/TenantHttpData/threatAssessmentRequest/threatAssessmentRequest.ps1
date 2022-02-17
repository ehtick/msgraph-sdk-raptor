# Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
Param(
    [string] $IdentifiersPath = (Join-Path $PSScriptRoot "../../../../TestsCommon/identifiers.json" -Resolve)
)
$raptorUtils = Join-Path $PSScriptRoot "../../RaptorUtils.ps1" -Resolve
. $raptorUtils

$identifiers = Get-CurrentIdentifiers -IdentifiersPath $IdentifiersPath

$threatAssessmentRequest = Request-DelegatedResource -Uri "informationProtection/threatAssessmentRequests" -ScopeOverride "ThreatAssessment.ReadWrite.All" | Select-Object -First 1
if(!$threatAssessmentRequest) {
    $threatAssessmentRequestContent = Get-RequestData -ChildEntity "threatAssessmentRequest"
    $threatAssessmentRequest = Request-DelegatedResource -Uri "informationProtection/threatAssessmentRequests" -Method "POST" -Body $threatAssessmentRequestContent -ScopeOverride "ThreatAssessment.ReadWrite.All"
}
$identifiers = Add-Identifier $identifiers @("threatAssessmentRequest") $threatAssessmentRequest.id

$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
