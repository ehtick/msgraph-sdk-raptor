# Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
Param(
    [string] $IdentifiersPath = (Join-Path $PSScriptRoot "../../../../TestsCommon/identifiers.json")
)

$raptorUtils = Join-Path $PSScriptRoot "../../RaptorUtils.ps1" -Resolve
. $raptorUtils

$appSettings = Get-AppSettings
$identifiers = Get-CurrentIdentifiers -IdentifiersPath $IdentifiersPath

# Connect To Microsoft Graph Using ClientId, TenantId and Certificate in AppSettings
Connect-DefaultTenant -AppSettings $appSettings

# There is no way to query onlineMeetings without knowing the conferenceId or joinWebUrl beforehand.
# So we will just create a new onlineMeeting each time.
$onlineMeetingData = Get-RequestData -ChildEntity "onlineMeeting"
$onlineMeeting = Request-DelegatedResource -Uri "me/onlineMeetings" -Method POST -Body $onlineMeetingData
$identifiers = Add-Identifier $identifiers @("onlineMeeting") $onlineMeeting.id

$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
