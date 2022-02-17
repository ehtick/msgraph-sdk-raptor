# Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
Param(
    [string] $IdentifiersPath = (Join-Path $PSScriptRoot "../../../../TestsCommon/identifiers.json" -Resolve)
)

$raptorUtils = Join-Path $PSScriptRoot "../../RaptorUtils.ps1" -Resolve
. $raptorUtils

$identifiers = Get-CurrentIdentifiers -IdentifiersPath $IdentifiersPath

Connect-DefaultTenant -AppSettings $appSettings

$siteId = $identifiers.site._value;

# attempt to read the permission if already exists
$permission = Invoke-RequestHelper -Uri "sites/$siteId/permissions" |
    Select-Object -First 1

# create a new one if it doesn't exist
if($null -eq $permission)
{
    $application = Invoke-RequestHelper -Uri "applications?`$filter=displayName eq 'PermissionManager'" |
        Select-Object -First 1

    if($null -eq $application)
    {
        Write-Error "Can't find PermissionManager application in the tenant!"
        exit
    }

    $permissionData = Get-RequestData -ChildEntity "permission"
    $permissionData.grantedToIdentities[0].application.id = $application.id
    $permission = Invoke-RequestHelper -Uri "sites/$siteId/permissions" -Method "POST" -Body $permissionData
}

$identifiers = Add-Identifier $identifiers @("site", "permission") $permission.id

$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
