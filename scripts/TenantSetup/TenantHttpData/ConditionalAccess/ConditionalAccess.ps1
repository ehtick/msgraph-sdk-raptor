# Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
Param(
    [string] $IdentifiersPath = (Join-Path $PSScriptRoot "../../../../TestsCommon/identifiers.json" -Resolve)
)
$raptorUtils = Join-Path $PSScriptRoot "../../RaptorUtils.ps1" -Resolve
. $raptorUtils

$appSettings = Get-AppSettings
$identifiers = Get-CurrentIdentifiers -IdentifiersPath $IdentifiersPath
#Connect To Microsoft Graph Using ClientId, TenantId and Certificate in AppSettings
Connect-DefaultTenant -AppSettings $appSettings

#Create namedLocation https://docs.microsoft.com/en-us/graph/api/conditionalaccessroot-post-namedlocations?view=graph-rest-1.0&tabs=http
$namedLocationData = Get-RequestData -ChildEntity "NamedLocation"
$nameLocationUrl = "identity/conditionalAccess/namedLocations"
$currentNameLocation = Invoke-RequestHelper -Uri $nameLocationUrl -Method GET |
    Where-Object { $_.displayName -eq $namedLocationData.displayName } |
    Select-Object -First 1

if ($null -eq $currentNameLocation) {
    $currentNameLocation = Invoke-RequestHelper -Uri $nameLocationUrl -Body $namedLocationData -Method POST
}

$identifiers = Add-Identifier $identifiers @("namedLocation") $currentNameLocation.id

$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
