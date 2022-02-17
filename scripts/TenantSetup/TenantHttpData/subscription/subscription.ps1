# Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
Param(
    [string] $IdentifiersPath = (Join-Path $PSScriptRoot "../../../../TestsCommon/identifiers.json" -Resolve)
)

$raptorUtils = Join-Path $PSScriptRoot "../../RaptorUtils.ps1" -Resolve
. $raptorUtils

$identifiers = Get-CurrentIdentifiers -IdentifiersPath $IdentifiersPath

$subscription = Request-DelegatedResource -Uri "subscriptions" -ScopeOverride "Mail.Read"| Select-Object -First 1
if(!$subscription){
    $subData = Get-RequestData -ChildEntity "subscription"
    $subData.expirationDateTime = (Get-Date).AddDays(1).ToString("yyyy-MM-ddThh:mm:ss.0000000Z")

    #Wake Up The Callbacks Site to prevent timeouts.
    Invoke-CallbackSiteWakeup

    $subscription = Request-DelegatedResource -Uri "subscriptions" -Method "POST" -Body $subData -ScopeOverride "Mail.Read"
}

$identifiers = Add-Identifier $identifiers @("subscription") $subscription.id

$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
