# Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
Param(
    [string] $IdentifiersPath = (Join-Path $PSScriptRoot "../../../../TestsCommon/identifiers.json" -Resolve)
)

$raptorUtils = Join-Path $PSScriptRoot "../../RaptorUtils.ps1" -Resolve
. $raptorUtils

$appSettings = Get-AppSettings
$identifiers = Get-CurrentIdentifiers -IdentifiersPath $IdentifiersPath
$userId = Get-DefaultAdminUserId

# Connect To Microsoft Graph Using ClientId, TenantId and Certificate in AppSettings
Connect-DefaultTenant -AppSettings $appSettings

$contactData = Get-RequestData -ChildEntity "Contact"
$contactUrl = "/users/$($userId)/contacts"
$currentContact = Request-DelegatedResource -Uri $contactUrl |
    Where-Object { $_.displayName -eq $contactData.displayName } |
    Select-Object -First 1

if($null -eq $currentContact) {
    $currentContact = Request-DelegatedResource -Uri $contactUrl -Body $contactData -Method POST
}

$identifiers = Add-Identifier $identifiers @("contact") $currentContact.id

# Create Contact Folder https://docs.microsoft.com/en-us/graph/api/user-post-contactfolders?view=graph-rest-1.0&tabs=http
$contactFolderData = Get-RequestData -ChildEntity "ContactFolder"
$contactFolderUrl = "/users/$($userId)/contactFolders"
$currentContactFolder = Request-DelegatedResource -Uri $contactFolderUrl |
    Where-Object { $_.displayName -eq $contactFolderData.displayName } |
    Select-Object -First 1

if($null -eq $currentContactFolder) {
    $currentContactFolder = Request-DelegatedResource -Uri $contactFolderUrl -Body $contactFolderData -Method POST
}

$identifiers = Add-Identifier $identifiers @("contactFolder") $currentContactFolder.id

$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
