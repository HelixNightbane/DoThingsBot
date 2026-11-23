param(
    [Parameter(Mandatory=$true)][string]$Payload,
    [Parameter(Mandatory=$true)][string]$MakeNsis,
    [string]$Helper=(Join-Path (Split-Path -Parent $PSScriptRoot) 'Install-DoThingsBot.ps1'),
    [string]$ResultDirectory=$env:TEMP
)
$ErrorActionPreference='Stop'
$testId=[Guid]::NewGuid().ToString('N')
$testRoot="Software\CodexInstallerTests\$testId"
$work=Join-Path $ResultDirectory "installer-tests-$testId"
New-Item -ItemType Directory -Path $work | Out-Null
$reg=[Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::CurrentUser,[Microsoft.Win32.RegistryView]::Registry32)
$plugin="$testRoot\Decal\Plugins\{96A085A9-7A75-4D36-9437-07DB39EF0BA0}"
$product="$testRoot\SunnujDecalPlugins\DoThingsBot"
$psExe=Join-Path $env:WINDIR 'SysWOW64\WindowsPowerShell\v1.0\powershell.exe'
$results=[Collections.Generic.List[string]]::new()
function Set-TestKey($Path,$Values){
    $key=$reg.CreateSubKey($Path)
    try{foreach($name in $Values.Keys){$key.SetValue($name,$Values[$name])}}finally{$key.Dispose()}
}
function Read-TestValue($Path,$Name){
    $key=$reg.OpenSubKey($Path)
    if($null -eq $key){return $null}
    try{return $key.GetValue($Name)}finally{$key.Dispose()}
}
function Reset-Test{
    $reg.DeleteSubKeyTree($testRoot,$false)
}
function Require($Condition,$Message){if(-not $Condition){throw $Message}}
function Invoke-Helper([string]$Operation='Install',[string]$Directory=''){
    $output=& $psExe -NoProfile -NonInteractive -ExecutionPolicy RemoteSigned -File $Helper -Operation $Operation -Payload $Payload -InstallDirectory $Directory -TestRoot $testRoot
    $script:code=$LASTEXITCODE
    $script:output=$output
    return ($output -join "`n")
}
function Verify-Installed($Folder){
    Require ($script:code -eq 0) "Install failed: $script:output"
    foreach($file in Get-Content -LiteralPath (Join-Path $Payload 'manifest.json') -Raw | ConvertFrom-Json){
        Require ((Get-FileHash -LiteralPath (Join-Path $Folder $file.Path)).Hash -eq $file.Sha256) "Wrong payload: $($file.Path)"
    }
    Require ((Read-TestValue $plugin 'Path') -eq $Folder) 'Wrong registered folder'
    Require ((Read-TestValue $plugin 'Assembly') -eq 'DoThingsBot.dll') 'Wrong assembly'
    Require ((Read-TestValue $plugin 'Object') -eq 'DoThingsBot.PluginCore') 'Wrong class'
    Require ((Read-TestValue $plugin 'Enabled') -eq 1) 'Plugin not enabled'
}
function Pass($Name){$results.Add($Name);Write-Output "PASS $Name"}
try{
    $fresh=Join-Path $work 'A new installation'
    $null=Invoke-Helper -Directory $fresh
    Verify-Installed $fresh
    Pass 'fresh files and Decal registration'

    $custom=Join-Path $fresh 'Resources\BuffProfiles\mage.xml'
    [IO.File]::WriteAllText($custom,'<custom-profile/>')
    $character=Join-Path $fresh 'existing-character-config.xml'
    [IO.File]::WriteAllText($character,'<keep/>')
    Set-TestKey $plugin @{Object='Wrong.Class';Assembly='Wrong.dll';Enabled=0;Path=($fresh+'\DoThingsBot.dll')}
    Require ((Invoke-Helper Discover) -eq $fresh) 'Could not normalize DLL-valued Path'
    $null=Invoke-Helper
    Require ($script:code -eq 0) "Repair failed $script:output"
    Require ((Read-TestValue $plugin 'Path') -eq $fresh) 'Path repair failed'
    Require ((Read-TestValue $plugin 'Object') -eq 'DoThingsBot.PluginCore') 'Class repair failed'
    Require ((Read-TestValue $plugin 'Assembly') -eq 'DoThingsBot.dll') 'Assembly repair failed'
    Require ((Read-TestValue $plugin 'Enabled') -eq 1) 'Enabled repair failed'
    Require ([IO.File]::ReadAllText($custom) -eq '<custom-profile/>') 'Customized profile overwritten'
    Require ([IO.File]::ReadAllText($character) -eq '<keep/>') 'Existing settings overwritten'
    Pass 'update, invalid fields, DLL-valued path and custom files'

    $null=Invoke-Helper
    Require ($script:code -eq 0) 'Repeat install failed'
    Pass 'repeat installation'

    Set-TestKey $plugin @{Path=(Join-Path $work 'stale')}
    Require ((Invoke-Helper Discover) -eq $fresh) 'Metadata fallback failed'
    $null=Invoke-Helper
    Require ((Read-TestValue $plugin 'Path') -eq $fresh) 'Stale registration not repaired'
    Pass 'stale Decal path recovered from installer metadata'

    Set-TestKey $plugin @{Path='C:\bad|registration'}
    Require ((Invoke-Helper Discover) -eq $fresh) 'Malformed registration prevented discovery'
    Pass 'malformed registry path recovered'

    Reset-Test
    $alternate="$testRoot\Decal\Plugins\{11111111-2222-3333-4444-555555555555}"
    Set-TestKey $alternate @{Assembly='DoThingsBot.dll';Object='Wrong.Class';Path=$fresh;Enabled=1}
    $null=Invoke-Helper
    Require ($script:code -eq 0) "Alternate registration update failed $script:output"
    Require ((Read-TestValue $alternate 'Object') -eq 'DoThingsBot.PluginCore') 'Existing registration was not repaired'
    Require ((Read-TestValue $alternate 'Enabled') -eq 1) 'Existing registration was not enabled'
    Require ($null -eq (Read-TestValue $plugin 'Assembly')) 'Installer created a second plugin entry'
    Pass 'existing registration ID reused without adding another entry'

    Reset-Test
    $agent=Join-Path $work 'Game\Decal 3.0'
    Set-TestKey "$testRoot\Decal\Agent" @{AgentPath=$agent}
    $expected=Join-Path $work 'Game\Decal Plugins\DoThingsBot'
    Require ((Invoke-Helper Discover) -eq $expected) 'Fresh Decal-derived destination wrong'
    $null=Invoke-Helper
    Verify-Installed $expected
    Pass 'new installation beside detected Decal location'

    Set-TestKey $plugin @{Path=(Join-Path $work 'missing')}
    Require ((Invoke-Helper Discover) -eq $expected) 'Could not recover nearby installation'
    Pass 'stale registration recovered beside Decal'

    Reset-Test
    $loaderKey="$testRoot\Decal\NetworkFilters\{57937a36-b956-4322-a059-ec2297d23f0d}"
    Set-TestKey $loaderKey @{Assembly='DoThingsBot.Loader.dll';Object='Wrong.Class';Path=$fresh;Enabled=1}
    [IO.File]::WriteAllText((Join-Path $fresh 'DoThingsBot.Loader.dll'),'existing loader fixture')
    Set-TestKey $plugin @{Assembly='DoThingsBot.dll';Path=$fresh;Enabled=1}
    $null=Invoke-Helper
    Require ($script:code -eq 0) "Loader update failed $script:output"
    Require ((Read-TestValue $loaderKey 'Object') -eq 'DoThingsBot.LoaderCore') 'Loader entry not repaired'
    Require ((Read-TestValue $loaderKey 'Enabled') -eq 1) 'Loader disabled'
    Require ((Read-TestValue $plugin 'Enabled') -eq 0) 'Duplicate plugin left enabled'
    Pass 'existing Helix loader and duplicate registrations'

    Reset-Test
    $broken=Join-Path $work 'A partial installation'
    New-Item -ItemType Directory -Path $broken | Out-Null
    Set-TestKey $plugin @{Path=$broken;Object='Old.Class'}
    $null=Invoke-Helper
    Verify-Installed $broken
    Pass 'missing plugin files installed into registered folder'

    [IO.File]::WriteAllText((Join-Path $broken 'DoThingsBot.dll'),'old plugin')
    $locked=[IO.File]::Open((Join-Path $broken 'Newtonsoft.Json.dll'),'Open','ReadWrite','None')
    try{$null=Invoke-Helper}finally{$locked.Dispose()}
    Require ($script:code -ne 0) 'Locked-file update unexpectedly succeeded'
    Require ([IO.File]::ReadAllText((Join-Path $broken 'DoThingsBot.dll')) -eq 'old plugin') 'Earlier file replacement not rolled back'
    Require ((Read-TestValue $plugin 'Path') -eq $broken) 'Registration changed after failed update'
    Pass 'failed update restores previously replaced files'

    # Compile the production wizard with a test-only user registry root.
    $testExe=Join-Path $work 'Setup-Test.exe'
    $arguments=@('/V2',('"/DTEST_ROOT='+$testRoot+'"'),('"/DPAYLOAD='+$Payload+'"'),
        ('"/DOUTPUT='+$testExe+'"'),('"'+(Join-Path (Split-Path -Parent $Helper) 'Install.nsi')+'"'))
    $build=Start-Process -FilePath $MakeNsis -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
    Require ($build.ExitCode -eq 0) 'Wizard test build failed'
    $native=Join-Path $work 'Native setup destination'
    $run=Start-Process -FilePath $testExe -ArgumentList @('/S',('/D='+$native)) -WindowStyle Hidden -Wait -PassThru
    $script:code=$run.ExitCode
    $script:output=Get-Content -LiteralPath (Join-Path $env:TEMP 'DoThingsBot-Setup.log') -Raw -ErrorAction SilentlyContinue
    Verify-Installed $native
    Pass 'compiled installer entry point and directory override'
    $run=Start-Process -FilePath $testExe -ArgumentList '/S' -WindowStyle Hidden -Wait -PassThru
    Require ($run.ExitCode -eq 0) 'Native automatic destination failed'
    Require ((Read-TestValue $plugin 'Path') -eq $native) 'Native installer ignored detected destination'
    Pass 'compiled installer automatic destination and repeat update'
} finally {
    $reg.DeleteSubKeyTree($testRoot,$false)
    $reg.Dispose()
    $results | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $work 'results.json')
}
