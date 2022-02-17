# Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
Param(
    [string] $IdentifiersPath = (Join-Path $PSScriptRoot "../../../../TestsCommon/identifiers.json" -Resolve)
)
$raptorUtils = Join-Path $PSScriptRoot "../../RaptorUtils.ps1" -Resolve
. $raptorUtils

$identifiers = Get-CurrentIdentifiers -IdentifiersPath $IdentifiersPath

#Wake Up The Callbacks Site to prevent timeouts.
Invoke-CallbackSiteWakeup

$integration = Request-DelegatedResource -Uri "teamwork/workforceintegrations" -ScopeOverride "WorkforceIntegration.Read.All"| Select-Object -First 1
if (!$integration){
    $integrationData = Get-RequestData -ChildEntity "workforceIntegration"
    $integrationData.encryption.secret = Get-RandomAlphanumericString -length 64
    $integration = Request-DelegatedResource -Uri "teamwork/workforceintegrations" -Method "POST" -Body $integrationData -ScopeOverride "WorkforceIntegration.ReadWrite.All"
}

$identifiers = Add-Identifier $identifiers @("workforceIntegration") $integration.id

$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
