# Script para aplicar la migración de base de datos manualmente
# Ejecuta este script en PowerShell cuando el servidor esté detenido, O en cualquier momento
# ya que psql puede conectarse directamente sin necesidad de detener el backend

Write-Host "Applying database migration: IncreaseCategoryMaxLength" -ForegroundColor Cyan

# Ejecutar SQL directamente en Docker PostgreSQL
$sql = "ALTER TABLE ""TaskItems"" ALTER COLUMN ""Category"" TYPE character varying(100);"

docker exec smarttask-postgres psql -U postgres -d SmartTaskDb -c $sql

if ($LASTEXITCODE -eq 0) {
    Write-Host "[OK] Migration applied successfully - Category column extended to 100 chars." -ForegroundColor Green
} else {
    Write-Host "[ERROR] Failed to apply migration. Check PostgreSQL connection." -ForegroundColor Red
}
