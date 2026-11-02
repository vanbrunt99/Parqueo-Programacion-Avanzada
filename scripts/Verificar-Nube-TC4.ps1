# Verificar-Nube-TC4.ps1 — verificación de SOLO LECTURA del MVC contra la base configurada en User Secrets
# (en el equipo del estudiante: la base del curso en Azure, esquema user_13), cambiando "OrigenDatos" en
# Parqueo.Api/appsettings.json. Repite la misma consulta con "Json" para comparar resultados.
#
# Uso (PowerShell, con la rama feature/trabajo-en-clase-04 abierta y sin cambios pendientes):
#   powershell -ExecutionPolicy Bypass -File .\Verificar-Nube-TC4.ps1 -Repositorio "C:\Users\Usuario\Source\Repos\Parqueo-Programacion-Avanzada"
#
# Garantías:
#   - Solo hace solicitudes GET: no crea, modifica ni borra datos en la base del profesor.
#   - No lee ni imprime la cadena de conexión: la evidencia de que respondió la nube es el encabezado
#     X-Origen-Datos y las conexiones TCP del proceso del API al puerto 5432 (dirección remota), comparadas
#     con la dirección IP del servidor del curso.
#   - Restaura appsettings.json a "Json" al terminar (también si algo falla) y comprueba que no quedó diferencia.
#   - Detiene solo los procesos que inició.
# Salida: docs/evidencias/tc4/verificacion-nube-*.txt y verificacion-nube-respuestas-*.json en el repositorio.
param(
    [Parameter(Mandatory = $true)][string]$Repositorio,
    [string]$ServidorCurso = 'postgresql-hecferme.postgres.database.azure.com',
    [string]$Salida = ''
)
$ErrorActionPreference = 'Stop'
$Rama = 'feature/trabajo-en-clase-04'
# PowerShell no distingue mayúsculas: $Api y $Mvc no deben reutilizarse para los procesos.
$Api = 'http://localhost:5246'
$Mvc = 'http://localhost:5130'
$Ajustes = Join-Path $Repositorio 'Parqueo.Api/appsettings.json'
if (-not $Salida) { $Salida = Join-Path $Repositorio 'docs/evidencias/tc4' }
$Temporal = Join-Path ([IO.Path]::GetTempPath()) ('tc4-nube-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $Temporal | Out-Null
$EnWindows = [Environment]::OSVersion.Platform -eq 'Win32NT'
$Utf8 = New-Object System.Text.UTF8Encoding($false)

function Invoke-Git { $s = & git -C $Repositorio @args; if ($LASTEXITCODE -ne 0) { throw ('Falló: git ' + ($args -join ' ')) }; return $s }

function Set-Origen([string]$origen) {
    $texto = [IO.File]::ReadAllText($Ajustes)
    $nuevo = [regex]::Replace($texto, '("OrigenDatos"\s*:\s*)"[A-Za-z]+"', ('$1"' + $origen + '"'))
    [IO.File]::WriteAllText($Ajustes, $nuevo, $Utf8)
}

function Get-Descendientes([int]$id) {
    if ($EnWindows) {
        $hijos = @(Get-CimInstance Win32_Process -Filter "ParentProcessId=$id" | ForEach-Object { [int]$_.ProcessId })
    } else {
        $hijos = @(& ps -o pid= --ppid $id | ForEach-Object { [int]$_.Trim() })
    }
    $todos = @()
    foreach ($h in $hijos) { $todos += $h; $todos += Get-Descendientes $h }
    return $todos
}

function Start-Servicio([string]$proyecto, [string]$nombre) {
    $argumentos = @('run', '--project', $proyecto, '--launch-profile', 'http', '--no-build')
    $ventana = @{}
    if ($EnWindows) { $ventana['WindowStyle'] = 'Hidden' }
    return Start-Process -FilePath 'dotnet' -ArgumentList $argumentos -WorkingDirectory $Repositorio -PassThru `
        -RedirectStandardOutput (Join-Path $Temporal "$nombre.log") -RedirectStandardError (Join-Path $Temporal "$nombre.err") @ventana
}

function Stop-Servicio($proceso) {
    if (-not $proceso) { return }
    $ids = @(Get-Descendientes $proceso.Id) + @($proceso.Id)
    foreach ($id in $ids) { Stop-Process -Id $id -Force -ErrorAction SilentlyContinue }
    Start-Sleep -Seconds 1
}

function Wait-Url([string]$url) {
    for ($i = 0; $i -lt 40; $i++) {
        try { Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 2 | Out-Null; return } catch { Start-Sleep -Milliseconds 500 }
    }
    throw "No respondió $url"
}

function Get-Respuesta([string]$url) {
    $r = Invoke-WebRequest -Uri $url -UseBasicParsing -Headers @{ Accept = 'application/json' } -TimeoutSec 60
    $origen = $r.Headers['X-Origen-Datos']; if ($origen -is [array]) { $origen = $origen[0] }
    $total = $r.Headers['X-Total-Registros']; if ($total -is [array]) { $total = $total[0] }
    $cuerpo = $r.Content
    if ($cuerpo -is [byte[]]) { $cuerpo = [Text.Encoding]::UTF8.GetString($cuerpo) }
    return [pscustomobject]@{ Estado = [int]$r.StatusCode; Origen = $origen; Total = $total; Cuerpo = $cuerpo }
}

# Normaliza el JSON para comparar valores (1250 y 1250.00 son iguales) sin el campo origenDatos.
function ConvertTo-Normal($v) {
    if ($null -eq $v) { return $null }
    # PowerShell 7 convierte las fechas ISO en DateTime; PowerShell 5.1 las deja como texto.
    if ($v -is [datetime]) { return $v.ToString('yyyy-MM-ddTHH:mm:ss', [Globalization.CultureInfo]::InvariantCulture) }
    if ($v -is [string] -or $v -is [bool]) { return $v }
    if ($v -is [ValueType]) { return ([double]$v).ToString('R', [Globalization.CultureInfo]::InvariantCulture) }
    if ($v -is [System.Collections.IEnumerable]) { return ,@($v | ForEach-Object { ConvertTo-Normal $_ }) }
    $o = [ordered]@{}
    foreach ($p in ($v.PSObject.Properties | Sort-Object Name)) { if ($p.Name -ne 'origenDatos') { $o[$p.Name] = ConvertTo-Normal $p.Value } }
    return $o
}

$Rutas = @(
    "$Mvc/MaestroDetalle/Automoviles?tipo=sed%C3%A1n", "$Mvc/MaestroDetalle/Automoviles?tipo=4x4", "$Mvc/MaestroDetalle/Automoviles?tipo=moto",
    "$Mvc/MaestroDetalle/Ingresos/1", "$Mvc/MaestroDetalle/Ingresos/2", "$Mvc/MaestroDetalle/Ingresos/3",
    "$Mvc/MaestroDetalle/Ingresos/4", "$Mvc/MaestroDetalle/Ingresos/5", "$Mvc/MaestroDetalle/Ingresos/6",
    "$Api/api/automoviles?tamanoPagina=100", "$Api/api/parqueos?tamanoPagina=100", "$Api/api/ingresos?tamanoPagina=100")

# ---------------------------------------------------------------- comprobaciones previas
if ((Invoke-Git rev-parse --abbrev-ref HEAD | Select-Object -First 1).Trim() -ne $Rama) { throw "Abra la rama $Rama (git switch $Rama)." }
if (@(Invoke-Git status --porcelain).Count -gt 0) { throw 'El repositorio tiene cambios pendientes. Confírmelos o guárdelos antes de verificar.' }
if ([IO.File]::ReadAllText($Ajustes) -notmatch '"OrigenDatos"\s*:\s*"Json"') { throw 'appsettings.json del API debe estar en "Json" al empezar.' }
$variables = @(Get-ChildItem Env: | Where-Object { $_.Name -match '^(OrigenDatos|RutaDatosJson|ConnectionStrings__)' })
if ($variables.Count -gt 0) { throw ('Hay variables de entorno que sobrescriben appsettings.json: ' + (($variables | ForEach-Object Name) -join ', ')) }
if (Test-Path Env:Api__UrlBase) { throw 'Api__UrlBase sobrescribe la URL del API; no se inicia la verificacion.' }
if ($EnWindows) {
    $ocupados = @(Get-NetTCPConnection -State Listen -ErrorAction Stop | Where-Object { $_.LocalPort -in @(5246, 5130) })
    if ($ocupados.Count -gt 0) { throw 'Los puertos 5246 o 5130 ya estan ocupados. No se detienen procesos ajenos.' }
}
New-Item -ItemType Directory -Path $Salida -Force | Out-Null

Write-Host 'Compilando...'
& dotnet build (Join-Path $Repositorio 'Parqueo.slnx') -nologo -v q | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'La compilación falló.' }

$ipsCurso = @()
try { $ipsCurso = @([System.Net.Dns]::GetHostAddresses($ServidorCurso) | ForEach-Object { $_.ToString() }) } catch { $ipsCurso = @() }
$respuestas = @{}
$fecha = [TimeZoneInfo]::ConvertTime([DateTime]::UtcNow, [TimeZoneInfo]::FindSystemTimeZoneById($(if ($EnWindows) { 'Central America Standard Time' } else { 'America/Costa_Rica' }))).ToString('yyyy-MM-dd HH:mm')

try {
    foreach ($origen in @('Json', 'BaseDatos')) {
        Write-Host "Verificando OrigenDatos = $origen ..."
        Set-Origen $origen
        $procesoApi = $null; $procesoMvc = $null
        try {
            $procesoApi = Start-Servicio 'Parqueo.Api' "api-$origen"
            $procesoMvc = Start-Servicio 'Parqueo.Mvc' "mvc-$origen"
            Wait-Url "$Api/api/parqueos"; Wait-Url "$Mvc/"
            $idsApi = @(Get-Descendientes $procesoApi.Id)
            $l = New-Object System.Collections.Generic.List[string]
            $l.Add("# Verificación del MVC con ""OrigenDatos"": ""$origen"" en Parqueo.Api/appsettings.json, en el equipo del estudiante.")
            $l.Add($(if ($origen -eq 'BaseDatos') { '# BaseDatos = la base indicada por ConnectionStrings:ParqueoDb en los User Secrets del equipo (no se lee ni se muestra la cadena).' } else { '# Json = copia de trabajo de Datos/ junto al ejecutable (bin/Debug/net10.0/Datos).' }))
            $l.Add("# Fecha: $fecha (hora de Costa Rica). Solo solicitudes GET. Salida real, generada por Verificar-Nube-TC4.ps1.")
            $l.Add('')
            $l.Add('## 1. Configuración efectiva')
            $l.Add(([regex]::Match([IO.File]::ReadAllText($Ajustes), '"OrigenDatos"\s*:\s*"[A-Za-z]+"')).Value)
            $l.Add('Variables de entorno OrigenDatos/RutaDatosJson/ConnectionStrings__* en la sesión: ninguna')
            $l.Add('Comando: dotnet run --project Parqueo.Api --launch-profile http --no-build   (sin argumentos)')
            foreach ($linea in (Get-Content (Join-Path $Temporal "api-$origen.log") | Where-Object { $_ -match 'Origen de datos|Now listening|Hosting environment' })) { $l.Add($linea.Trim()) }
            $l.Add('')
            $l.Add('## 2. Respuestas del API y del MVC')
            $r1 = Get-Respuesta "$Api/api/automoviles?anioDesde=2015&anioHasta=2020"
            $l.Add("GET $Api/api/automoviles?anioDesde=2015&anioHasta=2020 -> $($r1.Estado); X-Origen-Datos: $($r1.Origen); X-Total-Registros: $($r1.Total)")
            $r2 = Get-Respuesta "$Api/api/ingresos/6"
            $l.Add("GET $Api/api/ingresos/6 -> $($r2.Estado); X-Origen-Datos: $($r2.Origen)")
            $l.Add($r2.Cuerpo)
            $html = (Invoke-WebRequest -Uri "$Mvc/Automoviles?anioDesde=2015&anioHasta=2020" -UseBasicParsing).Content
            $l.Add("GET $Mvc/Automoviles?anioDesde=2015&anioHasta=2020 -> pie del MVC: " + ([regex]::Match($html, 'data-origen="[A-Za-z]+"')).Value)
            $r3 = Get-Respuesta "$Mvc/MaestroDetalle/Ingresos/2"
            $l.Add("GET $Mvc/MaestroDetalle/Ingresos/2 -> $($r3.Estado)")
            $l.Add($r3.Cuerpo)
            $l.Add('')
            $l.Add('## 3. Conexiones TCP del proceso del API al puerto 5432')
            $conexiones = @()
            foreach ($id in $idsApi) {
                if ($EnWindows) {
                    $conexiones += @(Get-NetTCPConnection -OwningProcess $id -RemotePort 5432 -State Established -ErrorAction SilentlyContinue | ForEach-Object { "$($_.LocalAddress):$($_.LocalPort) -> $($_.RemoteAddress):$($_.RemotePort) (proceso $id)" })
                } else {
                    $conexiones += @(& lsof -nP -a -p $id -iTCP:5432 -sTCP:ESTABLISHED 2>$null | Select-Object -Skip 1 | ForEach-Object { "$_ (proceso $id)" })
                }
            }
            if ($conexiones.Count -eq 0) { $l.Add('(ninguna: el proceso del API no abrió conexiones a PostgreSQL)') } else { $conexiones | ForEach-Object { $l.Add($_) } }
            $l.Add("Direcciones IP de $ServidorCurso (DNS): " + $(if ($ipsCurso.Count) { $ipsCurso -join ', ' } else { 'no se pudo resolver' }))
            $l.Add('')
            $l.Add('## 4. Llamadas del MVC al API (registro del HttpClient, extracto)')
            Get-Content (Join-Path $Temporal "mvc-$origen.log") | Where-Object { $_ -match 'Start processing HTTP request' } | Select-Object -First 6 | ForEach-Object { $l.Add($_.Trim()) }

            $datos = [ordered]@{}; $origenes = [ordered]@{}
            foreach ($u in $Rutas) { $r = Get-Respuesta $u; $origenes[$u] = $(if ($r.Origen) { $r.Origen } else { ($r.Cuerpo | ConvertFrom-Json).origenDatos }); $datos[$u] = ConvertTo-Normal ($r.Cuerpo | ConvertFrom-Json) }
            $respuestas[$origen] = @{ Datos = $datos; Origenes = $origenes }
            [IO.File]::WriteAllText((Join-Path $Salida ("verificacion-nube-respuestas-" + $origen.ToLower() + '.json')), (([ordered]@{ origenInformado = $origenes; respuestas = $datos }) | ConvertTo-Json -Depth 30), $Utf8)
            [IO.File]::WriteAllLines((Join-Path $Salida ("verificacion-nube-" + $origen.ToLower() + '.txt')), $l, $Utf8)
        } catch {
            # Muestra el final de los registros del API y del MVC para diagnosticar (no contienen la cadena de conexión).
            foreach ($n in @("api-$origen.log", "api-$origen.err", "mvc-$origen.log", "mvc-$origen.err")) {
                $ruta = Join-Path $Temporal $n
                if (Test-Path $ruta) { Write-Warning "--- $n"; Get-Content $ruta -Tail 15 | ForEach-Object { Write-Warning $_ } }
            }
            throw
        } finally {
            Stop-Servicio $procesoMvc; Stop-Servicio $procesoApi
        }
    }
} finally {
    Set-Origen 'Json'
    & git -C $Repositorio diff --quiet -- 'Parqueo.Api/appsettings.json'
    if ($LASTEXITCODE -ne 0) { Write-Warning 'appsettings.json no quedó igual al commit: revíselo antes de confirmar.' } else { Write-Host 'appsettings.json restaurado en Json (sin diferencias con el commit).' }
    $raizTemporal = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([char[]]'\/') + [IO.Path]::DirectorySeparatorChar
    $rutaTemporal = [IO.Path]::GetFullPath($Temporal)
    if (-not $rutaTemporal.StartsWith($raizTemporal, [StringComparison]::OrdinalIgnoreCase) -or
        -not ([IO.Path]::GetFileName($rutaTemporal)).StartsWith('tc4-nube-')) { throw 'Ruta temporal fuera del directorio permitido.' }
    Remove-Item -LiteralPath $rutaTemporal -Recurse -Force -ErrorAction SilentlyContinue
}

# ---------------------------------------------------------------- equivalencia
$e = New-Object System.Collections.Generic.List[string]
$e.Add('# Equivalencia de resultados entre OrigenDatos=Json y OrigenDatos=BaseDatos (base de los User Secrets del equipo del estudiante).')
$e.Add('# Mismas URL; solo cambió "OrigenDatos" en Parqueo.Api/appsettings.json. Valores del JSON interpretado, sin el campo origenDatos.')
$e.Add('')
$iguales = 0
foreach ($u in $Rutas) {
    $a = $respuestas['Json'].Datos[$u] | ConvertTo-Json -Depth 30 -Compress
    $b = $respuestas['BaseDatos'].Datos[$u] | ConvertTo-Json -Depth 30 -Compress
    $igual = $a -eq $b; if ($igual) { $iguales++ }
    $e.Add(('{0,-58} {1,-10} {2,-10} {3}' -f ($u -replace 'http://localhost:\d+', ''), $respuestas['Json'].Origenes[$u], $respuestas['BaseDatos'].Origenes[$u], $(if ($igual) { 'idéntico' } else { 'DIFERENTE' })))
}
$e.Add('')
$e.Add("Resultado: $iguales de $($Rutas.Count) respuestas con contenido idéntico en ambos orígenes.")
[IO.File]::WriteAllLines((Join-Path $Salida 'verificacion-nube-equivalencia.txt'), $e, $Utf8)
$e | ForEach-Object { Write-Host $_ }
Write-Host "Evidencias en $Salida"
