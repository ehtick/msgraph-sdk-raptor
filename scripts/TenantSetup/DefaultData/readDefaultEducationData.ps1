# Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
# - reads data from default data pack in CDX's Enterprise Data Pack.
# - updates identifiers.json file with IDs obtained from the tenant whose
#   credentials are given in the appsettings.json file.
# - uses application permissions to access the data.

Param(
    [string] $IdentifiersPath = (Join-Path $PSScriptRoot "../../../TestsCommon/identifiers.json" -Resolve)
)
$raptorUtils = Join-Path $PSScriptRoot "../RaptorUtils.ps1" -Resolve
. $raptorUtils

$appSettings = Get-AppSettings
$identifiers = Get-CurrentIdentifiers -IdentifiersPath $IdentifiersPath

# Connect To Microsoft Graph Using ClientId, TenantId and Certificate in AppSettings
Connect-EduTenant -AppSettings $appSettings

$educationUser = Invoke-RequestHelper -Uri "education/users" |
    Where-Object { $_.mailNickname -eq "admin" }
$identifiers = Add-Identifier $identifiers @("educationUser") $educationUser.id

$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
