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

# Use Team and User already in identifiers.json
$team = $identifiers.team._value
$user = $identifiers.user._value

#Create or Replace Schedule https://docs.microsoft.com/en-us/graph/api/team-put-schedule?view=graph-rest-1.0
$scheduleData = Get-RequestData -ChildEntity "Schedule"
Request-DelegatedResource -Uri "teams/$($team)/schedule" -Body $scheduleData -Method PUT

#Create Scheduling Group https://docs.microsoft.com/en-us/graph/api/schedule-post-schedulinggroups?view=graph-rest-1.0&tabs=http
<#
    Create a Scheduling Group with all the Team members as members.
#>
$schedulingGroupData = Get-RequestData -ChildEntity "SchedulingGroup"
# Get Other Group Members and Add them to the SchedulingGroup
$groupMembers = Request-DelegatedResource -Uri "teams/$($team)/members" |
    Sort-Object -Property { $_.displayName }
    Select-Object -First 2

# Pick an Existing User
$swapGroupMember = Request-DelegatedResource -Uri "teams/$($team)/members" |
    Where-Object { $_.displayName -eq "Isaiah Langer" } |
    Select-Object -First 1

$schedulingGroupData.userIds = $groupMembers.userId
# Check and Get Scheduling Group if it exists
$currentSchedulingGroup = Request-DelegatedResource -Uri "teams/$($team)/schedule/schedulingGroups" -Method GET |
    Where-Object { $_.displayName -eq $schedulingGroupData.displayName } |
    Select-Object -First 1

if ($null -eq $currentSchedulingGroup) {
    $currentSchedulingGroup = Request-DelegatedResource -Uri "teams/$($team)/schedule/schedulingGroups" -Method POST -Body $schedulingGroupData
}

# Create Shift https://docs.microsoft.com/en-us/graph/api/schedule-post-shifts?view=graph-rest-1.0&tabs=http
$shiftData = Get-RequestData -ChildEntity "Shift"
$shiftData.userId = $user
$shiftData.schedulingGroupId = $currentSchedulingGroup.id
$createdShift = Request-DelegatedResource -Uri "teams/$($team)/schedule/shifts" -Body $shiftData -Method POST

# Create OpenShift https://docs.microsoft.com/en-us/graph/api/openshift-post?view=graph-rest-1.0
$openShiftData = Get-RequestData -ChildEntity "OpenShift"
$openShiftData.schedulingGroupId = $currentSchedulingGroup.id
$createdOpenShift = Request-DelegatedResource -Uri "teams/$($team)/schedule/openshifts" -Body $openShiftData -Method POST

# Create OfferShiftRequests https://docs.microsoft.com/en-us/graph/api/offershiftrequest-post?view=graph-rest-1.0&tabs=http
$offerShiftData = Get-RequestData -ChildEntity "OfferShiftRequest"
$offerShiftData.senderShiftId = $createdShift.id
$offerShiftData.recipientUserId = $groupMembers[0].userId
$createdOfferShift = Request-DelegatedResource -Uri "teams/$($team)/schedule/offershiftrequests" -Body $offerShiftData -Method POST

# Create OpenShiftChangeRequest https://docs.microsoft.com/en-us/graph/api/openshiftchangerequest-post?view=graph-rest-1.0
$openShiftChangeRequestData = Get-RequestData -ChildEntity "OpenShiftChangeRequest"
$openShiftChangeRequestData.openShiftId = $createdOpenShift.id
$createOpenShiftChangeRequest = Request-DelegatedResource -Uri "teams/$($team)/schedule/openShiftChangeRequests" -Body $openShiftChangeRequestData -Method POST

# Create SwapShiftsChangeRequest https://docs.microsoft.com/en-us/graph/api/swapshiftschangerequest-post?view=graph-rest-1.0
<#
    Swap Shift Change Request, requires a Shift on the destination user.
#>

#Create a Shift for the Destination User
$recipientShiftData = Get-RequestData -ChildEntity "Shift"
$recipientShiftData.userId = $groupMembers[1].userId
$recipientShiftData.schedulingGroupId = $currentSchedulingGroup.id
$createdRecipientShift = Request-DelegatedResource -Uri "teams/$($team)/schedule/shifts" -Body $recipientShiftData -Method POST

$swapShiftsChangeRequestData = Get-RequestData -ChildEntity "SwapShiftsChangeRequest"
$swapShiftsChangeRequestData.senderShiftId = $createdOpenShift.id
$swapShiftsChangeRequestData.recipientUserId = $swapGroupMember.UserId
$swapShiftsChangeRequestData.senderShiftId = $createdShift.id
$swapShiftsChangeRequestData.recipientShiftId = $createdRecipientShift.id
$createdSwapShiftsChangeRequest = Request-DelegatedResource -Uri "teams/$($team)/schedule/swapShiftsChangeRequests" -Body $swapShiftsChangeRequestData -Method POST

# Get Or Create TimeOffReason if does not exist https://docs.microsoft.com/en-us/graph/api/schedule-post-timeoffreasons?view=graph-rest-1.0&tabs=http
$timeOffReasonData = Get-RequestData -ChildEntity "TimeOffReason"
$currentTimeOffReason = Request-DelegatedResource -Uri "teams/$($team)/schedule/timeOffReasons" -Method GET |
    Where-Object { $_.displayName -eq $timeOffReasonData.displayName } |
    Select-Object -First 1
if ($null -eq $timeOffReasonData) {
    $currentTimeOffReason = Request-DelegatedResource -Uri "teams/$($team)/schedule/timeOffReasons" -Method POST -Body $timeOffReasonData
}

$timeoffData = Get-RequestData -ChildEntity "TimesOff"
$timeoffData.userId = $user
$timeoffData.draftTimeOff.timeOffReasonId = $currentTimeOffReason.id
$createdTimesOffData = Request-DelegatedResource -Uri "teams/$($team)/schedule/timesOff" -Body $timeoffData -Method POST

$timeOffRequest = Request-DelegatedResource -Uri "teams/$($team)/schedule/timeOffRequests/$($createOpenShiftChangeRequest.id)" -Method GET |
    Select-Object -First 1

$identifiers = Add-Identifier $identifiers @("team", "shift") $createdShift.id
$identifiers = Add-Identifier $identifiers @("team", "schedulingGroup") $currentSchedulingGroup.id
$identifiers = Add-Identifier $identifiers @("team", "openShift") $createdOpenShift.id
$identifiers = Add-Identifier $identifiers @("team", "offerShiftRequest") $createdOfferShift.id
$identifiers = Add-Identifier $identifiers @("team", "openShiftChangeRequest") $createOpenShiftChangeRequest.id
$identifiers = Add-Identifier $identifiers @("team", "swapShiftsChangeRequest") $createdSwapShiftsChangeRequest.id
$identifiers = Add-Identifier $identifiers @("team", "timeOffReason") $currentTimeOffReason.id
$identifiers = Add-Identifier $identifiers @("team", "timeOff") $createdTimesOffData.id
$identifiers = Add-Identifier $identifiers @("team", "timeOffRequest") $timeOffRequest.id

$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
