#requires -version 5.1

$ErrorActionPreference = "Stop"

# =========================
# Пути
# =========================
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$DataFile  = Join-Path $ScriptDir "vpnexclude.json"
$LogFile   = Join-Path $ScriptDir "vpnexclude.log"

# =========================
# Лог
# =========================
function Write-Log {
    param([string]$Message)

    $ts = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $line = "[$ts] $Message"
    Write-Host $line
    Add-Content -Path $LogFile -Value $line
}

# =========================
# Проверка админа
# =========================
function Test-IsAdmin {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# =========================
# Безопасная установка поля
# =========================
function Set-EntryField {
    param(
        [object]$Entry,
        [string]$Name,
        $Value
    )

    if ($Entry.PSObject.Properties.Name -contains $Name) {
        $Entry.$Name = $Value
    }
    else {
        $Entry | Add-Member -MemberType NoteProperty -Name $Name -Value $Value
    }
}

# =========================
# Нормализация записи
# =========================
function Normalize-Entry {
    param([object]$Entry)

    if (-not ($Entry.PSObject.Properties.Name -contains "Target")) {
        return $null
    }

    if ([string]::IsNullOrWhiteSpace($Entry.Target)) {
        return $null
    }

    Set-EntryField -Entry $Entry -Name "Type" -Value $(if ($Entry.PSObject.Properties.Name -contains "Type") { $Entry.Type } else { "Domain" })

    if (-not ($Entry.PSObject.Properties.Name -contains "Ips") -or $null -eq $Entry.Ips) {
        Set-EntryField -Entry $Entry -Name "Ips" -Value @()
    }
    else {
        $ips = @($Entry.Ips | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)
        $Entry.Ips = @($ips)
    }

    Set-EntryField -Entry $Entry -Name "Gateway"       -Value $(if ($Entry.PSObject.Properties.Name -contains "Gateway") { $Entry.Gateway } else { "" })
    Set-EntryField -Entry $Entry -Name "Comment"       -Value $(if ($Entry.PSObject.Properties.Name -contains "Comment") { $Entry.Comment } else { "" })
    Set-EntryField -Entry $Entry -Name "CreatedAt"     -Value $(if ($Entry.PSObject.Properties.Name -contains "CreatedAt") { $Entry.CreatedAt } else { (Get-Date).ToString("yyyy-MM-dd HH:mm:ss") })
    Set-EntryField -Entry $Entry -Name "UpdatedAt"     -Value $(if ($Entry.PSObject.Properties.Name -contains "UpdatedAt") { $Entry.UpdatedAt } else { $null })
    Set-EntryField -Entry $Entry -Name "LastCheckedAt" -Value $(if ($Entry.PSObject.Properties.Name -contains "LastCheckedAt") { $Entry.LastCheckedAt } else { $null })

    return $Entry
}

# =========================
# Загрузка / сохранение JSON
# =========================
function Load-Data {
    if (-not (Test-Path $DataFile)) {
        return @()
    }

    try {
        $raw = Get-Content $DataFile -Raw -Encoding UTF8
        if ([string]::IsNullOrWhiteSpace($raw)) {
            return @()
        }

        $data = $raw | ConvertFrom-Json
        if ($null -eq $data) {
            return @()
        }

        $arr = @($data)
        $result = @()

        foreach ($entry in $arr) {
            $normalized = Normalize-Entry -Entry $entry
            if ($null -ne $normalized) {
                $result += $normalized
            }
        }

        return @($result)
    }
    catch {
        Write-Log "Ошибка чтения JSON: $($_.Exception.Message)"
        return @()
    }
}

function Save-Data {
    param([array]$Data)

    $json = $Data | ConvertTo-Json -Depth 10
    Set-Content -Path $DataFile -Value $json -Encoding UTF8
}

# =========================
# Определение локального шлюза
# =========================
function Get-LocalGateway {
    $cfgs = Get-NetIPConfiguration | Where-Object {
        $_.IPv4DefaultGateway -ne $null -and
        $_.NetAdapter.Status -eq "Up" -and
        $_.InterfaceAlias -notmatch "WireGuard|WG|TAP|VPN"
    }

    if (-not $cfgs) {
        return $null
    }

    $preferred = $cfgs | Sort-Object @{
        Expression = {
            if ($_.InterfaceAlias -match "Wi-Fi|Беспровод|Wireless") { 1 }
            elseif ($_.InterfaceAlias -match "Ethernet|Ethernet 2|Подключение по локальной сети") { 2 }
            else { 9 }
        }
    }

    return $preferred[0].IPv4DefaultGateway.NextHop
}

# =========================
# DNS / IP
# =========================
function Test-IPv4 {
    param([string]$Value)

    return $Value -match '^(25[0-5]|2[0-4]\d|1?\d?\d)\.(25[0-5]|2[0-4]\d|1?\d?\d)\.(25[0-5]|2[0-4]\d|1?\d?\d)\.(25[0-5]|2[0-4]\d|1?\d?\d)$'
}

function Resolve-TargetIPs {
    param([string]$Target)

    if (Test-IPv4 $Target) {
        return @($Target)
    }

    try {
        $records = Resolve-DnsName -Name $Target -Type A -ErrorAction Stop
        $ips = $records |
            Where-Object { $_.IPAddress } |
            Select-Object -ExpandProperty IPAddress -Unique
        return @($ips)
    }
    catch {
        Write-Log "Не удалось определить IP для '$Target': $($_.Exception.Message)"
        return @()
    }
}

# =========================
# Маршруты
# =========================
function Route-Exists {
    param([string]$Ip)

    $out = cmd /c "route print $Ip"
    return ($out | Select-String -SimpleMatch $Ip) -ne $null
}

function Add-BypassRoute {
    param(
        [string]$Ip,
        [string]$Gateway
    )

    if (Route-Exists $Ip) {
        Write-Log "Маршрут для $Ip уже есть"
        return
    }

    $cmd = "route add $Ip mask 255.255.255.255 $Gateway -p"
    Write-Log "Выполняю: $cmd"
    cmd /c $cmd | Out-Null
    Write-Log "Маршрут добавлен: $Ip -> $Gateway"
}

function Remove-BypassRoute {
    param([string]$Ip)

    $cmd = "route delete $Ip"
    Write-Log "Выполняю: $cmd"
    cmd /c $cmd | Out-Null
    Write-Log "Маршрут удалён: $Ip"
}

# =========================
# Вывод таблицы
# =========================
function Show-Entries {
    param([array]$Data)

    Write-Host ""
    Write-Host "ТЕКУЩИЕ ИСКЛЮЧЕНИЯ" -ForegroundColor Cyan
    Write-Host "==================" -ForegroundColor Cyan

    if (-not $Data -or $Data.Count -eq 0) {
        Write-Host "Список пуст."
        Write-Host ""
        return
    }

    $i = 1
    foreach ($entry in $Data) {
        $ipsText = ($entry.Ips -join ", ")
        Write-Host ("[{0}] {1}" -f $i, $entry.Target) -ForegroundColor Yellow
        Write-Host ("     Тип:          {0}" -f $entry.Type)
        Write-Host ("     IP:           {0}" -f $ipsText)
        Write-Host ("     Шлюз:         {0}" -f $entry.Gateway)
        Write-Host ("     Комментарий:  {0}" -f $entry.Comment)
        Write-Host ("     Добавлено:    {0}" -f $entry.CreatedAt)
        if ($entry.UpdatedAt) {
            Write-Host ("     Изменено:     {0}" -f $entry.UpdatedAt)
        }
        if ($entry.LastCheckedAt) {
            Write-Host ("     Проверено:    {0}" -f $entry.LastCheckedAt)
        }
        Write-Host ""
        $i++
    }
}

# =========================
# Добавление
# =========================
function Add-Entry {
    param([array]$Data)

    Write-Host ""
    $target = Read-Host "Введите домен или IP"
    if ([string]::IsNullOrWhiteSpace($target)) {
        Write-Host "Пустое значение."
        return ,$Data
    }

    $target = $target.Trim()
    $comment = Read-Host "Комментарий (необязательно)"

    $gateway = Get-LocalGateway
    if ([string]::IsNullOrWhiteSpace($gateway)) {
        $gateway = Read-Host "Не удалось определить локальный шлюз автоматически. Введите его вручную"
    }

    if ([string]::IsNullOrWhiteSpace($gateway)) {
        Write-Host "Шлюз не указан."
        return ,$Data
    }

    $ips = Resolve-TargetIPs -Target $target
    if (-not $ips -or $ips.Count -eq 0) {
        Write-Host "IP не найдены для '$target'."
        return ,$Data
    }

    Write-Host ""
    Write-Host "Найдены IP:" -ForegroundColor Green
    $ips | ForEach-Object { Write-Host " - $_" }

    $confirm = Read-Host "Добавить маршруты? (y/n)"
    if ($confirm -notin @("y", "Y", "д", "Д")) {
        Write-Host "Отмена."
        return ,$Data
    }

    foreach ($ip in $ips) {
        Add-BypassRoute -Ip $ip -Gateway $gateway
    }

    $existing = @($Data | Where-Object { $_.Target -eq $target })
    if ($existing.Count -gt 0) {
        foreach ($entry in $existing) {
            $oldIps = @($entry.Ips)
            $combinedIps = @($oldIps + $ips | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)

            $entry.Ips = @($combinedIps)
            $entry.Gateway = $gateway
            $entry.Comment = $comment

            Set-EntryField -Entry $entry -Name "UpdatedAt" -Value ((Get-Date).ToString("yyyy-MM-dd HH:mm:ss"))
            Set-EntryField -Entry $entry -Name "LastCheckedAt" -Value ((Get-Date).ToString("yyyy-MM-dd HH:mm:ss"))
        }
    }
    else {
        $newEntry = [pscustomobject]@{
            Target        = $target
            Type          = $(if (Test-IPv4 $target) { "IP" } else { "Domain" })
            Ips           = @($ips)
            Gateway       = $gateway
            Comment       = $comment
            CreatedAt     = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
            UpdatedAt     = $null
            LastCheckedAt = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
        }
        $Data = @($Data) + @($newEntry)
    }

    Save-Data -Data $Data
    Write-Host "Добавлено." -ForegroundColor Green
    return ,$Data
}

# =========================
# Редактирование комментария
# =========================
function Edit-EntryComment {
    param([array]$Data)

    if (-not $Data -or $Data.Count -eq 0) {
        Write-Host "Редактировать нечего."
        return ,$Data
    }

    Show-Entries -Data $Data
    $num = Read-Host "Введите номер записи для редактирования комментария"

    if (-not ($num -match '^\d+$')) {
        Write-Host "Неверный номер."
        return ,$Data
    }

    $idx = [int]$num - 1
    if ($idx -lt 0 -or $idx -ge $Data.Count) {
        Write-Host "Номер вне диапазона."
        return ,$Data
    }

    $entry = $Data[$idx]

    Write-Host ""
    Write-Host "Текущий комментарий: $($entry.Comment)" -ForegroundColor Yellow
    $newComment = Read-Host "Введите новый комментарий (можно пустой)"

    $entry.Comment = $newComment
    Set-EntryField -Entry $entry -Name "UpdatedAt" -Value ((Get-Date).ToString("yyyy-MM-dd HH:mm:ss"))

    Save-Data -Data $Data
    Write-Host "Комментарий обновлён." -ForegroundColor Green

    return ,$Data
}

# =========================
# Проверка домена
# =========================
function Check-DomainEntry {
    param([array]$Data)

    $domainEntries = @($Data | Where-Object { $_.Type -eq "Domain" })

    if (-not $domainEntries -or $domainEntries.Count -eq 0) {
        Write-Host "В списке нет доменов для проверки."
        return ,$Data
    }

    Write-Host ""
    Write-Host "СПИСОК ДОМЕНОВ" -ForegroundColor Cyan
    Write-Host "==============" -ForegroundColor Cyan
    for ($i = 0; $i -lt $domainEntries.Count; $i++) {
        Write-Host ("[{0}] {1}" -f ($i + 1), $domainEntries[$i].Target) -ForegroundColor Yellow
    }
    Write-Host ""

    $num = Read-Host "Введите номер домена для проверки"

    if (-not ($num -match '^\d+$')) {
        Write-Host "Неверный номер."
        return ,$Data
    }

    $idx = [int]$num - 1
    if ($idx -lt 0 -or $idx -ge $domainEntries.Count) {
        Write-Host "Номер вне диапазона."
        return ,$Data
    }

    $entry = $domainEntries[$idx]

    Write-Host ""
    Write-Host "Проверяю домен: $($entry.Target)" -ForegroundColor Cyan

    $oldIps = @($entry.Ips)
    $newIps = Resolve-TargetIPs -Target $entry.Target

    if (-not $newIps -or $newIps.Count -eq 0) {
        Write-Host "Не удалось получить новые IP."
        return ,$Data
    }

    $oldSet = @($oldIps | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)
    $newSet = @($newIps | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)

    $addedIps = @($newSet | Where-Object { $_ -notin $oldSet })
    $removedIps = @($oldSet | Where-Object { $_ -notin $newSet })

    Write-Host ""
    Write-Host "Сохранённые IP:" -ForegroundColor Yellow
    if ($oldSet.Count -eq 0) {
        Write-Host " - нет"
    }
    else {
        $oldSet | ForEach-Object { Write-Host " - $_" }
    }

    Write-Host ""
    Write-Host "Текущие IP из DNS:" -ForegroundColor Green
    $newSet | ForEach-Object { Write-Host " - $_" }

    Write-Host ""

    if ($addedIps.Count -gt 0) {
        Write-Host "Найдены новые IP:" -ForegroundColor Green
        $addedIps | ForEach-Object { Write-Host " + $_" }

        $confirm = Read-Host "Добавляем новые? (Д/н)"
        if ($confirm -in @("д", "Д", "y", "Y")) {
            foreach ($ip in $addedIps) {
                Add-BypassRoute -Ip $ip -Gateway $entry.Gateway
            }

            $combinedIps = @($oldSet + $addedIps | Select-Object -Unique)
            $entry.Ips = @($combinedIps)

            Set-EntryField -Entry $entry -Name "UpdatedAt" -Value ((Get-Date).ToString("yyyy-MM-dd HH:mm:ss"))
            Set-EntryField -Entry $entry -Name "LastCheckedAt" -Value ((Get-Date).ToString("yyyy-MM-dd HH:mm:ss"))

            Save-Data -Data $Data
            Write-Host "Новые IP добавлены. Старые IP сохранены." -ForegroundColor Green
        }
        else {
            Set-EntryField -Entry $entry -Name "LastCheckedAt" -Value ((Get-Date).ToString("yyyy-MM-dd HH:mm:ss"))
            Save-Data -Data $Data
            Write-Host "Новые IP не добавлены." -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "Новых IP не найдено." -ForegroundColor Green
        Set-EntryField -Entry $entry -Name "LastCheckedAt" -Value ((Get-Date).ToString("yyyy-MM-dd HH:mm:ss"))
        Save-Data -Data $Data
    }

    if ($removedIps.Count -gt 0) {
        Write-Host ""
        Write-Host "Старые IP, которых больше нет в DNS:" -ForegroundColor Yellow
        $removedIps | ForEach-Object { Write-Host " - $_" }
        Write-Host "Их я автоматически не удаляю и не убираю из записи."
    }

    return ,$Data
}

# =========================
# Удаление
# =========================
function Remove-Entry {
    param([array]$Data)

    if (-not $Data -or $Data.Count -eq 0) {
        Write-Host "Удалять нечего."
        return ,$Data
    }

    Show-Entries -Data $Data
    $num = Read-Host "Введите номер записи для удаления"

    if (-not ($num -match '^\d+$')) {
        Write-Host "Неверный номер."
        return ,$Data
    }

    $idx = [int]$num - 1
    if ($idx -lt 0 -or $idx -ge $Data.Count) {
        Write-Host "Номер вне диапазона."
        return ,$Data
    }

    $entry = $Data[$idx]

    Write-Host ""
    Write-Host "Будет удалено: $($entry.Target)" -ForegroundColor Yellow
    Write-Host "IP: $($entry.Ips -join ', ')"

    $confirm = Read-Host "Удалить маршруты и запись? (y/n)"
    if ($confirm -notin @("y", "Y", "д", "Д")) {
        Write-Host "Отмена."
        return ,$Data
    }

    foreach ($ip in $entry.Ips) {
        Remove-BypassRoute -Ip $ip
    }

    $newData = @()
    for ($i = 0; $i -lt $Data.Count; $i++) {
        if ($i -ne $idx) {
            $newData += $Data[$i]
        }
    }

    Save-Data -Data $newData
    Write-Host "Удалено." -ForegroundColor Green

    return ,$newData
}

# =========================
# Главное меню
# =========================
function Show-Menu {
    Write-Host "Выберите действие:" -ForegroundColor Cyan
    Write-Host "1 - Добавить"
    Write-Host "2 - Редактировать комментарий"
    Write-Host "3 - Удалить"
    Write-Host "4 - Проверить домен"
    Write-Host "5 - Выйти"
    Write-Host ""
}

# =========================
# MAIN
# =========================
if (-not (Test-IsAdmin)) {
    Write-Host "Скрипт нужно запускать от имени администратора." -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $LogFile)) {
    New-Item -ItemType File -Path $LogFile -Force | Out-Null
}

$data = Load-Data

# один раз пересохраняем нормализованный JSON
Save-Data -Data $data

while ($true) {
    Clear-Host
    Write-Host "VPNExclude" -ForegroundColor Cyan
    Write-Host "==========" -ForegroundColor Cyan

    Show-Entries -Data $data
    Show-Menu

    $choice = Read-Host "Ваш выбор"

    switch ($choice) {
        "1" {
            $data = Add-Entry -Data $data
            Write-Host ""
            Read-Host "Нажмите Enter для продолжения"
        }
        "2" {
            $data = Edit-EntryComment -Data $data
            Write-Host ""
            Read-Host "Нажмите Enter для продолжения"
        }
        "3" {
            $data = Remove-Entry -Data $data
            Write-Host ""
            Read-Host "Нажмите Enter для продолжения"
        }
        "4" {
            $data = Check-DomainEntry -Data $data
            Write-Host ""
            Read-Host "Нажмите Enter для продолжения"
        }
        "5" {
            Write-Host "Выход."
            break
        }
        default {
            Write-Host "Неверный выбор."
            Start-Sleep -Seconds 1
        }
    }
}