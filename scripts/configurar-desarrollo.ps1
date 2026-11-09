<#
.SYNOPSIS
  Prepara los secretos de desarrollo local de ServiYa (tasks.md T041).

.DESCRIPTION
  - Crea .env en la raíz con contraseñas aleatorias para docker-compose (si ya existe, lo reutiliza).
  - Copia esas contraseñas a los user-secrets de la API (cadenas de conexión y almacenamiento).
  - Genera claves VAPID y la clave del webhook si faltan.
  - Deja un token de Mercado Pago "pendiente" si no hay uno: se cambia por el de prueba real en la
    Etapa 6 (dotnet user-secrets set MercadoPago:AccessToken TEST-...).
  Nada de esto se sube al repositorio. Se puede ejecutar varias veces: no cambia lo que ya existe.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File scripts/configurar-desarrollo.ps1
#>
$ErrorActionPreference = 'Stop'

$raiz = Split-Path -Parent $PSScriptRoot
$archivoEnv = Join-Path $raiz '.env'
$proyectoApi = Join-Path $raiz 'api/src/ServiYa.Api'

function Nuevo-Secreto([int] $bytes = 24) {
    $datos = New-Object byte[] $bytes
    $generador = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $generador.GetBytes($datos) } finally { $generador.Dispose() }
    return Base64Url $datos
}

function Base64Url([byte[]] $datos) {
    return [Convert]::ToBase64String($datos).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

function Leer-Env([string] $ruta) {
    $valores = @{}
    foreach ($linea in Get-Content -LiteralPath $ruta) {
        if ($linea -match '^\s*([A-Z_]+)=(.*)$') { $valores[$Matches[1]] = $Matches[2].Trim() }
    }
    return $valores
}

# 1. .env para docker-compose
$claves = 'POSTGRES_PASSWORD', 'SERVIYA_OWNER_PASSWORD', 'SERVIYA_APP_PASSWORD', 'ALMACEN_ACCESS_KEY', 'ALMACEN_SECRET_KEY'
$entorno = if (Test-Path -LiteralPath $archivoEnv) { Leer-Env $archivoEnv } else { @{} }
$faltaron = $false
foreach ($clave in $claves) {
    if (-not $entorno[$clave]) {
        $entorno[$clave] = if ($clave -eq 'ALMACEN_ACCESS_KEY') { 'serviya-dev' } else { Nuevo-Secreto }
        $faltaron = $true
    }
}
if ($faltaron) {
    $contenido = "# Generado por scripts/configurar-desarrollo.ps1. Solo desarrollo local; no se sube al repositorio.`n"
    foreach ($clave in $claves) { $contenido += "$clave=$($entorno[$clave])`n" }
    # Sin BOM: docker compose no lo acepta.
    [System.IO.File]::WriteAllText($archivoEnv, $contenido, (New-Object System.Text.UTF8Encoding $false))
    Write-Output 'Se escribió .env'
} else {
    Write-Output '.env ya estaba completo'
}

# 2. user-secrets de la API. Se edita su secrets.json directamente (mismo formato que usa
#    `dotnet user-secrets`): así los valores no pasan por la línea de comandos, donde otros procesos
#    los verían, y un valor que empieza con "-" no se confunde con una opción.
[xml] $csproj = Get-Content -LiteralPath (Join-Path $proyectoApi 'ServiYa.Api.csproj')
$idSecretos = ($csproj.Project.PropertyGroup | Where-Object { $_.UserSecretsId } | Select-Object -First 1).UserSecretsId
if (-not $idSecretos) { throw 'ServiYa.Api.csproj no tiene UserSecretsId' }
$carpetaSecretos = Join-Path $env:APPDATA "Microsoft\UserSecrets\$idSecretos"
$archivoSecretos = Join-Path $carpetaSecretos 'secrets.json'

$existentes = [ordered]@{}
if (Test-Path -LiteralPath $archivoSecretos) {
    $json = Get-Content -LiteralPath $archivoSecretos -Raw
    if ($json.Trim()) {
        foreach ($propiedad in (ConvertFrom-Json $json).PSObject.Properties) { $existentes[$propiedad.Name] = [string] $propiedad.Value }
    }
}
$cambios = 0

function Fijar([string] $clave, [string] $valor, [switch] $SoloSiFalta) {
    if ($SoloSiFalta -and $existentes.Contains($clave)) { return }
    $existentes[$clave] = $valor
    $script:cambios++
    Write-Output "user-secrets: $clave"
}

$baseConexion = 'Host=localhost;Port=5432;Database=serviya'
Fijar 'ConnectionStrings:Default' "$baseConexion;Username=serviya_app;Password=$($entorno['SERVIYA_APP_PASSWORD'])"
Fijar 'ConnectionStrings:Migraciones' "$baseConexion;Username=serviya_owner;Password=$($entorno['SERVIYA_OWNER_PASSWORD'])"
Fijar 'Storage:AccessKey' $entorno['ALMACEN_ACCESS_KEY']
Fijar 'Storage:SecretKey' $entorno['ALMACEN_SECRET_KEY']
Fijar 'MercadoPago:AccessToken' 'TEST-pendiente-etapa-6' -SoloSiFalta
Fijar 'MercadoPago:WebhookSecret' (Nuevo-Secreto 32) -SoloSiFalta

# 3. Claves VAPID (P-256): pública = 0x04 || X || Y, privada = D, en base64url.
if (-not $existentes.Contains('WebPush:PublicKey') -or -not $existentes.Contains('WebPush:PrivateKey')) {
    $ec = [System.Security.Cryptography.ECDsa]::Create([System.Security.Cryptography.ECCurve+NamedCurves]::nistP256)
    try {
        $parametros = $ec.ExportParameters($true)
        $publica = [byte[]](@(4) + $parametros.Q.X + $parametros.Q.Y)
        Fijar 'WebPush:PublicKey' (Base64Url $publica)
        Fijar 'WebPush:PrivateKey' (Base64Url $parametros.D)
    } finally {
        $ec.Dispose()
    }
}

if ($cambios -gt 0) {
    New-Item -ItemType Directory -Force -Path $carpetaSecretos | Out-Null
    $contenidoSecretos = [pscustomobject] $existentes | ConvertTo-Json
    [System.IO.File]::WriteAllText($archivoSecretos, $contenidoSecretos, (New-Object System.Text.UTF8Encoding $false))
}

Write-Output 'Listo. Siguiente: docker compose up -d'
