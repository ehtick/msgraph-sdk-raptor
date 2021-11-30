# Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
Param(
    [string] $IdentifiersPath = (Join-Path $PSScriptRoot "../../../../msgraph-sdk-raptor-compiler-lib/identifiers.json" -Resolve)
)
$raptorUtils = Join-Path $PSScriptRoot "../../RaptorUtils.ps1" -Resolve
. $raptorUtils

$appSettings = Get-AppSettings
$identifiers = Get-CurrentIdentifiers -IdentifiersPath $IdentifiersPath

#Connect To Microsoft Graph Using ClientId, TenantId and Certificate in AppSettings
Connect-DefaultTenant -AppSettings $appSettings

#Create activityBasedTimeoutPolicy https://docs.microsoft.com/en-us/graph/api/activitybasedtimeoutpolicy-post-activitybasedtimeoutpolicies?view=graph-rest-1.0&tabs=http
$activityBasedTimeoutPolicyData = Get-RequestData -ChildEntity "ActivityBasedTimeoutPolicy"
$activityBaseTimeoutPolicyUrl = "policies/activityBasedTimeoutPolicies"
$currentActivityBasedTimeoutPolicyPolicy = Invoke-RequestHelper -Uri $activityBaseTimeoutPolicyUrl -Method GET |
        Where-Object { $_.displayName -eq $activityBasedTimeoutPolicyData.displayName } |
        Select-Object -First 1

if($null -eq $currentActivityBasedTimeoutPolicyPolicy){
    $currentActivityBasedTimeoutPolicyPolicy = Invoke-RequestHelper -Uri $activityBaseTimeoutPolicyUrl -Method POST -Body $activityBasedTimeoutPolicyData
    $currentActivityBasedTimeoutPolicyPolicy.id
}

$claimsMappingPolicyData = Get-RequestData -ChildEntity "ClaimsMappingPolicy"
$claimMappingPolicyUrl = "policies/claimsMappingPolicies"
$currentClaimsMappingPolicy = Invoke-RequestHelper -Uri $claimMappingPolicyUrl -Method GET |
        Where-Object { $_.displayName -eq $claimsMappingPolicyData.displayName } |
        Select-Object -First 1

if($null -eq $currentClaimsMappingPolicy){
    $currentClaimsMappingPolicy = Invoke-RequestHelper -Uri $claimMappingPolicyUrl -Method POST -Body $claimsMappingPolicyData
    $currentClaimsMappingPolicy.id
}

$homeRealmDiscoveryPolicyData = Get-RequestData -ChildEntity "HomeRealmDiscoveryPolicy"
$homeRealDiscoveryPolcyUrl = "policies/homeRealmDiscoveryPolicies"
$currentHomeRealmDiscoveryPolicyData = Invoke-RequestHelper -Uri $homeRealDiscoveryPolcyUrl -Method GET |
        Where-Object { $_.displayName -eq $homeRealmDiscoveryPolicyData.displayName } |
        Select-Object -First 1

if($null -eq $currentHomeRealmDiscoveryPolicyData){
    $currentHomeRealmDiscoveryPolicyData = Invoke-RequestHelper -Uri $homeRealDiscoveryPolcyUrl -Method POST -Body $homeRealmDiscoveryPolicyData
    $currentHomeRealmDiscoveryPolicyData.id
}

$tokenIssuancePolicyData = Get-RequestData -ChildEntity "TokenIssuancePolicy"
$tokenIssuancePolicyUrl = "policies/tokenIssuancePolicies"
$currentTokenIssuancePolicy = Invoke-RequestHelper -Uri $tokenIssuancePolicyUrl -Method GET |
        Where-Object { $_.displayName -eq $tokenIssuancePolicyData.displayName } |
        Select-Object -First 1

if($null -eq $currentTokenIssuancePolicy){
    $currentTokenIssuancePolicy = Invoke-RequestHelper -Uri $tokenIssuancePolicyUrl -Method POST -Body $tokenIssuancePolicyData
    $currentTokenIssuancePolicy.id
}
#Create tokenLifetimePolicy https://docs.microsoft.com/en-us/graph/api/tokenlifetimepolicy-post-tokenlifetimepolicies?view=graph-rest-1.0&tabs=http
$tokenLifetimePolicyData = Get-RequestData -ChildEntity "TokenLifetimePolicy"
$tokenLifeTimePolicyUrl = "policies/tokenLifetimePolicies"
$currentTokenLifetimePolicy = Invoke-RequestHelper -Uri $tokenLifeTimePolicyUrl -Method GET |
        Where-Object { $_.displayName -eq $tokenLifetimePolicyData.displayName } |
        Select-Object -First 1

if($null -eq $currentTokenLifetimePolicy){
    $currentTokenLifetimePolicy = Invoke-RequestHelper -Uri $tokenLifeTimePolicyUrl -Method POST -Body $tokenLifetimePolicyData
    $currentTokenLifetimePolicy.id
}

#Create featureRolloutPolicy https://docs.microsoft.com/en-us/graph/api/featurerolloutpolicies-post?view=graph-rest-1.0&tabs=http
#Required Delegated Permission "Directory.ReadWrite.All"
$featureRolloutPolicyData = Get-RequestData -ChildEntity "FeatureRolloutPolicy"
$featureRolloutPolicyUrl = "policies/featureRolloutPolicies"
$currentFeatureRolloutPolicy = Request-DelegatedResource -Uri $featureRolloutPolicyUrl -Method GET -ScopeOverride "Directory.ReadWrite.All" |
        Where-Object { $_.displayName -eq $featureRolloutPolicyData.displayName } |
        Select-Object -First 1

if($null -eq $currentFeatureRolloutPolicy){
    $currentFeatureRolloutPolicy = Request-DelegatedResource -Uri $featureRolloutPolicyUrl -Method POST -Body $featureRolloutPolicyData -ScopeOverride "Directory.ReadWrite.All"
    $currentFeatureRolloutPolicy.id
}

$identifiers.activityBasedTimeoutPolicy._value = $currentActivityBasedTimeoutPolicyPolicy.id
$identifiers.claimsMappingPolicy._value = $currentClaimsMappingPolicy.id
$identifiers.homeRealmDiscoveryPolicy._value = $currentHomeRealmDiscoveryPolicyData.id
$identifiers.tokenIssuancePolicy._value = $currentHomeRealmDiscoveryPolicyData.id
$identifiers.tokenLifetimePolicy._value = $currentTokenLifetimePolicy.id
$identifiers.featureRolloutPolicy._value = $currentFeatureRolloutPolicy.id

$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath