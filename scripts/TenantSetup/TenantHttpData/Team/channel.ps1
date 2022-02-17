# Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
Param(
    [string] $IdentifiersPath = (Join-Path $PSScriptRoot "../../../../TestsCommon/identifiers.json" -Resolve)
)
$raptorUtils = Join-Path $PSScriptRoot "../../RaptorUtils.ps1" -Resolve
. $raptorUtils

$identifiers = Get-CurrentIdentifiers -IdentifiersPath $IdentifiersPath

# Use Team and channel already in identifiers.json to get chnnel message and replies
$teamId = $identifiers.team._value
$channelId = $identifiers.team.channel._value
# Get channel message default data
$channelMessage = Request-DelegatedResource -Uri "teams/$teamId/channels/$channelId/messages" -ScopeOverride "ChannelMessage.Read.All" |
    Where-Object { $_.from.user.displayName -eq "Adele Vance"} |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("team", "channel", "chatMessage") $channelMessage.id

# Get channel message reply that has hosted content
$senderId = $identifiers.user._value
$replies = Request-DelegatedResource -Uri "teams/$teamId/channels/$channelId/messages/$($channelMessage.id)/replies" -ScopeOverride "ChannelMessage.Read.All"
$messageReply = $replies |
    Where-Object { $_.from.user.id -eq "$senderId"} |
    Select-Object -First 1

# create a reply with hostedContent as channel message reply
if (!$messageReply){
    $messageBody = Get-RequestData -ChildEntity "chatMessage"
    $imageFilePath = Join-Path $PSScriptRoot ".\chatImage.png" -Resolve
    if(Test-Path -Path $imageFilePath){
        $base64EncodedImg =[System.Convert]::ToBase64String([System.IO.File]::ReadAllBytes($imageFilePath))
        $messageBody.hostedContents[0].contentBytes = $base64EncodedImg
        # post a reply with hosted content to selected channel message
        $messageReply = Request-DelegatedResource -Uri "teams/$teamId/channels/$channelId/messages/$($channelMessage.id)/replies" -Method "POST" -Body $messageBody -ScopeOverride "ChannelMessage.Send"
    }
    else
    {
        Write-Error "Can't find path $($imageFilePath)"
        exit
    }
}

# if still no reply created, pick first reply on the message
if(!$messageReply){
    $messageReply = replies[0]
}

$identifiers = Add-Identifier $identifiers @("team", "channel", "chatMessage", "reply") $messageReply.id
$identifiers = Add-Identifier $identifiers @("team", "channel", "chatMessage", "chatMessage") $messageReply.id  # To cover for error in docs where reply-id is saved as chatMessage-id

$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
