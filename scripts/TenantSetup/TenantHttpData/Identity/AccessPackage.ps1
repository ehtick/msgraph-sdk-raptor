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

# List accessPackages https://docs.microsoft.com/en-us/graph/api/entitlementmanagement-list-accesspackages?view=graph-rest-1.0&tabs=http
$accessPackage = Get-RequestData -ChildEntity "AccessPackage"
$accessPackageUrl = "/identityGovernance/entitlementManagement/accessPackages"
$currentAccessPackage = Invoke-RequestHelper -Uri $accessPackageUrl -Method GET |
    Where-Object { $_.displayName -eq $accessPackage.displayName } |
    Select-Object -First 1

if($null -eq $currentAccessPackage){
    $currentAccessPackage = Invoke-RequestHelper -Uri $accessPackageUrl -Method POST -Body $accessPackage
}

$identifiers = Add-Identifier $identifiers @("accessPackage") $currentAccessPackage.id

# List AccessPackageCatalog https://docs.microsoft.com/en-us/graph/api/entitlementmanagement-post-assignmentrequests?view=graph-rest-1.0&tabs=http
$accessPackageCatalog = Get-RequestData -ChildEntity "AccessPackageCatalog"
$accessPackageCatalogUrl = "/identityGovernance/entitlementManagement/catalogs"
$currentAccessPackageCatalog = Invoke-RequestHelper -Uri $accessPackageCatalogUrl -Method GET |
    Where-Object { $_.displayName -eq $accessPackageCatalog.displayName } |
    Select-Object -First 1

if($null -eq $currentAccessPackageCatalog){
    $currentAccessPackageCatalog = Invoke-RequestHelper -Uri $accessPackageCatalogUrl -Method POST -Body $accessPackageCatalog
}

$identifiers = Add-Identifier $identifiers @("accessPackageCatalog") $currentAccessPackageCatalog.id

$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
