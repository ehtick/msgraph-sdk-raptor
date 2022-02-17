# Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
Param(
    [string] $IdentifiersPath = (Join-Path $PSScriptRoot "../../../../TestsCommon/identifiers.json" -Resolve)
)
$raptorUtils = Join-Path $PSScriptRoot "../../RaptorUtils.ps1" -Resolve
. $raptorUtils

$appSettings = Get-AppSettings
$identifiers = Get-CurrentIdentifiers -IdentifiersPath $IdentifiersPath

# Connect To Microsoft Graph Using ClientId, TenantId and Certificate in AppSettings
Connect-DefaultTenant -AppSettings $appSettings

# Create Application to be Deleted https://docs.microsoft.com/en-us/graph/api/application-delete?view=graph-rest-1.0&tabs=http
$deletedApplicationData = Get-RequestData -ChildEntity "DeletedApplication"
$deletedApplicationUrl = "directory/deletedItems/microsoft.graph.application"
$currentDeletedApplication = Invoke-RequestHelper -Uri $deletedApplicationUrl -Method GET |
        Where-Object { $_.displayName -eq $deletedApplicationData.displayName } |
        Select-Object -First 1

# If there is no deletedApp, create one and delete it
if($null -eq $currentDeletedApplication){
    $currentDeletedApplication = Invoke-RequestHelper -Uri "applications" -Method POST -Body $deletedApplicationData
    # If App was created successfully, Delete it. Deletion returns No Content, so no expected return object.
    if($null -ne $currentDeletedApplication){
        Invoke-RequestHelper -Uri "applications/$($currentDeletedApplication.id)" -Method DELETE
    }
}

$identifiers = Add-Identifier $identifiers @("deletedDirectoryObject") $currentDeletedApplication.id

# save identifiers
$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
