[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$ReferenceDirectory,
    [Parameter(Mandatory=$true)][string]$FrameworkReferences,
    [Parameter(Mandatory=$true)][string]$JsonAssembly,
    [Parameter(Mandatory=$true)][string]$Compiler,
    [Parameter(Mandatory=$true)][string]$MakeNsis,
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference='Stop'
$source=Split-Path -Parent $PSScriptRoot
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
$payload=Join-Path $OutputDirectory payload
New-Item -ItemType Directory -Path $payload -Force | Out-Null
[xml]$project=Get-Content -LiteralPath (Join-Path $source 'DoThingsBot.csproj') -Raw
$ns=[Xml.XmlNamespaceManager]::new($project.NameTable)
$ns.AddNamespace('m','http://schemas.microsoft.com/developer/msbuild/2003')
$argsList=[Collections.Generic.List[string]]::new()
foreach($option in '/target:library','/platform:x86','/unsafe+','/optimize+','/nostdlib+','/langversion:7.3','/define:TRACE;VVS_REFERENCED'){
    $argsList.Add($option)
}
$argsList.Add('/out:"'+(Join-Path $payload 'DoThingsBot.dll')+'"')
foreach($name in 'mscorlib','System','System.Core','System.Xml','System.Drawing','System.Windows.Forms','Accessibility','Microsoft.CSharp'){
    $dll=Get-Item -LiteralPath (Join-Path $FrameworkReferences ($name+'.dll'))
    $argsList.Add('/reference:"'+$dll.FullName+'"')
}
foreach($dll in Get-ChildItem -LiteralPath $ReferenceDirectory -Filter '*.dll' | Where-Object {$_.Name -like 'Decal.*' -or $_.Name -eq 'VirindiViewService.dll'}){
    $argsList.Add('/reference:"'+$dll.FullName+'"')
}
$argsList.Add('/reference:"'+$JsonAssembly+'"')
foreach($node in $project.SelectNodes('//m:Compile',$ns)){
    $argsList.Add('"'+(Join-Path $source $node.Include)+'"')
}
foreach($node in $project.SelectNodes('//m:EmbeddedResource',$ns)){
    $name='DoThingsBot.'+$node.Include.Replace('\','.')
    $argsList.Add('/resource:"'+(Join-Path $source $node.Include)+'",'+$name)
}
$response=Join-Path $OutputDirectory 'compile.rsp'
$argsList | Set-Content -LiteralPath $response -Encoding UTF8
& dotnet $Compiler "@$response" > (Join-Path $OutputDirectory 'compile.log')
if($LASTEXITCODE){throw 'Plugin build failed. See compile.log.'}
Copy-Item -LiteralPath $JsonAssembly -Destination (Join-Path $payload 'Newtonsoft.Json.dll') -Force
Copy-Item -LiteralPath (Join-Path $source 'Resources') -Destination $payload -Recurse -Force
Copy-Item -LiteralPath (Join-Path $source 'LICENSE') -Destination $payload -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.txt') -Destination $payload -Force
$sourceArchive=Join-Path $payload 'Source.zip'
if(Test-Path -LiteralPath $sourceArchive){Remove-Item -LiteralPath $sourceArchive}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip=[IO.Compression.ZipFile]::Open($sourceArchive,[IO.Compression.ZipArchiveMode]::Create)
try{
    foreach($file in Get-ChildItem -LiteralPath $source -File -Recurse){
        if($file.FullName.StartsWith($OutputDirectory+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){continue}
        $relative=$file.FullName.Substring($source.Length+1)
        if($relative -match '(^|\\)(bin|obj|\.git)(\\|$)'){continue}
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$file.FullName,$relative.Replace('\','/')) | Out-Null
    }
} finally {$zip.Dispose()}
$manifest=@(Get-ChildItem -LiteralPath $payload -File -Recurse | Where-Object {$_.Name -ne 'manifest.json'} | ForEach-Object{
    $relative=$_.FullName.Substring($payload.Length+1)
    [ordered]@{Path=$relative;Sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash;
        PreserveExisting=$relative.StartsWith('Resources\',[StringComparison]::OrdinalIgnoreCase)}
})
$manifest | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $payload 'manifest.json') -Encoding UTF8
$installer=Join-Path $OutputDirectory 'DoThingsBot-2.0.0.17-Setup.exe'
$nsisArguments=@('/V2',('"/DPAYLOAD='+$payload+'"'),('"/DOUTPUT='+$installer+'"'),('"'+(Join-Path $PSScriptRoot 'Install.nsi')+'"'))
$process=Start-Process -FilePath $MakeNsis -ArgumentList $nsisArguments -WindowStyle Hidden -Wait -PassThru -RedirectStandardOutput (Join-Path $OutputDirectory 'installer-compile.log') -RedirectStandardError (Join-Path $OutputDirectory 'installer-errors.log')
if($process.ExitCode){throw 'Installer build failed. See installer-compile.log and installer-errors.log.'}
Get-Item -LiteralPath $installer | Select-Object FullName,Length
