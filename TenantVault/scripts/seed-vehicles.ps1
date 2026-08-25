# seed-vehicles.ps1
# Reads vehicle-seed-data.csv and POSTs each row to your Add Vehicle endpoint.
#
# Usage:
#   .\seed-vehicles.ps1 -ApiUrl "https://localhost:7257/inventory/vehicle" -CsvPath ".\vehicle-seed-data.csv"

param(
    [Parameter(Mandatory = $true)]
    [string]$ApiUrl,

    [Parameter(Mandatory = $true)]
    [string]$CsvPath,

    # If your local dev cert isn't trusted, this skips cert validation.
    # Do NOT use against anything but localhost/emulator.
    [switch]$SkipCertCheck
)

if (-not (Test-Path $CsvPath)) {
    Write-Error "CSV file not found at: $CsvPath"
    exit 1
}

$rows = Import-Csv -Path $CsvPath
$total = $rows.Count
$success = 0
$failed = 0
$failedRows = @()

Write-Host "Loaded $total rows from $CsvPath"
Write-Host "Posting to $ApiUrl ..."
Write-Host ""

$i = 0
foreach ($row in $rows) {
    $i++

    $body = @{
        tenantId    = $row.tenantId
        make        = $row.make
        model       = $row.model
        year        = [int]$row.year
        warehouseId = [int]$row.warehouseId
        spotId      = [int]$row.spotId
    } | ConvertTo-Json

    try {
        $params = @{
            Uri         = $ApiUrl
            Method      = "Post"
            Body        = $body
            ContentType = "application/json"
        }
        if ($SkipCertCheck) {
            $params["SkipCertificateCheck"] = $true
        }

        $response = Invoke-RestMethod @params
        $success++
    }
    catch {
        $failed++
        $failedRows += [PSCustomObject]@{
            RowNumber = $i
            TenantId  = $row.tenantId
            Error     = $_.Exception.Message
        }
        Write-Host "Row $i FAILED (tenant: $($row.tenantId)): $($_.Exception.Message)" -ForegroundColor Red
    }

    if ($i % 25 -eq 0) {
        Write-Host "Progress: $i / $total"
    }
}

Write-Host ""
Write-Host "Done. Success: $success  Failed: $failed  Total: $total"

if ($failedRows.Count -gt 0) {
    $failedRows | Export-Csv -Path ".\failed-rows.csv" -NoTypeInformation
    Write-Host "Failed rows written to failed-rows.csv"
}
