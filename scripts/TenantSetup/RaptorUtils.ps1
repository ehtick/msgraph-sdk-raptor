# Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.

using namespace System.Diagnostics.CodeAnalysis
[SuppressMessageAttribute('PSAvoidUsingConvertToSecureStringWithPlainText', '', Scope='Function', Target='Get-HtmlDataRequest')]
[SuppressMessageAttribute('PSAvoidUsingConvertToSecureStringWithPlainText', '', Scope='Function', Target='Connect-AzureTenant')]
[SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Scope='Function', Target='Remove-PemHeaderOrFooter')]
[SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Scope='Function', Target='New-Certificate')]
[SuppressMessageAttribute('PSUseSingularNouns', '', Scope='Function', Target='Get-CurrentIdentifiers')]
[SuppressMessageAttribute('PSUseSingularNouns', '', Scope='Function', Target='Get-AppSettings')]
[SuppressMessageAttribute('PSAvoidGlobalVars', '', Scope='Function', Target='Get-Certificate')]
[SuppressMessageAttribute('PSUseSingularNouns', '', Scope='Function', Target='Get-Scopes')]
[SuppressMessageAttribute('PSAvoidGlobalVars', '')]
param()

function Get-AppSettings ()
{
    # read app settings from Azure App Config
    $appSettingsPath = "$env:TEMP/appSettings.json"
    # Support Reading Settings from a Custom Label, otherwise default to Development
    $settingsLabel = $env:RAPTOR_CONFIGLABEL
    if ([string]::IsNullOrWhiteSpace($settingsLabel))
    {
        $settingsLabel = "Development"
    }
    az appconfig kv export --connection-string $env:RAPTOR_CONFIGCONNECTIONSTRING --label $settingsLabel --destination file --path $appSettingsPath --format json --yes
    $appSettings = Get-Content $AppSettingsPath -Raw | ConvertFrom-Json
    Remove-Item $appSettingsPath

    if (    !$appSettings.CertificateThumbprint `
            -or !$appSettings.ClientID `
            -or !$appSettings.Username `
            -or !$appSettings.Password `
            -or !$appSettings.TenantID)
    {
        Write-Error -ErrorAction Stop -Message "please provide CertificateThumbprint, ClientID, Username, Password and TenantID in appsettings.json"
    }
    return $appSettings
}

function Get-CurrentIdentifiers (
    [string] $IdentifiersPath = (Join-Path $MyInvocation.PSScriptRoot "../../../../TestsCommon/identifiers.json")
)
{
    $identifiers = Get-Content $IdentifiersPath -Raw | ConvertFrom-Json
    return $identifiers
}

function Get-CurrentDomain (
    [PSObject]$AppSettings,
    [Bool]$IsEducation=$false
)
{
    $username = $IsEducation ? $AppSettings.EducationUsername : $AppSettings.Username
    $domain = $username.Split("@")[1]
    return $domain
}


<#
   Assumes that data is Stored in the following format:-
   Entity
        - ChildEntity.json
        - ChildEntity.json
    Such as:-
    Team
        - OpenShift.json
        - Schedule.json
    Based on the Tree Structure in Identifiers.json
#>
function Get-RequestData (
    [string] $ChildEntity
)
{
    $entityPath = Join-Path $MyInvocation.PSScriptRoot "./$($ChildEntity).json"
    $data = Get-Content -Path $entityPath -Raw | ConvertFrom-Json -AsHashtable
    return $data
}


<#
    Helpers handles:-
        1. GraphVersion,
        2. MS-APP-ACTS-AS Headers
        3. Content-Type header
        4. HttpMethod

    Basic Validation of Parameters
#>
function Invoke-RequestHelper (
    [string] $Uri,
    [parameter(Mandatory = $False)][ValidateSet("v1.0", "beta")][string] $GraphVersion = "v1.0",
    [Parameter(Mandatory = $False)][ValidateSet("GET", "POST", "PUT", "PATCH", "DELETE")][string] $Method = "GET",
    $Headers = @{ },
    $Body,
    $User,
    [string]$ResponseHeadersVariable = "script:ResponseHeaders"
)
{
    #Append Content-Type to headers collection
    #Append "MS-APP-ACTS-AS" to headers collection
    $headers += @{ "Content-Type" = "application/json" }
    if ($null -ne $User)
    {
        $headers += @{"MS-APP-ACTS-AS" = $User }
    }
    #Convert Body to Json
    $jsonData = $body | ConvertTo-Json -Depth 3

    $response = Invoke-MgGraphRequest -Headers $headers -Method $Method -Uri "https://graph.microsoft.com/$GraphVersion/$Uri" -Body $jsonData -OutputType PSObject -ResponseHeadersVariable $ResponseHeadersVariable

    return $response.value ?? $response
}

<#
    Get a token for the configured user from Azure AD with the specified Scope
#>
function Get-UserToken
{
    param(
        $AppSettings,
        $Application,
        $ScopeString,
        $GrantType = "password",
        $IsEducation = $false
    )

    $domain = Get-CurrentDomain -AppSettings $AppSettings -IsEducation $IsEducation
    $tokenEndpoint = "https://login.microsoftonline.com/$domain/oauth2/v2.0/token"
    try
    {
        ($username, $password) = $IsEducation ? ($AppSettings.EducationUsername, $AppSettings.EducationPassword) : ($AppSettings.Username, $AppSettings.Password)
        $body = "grant_type=$GrantType&username=$($username)&password=$($password)&client_id=$($Application.ApplicationIdentifier)&scope=$($ScopeString)"
        $token = Invoke-RestMethod -Method Post -Uri $tokenEndpoint -Body $body -ContentType 'application/x-www-form-urlencoded'

        Write-Debug "Received Token with the following Scopes"
        foreach ($scope in $token.scope.Split())
        {
            Write-Debug "\t\t$scope"
        }
        return $token
    } catch
    {
        Write-Error $_
        throw
    }
}

<#
    Get a Registered Delegated Appplication from Tenant.
    Apps are registered as 'DelegatedApp {Scope Name}` such as `DelegatedApp ChannelMember.ReadWrite.All`.
    If Application cannot be found or scope was determined to be .default, use our Raptor Default Client
#>
function Get-Application
{
    param(
        $AppSettings,
        [string]$joinedScopeString,
        [string]$Scope,
        [Bool] $IsEducation = $false
    )
    #Connect using the current Application stored in settings
    Connect-Tenant -IsEducation $IsEducation
    $application = @{}
    if ($joinedScopeString -eq ".default")
    {
        $application.ApplicationIdentifier = $IsEducation ? $AppSettings.EducationClientID : $AppSettings.ClientID
        $application.DisplayName = "Raptor Default Client"
    } else
    {
        #Assume for now that its not default scope (.default)
        $scopeFilter = $Scope
        $filterParam = "DelegatedApp $($scopeFilter)"
        $filterQuery = "displayName eq '$($filterParam)' "
        $requiredApplication = Get-MgApplication -Filter $filterQuery
        $application.ApplicationIdentifier = $requiredApplication.AppId
        $application.DisplayName = $requiredApplication.DisplayName
    }
    return $application
}

<#
    Get Scopes from DevX API
#>
function Get-Scopes
{
    param (
        [Parameter(Mandatory = $False)][ValidateSet("GET", "POST", "PUT", "PATCH", "DELETE")][string] $Method = "GET",
        [Parameter(Mandatory = $True)][string] $Path
    )
    $scopes = @()
    if ($Path[0] -ne "/") # DevX API expects a leading slash
    {
        $Path = "/$Path"
    }

    try
    {
        $graphExplorerApi = "https://graphexplorerapi.azurewebsites.net/permissions?requesturl=$Path&method=$Method"
        $scopes = Invoke-RestMethod -Method "GET" -Uri $graphExplorerApi
    } catch
    {
        Write-Error $_
        Write-Warning "No Scopes returned for $Path with Method $Method"
    }
    return $scopes
}

<#
    Produces a Scope String from provided scope array
    Give ("Files.Read", "Files.ReadWrite") => "Files.Read Files.ReadWrite"
    If no scopes are provides, returns ".default"
#>
function Get-ScopeString
{
    param (
        [Parameter(Mandatory = $False)] $Scopes = @()
    )
    $joinedScopeString = ""
    if ($null -eq $Scopes -or ($Scopes.Count -eq 1 -and $Scopes[0].value -eq "Not supported."))
    {
        $joinedScopeString = ".default"
    } else
    {
        $joinedScopeString = $Scopes.value |
            Join-String -Separator " "
    }
    return $joinedScopeString
}

<#
    Gets a delegated access resource
    Handles:
        - pre-appending the $Uri with a forward slash at the beginning
        - converting passed in powershell object to json object for the request
        - Adding the content-type: {"application/json"} header to request headers
        - Requesting an auth token for the delegated permission
    Returns:
        - http response.value for odata collections or
        - http response if response is a single item
        - http response headers if the first two options return $null.
#>
Function Request-DelegatedResource
{
    param(
        [Parameter(Mandatory = $True)][string] $Uri,
        [Parameter(Mandatory = $False)] $Body,
        [ValidateSet("GET", "POST", "PUT", "PATCH", "DELETE")][string] $Method = "GET",
        [parameter(Mandatory = $False)][string] $ScopeOverride,
        [parameter(Mandatory = $False)][ValidateSet("v1.0", "beta")][string] $GraphVersion = "v1.0",
        [parameter(Mandatory = $False)][ValidateSet("Json", "PSObject", "HttpRequestMessage", "HashTable")][string] $OutputType = "PSObject",
        [Parameter(Mandatory=$False)][Bool] $IsEducation=$False,
        $Headers = @{ },
        $FilePath,
        $AppSettings
    )
    if ($null -eq $AppSettings)
    {
        $AppSettings = Get-AppSettings
    }
    # If content-type not specified assume application/json
    if (!$headers.ContainsKey("Content-Type"))
    {
        $Headers += @{ "Content-Type" = "application/json" }
    }
    $joinedScopeString = ""
    $devxScopes = @()
    $flattenedScopes = @()
    # When Override is set, skip fetching scopes from DeVX API
    if (-not [string]::IsNullOrWhiteSpace($ScopeOverride))
    {
        $joinedScopeString = $ScopeOverride
        $flattenedScopes += $ScopeOverride
    } else
    {
        $devxScopes = Get-Scopes -Path "$Uri" -Method $Method
        $flattenedScopes = $devxScopes.value
        $joinedScopeString = Get-ScopeString -Scopes $devxScopes
        # If the JoinedScopeString is .default, currentScopes will be empty, insert that as the first and only scope.
        if ($joinedScopeString -eq ".default")
        {
            $flattenedScopes += $joinedScopeString
        }
    }
    foreach ($currentScope in $flattenedScopes)
    {
        # If JoinedScopeString is .default, we use the Default Raptor Client ID configured in AppSettings.
        # It implies we couldn't get a corresponding app for the specified permission.
        $application = Get-Application -AppSettings $AppSettings -joinedScopeString $joinedScopeString -Scope $currentScope -IsEducation $IsEducation
        try
        {
            $userToken = Get-UserToken -AppSettings $AppSettings -IsEducation $IsEducation -Application $application -ScopeString $currentScope
            if ($null -ne $userToken)
            {
                Connect-MgGraph -AccessToken $userToken.access_token | Out-Null
                $jsonBody = $Body | ConvertTo-Json -Depth 5
                if ($FilePath -and (Test-Path -Path $FilePath))
                {
                    # provide -InputFilePath param instead of -Body param
                    $response = Invoke-MgGraphRequest -Method $Method -Headers $Headers -Uri "https://graph.microsoft.com/$GraphVersion/$Uri" -InputFilePath $FilePath -ResponseHeadersVariable "responseHeaderValue" -OutputType $OutputType
                } else
                {
                    $response = Invoke-MgGraphRequest -Method $Method -Headers $Headers -Uri "https://graph.microsoft.com/$GraphVersion/$Uri" -Body $jsonBody -ResponseHeadersVariable "responseHeaderValue" -OutputType $OutputType
                }
                $responseBody = $response.value -is [System.Array] ? $response.value : $response
                return $responseBody ?? $responseHeaderValue
            }
        } catch
        {
            $currentApplicationName = $application.DisplayName
            $currentApplicationIdentifier = $application.DisplayName
            Write-Warning "Request Failed for $Uri with Scope: $mergedScope Current RegisteredApp:$currentApplicationIdentifier=>:$currentApplicationName"
            Write-Warning $_
            continue
        }
    }
}

Function Get-RandomAlphanumericString
{
    [CmdletBinding()]
    Param (
        [int] $length
    )
    $randString = ""; do
    { $randString = $randString + ((0x30..0x39) + (0x41..0x5A) + (0x61..0x7A) | Get-Random | ForEach-Object { [char]$_ })
    } until ($randString.length -eq $length)
    return $randString
}


Function Get-EduAdmin
{
    $eduAdmin = "admin"
    $adminUser = Invoke-RequestHelper -Uri "users?`$filter=mailNickname eq  '$($eduAdmin)'"
    return $adminUser
}

Function Get-DefaultAdminUser
{
    Connect-Tenant
    $admin = "MOD Administrator"
    $adminUser = Invoke-RequestHelper -Uri "users?`$filter=displayName eq '$($admin)'"
    return $adminUser
}


Function Get-DefaultAdminUserId
{
    $user = Get-DefaultAdminUser
    return $user.id
}


Function New-Certificate
{
    $selfSignedCert = New-SelfSignedCertificate -Type Custom -NotAfter (Get-Date).AddYears(2) -Subject "CN=Microsoft,O=Microsoft Corp,L=Redmond,ST=Washington,C=US" -CertStoreLocation "Cert:\CurrentUser\My"
    $exportedCert = [System.Convert]::ToBase64String($selfSignedCert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert), [System.Base64FormattingOptions]::InsertLineBreaks)
    return $exportedCert
}

Function Remove-PemHeaderOrFooter
{
    [CmdletBinding()]
    Param (
        [string] $pemInput
    )
    $headerAndFooterList = @(
        "-----BEGIN CERTIFICATE-----",
        "-----END CERTIFICATE-----"
    )
    $trimmed = $pemInput
    foreach ($headerOrFooter  in $headerAndFooterList)
    {
        $trimmed = $trimmed.Replace($headerOrFooter, [string]::Empty)
    }
    return $trimmed.Replace("\r\n", [string]::Empty)
}

function Install-Az()
{
    if (-not (Get-Module Az -ListAvailable))
    {
        Install-Module Az -Force -AllowClobber -Scope CurrentUser
    }
}

<#
    Executes a HTTP Request where content-type is Form-Data
    as required by some graph endpoints such as OneNote Create Page
#>
Function Invoke-FormDataRequest
{
    [CmdletBinding()]
    param (
        $FormData = @(),
        [string] $FormBoundary = [guid]::NewGuid().ToString(),
        [string] $Uri,
        [parameter(Mandatory = $False)][ValidateSet("v1.0", "beta")][string] $GraphVersion = "v1.0",
        [Parameter(Mandatory = $False)][ValidateSet("POST", "PUT", "PATCH", "DELETE")][string] $Method = "POST",
        $Headers = @{ }
    )
    $bodyLines = [System.Collections.ArrayList]::new()

    $FormData | ForEach-Object {
        $currentFormData = $_
        $data = @(
            "--$FormBoundary",
            "Content-Disposition:form-data; name=`"$($currentFormData.Name)`"",
            "Content-Type:$($currentFormData.ContentType)",
            [System.Environment]::NewLine
            $currentFormData.Content,
            [System.Environment]::NewLine
        )
        $bodyLines.AddRange($data)
    }
    $bodyLines.Add("--$FormBoundary--");
    $postFormData = $bodyLines -join [System.Environment]::NewLine
    $Headers += @{ "Content-Type" = "multipart/form-data; boundary=$($FormBoundary)" }
    $formPostResults = Invoke-MgGraphRequest -Uri  "https://graph.microsoft.com/$GraphVersion/$Uri" -Method $Method -Headers $Headers -Body $postFormData
    return $formPostResults
}

<#
    Gets Html Data using Invoke-WebRequest and
    the token from the current Graph Session.
#>
Function Get-HtmlDataRequest
{
    [CmdletBinding()]
    param (
        [string] $Uri,
        [parameter(Mandatory = $False)][ValidateSet("v1.0", "beta")][string] $GraphVersion = "v1.0",
        $GraphSession = [Microsoft.Graph.PowerShell.Authentication.GraphSession]::Instance
    )

    $encoding = [System.Text.Encoding]::GetEncoding("utf-8")
    $MSALToken = $encoding.GetString($GraphSession::Instance.MSALToken)
    $currentAccessToken = ConvertFrom-Json $MSALToken -AsHashtable
    $token = $currentAccessToken.AccessToken.Values.secret
    $htmlData = Invoke-WebRequest -Uri "https://graph.microsoft.com/$GraphVersion/$Uri" -Authentication Bearer -Token (ConvertTo-SecureString $token -AsPlainText -Force)
    return $htmlData
}


<#
    Connect to the Raptor Tenant
#>
Function Connect-Tenant
{
    [CmdletBinding()]
    param(
        [PSObject] $AppSettings,
        [Bool]$IsEducation = $false
    )
    if ($null -eq $AppSettings)
    {
        $AppSettings = Get-AppSettings
    }
    ($tenantID, $clientId) = $IsEducation ? ($AppSettings.EducationTenantID, $AppSettings.EducationClientID)
    : ($AppSettings.TenantID, $AppSettings.ClientID);
    $defaultCertificate = Get-Certificate -AppSettings $AppSettings
    #Connect To Microsoft Graph Raptor Default Tenant Using ClientId, TenantId and Certificate
    Connect-MgGraph -Certificate $defaultCertificate -ClientId $clientId -TenantId $tenantID | Out-Null
}


<#
    Helper function to Connect to the Education Raptor Tenant
#>
Function Connect-EduTenant
{
    [CmdletBinding()]
    param(
        [PSObject] $AppSettings
    )
    Connect-Tenant -IsEducation $true -AppSettings $AppSettings
}


<#
    Helper function to Connect to the Regular Raptor Tenant
#>
Function Connect-DefaultTenant
{
    [CmdletBinding()]
    param(
        [PSObject] $AppSettings
    )
    Connect-Tenant -AppSettings $AppSettings
}


<#
    Connect to Azure Tenant to Access KeyVault and other resources.
#>
Function Connect-AzureTenant
{
    [CmdletBinding()]
    param(
        [PSObject] $AppSettings
    )

    $AzureTenantID = $AppSettings.AzureTenantID
    $AzureApplicationID = $AppSettings.AzureApplicationID
    $AzureClientSecret = $AppSettings.AzureClientSecret

    $securePassword = ConvertTo-SecureString -String $AzureClientSecret -AsPlainText -Force

    $Credential = New-Object -TypeName PSCredential -ArgumentList $AzureApplicationID, $securePassword
    Connect-AzAccount -Credential $Credential -Tenant $AzureTenantID -ServicePrincipal | Out-Null
}

<#
    Get Certificate from Azure KeyVault
#>
$global:DefaultCertificate = $null
Function Get-Certificate
{
    [CmdletBinding()]
    param(
        [PSObject] $AppSettings
    )
    if ($null -eq $global:DefaultCertificate)
    {
        Connect-AzureTenant -AppSettings $AppSettings
        # Certificate must be downloaded as a Secret instead of a Certificate to bring down the PrivateKey as well.
        $keyVaultCertSecret = Get-AzKeyVaultSecret -VaultName $AppSettings.AzureKeyVaultName -Name $AppSettings.CertificateName
        # Convert the Secret Value in the response      to plainText
        $secureCertData = ConvertFrom-SecureString -SecureString $keyVaultCertSecret.SecretValue -AsPlainText
        # KeyVault Secrets are Base64 Encoded, thus decode.
        $base64CertData = [Convert]::FromBase64String($secureCertData)
        # Create an In-Memory cert from Cert Data
        $pfxCertificate = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 -ArgumentList @($base64CertData, "", [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable)
        $global:DefaultCertificate = $pfxCertificate
    }
    return $global:DefaultCertificate
}
<#
    Uses Write-Output to write text with color
#>
Function Write-ColorOutput
{
    param(
        [System.ConsoleColor] $ForegroundColor = $host.UI.RawUI.ForegroundColor
    )

    # Get Current UI Color
    $originalColor = $host.UI.RawUI.ForegroundColor

    # Set the new UI Color
    $host.UI.RawUI.ForegroundColor = $ForegroundColor

    # Handle Input
    if ($args)
    {
        Write-Output $args
    } else
    {
        $input | Write-Output
    }

    # Restore Current UI to original color
    $host.UI.RawUI.ForegroundColor = $originalColor
}

<#
    Adds a new identifier into the identifiers object
    To add identifiers->site->list->_value to "newValue"
      Add-Identifier $identifiers @("site", "list") "newValue"
#>
function Add-Identifier ($identifiers, [string[]]$path, $value) {
    if ($null -eq $path) {
        return $identifiers
    }

    if ([string]::IsNullOrEmpty($value)) {
        Write-Error "[EMPTY VALUE] $([string]::join('->', $path))"
        return $identifiers
    }

    $current = $identifiers
    foreach ($p in $path) {
        if ($null -eq $current."$p") { # create path if it doesn't exist
            $pObject = New-Object PSObject
            $pObject | Add-Member -NotePropertyName "_value" -NotePropertyValue ""
            $current | Add-Member -NotePropertyName $p -NotePropertyValue $pObject
        }
        $current = $current."$p"
    }

    $current._value = $value

    Write-Host "[ADDED] $([string]::join('->', $path)) = $value" -ForegroundColor Cyan

    return $identifiers
}

<#
    Wake up CallBack Validation Site
#>
function Invoke-CallbackSiteWakeup{
    [CmdletBinding()]
    param (
        [Parameter()]
        [string]
        $CallBackSiteUrl = "https://raptorcallbacks.azurewebsites.net/Values"
    )
    Invoke-RestMethod -Uri $CallBackSiteUrl -Method POST
}
