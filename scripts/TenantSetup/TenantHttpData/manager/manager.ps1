# Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
Param(
    [string] $IdentifiersPath = (Join-Path $PSScriptRoot "../../../../TestsCommon/identifiers.json" -Resolve)
)

$raptorUtils = Join-Path $PSScriptRoot "../../RaptorUtils.ps1" -Resolve
. $raptorUtils

$identifiers = Get-CurrentIdentifiers -IdentifiersPath $IdentifiersPath

$userId = $identifiers.user._value;

# assign same user as the manager (allowed)
$managerRequest = @{
    "@odata.id" = "https://graph.microsoft.com/v1.0/users/$userId";
}

Request-DelegatedResource -Uri "me/manager/`$ref" -Method "PUT" -Body $managerRequest -scopeOverride "Directory.ReadWrite.All" | Out-Null
