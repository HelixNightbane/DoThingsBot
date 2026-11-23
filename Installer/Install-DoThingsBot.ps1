[CmdletBinding()]
param(
    [ValidateSet('Discover','Install')][string]$Operation = 'Install',
    [string]$Payload,
    [string]$InstallDirectory,
    [string]$ResultPath,
    [string]$TestRoot
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$version = '2.0.0.17-character-recovery.1'
$pluginId = '{96A085A9-7A75-4D36-9437-07DB39EF0BA0}'
$surrogate = '{71A69713-6593-47EC-0002-0000000DECA1}'
$software = 'Software'
$hive = [Microsoft.Win32.RegistryHive]::LocalMachine
if ($TestRoot) {
    if ($TestRoot -notmatch '^Software\\CodexInstallerTests\\[a-f0-9]{32}$') { throw 'Invalid isolated registry test root.' }
    $software = $TestRoot
    $hive = [Microsoft.Win32.RegistryHive]::CurrentUser
}
$registry = [Microsoft.Win32.RegistryKey]::OpenBaseKey($hive, [Microsoft.Win32.RegistryView]::Registry32)

function Read-Key([string]$Path) {
    $key = $registry.OpenSubKey($Path)
    if ($null -eq $key) { return $null }
    try {
        $values = @{}
        foreach ($name in $key.GetValueNames()) {
            $values[$name] = @{ Value=$key.GetValue($name); Kind=$key.GetValueKind($name).ToString() }
        }
        return $values
    } finally { $key.Dispose() }
}

function Set-Value([string]$Path, [string]$Name, $Value, [string]$Kind='String') {
    $key = $registry.CreateSubKey($Path)
    try { $key.SetValue($Name, $Value, [Microsoft.Win32.RegistryValueKind]$Kind) }
    finally { $key.Dispose() }
}

function Get-Value($Values, [string]$Name) {
    if ($null -ne $Values -and $Values.ContainsKey($Name)) { return $Values[$Name].Value }
    return $null
}

function Get-Hash([string]$Path) {
    $stream=[IO.File]::OpenRead($Path)
    $hash=[Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($hash.ComputeHash($stream)).Replace('-','') }
    finally { $stream.Dispose(); $hash.Dispose() }
}

function Normalize-Folder([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) { return $null }
    try {
        $value = [Environment]::ExpandEnvironmentVariables($Path.Trim().Trim('"'))
        if (-not [IO.Path]::IsPathRooted($value)) { return $null }
        if ([IO.Path]::GetExtension($value) -eq '.dll') { $value = Split-Path -Parent $value }
        return [IO.Path]::GetFullPath($value).TrimEnd('\')
    } catch [ArgumentException] { return $null }
    catch [NotSupportedException] { return $null }
    catch [IO.PathTooLongException] { return $null }
}

function Get-Entries {
    foreach ($category in 'Plugins','NetworkFilters') {
        $parent = $registry.OpenSubKey("$software\Decal\$category")
        if ($null -eq $parent) { continue }
        try { $ids = $parent.GetSubKeyNames() } finally { $parent.Dispose() }
        foreach ($id in $ids) {
            $path = "$software\Decal\$category\$id"
            $values = Read-Key $path
            $assembly = [string](Get-Value $values 'Assembly')
            $object = [string](Get-Value $values 'Object')
            $name = [string](Get-Value $values '')
            if ($id -eq $pluginId -or $assembly -match '^(?i)DoThingsBot(\.Loader)?\.dll$' -or
                $object -match '^DoThingsBot\.' -or $name -match '^(?i)DoThingsBot(\.Loader)?$') {
                [pscustomobject]@{ Key=$path; Category=$category; Values=$values;
                    Folder=(Normalize-Folder ([string](Get-Value $values 'Path')));
                    Loader=($assembly -ieq 'DoThingsBot.Loader.dll' -or $object -eq 'DoThingsBot.LoaderCore');
                    Enabled=(Get-Value $values 'Enabled') }
            }
        }
    }
}

function Find-Destination($Entries) {
    $candidates = [Collections.Generic.List[string]]::new()
    foreach ($entry in @($Entries | Sort-Object @{Expression='Enabled';Descending=$true})) {
        if ($entry.Folder) { $candidates.Add($entry.Folder) }
    }
    $stored = Get-Value (Read-Key "$software\SunnujDecalPlugins\DoThingsBot") ''
    if ($stored) { $candidates.Add((Normalize-Folder $stored)) }
    $agent = Normalize-Folder ([string](Get-Value (Read-Key "$software\Decal\Agent") 'AgentPath'))
    if ($agent) { $candidates.Add((Join-Path (Split-Path -Parent $agent) 'Decal Plugins\DoThingsBot')) }
    if (-not $TestRoot) {
        $candidates.Add('C:\Games\Decal Plugins\DoThingsBot')
        $candidates.Add("$([Environment]::GetFolderPath('ProgramFilesX86'))\DoThingsBot")
    }
    # Other registered plugin folders can reveal the shared plugins directory.
    $parent = $registry.OpenSubKey("$software\Decal\Plugins")
    if ($null -ne $parent) {
        try { $ids=$parent.GetSubKeyNames() } finally { $parent.Dispose() }
        foreach ($id in $ids) {
            $folder=Normalize-Folder ([string](Get-Value (Read-Key "$software\Decal\Plugins\$id") 'Path'))
            if ($folder) { $candidates.Add((Join-Path (Split-Path -Parent $folder) 'DoThingsBot')) }
        }
    }
    $folders=@($candidates | Where-Object { $_ } | Select-Object -Unique)
    foreach ($folder in $folders) {
        if (Test-Path -LiteralPath (Join-Path $folder 'DoThingsBot.dll') -PathType Leaf) { return $folder }
    }
    if ($Entries.Count -gt 0 -and $Entries[0].Folder) { return $Entries[0].Folder }
    if ($agent) { return Join-Path (Split-Path -Parent $agent) 'Decal Plugins\DoThingsBot' }
    return 'C:\Games\Decal Plugins\DoThingsBot'
}

try {
    $entries = @(Get-Entries)
    $destination = Find-Destination $entries
    if ($Operation -eq 'Discover') {
        if ($ResultPath) { [IO.File]::WriteAllText($ResultPath, "[Install]`r`nDirectory=$destination`r`n", [Text.Encoding]::Unicode) }
        Write-Output $destination
        exit 0
    }
    if ($InstallDirectory) { $destination = Normalize-Folder $InstallDirectory }
    if (-not $destination) { throw 'Choose an absolute install folder.' }
    $forbidden = @([IO.Path]::GetPathRoot($destination), $env:WINDIR, $env:ProgramFiles,
        ${env:ProgramFiles(x86)}, [Environment]::GetFolderPath('MyDocuments'))
    if ($forbidden -contains $destination -or $destination -match '\\Decal Plugins\\DoThingsBot\\[^\\]+\\[^\\]+') {
        throw 'Choose the plugin program folder containing Resources, outside a character data folder.'
    }
    if (-not $TestRoot) {
        if (Get-Process -Name acclient -ErrorAction SilentlyContinue) { throw "Close Asheron's Call before installing." }
        $release=Get-Value (Read-Key "$software\Microsoft\NET Framework Setup\NDP\v4\Full") 'Release'
        if ($null -eq $release -or [int]$release -lt 528040) { throw 'Install .NET Framework 4.8 before running setup.' }
        if ($null -eq (Read-Key "$software\Decal\Surrogates\$surrogate")) { throw 'Install Decal before running setup.' }
    }
    $manifest = Get-Content -LiteralPath (Join-Path $Payload 'manifest.json') -Raw | ConvertFrom-Json
    foreach ($file in $manifest) {
        $path=Join-Path $Payload $file.Path
        if ((Get-Hash $path) -ne $file.Sha256) { throw "Damaged installer payload: $($file.Path)" }
    }
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    $backup=Join-Path $destination ('Backups\' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N').Substring(0,8))
    New-Item -ItemType Directory -Path $backup -Force | Out-Null
    $directKey="$software\Decal\Plugins\$pluginId"
    $existingPlugin=@($entries | Where-Object { $_.Category -eq 'Plugins' -and -not $_.Loader } |
        Sort-Object @{Expression={ $_.Folder -eq $destination };Descending=$true},@{Expression='Enabled';Descending=$true} |
        Select-Object -First 1)
    if ($existingPlugin.Count -gt 0) { $directKey=$existingPlugin[0].Key }
    $productKey="$software\SunnujDecalPlugins\DoThingsBot"
    $keys=@($entries | ForEach-Object Key) + @($directKey,$productKey)
    $snapshots=@{}
    foreach ($key in ($keys | Select-Object -Unique)) { $snapshots[$key]=Read-Key $key }
    $snapshots | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $backup 'registration.json') -Encoding UTF8
    $published=[Collections.Generic.List[object]]::new()
    try {
        foreach ($file in $manifest) {
            $target=Join-Path $destination $file.Path
            $exists=Test-Path -LiteralPath $target -PathType Leaf
            if ($exists -and $file.PreserveExisting) { continue }
            if ($exists -and (Get-Hash $target) -eq $file.Sha256) { continue }
            $saved=Join-Path $backup $file.Path
            if ($exists) {
                New-Item -ItemType Directory -Path (Split-Path -Parent $saved) -Force | Out-Null
                Copy-Item -LiteralPath $target -Destination $saved
            }
            New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
            $published.Add([pscustomobject]@{ Target=$target; Saved=$saved; Existed=$exists })
            $temp=$target+'.'+[Guid]::NewGuid().ToString('N')+'.tmp'
            try {
                Copy-Item -LiteralPath (Join-Path $Payload $file.Path) -Destination $temp
                if ($exists) { [IO.File]::Replace($temp,$target,$saved) }
                else { [IO.File]::Move($temp,$target) }
            } finally { if(Test-Path -LiteralPath $temp){ Remove-Item -LiteralPath $temp } }
        }
        $loader=@($entries | Where-Object { $_.Loader -and $_.Category -eq 'NetworkFilters' -and
            $_.Enabled -eq 1 -and $_.Folder -eq $destination -and
            (Test-Path -LiteralPath (Join-Path $destination 'DoThingsBot.Loader.dll') -PathType Leaf) } | Select-Object -First 1)
        foreach ($entry in $entries) { Set-Value $entry.Key 'Enabled' 0 'DWord' }
        if ($loader.Count -gt 0) {
            $active=$loader[0].Key
            $assembly='DoThingsBot.Loader.dll'
            $object='DoThingsBot.LoaderCore'
        } else {
            $active=$directKey
            $assembly='DoThingsBot.dll'
            $object='DoThingsBot.PluginCore'
        }
        Set-Value $active '' 'DoThingsBot'
        Set-Value $active 'Path' $destination
        Set-Value $active 'Assembly' $assembly
        Set-Value $active 'Object' $object
        Set-Value $active 'Surrogate' $surrogate
        Set-Value $active 'Enabled' 1 'DWord'
        Set-Value $productKey '' $destination
        Set-Value $productKey 'Version' $version
        [ordered]@{ Version=$version; Directory=$destination; ActiveRegistration=$active;
            Backup=$backup; ReplacedFiles=@($published | ForEach-Object Target) } | ConvertTo-Json -Depth 4 |
            Set-Content -LiteralPath (Join-Path $destination 'install-receipt.json') -Encoding UTF8
        Write-Output "Installed $version in $destination. Backup in $backup."
    } catch {
        $failure=$_
        foreach ($file in $published) {
            if ($file.Existed) { Copy-Item -LiteralPath $file.Saved -Destination $file.Target -Force }
            elseif (Test-Path -LiteralPath $file.Target) { Remove-Item -LiteralPath $file.Target }
        }
        foreach ($key in $snapshots.Keys) {
            if ($null -eq $snapshots[$key]) { $registry.DeleteSubKeyTree($key,$false); continue }
            $current=$registry.OpenSubKey($key,$true)
            if ($null -ne $current) {
                try { foreach ($name in $current.GetValueNames()) { $current.DeleteValue($name) } }
                finally { $current.Dispose() }
            }
            foreach ($name in $snapshots[$key].Keys) {
                $value=$snapshots[$key][$name]
                Set-Value $key $name $value.Value $value.Kind
            }
        }
        throw $failure
    }
} catch {
    Write-Output $_.Exception.Message
    exit 1
} finally { $registry.Dispose() }
