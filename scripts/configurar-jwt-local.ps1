$ErrorActionPreference = 'Stop'
$proyecto = Join-Path $PSScriptRoot '../BanosQuimicos.Api/BanosQuimicos.Api.csproj'

# Capturar la salida evita imprimir los valores de secretos existentes en la terminal.
$existentes = & dotnet user-secrets list --project $proyecto
if ($LASTEXITCODE -ne 0) { throw 'No se pudieron consultar los secretos locales.' }
if ($existentes | Where-Object { $_ -match '^Jwt:ClaveBase64\s*=' }) {
    Write-Host 'La clave JWT local ya existe; se conserva sin mostrarla.'
    exit 0
}

$bytes = New-Object byte[] 64
$generador = [System.Security.Cryptography.RandomNumberGenerator]::Create()
try { $generador.GetBytes($bytes) } finally { $generador.Dispose() }
$secreto = @{ 'Jwt:ClaveBase64' = [Convert]::ToBase64String($bytes) } | ConvertTo-Json -Compress
# El secreto viaja por stdin: no queda como argumento, archivo del proyecto ni historial.
$secreto | & dotnet user-secrets set --project $proyecto
if ($LASTEXITCODE -ne 0) { throw 'No se pudo guardar la clave JWT local.' }
Write-Host 'Clave aleatoria guardada en User Secrets, fuera del repositorio.'
