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

# Create External Connection https://docs.microsoft.com/en-us/graph/api/externalconnectors-external-post-connections?view=graph-rest-1.0&tabs=http
# Missing Permissions https://github.com/microsoftgraph/msgraph-sdk-raptor/issues/748
# $externalConnectionData = Get-RequestData -ChildEntity "ExternalConnection"
# $externalConnectionUrl =  "/external/connections"
# $currentExternalConnectionData = Invoke-RequestHelper -Uri $externalConnectionUrl -Method GET -ResponseHeadersVariable "script:ResponseHeaders" |
#     Where-Object { $_.id -eq $externalConnectionData.id } |
#     Select-Object -First 1

# if($null -eq $currentExternalConnectionData){
#     $currentExternalConnectionData = Invoke-RequestHelper -Uri $externalConnectionUrl -Method POST -Body $externalConnectionData -ResponseHeadersVariable "script:ResponseHeaders"
# }
# $identifiers = Add-Identifier $identifiers @("externalConnectors.externalConnection") $currentExternalConnectionData.id

# Create Schema https://docs.microsoft.com/en-us/graph/api/externalconnectors-schema-create?view=graph-rest-1.0&tabs=hclearttp
$schemaData = Get-RequestData -ChildEntity "Schema"
$schemaUrl =  "/external/connections/$($currentExternalConnectionData.id)/schema"
$currentSchema = Invoke-RequestHelper -Uri $schemaUrl -Method GET -ResponseHeadersVariable "script:ResponseHeaders" |
    Where-Object { $_.baseType -eq $schemaData.baseType } |
    Select-Object -First 1

if($null -eq $currentSchema){
    # Creating Schema Returns HTTP 202 Accepted with a URL to the operation
    Invoke-RequestHelper -Uri $schemaUrl -Method POST -Body $schemaData -ResponseHeadersVariable "script:ResponseHeaders"
    # Since Schema Creation
    $operationLocation = $ResponseHeaders["Location"]
    if($null -ne $operationLocation) {
        $operationLocationUri = [System.Uri]::new($operationLocation)
        $operationId = $operationLocationUri.Segments[$operationLocationUri.Segments.Length - 1]
        $identifiers = Add-Identifier $identifiers @("externalConnectors.externalConnection", "externalConnectors.connectionOperation") $operationId
    }
}

# Create externalGroup https://docs.microsoft.com/en-us/graph/api/externalconnectors-externalconnection-post-groups?view=graph-rest-1.0&tabs=http
$groupData = Get-RequestData -ChildEntity "Group"
$groupCreateUrl =  "/external/connections/$($currentExternalConnectionData.id)/groups"
$currentGroupUrl =  "$($groupCreateUrl)/$($groupData.id)"
# There is no endpoint to List ALL Groups. Use value stored in our data to check if it already exists.
$currentGroup = Invoke-RequestHelper -Uri $currentGroupUrl -Method GET -ResponseHeadersVariable "script:ResponseHeaders" |
    Select-Object -First 1

if($null -eq $currentGroup){
    $currentGroup = Invoke-RequestHelper -Uri $groupCreateUrl -Method POST -Body $groupData -ResponseHeadersVariable "script:ResponseHeaders"
}

$identifiers = Add-Identifier $identifiers @("externalConnectors.externalConnection", "externalConnectors.externalGroup") $currentGroup.id

$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
