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

# Use User already in identifiers.json
$user = $identifiers.user._value

# Set User as a default parameter to avoid setting it with every call
$PSDefaultParameterValues = @{"Invoke-RequestHelper:User" = $user }

# Create Notebook https://docs.microsoft.com/en-us/graph/api/onenote-post-notebooks?view=graph-rest-1.0&tabs=http
$noteBookData = Get-RequestData -ChildEntity "Notebook"
$currentNoteBook = Invoke-RequestHelper -Uri "users/$($user)/onenote/notebooks" -Method GET |
    Where-Object { $_.displayName -eq $noteBookData.displayName } |
    Select-Object -First 1

if ($null -eq $currentNoteBook) {
    $currentNoteBook = Invoke-RequestHelper -Uri "users/$($user)/onenote/notebooks" -Method POST -Body $noteBookData
}

$identifiers = Add-Identifier $identifiers @("notebook") $currentNoteBook.id

# Create Section https://docs.microsoft.com/en-us/graph/api/resources/section?view=graph-rest-1.0
$sectionData = Get-RequestData -ChildEntity "Section"
$currentSection = Invoke-RequestHelper -Uri "users/$($user)/onenote/notebooks/$($currentNoteBook.id)/sections" -Method GET |
    Where-Object { $_.displayName -eq $sectionData.displayName } |
    Select-Object -First 1

if ($null -eq $currentSection) {
    $currentSection = Invoke-RequestHelper -Uri "users/$($user)/onenote/notebooks/$($currentNoteBook.id)/sections" -Method POST -Body $sectionData
}

$identifiers = Add-Identifier $identifiers @("onenoteSection") $currentSection.id

# Create Page https://docs.microsoft.com/en-us/graph/api/resources/page?view=graph-rest-1.0
$pageData = Get-RequestData -ChildEntity "Page"
$currentPage = Invoke-RequestHelper -Uri "users/$($user)/onenote/sections/$($currentSection.id)/pages" -Method GET |
    Where-Object { $_.title -eq $pageData.title } |
    Select-Object -First 1

if ($null -eq $currentPage) {
    $raptorPresentationPage = Get-Content (Join-Path $PSScriptRoot "Page.html") -Raw -Encoding utf8
    $image = Get-Content (Join-Path $PSScriptRoot "RaptorDummyImage.jpg") -Raw -Encoding utf8
    $raptorDummyDocument = Get-Content  (Join-Path $PSScriptRoot "RaptorDummyDocument.pdf") -Raw -Encoding utf8

    $data = @(
        @{ ContentType = "text/html"; Content = $raptorPresentationPage; Name = "Presentation" },
        @{ ContentType = "image/jpeg"; Content = $image; Name = "RaptorDummyImage" },
        @{ ContentType = "application/pdf"; Content = $raptorDummyDocument; Name = "RaptorDummyDocument" }
    )

    $currentPage = Invoke-FormDataRequest -FormData $data -Uri "users/$($user)/onenote/sections/$($currentSection.id)/pages"
}

# Get Page Content https://docs.microsoft.com/en-us/graph/api/page-get?view=graph-rest-1.0
$htmlData = Get-HtmlDataRequest -Uri "users/$($user)/onenote/pages/$($currentPage.Id)/content"
$imageResource = $htmlData.Images[0]
$imageResourceUri = [System.Uri]::new($imageResource.src)
$imageResourceSegment = $imageResourceUri.Segments[$imageResourceUri.Segments.Length - 2]
$identifiers = Add-Identifier $identifiers @("onenoteResource") ($imageResourceSegment -Replace ".$")

# Copy Notebook Operation https://docs.microsoft.com/en-us/graph/api/notebook-copynotebook?view=graph-rest-1.0&tabs=http
$copyOperationData = Get-RequestData -ChildEntity "CopyOperation"
$renameRandomString = Get-RandomAlphanumericString -length 10
$copyOperationData.renameAs = "Raptor Rename $renameRandomString"
$copyOperation = Request-DelegatedResource -Uri "users/$($user)/onenote/notebooks/$($currentNoteBook.id)/copyNotebook" -Method POST -Body $copyOperationData
$identifiers = Add-Identifier $identifiers @("onenoteOperation") $copyOperation.id

#Create OneNote SectionGroups https://docs.microsoft.com/en-us/graph/api/notebook-post-sectiongroups?view=graph-rest-1.0&tabs=http
$sectionGroupData = Get-RequestData -ChildEntity "SectionGroup"
$sectionGroupUrl = "/me/onenote/sectionGroups"
$currentSectionGroupData = Request-DelegatedResource -Uri $sectionGroupUrl |
    Where-Object { $_.displayName -eq $sectionGroupData.displayName } |
    Select-Object -First 1

if($null -eq $currentSectionGroupData){
    $sectionGroupCreateUrl = "/me/onenote/notebooks/$($currentNoteBook.id)/sectionGroups"
    $currentSectionGroupData = Request-DelegatedResource -Uri $sectionGroupCreateUrl -Method POST -Body $sectionGroupData
}
$identifiers = Add-Identifier $identifiers @("sectionGroup") $currentSectionGroupData.id

$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
