# ============================================
# DKPT — Script d'import complet des données
# ============================================
# 
# Exécuter depuis la racine du projet après `docker compose up -d`
# et que le backend ait appliqué les migrations (attendre ~10s).
#
# Usage :
#   .\docker\seed\import-all.ps1
# ============================================

$ErrorActionPreference = "Stop"
$ROOT = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

Write-Host "=== DKPT Seed Import ===" -ForegroundColor Cyan

# 1. Settings + Contribution Amounts
Write-Host "`n[1/5] Import settings & contribution_amounts..." -ForegroundColor Yellow
Get-Content "$ROOT\docker\seed\seed-data.sql" -Raw | docker compose exec -T db psql -U postgres -d dkpt
Write-Host "  OK" -ForegroundColor Green

# 2. Members  
Write-Host "[2/5] Import members (~450 membres)..." -ForegroundColor Yellow
Get-Content "$ROOT\..\docs\dkpt_sql\members_rows.sql" -Raw | docker compose exec -T db psql -U postgres -d dkpt
Write-Host "  OK" -ForegroundColor Green

# 3. Payments
Write-Host "[3/5] Import payments (~950 paiements)..." -ForegroundColor Yellow
Get-Content "$ROOT\..\docs\dkpt_sql\payments_rows.sql" -Raw | docker compose exec -T db psql -U postgres -d dkpt
Write-Host "  OK" -ForegroundColor Green

# 4. Create users via API
Write-Host "[4/5] Creation des utilisateurs via l'API..." -ForegroundColor Yellow
$baseUrl = "http://localhost/api"  # via Nginx

$users = @(
    @{ email = "admin@dkpt.com"; password = "Dkpt@2026"; role = "Admin" },
    @{ email = "sg@dkpt.com"; password = "Dkpt@2026"; role = "Secretaire" },
    @{ email = "tr@dkpt.com"; password = "Dkpt@2026"; role = "Tresorier" },
    @{ email = "membre@dkpt.com"; password = "Dkpt@2026" }
)

foreach ($user in $users) {
    $body = $user | ConvertTo-Json
    try {
        Invoke-RestMethod -Method Post -Uri "$baseUrl/Auth/register" -ContentType "application/json" -Body $body | Out-Null
        Write-Host "  + $($user.email)" -ForegroundColor Green
    } catch {
        Write-Host "  ~ $($user.email) (deja existant ou erreur)" -ForegroundColor DarkYellow
    }
}

# 5. Verification
Write-Host "`n[5/5] Verification..." -ForegroundColor Yellow
docker compose exec -T db psql -U postgres -d dkpt -c "SELECT 'members' as tbl, count(*) FROM members UNION ALL SELECT 'payments', count(*) FROM payments UNION ALL SELECT 'users', count(*) FROM users UNION ALL SELECT 'contributions', count(*) FROM contribution_amounts UNION ALL SELECT 'settings', count(*) FROM settings;"

Write-Host "`n=== Import termine ! ===" -ForegroundColor Cyan
Write-Host "Connectez-vous sur http://localhost avec admin@dkpt.com / Dkpt@2026" -ForegroundColor White
