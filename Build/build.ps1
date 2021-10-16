#Requires -Version 7.0

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [Alias('TargetFramework')]
    [ValidateSet('net5.0', 'netstandard2.0')]
    [ValidateLength(0, 2)]
    [string]$Framework = 'netstandard2.0',

    [version]$Version = '1.0.0.0'
)

[string]$ModuleName = 'PSSharp.WinXQuickLink'
[string]$BinaryProjectName = $ModuleName

Write-Host 'Identifying directories:'
$dir = @{
    'build' = $PSScriptRoot
}
$dir['project'] = Split-Path $dir['build'] -Parent
$dir['output'] = Join-Path $dir['build'] $ModuleName $Version
$dir['module'] = Join-Path $dir['project'] 'src' 'PowerShell'
$dir['binsrc'] = Join-Path $dir['project'] 'src' 'CSharp'
$dir['bin'] = Join-Path $dir['binsrc'] $BinaryProjectName 'bin' $Configuration $Framework 'publish'
$dir['docs'] = Join-Path $dir['project'] 'src' 'documentation'

Set-Location $dir['project']

dotnet publish --configuration $Configuration --framework $framework

if (Test-Path $dir['output']) {
    Remove-Item $dir['output'] -Force -Recurse -ErrorAction Stop
}

New-Item -ItemType Directory -Path $dir['output']
#Write-Host "Copying from '$($dir['module'])'..."
Get-ChildItem -Path "$($dir['module'])/*" | Copy-Item -Recurse -Destination $dir['output'] -PassThru
#Write-Host "Copying from '$($dir['bin'])'..."
Get-ChildItem -Path "$($dir['bin'])/*" -Filter '*.dll' | Copy-Item -Recurse -Destination $dir['output'] -Filter '*.dll' -Exclude 'System.Management.Automation.dll' -PassThru
#Write-Host "Copying from '$($dir['docs'])'..."
New-ExternalHelp -Path $dir['docs'] -OutputPath $dir['output']

function Get-PathFromOutputRoot {
    param(
        [Parameter( Mandatory,
                    ValueFromPipelineByPropertyName)]
        [string]
        $FullName
    )
    process {
        return $FullName.Replace($dir['output'], '', [System.StringComparison]::OrdinalIgnoreCase).Trim('/\')
    }
}

$Manifest = Import-PowerShellDataFile -Path (Join-Path $dir['module'] "$ModuleName.psd1") -ErrorAction Ignore
if (!$Manifest) { $Manifest = @{
    Author = 'Caleb Frederickson'
    License = 'https://licenses.nuget.org/MIT'
    Copyright = 'Caleb Frederickson (c) 2021'
    CompanyName = 'PSSharp'
} }
$Manifest['ModuleVersion'] = $Version
$Manifest['Path'] = Join-Path $dir['output'] "$ModuleName.psd1"
if (!$Manifest.ContainsKey('TypesToProcess')) {
    $Manifest['TypesToProcess'] = Get-ChildItem -Path $dir['output'] -Recurse -Filter '*.types.ps1xml' | Get-PathFromOutputRoot
}
if (!$Manifest.ContainsKey('FormatsToProcess')) {
    $Manifest['FormatsToProcess'] = Get-ChildItem -Path $dir['output'] -Recurse -Filter '*.format.ps1xml' | Get-PathFromOutputRoot
}
if (!$Manifest.ContainsKey('RootModule')) {
    $Manifest['RootModule'] = Get-Item -Path (Join-Path $dir['output'] "$BinaryProjectName.dll") | Get-PathFromOutputRoot
}
if (!$Manifest.ContainsKey('RequiredAssemblies')) {
    $Manifest['RequiredAssemblies'] = Get-ChildItem -Path $dir['output'] -Filter '*.dll' | Get-PathFromOutputRoot
}
$Manifest['FileList'] = Get-ChildItem -Path $dir['output'] -Recurse | Get-PathFromOutputRoot
if (Test-Path $Manifest['Path']) {
    Update-ModuleManifest @Manifest
}
else {
    New-ModuleManifest @Manifest
}