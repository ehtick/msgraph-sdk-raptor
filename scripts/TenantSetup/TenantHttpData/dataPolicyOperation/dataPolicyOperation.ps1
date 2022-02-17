# Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
Param(
    [string] $IdentifiersPath = (Join-Path $PSScriptRoot "../../../../TestsCommon/identifiers.json" -Resolve)
)
$raptorUtils = Join-Path $PSScriptRoot "../../RaptorUtils.ps1" -Resolve
. $raptorUtils

$appSettings = Get-AppSettings
$identifiers = Get-CurrentIdentifiers -IdentifiersPath $IdentifiersPath

$userId = Get-DefaultAdminUserId
$exportDataRequest = az storage blob exists --container-name exportpersonaldatastorage --name RequestInfo.json --connection-string $appSettings.RaptorStorageConnectionString
$exportDataExists = $exportDataRequest[1].split(":")[1]
if ($exportDataExists.trim() -eq "true"){
    az storage blob delete --container-name exportpersonaldatastorage --name RequestInfo.json --connection-string $appSettings.RaptorStorageConnectionString
}
$dataPolicyOperationContent = Get-RequestData -ChildEntity "dataPolicyOperation"
$dataPolicyOperationContent.storageLocation = $appSettings.SASUrl  # Retrieve SASUrl from azure key-Vault
$dataPolicyOperation = Request-DelegatedResource -Uri "users/$($userId)/exportPersonalData" -Method "Post" -Body $dataPolicyOperationContent -ScopeOverride "User.Export.All"
$dataPolicyOperation.Location
$dataPolicyOperation_id = $dataPolicyOperation.Location -split "/" | Select-Object -last 1

$identifiers = Add-Identifier $identifiers @("dataPolicyOperation") $dataPolicyOperation_id

$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
