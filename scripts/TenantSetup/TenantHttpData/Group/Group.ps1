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

# Create groupSetting  https://docs.microsoft.com/en-us/graph/api/groupsetting-post-groupsettings?view=graph-rest-1.0&tabs=http
$groupSettings = Get-RequestData -ChildEntity "GroupSetting"
$groupSettingUri = "groupSettings"
$currentGroupSettings = Invoke-RequestHelper -Uri $groupSettingUri -Method GET |
    Where-Object { $_.displayName -eq $groupSettings.displayName } |
    Select-Object -First 1

if ($null -eq $currentGroupSettings) {
    $currentGroupSettings = Invoke-RequestHelper -Uri $groupSettingUri -Method POST -Body $groupSettings
}

# Create groupLifecyclePolicy https://docs.microsoft.com/en-us/graph/api/grouplifecyclepolicy-post-grouplifecyclepolicies?view=graph-rest-1.0&tabs=http
$groupLifeCyclePolicy = Get-RequestData -ChildEntity "GroupLifeCyclePolicy"
$groupLifeCyclePolicyUri = "groupLifecyclePolicies"
$currentGroupLifeCyclePolicy = Invoke-RequestHelper -Uri $groupLifeCyclePolicyUri -Method GET |
    Where-Object { $_.managedGroupTypes -eq $groupLifeCyclePolicy.managedGroupTypes } |
    Select-Object -First 1

if ($null -eq $currentGroupLifeCyclePolicy) {
    $currentGroupLifeCyclePolicy = Invoke-RequestHelper -Uri $groupLifeCyclePolicyUri -Method POST -Body $groupLifeCyclePolicy
}

# Create Group Calendar Event https://docs.microsoft.com/en-us/graph/api/group-post-events?view=graph-rest-1.0&tabs=http
$groupCalendarEvents = Get-RequestData -ChildEntity "Event"
$groupCalendarEventsUri = "groups/$($identifiers.group._value)/calendar/events"
$currentGroupCalendarEvent = Request-DelegatedResource -Uri $groupCalendarEventsUri -AppSettings $appSettings |
    Where-Object { $_.subject -eq $groupCalendarEvents.subject } |
    Select-Object -First 1

if ($null -eq $currentGroupCalendarEvent) {
    $currentGroupCalendarEvent = Request-DelegatedResource -Uri $groupCalendarEventsUri -Method POST -Body $groupCalendarEvents -AppSettings $appSettings
}

$identifiers = Add-Identifier $identifiers @("groupSetting") $currentGroupSettings.id
$identifiers = Add-Identifier $identifiers @("groupLifeCyclePolicy") $currentGroupLifeCyclePolicy.id
$identifiers = Add-Identifier $identifiers @("group", "event") $currentGroupCalendarEvent.id

$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
