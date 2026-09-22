param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'artifacts\TeachLens-v1.0-Acorn')
)

$ErrorActionPreference = 'Stop'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$sourceDirectory = Join-Path $PSScriptRoot 'src'
$manifestPath = Join-Path $sourceDirectory 'app.manifest'
$mainSource = Join-Path $sourceDirectory 'TeachLens.cs'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'The .NET Framework x64 compiler was not found.'
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$iconBuilder = Join-Path $OutputDirectory 'TeachLens.IconBuilder.exe'
$iconPath = Join-Path $OutputDirectory 'TeachLens.ico'
& $compiler `
    /nologo `
    /target:exe `
    /platform:x64 `
    /optimize+ `
    /reference:System.dll `
    /reference:System.Drawing.dll `
    /out:"$iconBuilder" `
    (Join-Path $sourceDirectory 'IconBuilder.cs')

if ($LASTEXITCODE -ne 0) {
    throw "Icon builder compilation failed with exit code $LASTEXITCODE."
}

& $iconBuilder $iconPath
if ($LASTEXITCODE -ne 0) {
    throw "Icon generation failed with exit code $LASTEXITCODE."
}

$executable = Join-Path $OutputDirectory 'TeachLens.exe'
& $compiler `
    /nologo `
    /target:winexe `
    /platform:x64 `
    /optimize+ `
    /win32manifest:"$manifestPath" `
    /win32icon:"$iconPath" `
    /reference:System.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    /out:"$executable" `
    "$mainSource"

if ($LASTEXITCODE -ne 0) {
    throw "TeachLens compilation failed with exit code $LASTEXITCODE."
}

Remove-Item -LiteralPath $iconBuilder -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination (Join-Path $OutputDirectory 'README.md') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.vi.md') -Destination (Join-Path $OutputDirectory 'README.vi.md') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'LICENSE') -Destination (Join-Path $OutputDirectory 'LICENSE') -Force

$hash = Get-FileHash -Algorithm SHA256 -LiteralPath $executable
Set-Content -LiteralPath (Join-Path $OutputDirectory 'SHA256SUMS.txt') -Value ($hash.Hash.ToLowerInvariant() + '  TeachLens.exe') -Encoding ascii

Write-Output $executable
