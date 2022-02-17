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

# Get Agreements https://docs.microsoft.com/en-us/graph/api/agreement-list?view=graph-rest-1.0&tabs=http
$agreement = Get-RequestData -ChildEntity "Agreement"
$currentAgreement = Request-DelegatedResource -Uri "identityGovernance/termsOfUse/agreements" -Method GET |
    Where-Object { $_.displayName -eq $agreement.displayName } |
    Select-Object -First 1

if($null -eq $currentAgreement){
    $currentAgreement = Request-DelegatedResource -Uri "identityGovernance/termsOfUse/agreements" -Method POST -Body $agreement
}

$identifiers = Add-Identifier $identifiers @("agreement") $currentAgreement.id

# Get identityProvider  https://docs.microsoft.com/en-us/graph/api/resources/identityprovider?view=graph-rest-1.0
# This API has been DEPRECATED
# Only Supports Delegated Permissions
$identityProvider = Get-RequestData -ChildEntity "IdentityProvider"
$currentIdentityProvider = Request-DelegatedResource -Uri "identityProviders" -Method GET |
    Where-Object { $_.type -eq $identityProvider.type} |
    Select-Object -First 1

# To avoid storing Secrets in Files, Generate them on the fly.
if($null -eq $currentIdentityProvider){
    $clientId = New-Guid
    $identityProvider.clientId = $clientId.ToString()
    $identityProvider.clientSecret = Get-RandomAlphanumericString -length 10

    $currentIdentityProvider = Request-DelegatedResource -Uri "identityProviders" -Method POST -Body $identityProvider -Debug
}

$identifiers = Add-Identifier $identifiers @("identityProvider") $currentIdentityProvider.id

# Get IdentityProviderBase  https://docs.microsoft.com/en-us/graph/api/identityproviderbase-get?view=graph-rest-1.0&tabs=csharp
$identityProviderBase = Get-RequestData -ChildEntity "IdentityProviderBase"
$currentIdentityProviderBase = Invoke-RequestHelper -Uri "identity/identityProviders" -Method GET |
    Where-Object { $_.type -eq $identityProviderBase.type} |
    Select-Object -First 1

# To avoid storing Secrets in Files, Generate them on the fly.
if($null -eq $currentIdentityProviderBase){
    $identityProviderBase.clientId = New-Guid
    $identityProviderBase.clientSecret = Get-RandomAlphanumericString -length 10
    $currentIdentityProviderBase = Invoke-RequestHelper -Uri "identity/identityProviders" -Method POST -Body $identityProviderBase
}

$identifiers = Add-Identifier $identifiers @("identityProviderBase") $currentIdentityProviderBase.id

# Get certificateBasedAuthConfiguration https://docs.microsoft.com/en-us/graph/api/certificatebasedauthconfiguration-get?view=graph-rest-1.0&tabs=http
$certificateBasedAuthConfiguration = Get-RequestData -ChildEntity "CertificateBasedAuthConfiguration"
$currentCertificateBasedAuthConfiguration = Request-DelegatedResource -Uri "organization/$($identifiers.organization._value)/certificateBasedAuthConfiguration" -Method GET |
    Select-Object -First 1

if($null -eq $currentCertificateBasedAuthConfiguration || $currentCertificateBasedAuthConfiguration.value.Count -lt 1){
    $selfSignedCertificate = New-Certificate
    $certificateBasedAuthConfiguration.certificateAuthorities[0].certificate = $selfSignedCertificate
    $currentCertificateBasedAuthConfiguration = Request-DelegatedResource -Uri "organization/$($identifiers.organization._value)/certificateBasedAuthConfiguration" -Method POST -Body $certificateBasedAuthConfiguration
}

$identifiers = Add-Identifier $identifiers @("organization", "certificateBasedAuthConfiguration") $currentCertificateBasedAuthConfiguration.id

# Get organizationalBrandingProperties https://docs.microsoft.com/en-us/graph/api/organizationalbrandingproperties-create?view=graph-rest-1.0&tabs=http
$organizationalBrandingProperties = Get-RequestData -ChildEntity "Branding"
$currentOrganizationalBrandingProperties = Request-DelegatedResource -Uri  "organization/$($identifiers.organization._value)/branding" -Method PATCH -Body $organizationalBrandingProperties
$currentOrganizationalBrandingProperties.id

# Get organizationalBrandingProperties https://docs.microsoft.com/en-us/graph/api/organizationalbrandingproperties-create?view=graph-rest-1.0&tabs=http
# French
$organizationalBrandingPropertiesLocalizationsFrench = Get-RequestData -ChildEntity "LocalizationsFrench"
$currentOrganizationalBrandingPropertiesLocalizationsFrench = Request-DelegatedResource -Uri "organization/$($identifiers.organization._value)/branding/localizations/$($organizationalBrandingPropertiesLocalizationsFrench.id)" -Method GET

if($null -eq $currentOrganizationalBrandingPropertiesLocalizationsFrench){
    $currentOrganizationalBrandingPropertiesLocalizationsFrench = Request-DelegatedResource -Uri  "organization/$($identifiers.organization._value)/branding/localizations" -Method POST -Body $organizationalBrandingPropertiesLocalizationsFrench
}
#Update French localization to have a banner required in beta execution tests.
if (!$currentOrganizationalBrandingPropertiesLocalizationsFrench.bannerLogoRelativeUrl){
    $bannerFilePath = Join-Path $PSScriptRoot ".\bannerLogo.png" -Resolve
    if(Test-Path -Path $bannerFilePath){
        $bannerLogoStream =[System.Convert]::ToBase64String([System.IO.File]::ReadAllBytes($bannerFilePath))
        $fileTypeHeader = @{"Content-Type" = "image/jpeg"}
        Request-DelegatedResource -Uri  "organization/$($identifiers.organization._value)/branding/localizations/$($currentOrganizationalBrandingPropertiesLocalizationsFrench.id)/bannerLogo" -Method PUT -Body $bannerLogoStream -Headers $fileTypeHeader -ScopeOverride "Organization.ReadWrite.All"
    }
    else{
        Write-Error "Image file path for banner logo couldn't be found"
    }
}

# German
$organizationalBrandingPropertiesLocalizationsGerman = Get-RequestData -ChildEntity "LocalizationsGerman"
$currentOrganizationalBrandingPropertiesLocalizationsGerman = Request-DelegatedResource -Uri "organization/$($identifiers.organization._value)/branding/localizations/$($organizationalBrandingPropertiesLocalizationsGerman.id)" -Method GET

if($null -eq $currentOrganizationalBrandingPropertiesLocalizationsGerman){
    $currentOrganizationalBrandingPropertiesLocalizationsGerman = Request-DelegatedResource -Uri  "organization/$($identifiers.organization._value)/branding/localizations" -Method POST -Body $organizationalBrandingPropertiesLocalizationsGerman
}
$identifiers = Add-Identifier $identifiers @("organization", "organizationalBrandingLocalization") $currentOrganizationalBrandingPropertiesLocalizationsGerman.id

#Update German localization to have a banner required in execution tests.
if (!$currentOrganizationalBrandingPropertiesLocalizationsGerman.bannerLogoRelativeUrl){
    $bannerFilePath = Join-Path $PSScriptRoot ".\bannerLogo.png" -Resolve
    if(Test-Path -Path $bannerFilePath){
        $bannerLogoStream =[System.Convert]::ToBase64String([System.IO.File]::ReadAllBytes($bannerFilePath))
        $fileTypeHeader = @{"Content-Type" = "image/jpeg"}
        Request-DelegatedResource -Uri  "organization/$($identifiers.organization._value)/branding/localizations/$($organizationalBrandingPropertiesLocalizationsGerman.id)/bannerLogo" -Method PUT -Body $bannerLogoStream -Headers $fileTypeHeader -ScopeOverride "Organization.ReadWrite.All"
    }
    else{
        Write-Error "Image file path for banner logo couldn't be found"
    }
}

# Create Connected Organization https://docs.microsoft.com/en-us/graph/api/entitlementmanagement-post-connectedorganizations?view=graph-rest-1.0&tabs=http
$connectedOrganizationData = Get-RequestData -ChildEntity "ConnectedOrganization"
$connectedOrganizationUrl = "identityGovernance/entitlementManagement/connectedOrganizations"
$currentConnectedOrganization = Invoke-RequestHelper -Uri $connectedOrganizationUrl -Method GET |
    Where-Object { $_.displayName -eq $connectedOrganizationData.displayName } |
    Select-Object -First 1

if($null -eq $currentConnectedOrganization){
    $currentConnectedOrganization = Invoke-RequestHelper -Uri $connectedOrganizationUrl -Method POST -Body $connectedOrganizationData
}

$identifiers = Add-Identifier $identifiers @("connectedOrganization") $currentConnectedOrganization.id

$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
