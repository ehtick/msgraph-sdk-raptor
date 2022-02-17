# Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
Param(
    [string] $IdentifiersPath = (Join-Path $PSScriptRoot "../../../../TestsCommon/identifiers.json" -Resolve)
)
$raptorUtils = Join-Path $PSScriptRoot "../../RaptorUtils.ps1" -Resolve
. $raptorUtils

$identifiers = Get-CurrentIdentifiers -IdentifiersPath $IdentifiersPath
$appSettings = Get-AppSettings

# Connect To Microsoft Graph Using ClientId, TenantId and Certificate in AppSettings
Connect-DefaultTenant -AppSettings $appSettings

$user = Get-DefaultAdminUser
$accessReviewScheduleDefinition = Invoke-RequestHelper -Uri "identityGovernance/accessReviews/definitions?`$top=1" -User $user
if (!$accessReviewScheduleDefinition) {
    $scheduleDefData = Get-RequestData -ChildEntity "accessReviewScheduleDefinition"
    $scheduleDefData.reviewers[0].query += $user.id  #the user to do the review
    $group = Invoke-RequestHelper -Uri "groups?`$filter=displayName eq 'Northwind Traders'"
    $scheduleDefData.scope.query += "$($group.id)/transitiveMembers"  # the group whose users should be reviewed
    $accessReviewScheduleDefinition = Invoke-RequestHelper -Uri "identityGovernance/accessReviews/definitions" -Method "POST" -Body $scheduleDefData
}
$identifiers = Add-Identifier $identifiers @("accessReviewScheduleDefinition") $accessReviewScheduleDefinition.id
$identifiers = Add-Identifier $identifiers @("accessReview") $accessReviewScheduleDefinition.id # Support for Beta

# AccessReviewInstance autocreated based on accessReviewScheduleDefinition creation settings
$accessReviewInstance = Invoke-RequestHelper -Uri "identityGovernance/accessReviews/definitions/$($accessReviewScheduleDefinition.id)/instances/?`$top=1" -User $user
$retryCount = 0
$maxRetries = 4
$backOffTime = 30  # Minimum time to sleep in seconds before retrying
while(!$accessReviewInstance){
    Write-Host "Failed to retrieve an instance of the accessReviewScheduleDefinition"
    if($retryCount -lt $maxRetries){
        Write-Host "sleeping for $backOffTime seconds before retry $($retryCount+1) to get an accessReviewInstance"
        start-Sleep -Seconds $backOffTime
        $accessReviewInstance = Invoke-RequestHelper -Uri "identityGovernance/accessReviews/definitions/$($accessReviewScheduleDefinition.id)/instances/?`$top=1" -User $user
        $retryCount++
        Write-Host "Waited for scheduled accessreview instance creation for $($backOffTime*$retryCount) Seconds"
    }
    else{
        Write-Host "Attempts to retry fetching an instance of the accessReviewScheduleDefinition failed after $($retryCount*$backOffTime) seconds "
        break
    }
}
$identifiers = Add-Identifier $identifiers @("accessReviewScheduleDefinition", "accessReviewInstance") $accessReviewInstance.id
$identifiers = Add-Identifier $identifiers @("accessReviewInstance") $accessReviewInstance.id # Support for Beta

# Access review decision should be autopopulated a couple of seconds after access review was scheduled and instance completed
if($accessReviewInstance.id){
    $accessReviewDecision = Invoke-RequestHelper -Uri "identityGovernance/accessReviews/definitions/$($accessReviewScheduleDefinition.id)/instances/$($accessReviewInstance.id)/decisions/?`$top=1"
    $retryCount=0
    $backOffTime = 10 # in seconds
    while (!$accessReviewDecision -and ($retryCount -lt $maxRetries)){
        $sleepTime = [Math]::Pow(2, $retryCount) * $backOffTime
        Write-Host "sleeping for $sleepTime seconds before retrying to get an accessReview decision"
        start-Sleep -Seconds $sleepTime
        $accessReviewDecision = Invoke-RequestHelper -Uri "identityGovernance/accessReviews/definitions/$($accessReviewScheduleDefinition.id)/instances/$($accessReviewInstance.id)/decisions/?`$top=1"
        $retryCount++
    }
    $consoleMsg = $accessReviewDecision ? "Successfully retrieved accessReviewDecision": "Failed to retrieve accessReviewDecision"
    Write-Host $consoleMsg
    $identifiers = Add-Identifier $identifiers @("accessReviewScheduleDefinition", "accessReviewInstance", "accessReviewInstanceDecisionItem") $accessReviewDecision.id
}

$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
