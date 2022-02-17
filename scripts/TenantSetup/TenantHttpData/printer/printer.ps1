# Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
Param(
    [string] $IdentifiersPath = (Join-Path $PSScriptRoot "../../../../TestsCommon/identifiers.json" -Resolve)
)

$raptorUtils = Join-Path $PSScriptRoot "../../RaptorUtils.ps1" -Resolve
. $raptorUtils

$identifiers = Get-CurrentIdentifiers -IdentifiersPath $IdentifiersPath
$appSettings = Get-AppSettings

# Get printer
$printerData = Get-RequestData -ChildEntity "printer"
$printer = Request-DelegatedResource -Uri "print/printers/?`$filter=displayName eq '$($printerData.displayName)'&`$top=1" -ScopeOverride "Printer.Read.All"
if ($printer){
    $printer_id = $printer.id
}
else{
    # if printer is $null, Create printer
    Connect-AzureTenant -AppSettings $appSettings
    $storedSecretContent = Get-AzKeyVaultSecret -VaultName $appSettings.AzureKeyVaultName -Name "csrData" -AsPlainText
    $storedSecretTransportKey = Get-AzKeyVaultSecret -VaultName $appSettings.AzureKeyVaultName -Name "transportKey" -AsPlainText
    $printerData.certificateSigningRequest.content = $storedSecretContent
    $printerData.certificateSigningRequest.transportKey = $storedSecretTransportKey
    $printerHeaderResp = Request-DelegatedResource -Uri "print/printers/create" -Method "POST" -Body $printerData -ScopeOverride "Printer.Create"
    $printerOperationUrl = $printerHeaderResp."Operation-Location" | Select-Object -First 1
    $printerOperationRequestUri = $printerOperationUrl.Split("v1.0/")[1]
    $createPrinterOps = Request-DelegatedResource -Uri $printerOperationRequestUri
    if (@("running", "notStarted") -contains $createPrinterOps.status.state){
        Start-Sleep -Milliseconds 1000  # wait for 1second before retrying
        $createPrinterOps = Request-DelegatedResource -Uri $printerOperationRequestUri
    }
    if ($createPrinterOps.status.state -ne "succeeded") {
        Write-Error  -Message "Printer creation failed. Status=$($createPrinterOps.status.state)"
        $printer_id = $null
    }
    else{
        $printer = $createPrinterOps.printer
        $printer_id = $printer.id
    }
}

$identifiers = Add-Identifier $identifiers @("printer") $printer_id

#create PrintJob
$printJob = Request-DelegatedResource -Uri "print/printers/$($printer_id)/jobs?`$expand=documents&`$top=1" -ScopeOverride "PrintJob.ReadWrite.All"
if(!$printJob)
{
    $printJobData = Get-RequestData -ChildEntity "printJob"
    $printJob = Request-DelegatedResource -Uri "print/printers/$($printer_id)/jobs" -Method "POST" -Body $printJobData -ScopeOverride "PrintJob.ReadWrite.All"
}

$identifiers = Add-Identifier $identifiers @("printer", "printJob") $printJob.id
$identifiers = Add-Identifier $identifiers @("printer", "printJob", "printDocument") $printJob.documents[0].id

# Connect to Default Tenant
Connect-DefaultTenant -AppSettings $appSettings
$printTaskDefinition = Invoke-RequestHelper -Uri "print/taskDefinitions" | Select-Object -First 1
if (!$printTaskDefinition){
    # create printTaskDefinition
    $printTaskDefinitionData = Get-RequestData -ChildEntity "printTaskDefinition"
    $printTaskDefinition = Invoke-RequestHelper -Uri "print/taskDefinitions" -Method "POST" -Body $printTaskDefinitionData
}

$printTaskTrigger = Request-DelegatedResource -Uri "print/printers/$($printer_id)/taskTriggers" -ScopeOverride "Printer.ReadWrite.All" | Select-Object -First 1
if(!$printTaskTrigger -and $printTaskDefinition.id){
    #Create PrintTaskTrigger
    $printTaskTriggerData = Get-RequestData -ChildEntity "printTaskTrigger"
    $printTaskTriggerData."definition@odata.bind" += $printTaskDefinition.id
    $printTaskTrigger = Request-DelegatedResource -Uri "print/printers/$($printer_id)/taskTriggers" -Method "POST" -Body $printTaskTriggerData -ScopeOverride "Printer.ReadWrite.All"
}
$identifiers = Add-Identifier $identifiers @("printer", "printTaskTrigger") $printTaskTrigger.id

<#
    Get Devices https://docs.microsoft.com/en-us/graph/api/device-list?view=graph-rest-1.0&tabs=http
    Once a Printer is Created by ealier cmds in this script, its treated as a device.
#>
$printerDevices = Request-DelegatedResource -Uri "devices" -Method "GET" -ScopeOverride "Device.Read.All" |
    Select-Object -First 1

$identifiers = Add-Identifier $identifiers @("device") $printerDevices.id
$identifiers = Add-Identifier $identifiers @("printTaskDefinition") $printTaskDefinition.id

# save identifiers
$identifiers | ConvertTo-Json -Depth 10 > $identifiersPath
