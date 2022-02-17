# Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
Param(
    [string] $workbookFilePath = (Join-Path $PSScriptRoot "./raptorWorkbookDriveItem.xlsx" -Resolve),
    [string] $IdentifiersPath = (Join-Path $PSScriptRoot "../../../../TestsCommon/identifiers.json" -Resolve)
)

$raptorUtils = Join-Path $PSScriptRoot "../../RaptorUtils.ps1" -Resolve
. $raptorUtils

$identifiers = Get-CurrentIdentifiers -IdentifiersPath $IdentifiersPath

$uploadFileName = "raptorWorkbookDriveItemTest.xlsx"

# no need to check the existence of driveItem, as it will be created if it does not exist
# if it exists, it will just generate a new version
$driveItem = Request-DelegatedResource -Uri "me/drive/root:/$($uploadFileName):/content" -Method "PUT" -FilePath $workbookFilePath -ScopeOverride "Files.ReadWrite.All"
# create a second version of the file
Request-DelegatedResource -Uri "me/drive/root:/$($uploadFileName):/content" -Method "PUT" -FilePath $workbookFilePath -ScopeOverride "Files.ReadWrite.All"

$identifiers = Add-Identifier $identifiers @("driveItem") $driveItem.id
$identifiers = Add-Identifier $identifiers @("driveItem", "driveItemVersion") "1.0" # standard first version number

$driveItemPermission = Request-DelegatedResource -Uri "me/drive/items/$($driveItem.id)/permissions" -ScopeOverride "Files.Read" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("driveItem", "permission") $driveItemPermission.id

$driveItemThumbnail = Request-DelegatedResource -Uri "me/drive/items/$($driveItem.id)/thumbnails?`$top=1" -ScopeOverride "Files.Read" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("driveItem", "thumbnailSet") $driveItemThumbnail.id
$identifiers = Add-Identifier $identifiers @("driveItem", "thumbnailSet", "thumbnailSet") "small"

$driveItemWorkbookTable = Request-DelegatedResource -Uri "me/drive/items/$($driveItem.id)/workbook/tables" -ScopeOverride "Files.Read" |
    Where-Object { $_.name -eq "Table1" } |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("driveItem", "workbookTable") $driveItemWorkbookTable.id

$tableColumn = Request-DelegatedResource -Uri "me/drive/items/$($driveItem.id)/workbook/tables/$($driveItemWorkbookTable.id)/columns" -ScopeOverride "Files.Read" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("driveItem", "workbookTable", "workbookTableColumn") $tableColumn.id

$tableRow = Request-DelegatedResource -Uri "me/drive/items/$($driveItem.id)/workbook/tables/$($driveItemWorkbookTable.id)/rows" -ScopeOverride "Files.Read" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("driveItem", "workbookTable", "workbookTableRow") "itemAt(index=$($tableRow.index))"

$worksheet = Request-DelegatedResource -Uri "me/drive/items/$($driveItem.id)/workbook/worksheets" -ScopeOverride "Files.Read" |
    Where-Object { $_.name -eq "Sheet1" } |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("driveItem", "workbookWorksheet") $worksheet.id
$identifiers = Add-Identifier $identifiers @("workbookWorksheet") $worksheet.id

$workbookNamedItem = Request-DelegatedResource -Uri "me/drive/items/$($driveItem.id)/workbook/names/test2" -ScopeOverride "Files.Read.All"
$identifiers = Add-Identifier $identifiers @("driveItem", "workbookNamedItem") "test2"

$namedItemFormatBorder = Request-DelegatedResource -Uri "me/drive/items/$($driveItem.id)/workbook/names/$($workbookNamedItem.name)/range/format/borders?`$top=1" -ScopeOverride "Files.Read" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("driveItem", "workbookNamedItem", "workbookRangeBorder") $namedItemFormatBorder.id

$workbookChart = Request-DelegatedResource -Uri "me/drive/items/$($driveItem.id)/workbook/worksheets/$($worksheet.id)/charts" -ScopeOverride "Files.Read" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("driveItem", "workbookWorkSheet", "workbookChart") $workbookChart.id

$workbookChartSeries = Request-DelegatedResource -Uri "me/drive/items/$($driveItem.id)/workbook/worksheets/$($worksheet.id)/charts/$($workbookChart.name)/series" -ScopeOverride "Files.Read" |
    Select-Object -First 1
$series_id = $workbookChartSeries."@odata.id".Split("series/")[1]
$identifiers = Add-Identifier $identifiers @("driveItem", "workbookWorkSheet", "workbookChart", "workbookChartSeries") $series_id

$workbookChartPoint = Request-DelegatedResource -Uri "me/drive/items/$($driveItem.id)/workbook/worksheets/$($worksheet.id)/charts/$($workbookChart.name)/series/$($series_id)/points" -ScopeOverride "Files.ReadWrite.All" |
    Select-Object -First 1
$chartPoint_id = $workbookChartPoint."@odata.id".split("points/")[1]
$identifiers = Add-Identifier $identifiers @("driveItem", "workbookWorkSheet", "workbookChart", "workbookChartSeries", "workbookChartPoint") $chartPoint_id

$workbookComment = Request-DelegatedResource -Uri "me/drive/items/$($driveItem.id)/workbook/comments" -ScopeOverride "Files.Read" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("driveItem", "workbookComment") $workbookComment.id

$workbookCommentReply = Request-DelegatedResource -Uri "me/drive/items/$($driveItem.id)/workbook/comments/$($workbookComment.id)/replies" -ScopeOverride "Files.Read" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("driveItem", "workbookComment", "workbookCommentReply") $workbookCommentReply.id

$currentOperationId = $identifiers.driveItem.workbookOperation._value
if ($currentOperationId){
    $driveItemWorkbookOperation = Request-DelegatedResource -Uri "me/drive/items/$($driveItem.id)/workbook/operations/$($currentOperationId)" -ScopeOverride "Files.Read"
}
if (!$driveItemWorkbookOperation){
    $operationData = Get-RequestData -ChildEntity "driveItemWorkbookOperation"
    $operationHeaders = @{"Prefer"="respond-async"}
    $driveItemWorkbookOperation = Request-DelegatedResource -Uri "me/drive/items/$($driveItem.id)/workbook/createSession" -Method "POST" -Headers $operationHeaders -Body $operationData -ScopeOverride "Files.ReadWrite"
}

$identifiers = Add-Identifier $identifiers @("driveItem", "workbookOperation") $driveItemWorkbookOperation.id

#SharedDriveItem
$sharingUrl = $null
$sharingDriveItem = Request-DelegatedResource -Uri "me/drive/root/children?`$filter=name eq 'Proposed_agenda_topics.docx'" -ScopeOverride "Files.Read"
$createLinkData = Get-RequestData -ChildEntity "sharedDriveItem"
if ($sharingDriveItem.shared.scope -eq "users"){
    $sharingPem = Request-DelegatedResource -Uri "me/drive/items/$($sharingDriveItem.id)/permissions?`$filter=(link/type eq '$($createLinkData.type)') and (link/scope eq '$($createLinkData.scope)')" -ScopeOverride "Files.Read" |
        Select-Object -First 1
    $sharingUrl = $sharingPem.link.webUrl
}
else{  # create link if it doesn't exist
    $DriveItemLink = Request-DelegatedResource -Uri "me/drive/items/$($sharingDriveItem.id)/createLink" -Method "POST" -Body $createLinkData -ScopeOverride "Files.ReadWrite"
    $sharingUrl = $DriveItemLink.link.webUrl;
}
if ( ![string]::IsNullOrWhitespace($sharingUrl)){
    # Encoding sharing URLs as detailed in docs:https://docs.microsoft.com/en-us/graph/api/shares-get?view=graph-rest-1.0&tabs=http#encoding-sharing-urls
    $base64Value = [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($sharingUrl));
    $encodedUrl = "u!" + $base64Value.TrimEnd('=').Replace('/', '_').Replace('+', '-');
    $identifiers = Add-Identifier $identifiers @("sharedDriveItem") $encodedUrl
}

# Get Delta https://docs.microsoft.com/en-us/graph/api/driveitem-delta?view=graph-rest-1.0&amp%3Btabs=csharp&tabs=http#request-1

$delta = Request-DelegatedResource -Uri "/me/drive/root/delta" -Method "GET" -OutputType "Json" | ConvertFrom-Json -AsHashtable
$deltaUri = [uri] $delta.'@odata.deltaLink'
$deltaParts = $deltaUri.Query.Split("=")
$deltaToken = $deltaParts[1]

$identifiers = Add-Identifier $identifiers @("driveDelta") $deltaToken

$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
