# Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
Param(
    [string] $IdentifiersPath = (Join-Path $PSScriptRoot "../../../../TestsCommon/identifiers.json" -Resolve)
)

$raptorUtils = Join-Path $PSScriptRoot "../../RaptorUtils.ps1" -Resolve
. $raptorUtils

$identifiers = Get-CurrentIdentifiers -IdentifiersPath $IdentifiersPath

$todoTaskList = Request-DelegatedResource -Uri "me/todo/lists" -ScopeOverride "Tasks.Read" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("todoTaskList") $todoTaskList.Id

# TodoTask
$todoTaskUrl = "me/todo/lists/$($todoTaskList.id)/tasks"
$todoTask = Request-DelegatedResource -Uri ($todoTaskUrl + "?`$top=1") -ScopeOverride "Tasks.Read"
if (!$todoTask) {
    $todoTaskData = Get-RequestData -ChildEntity "todoTask"
    $todoTask = Request-DelegatedResource -Uri $todoTaskUrl -Method "POST" -Body $todoTaskData -ScopeOverride "Tasks.ReadWrite"
}
$identifiers = Add-Identifier $identifiers @("todoTaskList", "todoTask") $todoTask.Id

# LinkedResource
$linkedResource = Request-DelegatedResource -Uri "me/todo/lists/$($todoTaskList.id)/tasks/$($todoTask.id)/linkedResources?`$top=1" -ScopeOverride "Tasks.Read"
if(!$linkedResource){
    $linkedResourceData = Get-RequestData -ChildEntity "linkedResource"
    $linkedResource = Request-DelegatedResource -Uri "me/todo/lists/$($todoTaskList.id)/tasks/$($todoTask.id)/linkedResources" -Method "POST" -Body $linkedResourceData -ScopeOverride "Tasks.ReadWrite"
}

$identifiers = Add-Identifier $identifiers @("todoTaskList", "todoTask", "linkedResource") $linkedResource.Id

$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
