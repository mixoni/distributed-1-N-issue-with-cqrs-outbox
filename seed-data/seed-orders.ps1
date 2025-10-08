param(
  [int]$Count = 500,
  [int]$Concurrency = 20,
  [string]$OrdersApi = "http://localhost:5002"
)

Write-Host "Seeding $Count orders to $OrdersApi (concurrency=$Concurrency)…"

$jobs = @()
for ($i=1; $i -le $Count; $i++) {
  while (@($jobs | Where-Object { $_.State -eq 'Running' }).Count -ge $Concurrency) {
    Start-Sleep -Milliseconds 100
    $jobs = $jobs | Where-Object { $_.State -eq 'Running' -or $_.State -eq 'Completed' }
  }

  $jobs += Start-Job -ScriptBlock {
    param($OrdersApi)
    $cid = Get-Random -Minimum 1 -Maximum 4         # 1..3
    $total = [math]::Round((Get-Random -Minimum 100 -Maximum 10000)/100, 2)
    try {
      Invoke-RestMethod -Method POST -Uri "$OrdersApi/api/orders" -ContentType "application/json" -Body (@{customerId=$cid; total=$total} | ConvertTo-Json) | Out-Null
    } catch { }
  } -ArgumentList $OrdersApi
}

$jobs | Wait-Job | Out-Null
$jobs | Remove-Job
Write-Host "Seed complete."
