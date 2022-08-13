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

$domain = Get-CurrentDomain -AppSettings $appSettings

#Connect To Microsoft Graph Using ClientId, TenantId and Certificate in AppSettings
Connect-DefaultTenant -AppSettings $appSettings

$identifiers = Add-Identifier $identifiers @("domain") $domain

$user = Get-DefaultAdminUser
$identifiers = Add-Identifier $identifiers @("user") $user.id
$identifiers = Add-Identifier $identifiers @("directoryObject") $user.id

#TODO: Place is missing from tenant
# Requires to be created manually via Admin UI Interaction.
# After Creation requires 48hrs to propagate
# https://github.com/microsoftgraph/msgraph-sdk-raptor/issues/747
$place = Invoke-RequestHelper -Uri "places/microsoft.graph.room" |
    Where-Object {$_.displayName -eq "Raptor Test Room"}
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("place") $place.id

$unifiedRoleDefinition = Invoke-RequestHelper -Uri "roleManagement/directory/roleDefinitions?`$filter=displayName eq 'Groups Administrator'" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("unifiedRoleDefinition") $unifiedRoleDefinition.id

$calendarPermission = Invoke-RequestHelper -Uri "users/$($user.id)/calendar/calendarPermissions" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("user", "calendarPermission") $calendarPermission.id

$userScopeTeamsAppInstallation = Invoke-RequestHelper -Uri "users/$($user.id)/teamwork/installedApps?`$expand=teamsApp&`$filter=teamsApp/displayName eq 'Approvals'" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("user", "userScopeTeamsAppInstallation") $userScopeTeamsAppInstallation.id

$team = Invoke-RequestHelper -Uri "groups" |
    Where-Object { $_.resourceProvisioningOptions -eq "Team" -and $_.displayName -eq "Mark 8 Project Team"} |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("team") $team.id
$identifiers = Add-Identifier $identifiers @("group") $team.id

$channel = Invoke-RequestHelper -Uri "teams/$($team.id)/channels" |
    Where-Object { $_.displayName -eq "Research and Development"}
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("team", "channel") $channel.id

$member = Invoke-RequestHelper -Uri "teams/$($team.id)/channels/$($channel.id)/members" |
    Where-Object { $_.displayName -eq "MOD Administrator"}
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("team", "channel", "conversationMember") $member.id
$identifiers = Add-Identifier $identifiers @("team", "conversationMember") $member.id

$installedApp = Invoke-RequestHelper -Uri "teams/$($team.id)/installedApps" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("team", "teamsAppInstallation") $installedApp.id

$drive = Invoke-RequestHelper -Uri "drives" |
    Where-Object { $_.createdBy.user.email -eq $user.email } |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("drive") $drive.id

$demoFilesFolder = Invoke-RequestHelper -Uri "drives/$($drive.id)/root/children" |
    Where-Object { $_.name -eq "Demo Files" } |
    Select-Object -First 1
$driveItem = Invoke-RequestHelper -Uri "drives/$($drive.id)/items/$($demoFilesFolder.id)/children" |
    Where-Object { $_.name -eq "Customer Data.xlsx" } |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("drive", "driveItem") $driveItem.id

$thumbnailSet = Invoke-RequestHelper -Uri "drives/$($drive.id)/items/$($driveItem.id)/thumbnails" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("drive", "thumbnailSet") $thumbnailSet.id

$permission = Invoke-RequestHelper -Uri "drives/$($drive.id)/items/$($driveItem.id)/permissions" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("driveItem", "permission") $permission.id

$application = Invoke-RequestHelper -Uri "applications" |
    Where-Object { $_.displayName -eq "PermissionManager" } |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("application") $application.id

$applicationTemplate = Invoke-RequestHelper -Uri "applicationTemplates?`$filter=(displayName eq 'Microsoft Teams')" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("applicationTemplate") $applicationTemplate.id

$directoryAudit = Invoke-RequestHelper -Uri "auditLogs/directoryAudits?`$top=1" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("directoryAudit") $directoryAudit.id

$signIn = Invoke-RequestHelper -Uri "auditLogs/signIns?`$top=1" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("signIn") $signIn.id

$directoryRole = Invoke-RequestHelper -Uri "directoryRoles" |
    Where-Object { $_.displayName -eq "Global Administrator" }
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("directoryRole") $directoryRole.id

$directoryRoleTemplate = Invoke-RequestHelper -Uri "directoryRoleTemplates" |
    Where-Object { $_.displayName -eq "Global Administrator" }
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("directoryRoleTemplate") $directoryRoleTemplate.id

$conversation = Invoke-RequestHelper -Uri "groups/$($team.id)/conversations" |
    Where-Object { $_.topic -eq "Mark 8 Project Sync" }
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("group", "conversation") $conversation.id

$conversationThread = Invoke-RequestHelper -Uri "groups/$($team.id)/conversations/$($conversation.id)/threads"
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("group", "conversationThread") $conversationThread.id

$post = Invoke-RequestHelper -Uri "groups/$($team.id)/conversations/$($conversation.id)/threads/$($conversationThread.id)/posts" |
    #Where-Object { $_.from.emailAddress.name -eq "U.S. Sales" } |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("group", "conversationThread", "post") $post.id

$conditionalAccessPolicy = Invoke-RequestHelper -Uri "identity/conditionalAccess/policies" |
    Where-Object { $_.displayName -eq "Office 365 App Control" }
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("conditionalAccessPolicy") $conditionalAccessPolicy.id

$oauth2PermissionGrant = Invoke-RequestHelper -Uri "oauth2PermissionGrants" |
    Where-Object { $_.scope.Trim() -eq "user_impersonation" } |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("oauth2PermissionGrant") $oauth2PermissionGrant.id

$organization = Invoke-RequestHelper -Uri "organization" |
    Where-Object { $_.displayName -eq "Contoso" } |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("organization") $organization.id

# id is not dynamic per tenant
$identifiers = Add-Identifier $identifiers @("permissionGrantPolicy") "microsoft-all-application-permissions"
$identifiers = Add-Identifier $identifiers @("secureScoreControlProfile") "OneAdmin"

$schemaExtension = Invoke-RequestHelper -Uri "schemaExtensions?`$filter=description eq 'Global settings'" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("schemaExtension") $schemaExtension.id

$subscribedSku = Invoke-RequestHelper -Uri "subscribedSkus" |
    Where-Object { $_.skuPartNumber -eq 'ENTERPRISEPACK'}
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("subscribedSku") $subscribedSku.id

$site = Invoke-RequestHelper -Uri "sites?search=site" |
    Where-Object { $_.displayName -eq 'The Landing' }
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("site") $site.id

$siteList = Invoke-RequestHelper -Uri "sites/$($site.id)/lists" |
    Where-Object {$_.displayName -eq "Demo Docs"}
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("site", "list") $siteList.id

$siteListItem = Invoke-RequestHelper -Uri "sites/$($site.id)/lists/$($siteList.id)/items" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("site", "list", "listItem") $siteListItem.id

$siteListItemVersion = Invoke-RequestHelper -Uri "sites/$($site.id)/lists/$($siteList.id)/items/$($siteListItem.id)/versions" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("site", "list", "listItem", "listItemVersion") $siteListItemVersion.id

$termStoreGroup =  Invoke-RequestHelper -Uri "sites/$($site.id)/termStore/groups" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("site", "termStore.group") $termStoreGroup.id

$termStoreSet =  Invoke-RequestHelper -Uri "sites/$($site.id)/termStore/groups/$($termStoreGroup.id)/sets" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("site", "termStore.set") $termStoreSet.id
$identifiers = Add-Identifier $identifiers @("site", "termStore.group", "termStore.set") $termStoreSet.id

$termStoreTerm = Invoke-RequestHelper -Uri "sites/$($site.id)/termStore/sets/$($termStoreSet.id)/terms" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("site", "termStore.set", "termStore.term") $termStoreTerm.id
$identifiers = Add-Identifier $identifiers @("site", "termStore.group", "termStore.set", "termStore.term") $termStoreTerm.id

$contentType = Invoke-RequestHelper -Uri "sites/$($site.id)/contentTypes?`$filter=name eq 'Phone Call Memo'" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("site", "contentType") $contentType.id

$columnDefinition = Invoke-RequestHelper -Uri "sites/$($site.id)/contentTypes/$($contentType.id)/columns?`$filter=displayName eq 'Title'" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("site", "contentType", "columnDefinition") $columnDefinition.id

#Missing Permission. Need to Create Permission on Root Site
#Azure AD Permission Issue.
#https://docs.microsoft.com/en-us/graph/api/site-post-permissions?view=graph-rest-1.0&tabs=http
# $sitePermission = Invoke-RequestHelper -Uri "sites/$($site.id)/permissions" |
#     Select-Object -First 1
# $sitePermission.id
# $identifiers.site.permission._value=$sitePermission.id

$servicePrincipal = Invoke-RequestHelper -Uri "servicePrincipals?`$filter=displayName eq 'PermissionManager'" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("servicePrincipal") $servicePrincipal.id

$permissionGrantPolicy = Invoke-RequestHelper -Uri "policies/permissionGrantPolicies" |
    Where-Object {$_.displayName -eq "All application permissions, for any client app"}
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("permissionGrantPolicy") $permissionGrantPolicy.id

#OData Invoke-RequestHelperuest is not Supported.
# $messageExtensions = Invoke-RequestHelper -Uri "users/$($identifiers.user._value)/messages/$($identifiers.message._value)/extensions" |
#     Select-Object -First 1
# $messageExtensions.id
# $identifiers.message.extension._value = $messageExtensions.id

$calendarGroup = Invoke-RequestHelper -Uri "users/$($identifiers.user._value)/calendarGroups" |
    Where-Object {$_.name -eq "My Calendars"}
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("calendarGroup") $calendarGroup.id

$orgContact = Invoke-RequestHelper -Uri "contacts" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("orgContact") $orgContact.id

#Outlook Categories are pre-defined https://docs.microsoft.com/en-us/graph/api/resources/outlookcategory?view=graph-rest-1.0
$outlookCategory = Invoke-RequestHelper -Uri "users/$($identifiers.user._value)/outlook/masterCategories" |
    Where-Object {$_.displayName -eq "Red Category"} |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("outlookCategory") $outlookCategory.id

$roleAssignmentPrincipal = Invoke-RequestHelper -Uri "/roleManagement/directory/roleAssignments?`$filter=roleDefinitionId eq '62e90394-69f5-4237-9190-012177145e10'" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("roleAssignmentPrincipal") $roleAssignmentPrincipal.principalId

$unifiedRoleAssignment = Invoke-RequestHelper -Uri "roleManagement/directory/roleAssignments?`$filter=principalId eq '$($user.id)'" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("unifiedRoleAssignment") $unifiedRoleAssignment.id

$unifiedRoleManagementPolicy = Invoke-RequestHelper "policies/roleManagementPolicies?`$filter=scopeId eq '/' and scopeType eq 'DirectoryRole'&`$top=1"
$identifiers = Add-Identifier $identifiers @("unifiedRoleManagementPolicy") $unifiedRoleManagementPolicy.id

$unifiedRoleManagementPolicyRule = Invoke-RequestHelper "policies/roleManagementPolicies/$($unifiedRoleManagementPolicy.id)/rules?`$top=1"
$identifiers = Add-Identifier $identifiers @("unifiedRoleManagementPolicy", "unifiedRoleManagementPolicyRule") $unifiedRoleManagementPolicyRule.id

$unifiedRoleManagementPolicyAssignment = Invoke-RequestHelper "policies/roleManagementPolicyAssignments?`$filter=scopeId eq '/' and scopeType eq 'Directory'&`$top=1"
$identifiers = Add-Identifier $identifiers @("unifiedRoleManagementPolicyAssignment") $unifiedRoleManagementPolicyAssignment.id

# existing constant value in the tenant
$identifiers = Add-Identifier $identifiers @("identityUserFlowAttribute") "city"

$serviceUpdateMessage = Invoke-RequestHelper -Uri "admin/serviceAnnouncement/messages" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("serviceUpdateMessage") $serviceUpdateMessage.id

$serviceHealthIssue = Invoke-RequestHelper -Uri "admin/serviceAnnouncement/issues?`$filter=status eq 'postIncidentReviewPublished'" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("serviceHealthIssue") $serviceHealthIssue.id

$riskyUser = Invoke-RequestHelper -Uri "identityProtection/riskyUsers" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("riskyUser") $riskyUser.id

$riskDetection = Invoke-RequestHelper -Uri "identityProtection/riskDetections" |
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("riskDetection") $riskDetection.id

$userEvent = Invoke-RequestHelper -Uri "users/$($identifiers.user._value)/calendar/events" |
    Where-Object { $_.recurrence -ne $null } | # need a recurring event for "get instances" API call
    Select-Object -First 1
$identifiers = Add-Identifier $identifiers @("event") $userEvent.id
$identifiers = Add-Identifier $identifiers @("user", "event") $userEvent.id
# existing constant value in the tenant
$identifiers = Add-Identifier $identifiers @("serviceHealth") "Exchange Online"

$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
