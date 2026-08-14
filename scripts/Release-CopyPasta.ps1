#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

$Root = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Join-Path $Root 'CopyPastaNative.csproj'))) {
    $Root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
}

$Project = Join-Path $Root 'CopyPastaNative.csproj'
$TestProject = Join-Path $Root 'CopyPastaNative.Tests\CopyPastaNative.Tests.csproj'
$PublishDir = Join-Path $Root 'artifacts\CopyPasta_v0.3.0'
$ZipPath = Join-Path $Root 'artifacts\CopyPasta_v0.3.0.zip'

Write-Host "CopyPasta v0.3.0 local release" -ForegroundColor Cyan
Write-Host "Root: $Root"

function Assert-NoUnsafeBinaryFormatter([string]$RuntimeConfigPath) {
    $json = Get-Content -Raw $RuntimeConfigPath | ConvertFrom-Json
    $flag = $json.runtimeOptions.configProperties.'System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization'
    if ($flag -eq $true) {
        throw "Unsafe BinaryFormatter serialization is enabled in $RuntimeConfigPath"
    }
}

Push-Location $Root
try {
    $commit = (git rev-parse HEAD).Trim()

    Write-Host "`n== clean / restore / build =="
    dotnet clean $Project --configuration Release | Out-Host
    dotnet restore $Project | Out-Host
    dotnet build $Project --configuration Release --no-restore | Out-Host

    Write-Host "`n== tests =="
    dotnet test $TestProject --configuration Release --no-restore | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "Tests failed." }

    Write-Host "`n== vulnerability audit =="
    $vulnOutput = & dotnet package list --project $Project --include-transitive --vulnerable 2>&1 | Out-String
    Write-Host $vulnOutput
    if ($LASTEXITCODE -ne 0) {
        throw "Vulnerability audit command failed (exit $LASTEXITCODE)."
    }
    if ($vulnOutput -match 'has the following vulnerable packages') {
        throw "Vulnerable packages were reported. High/Critical findings must be resolved."
    }
    if ($vulnOutput -notmatch 'has no vulnerable packages') {
        throw "Vulnerability audit did not confirm a clean result. Inspect the output above."
    }

    if (Test-Path $PublishDir) {
        Remove-Item $PublishDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $PublishDir | Out-Null

    Write-Host "`n== publish =="
    dotnet publish $Project --configuration Release -r win-x64 --self-contained true -o $PublishDir --nologo | Out-Host

    $runtimeConfig = Join-Path $PublishDir 'CopyPastaNative.runtimeconfig.json'
    Assert-NoUnsafeBinaryFormatter $runtimeConfig

    Get-ChildItem $PublishDir -Recurse -Include *.pdb, *.env, *.user, *.pubxml | Remove-Item -Force -ErrorAction SilentlyContinue

    $exe = Join-Path $PublishDir 'CopyPastaNative.exe'
    if (-not (Test-Path $exe)) {
        throw "Published executable not found: $exe"
    }

    if (Test-Path $ZipPath) {
        Remove-Item $ZipPath -Force
    }
    Compress-Archive -Path (Join-Path $PublishDir '*') -DestinationPath $ZipPath

    $exeHash = (Get-FileHash $exe -Algorithm SHA256).Hash
    $zipHash = (Get-FileHash $ZipPath -Algorithm SHA256).Hash
    $inventory = Join-Path $Root 'artifacts\DEPENDENCIES.md'
    $packages = & dotnet package list --project $Project --include-transitive | Out-String
    @"
# CopyPasta 0.3.0 dependency inventory

Git commit: ``$commit``

``````
$packages
``````
"@ | Set-Content -Path $inventory -Encoding UTF8

    $notes = Join-Path $Root 'artifacts\RELEASE_HASHES.md'
    @"
# CopyPasta v0.3.0 integrity

- Git commit: ``$commit``
- CopyPastaNative.exe SHA-256: ``$exeHash``
- CopyPasta_v0.3.0.zip SHA-256: ``$zipHash``

Do not modify binaries after these hashes are generated.
Until Authenticode signing is available, administrators should verify these hashes.
"@ | Set-Content -Path $notes -Encoding UTF8

    Write-Host "`nPublish folder: $PublishDir"
    Write-Host "Zip: $ZipPath"
    Write-Host "Commit: $commit"
    Write-Host "EXE SHA-256: $exeHash"
    Write-Host "ZIP SHA-256: $zipHash"
    Write-Host "`nManual verification still required: standard-user launch, no UAC, no leftover process, network disconnected still works."
}
finally {
    Pop-Location
}
