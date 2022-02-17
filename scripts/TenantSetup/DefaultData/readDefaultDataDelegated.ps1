# Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
# - reads data from default data pack in CDX's Enterprise Data Pack.
# - updates identifiers.json file with IDs obtained from the tenant whose
#   credentials are given in the appsettings.json file.
# - uses delegated permissions to access the data.

Param(
    [string] $IdentifiersPath = (Join-Path $PSScriptRoot "../../../TestsCommon/identifiers.json" -Resolve)
)

$raptorUtils = Join-Path $PSScriptRoot "../RaptorUtils.ps1" -Resolve
. $raptorUtils

$identifiers = Get-CurrentIdentifiers -IdentifiersPath $IdentifiersPath

$calendarGroup = Request-DelegatedResource -Uri "me/calendarGroups" |
    Where-Object { $_.name -eq "My Calendars" } |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("calendarGroup") $calendarGroup.id

$todoTaskList = Request-DelegatedResource -Uri "me/todo/lists" -ScopeOverride "Tasks.Read" |
Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("todoTaskList") $todoTaskList.id

$mailFolder = Request-DelegatedResource -Uri "me/mailFolders/inbox"
$identifiers = Add-Identifier $identifiers @("mailFolder") $mailFolder.id

# Get Group with plans
$groupWithPlan = Request-DelegatedResource -Uri "groups" |
    Where-Object {$_.displayName -eq "Mark 8 Project Team"}
    Select-Object -First 1
# Get Planner via Group
$plannerPlan = Request-DelegatedResource -Uri "groups/$($groupWithPlan.id)/planner/plans" |
    Where-Object {$_.title -eq "Mark8 project tracking"}
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("plannerPlan") $plannerPlan.id

$plannerTask = Request-DelegatedResource -Uri "planner/plans/$($plannerPlan.id)/tasks" |
    Where-Object {$_.title -eq "Organize Catering"}
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("plannerTask") $plannerTask.id

$plannerBucket = Request-DelegatedResource -Uri "planner/plans/$($plannerPlan.id)/buckets" |
    Where-Object {$_.name -eq "After party"}
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("plannerBucket") $plannerBucket.id

# tenant agnostic data
$printEndpointId = "mpsdiscovery"
$identifiers = Add-Identifier $identifiers @("printService", "printServiceEndpoint") $printEndpointId

$printService = Request-DelegatedResource -Uri "print/services" -ScopeOverride "Printer.Read.All" |
    Where-Object { $_.endpoints[0].id -eq $printEndpointId } |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("printService") $printService.id

$groupSettingTemplate = Request-DelegatedResource -Uri "groupSettingTemplates" |
    Where-Object { $_.displayName -eq "Prohibited Names Settings" } |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("groupSettingTemplate") $groupSettingTemplate.id

$presence = Request-DelegatedResource -Uri "users/$($identifiers.user._value)/presence"
$identifiers = Add-Identifier $identifiers @("presence") $presence.id

$teamsApp = Request-DelegatedResource -Uri "appCatalogs/teamsApps" |
    Where-Object { $_.displayName -eq "Developer Portal" } |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("teamsApp") $teamsApp.id

$appDefinition = Request-DelegatedResource -Uri "appCatalogs/teamsApps/$($teamsApp.id)/appDefinitions" -ScopeOverride "AppCatalog.Read.All" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("teamsApp", "teamsAppDefinition") $appDefinition.id

$secureScore = Request-DelegatedResource -Uri "security/secureScores?`$top=1" |
     Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("secureScore") $secureScore.id

$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
