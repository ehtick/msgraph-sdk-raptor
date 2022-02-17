# Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
Param(
    [string] $IdentifiersPath = (Join-Path $PSScriptRoot "../../../../TestsCommon/identifiers.json" -Resolve)
)

$raptorUtils = Join-Path $PSScriptRoot "../../RaptorUtils.ps1" -Resolve
. $raptorUtils

$appSettings = Get-AppSettings
$identifiers = Get-CurrentIdentifiers -IdentifiersPath $IdentifiersPath

# Connect To Microsoft Graph Using ClientId, TenantId and Certificate in AppSettings
Connect-DefaultTenant -AppSettings $appSettings

$user = Get-DefaultAdminUser
$eventMessageRequest = Request-DelegatedResource -Uri "/users/$($user.id)/messages?filter=subject eq 'Let''s go for lunch'" |
    Select-Object -First 1

if ($null -eq $eventMessageRequest)
{
    $my_event = Get-RequestData -ChildEntity "Event"
    $my_event.attendees[0].emailAddress.address = "adelev@" + $identifiers.domain._value
    Request-DelegatedResource -Uri "/me/events" -Method "POST" -Body $my_event | Out-Null
    Start-Sleep -Seconds 2
    $eventMessageRequest = Request-DelegatedResource -Uri "/users/$($user.id)/messages?filter=subject eq 'Let''s go for lunch'" |
        Select-Object -First 1
}

$identifiers = Add-Identifier $identifiers @("eventMessageRequest") $eventMessageRequest.id

$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
