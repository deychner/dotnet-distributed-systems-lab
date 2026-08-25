# seed-vehicles.ps1
# Reads vehicle-seed-data.csv and POSTs each row to your Add Vehicle endpoint, in parallel.
# Requires PowerShell 7+ (uses ForEach-Object -Parallel).
#
# Usage:
#   .\seed-vehicles.ps1 -ApiUrl "https://localhost:7257/inventory/vehicle" -CsvPath ".\vehicle-seed-data.csv"
#   .\seed-vehicles.ps1 -ApiUrl "https://localhost:7257/inventory/vehicle" -CsvPath ".\vehicle-seed-data.csv" -ThrottleLimit 16

param(
    [Parameter(Mandatory = $true)]
    [string]$ApiUrl,

    [Parameter(Mandatory = $true)]
    [string]$CsvPath,

    # If your local dev cert isn't trusted, this skips cert validation.
    # Do NOT use against anything but localhost/emulator.
    [switch]$SkipCertCheck,

    # Number of requests to run concurrently.
    [int]$ThrottleLimit = 8
)

if ($PSVersionTable.PSVersion.Major -lt 7) {
    Write-Error "This script requires PowerShell 7+ (uses ForEach-Object -Parallel). You are running $($PSVersionTable.PSVersion)."
    exit 1
}

if (-not (Test-Path $CsvPath)) {
    Write-Error "CSV file not found at: $CsvPath"
    exit 1
}

$rows = Import-Csv -Path $CsvPath
$total = $rows.Count

Write-Host "Loaded $total rows from $CsvPath"
Write-Host "Posting to $ApiUrl with $ThrottleLimit concurrent requests..."
Write-Host ""

$results = [System.Collections.Concurrent.ConcurrentBag[object]]::new()

$rows | ForEach-Object -ThrottleLimit $ThrottleLimit -Parallel {
    $row = $_
    $results = $using:results
    $apiUrl = $using:ApiUrl
    $skipCertCheck = $using:SkipCertCheck
    $total = $using:total

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
            Uri         = $apiUrl
            Method      = "Post"
            Body        = $body
            ContentType = "application/json"
        }
        if ($skipCertCheck) {
            $params["SkipCertificateCheck"] = $true
        }

        Invoke-RestMethod @params | Out-Null
        $results.Add([PSCustomObject]@{
            Success  = $true
            TenantId = $row.tenantId
            Error    = $null
        })
    }
    catch {
        $results.Add([PSCustomObject]@{
            Success  = $false
            TenantId = $row.tenantId
            Error    = $_.Exception.Message
        })
        Write-Host "FAILED (tenant: $($row.tenantId)): $($_.Exception.Message)" -ForegroundColor Red
    }

    if ($results.Count % 25 -eq 0) {
        Write-Host "Progress: $($results.Count) / $total"
    }
}

$success = ($results | Where-Object { $_.Success }).Count
$failed = ($results | Where-Object { -not $_.Success }).Count

Write-Host ""
Write-Host "Done. Success: $success  Failed: $failed  Total: $total"

if ($failed -gt 0) {
    $results | Where-Object { -not $_.Success } | Select-Object TenantId, Error | Export-Csv -Path ".\failed-rows.csv" -NoTypeInformation
    Write-Host "Failed rows written to failed-rows.csv"
}
