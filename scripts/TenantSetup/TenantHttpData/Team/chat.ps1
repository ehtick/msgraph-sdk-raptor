# Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
Param(
    [string] $IdentifiersPath = (Join-Path $PSScriptRoot "../../../../TestsCommon/identifiers.json" -Resolve)
)
$raptorUtils = Join-Path $PSScriptRoot "../../RaptorUtils.ps1" -Resolve
. $raptorUtils

$identifiers = Get-CurrentIdentifiers -IdentifiersPath $IdentifiersPath

$userId = Get-DefaultAdminUserId
$content = Get-RequestData -ChildEntity "chat"
$content.members[0].'user@odata.bind' += $userId
$user2 = Invoke-RequestHelper -Uri "users"  |Select-Object -First 2
$content.members[1].'user@odata.bind' += $user2[0].id
$content.members[2].'user@odata.bind' += $user2[1].id

$chat = Request-DelegatedResource -Uri "chats?`$filter=chatType eq 'group'" -ScopeOverride "Chat.Read" | Select-Object -First 1
if (!$chat){
    $chat = Request-DelegatedResource -Uri "chats" -Method "POST" -Body $content -ScopeOverride "Chat.ReadWrite"
}

$identifiers = Add-Identifier $identifiers @("chat") $chat.id
$identifiers = Add-Identifier $identifiers @("user", "chat") $chat.id

$conversationMember = Request-DelegatedResource -Uri "chats/$($identifiers.chat._value)/members" -ScopeOverride "ChatMember.Read" | Select-Object -Last 1
$identifiers = Add-Identifier $identifiers @("chat", "conversationMember") $conversationMember.id

$teamsAppInstallation = Request-DelegatedResource -Uri "chats/$($identifiers.chat._value)/installedApps?`$expand=teamsApp&`$filter=teamsApp/displayName eq 'Word'" -ScopeOverride "TeamsAppInstallation.ReadForChat" | Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("chat", "teamsAppInstallation") $teamsAppInstallation.id

$wordTeamsApp = Request-DelegatedResource -Uri "appCatalogs/teamsApps?`$filter=displayName eq 'Word'" -ScopeOverride "AppCatalog.Read.All"
$teamsTabContent = Get-RequestData -ChildEntity "teamsTab"
$teamsTabContent.'teamsApp@odata.bind' += $wordTeamsApp.id
$tabsUri = "chats/$($identifiers.chat._value)/tabs"
$teamsTab = Request-DelegatedResource -Uri $tabsUri -ScopeOverride "TeamsTab.Read.All"| Select-Object -First 1
if(!$teamsTab){
    $teamsTab = Request-DelegatedResource -Uri $tabsUri -Method "POST" -Body $teamsTabContent -ScopeOverride "TeamsTab.ReadWriteForChat"
}

$identifiers = Add-Identifier $identifiers @("chat", "teamsTab") $teamsTab.id

# get or create chat message
$chatMessage = Request-DelegatedResource -Uri "chats/$($chat.id)/messages?`$top=1" -ScopeOverride "Chat.Read"
if (!$chatMessage -or !$chatMessage.body.content.Contains("hostedContents") ){
    $chatBody = Get-RequestData -ChildEntity "chatMessage"
    $imageFilePath = Join-Path $PSScriptRoot ".\chatImage.png" -Resolve
    if(Test-Path -Path $imageFilePath){
        $base64EncodedImg =[System.Convert]::ToBase64String([System.IO.File]::ReadAllBytes($imageFilePath))
        $chatBody.hostedContents[0].contentBytes = $base64EncodedImg
        $chatMessage = Request-DelegatedResource -Uri "chats/$($chat.id)/messages" -Method "POST" -Body $chatBody -ScopeOverride "Chat.ReadWrite"
    }
    else
    {
        Write-Error "Can't find path $($imageFilePath)"
        exit
    }
}
$identifiers = Add-Identifier $identifiers @("chat", "chatMessage") $chatMessage.id
$identifiers = Add-Identifier $identifiers @("user", "chat", "chatMessage") $chatMessage.id

# Get Chat message hosted content created in chat message creation above
$hostedContent = Request-DelegatedResource -Uri "chats/$($chat.id)/messages/$($chatMessage.id)/hostedContents?`$top=1" -ScopeOverride "Chat.Read"
$identifiers = Add-Identifier $identifiers @("chat", "chatMessage", "hostedContent") $hostedContent.id
$identifiers = Add-Identifier $identifiers @("chat", "chatMessage", "chatMessageHostedContent") $hostedContent.id
$identifiers = Add-Identifier $identifiers @("user", "chat", "chatMessage", "hostedContent") $hostedContent.id

$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
