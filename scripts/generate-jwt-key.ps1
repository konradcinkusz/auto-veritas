# Generates the RS256 signing keypair for a LOCAL auto-veritas stack (Windows).
# See scripts/generate-jwt-key.sh for the trust rules; they apply identically here.
$ErrorActionPreference = 'Stop'

$outDir = Join-Path (Split-Path -Parent $PSScriptRoot) 'certs'
$keyFile = Join-Path $outDir 'jwt-signing.dev.pem'

if (Test-Path $keyFile) {
    Write-Host "Key already exists at $keyFile - refusing to overwrite."
    exit 0
}

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# PKCS#8 ("BEGIN PRIVATE KEY"), 2048-bit - authservice rejects PKCS#1.
$rsa = [System.Security.Cryptography.RSA]::Create(2048)
try {
    $pem = $rsa.ExportPkcs8PrivateKeyPem()
    Set-Content -Path $keyFile -Value $pem -NoNewline
    Add-Content -Path $keyFile -Value ""
} finally {
    $rsa.Dispose()
}

Write-Host "Wrote $keyFile"
Write-Host "docker compose mounts it automatically; for the Aspire AppHost run:"
Write-Host "  dotnet user-secrets set Parameters:jwt-signing-key (Get-Content $keyFile -Raw) --project src/AutoVeritas.AppHost"
