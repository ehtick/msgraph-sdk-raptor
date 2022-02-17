# Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
Param(
    [string] $IdentifiersPath = (Join-Path $PSScriptRoot "../../../../TestsCommon/identifiers.json" -Resolve)
)

$raptorUtils = Join-Path $PSScriptRoot "../../RaptorUtils.ps1" -Resolve
. $raptorUtils

$appSettings = Get-AppSettings
$identifiers = Get-CurrentIdentifiers -IdentifiersPath $IdentifiersPath
$user = Get-DefaultAdminUser

# Connect To Microsoft Graph Using ClientId, TenantId and Certificate in AppSettings
Connect-DefaultTenant -AppSettings $appSettings

$messageRuleData = Get-RequestData -ChildEntity "MessageRule"
$messageRuleUrl = "/users/$($user.id)/mailFolders/inbox/messageRules"
$currentMessageRuleData = Request-DelegatedResource -Uri $messageRuleUrl |
    Where-Object { $_.displayName -eq $messageRuleData.displayName } |
    Select-Object -First 1

if ($null -eq $currentMessageRuleData) {
    $currentMessageRuleData = Request-DelegatedResource -Uri $messageRuleUrl -Body $messageRuleData -Method POST
}
$identifiers = Add-Identifier $identifiers @("mailFolder", "messageRule") $currentMessageRuleData.id

# Create Message (Mail) https://docs.microsoft.com/en-us/graph/api/user-sendmail?view=graph-rest-1.0&tabs=http
<#
    Get the a message with an attachment.
#>
function Get-CurrentMessageWithAttachment {
    param(
        [Parameter(Mandatory = $True)][string] $Subject,
        [Parameter(Mandatory = $True)][string] $Uri
    )
    $currentSubject = $Subject
    $currentMessageWithAttachment = Request-DelegatedResource -Uri $Uri -Method GET |
        Where-Object { $_.subject -eq $currentSubject } |
        Select-Object -First 1
    return $currentMessageWithAttachment
}

$messageData = Get-RequestData -ChildEntity "Message"
$getMessageUrl = "/users/$($user.id)/messages?`$filter=hasAttachments eq true"
$currentMessageWithAttachment = Get-CurrentMessageWithAttachment -Uri $getMessageUrl -Subject $messageData.message.subject
if ($null -eq $currentMessageWithAttachment) {
    $createMessageUrl = "/users/$($user.id)/sendMail"
    # Scope Override to "User.Read.All" since DevX returns over 10 permissions with no order of privilege
    # This prevents unneccessary iterations over returned permissions.
    $users = Request-DelegatedResource -Uri "/users" -ScopeOverride "User.Read.All" |
        Select-Object -First 1

    $messageData.values.toRecipients.emailAddress.address = $users.mail

    Request-DelegatedResource -Uri $createMessageUrl -Method POST -Body $messageData

    $currentMessageWithAttachment = Get-CurrentMessageWithAttachment -Uri $getMessageUrl -Subject $messageData.message.subject
}

$messageAttachment = Request-DelegatedResource -Uri "/users/$($user.id)/messages/$($currentMessageWithAttachment.id)/attachments" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("message") $currentMessageWithAttachment.id

$identifiers = Add-Identifier $identifiers @("message", "attachment") $messageAttachment.id

$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
