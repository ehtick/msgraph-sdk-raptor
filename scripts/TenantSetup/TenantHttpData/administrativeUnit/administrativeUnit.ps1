# Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
Param(
    [string] $IdentifiersPath = (Join-Path $PSScriptRoot "../../../../TestsCommon/identifiers.json" -Resolve)
)

$raptorUtils = Join-Path $PSScriptRoot "../../RaptorUtils.ps1" -Resolve
. $raptorUtils

$identifiers = Get-CurrentIdentifiers -IdentifiersPath $IdentifiersPath

Connect-DefaultTenant -AppSettings $appSettings

$administrativeUnit = Request-DelegatedResource -Uri "/directory/administrativeUnits"
if ($null -eq $administrativeUnit) {
    $administrativeUnitData = Get-RequestData -ChildEntity "administrativeUnit"
    $administrativeUnit = Request-DelegatedResource -Uri "/directory/administrativeUnits" -Method "POST" -Body $administrativeUnitData
}

$administrativeUnitId = $administrativeUnit.id
$identifiers = Add-Identifier $identifiers @("administrativeUnit") $administrativeUnitId

# get scopeRoleMembership
$scopedRoleMembership = Request-DelegatedResource -Uri "directory/administrativeUnits/$administrativeUnitId/scopedRoleMembers" -ScopeOverride "Directory.AccessAsUser.All" |
    Select-Object -First 1

if ($null -eq $scopedRoleMembership)
{
    # Only the User account administrator and Helpdesk administrator roles are currently supported for scoped-role memberships.
    # https://docs.microsoft.com/en-us/graph/api/administrativeunit-post-scopedrolemembers?view=graph-rest-1.0&tabs=http

    # Helpdesk Administrator Role is not enabled by default. Check if this script is run on the tenant before
    # https://docs.microsoft.com/en-us/azure/active-directory/roles/permissions-reference
    $helpdeskAdministratorRoleTemplateId = "729827e3-9c14-49f7-bb1b-9608f156bbb8"
    $helpdeskAdministratorRole = Request-DelegatedResource -Uri "directoryRoles?`$filter=roleTemplateId eq '$helpdeskAdministratorRoleTemplateId'" -ScopeOverride "Directory.AccessAsUser.All" |
        Select-Object -First 1
    if ($null -eq $helpdeskAdministratorRole)
    {
        # activate role
        # https://docs.microsoft.com/en-us/graph/api/directoryrole-post-directoryroles?view=graph-rest-1.0&tabs=http
        $directoryRoleData = Get-RequestData -ChildEntity "directoryRole"
        $directoryRoleData.roleTemplateId = $helpdeskAdministratorRoleTemplateId
        $helpdeskAdministratorRole = Request-DelegatedResource -Uri "directoryRoles" -Method "POST" -Body $directoryRoleData -ScopeOverride "Directory.AccessAsUser.All"
    }

    # create scopedRoleMembership
    # https://docs.microsoft.com/en-us/graph/api/administrativeunit-post-scopedrolemembers?view=graph-rest-1.0&tabs=http
    $scopedRoleMemberData = Get-RequestData -ChildEntity "scopedRoleMember"
    $scopedRoleMemberData.roleId = $helpdeskAdministratorRole.id
    $scopedRoleMemberData.roleMemberInfo.id = $identifiers.user._value
    $scopedRoleMembership = Request-DelegatedResource -Uri "directory/administrativeUnits/$administrativeUnitId/scopedRoleMembers" -Method "POST" -Body $scopedRoleMemberData -ScopeOverride "Directory.AccessAsUser.All"
}

$identifiers = Add-Identifier $identifiers @("administrativeUnit", "scopedRoleMembership") $scopedRoleMembership.id

# save identifiers
$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
