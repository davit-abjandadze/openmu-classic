<#
.SYNOPSIS
Builds the client package of Old MU for the players: the game client, the launcher and its launcher.json, as one zip.

.DESCRIPTION
The launcher is published from launcher/OldMuLauncher as a self-contained single file, so the players don't need
to install .NET. launcher.json gets the address of the server, the website and the Discord link.
The client is copied without the backups and debug files of the development (backup-*, *.bak*, *.pdb, *.map, *.lib).

The zip contains one folder "OldMu": unpack it anywhere and start OldMuLauncher.exe.

.PARAMETER ServerHost
The domain or public IPv4 address of the server (PUBLIC_HOST of the deployment). 127.127.127.127 for a local test.

.PARAMETER WebsiteUrl
The address of the website. By default the server address itself, where the deployment serves the website on port 80.

.PARAMETER DiscordUrl
The Discord invitation. Without it, the Discord button of the launcher does nothing.

.EXAMPLE
.\New-ClientPackage.ps1 -ServerHost play.example.com -WebsiteUrl https://play.example.com -DiscordUrl https://discord.gg/abc
#>
param(
    [Parameter(Mandatory = $true)]
    [string] $ServerHost,

    [int] $Port = 44406,

    [string] $ServerName = 'Old MU',

    [string] $WebsiteUrl = '',

    [string] $DiscordUrl = '',

    [string] $ClientPath = (Join-Path $env:USERPROFILE 'Desktop\OldMu\Client'),

    [string] $OutputPath = (Join-Path $env:USERPROFILE 'Desktop\OldMu\dist\OldMu.zip')
)

$ErrorActionPreference = 'Stop'

if (-not $WebsiteUrl) {
    $WebsiteUrl = "http://$ServerHost"
}

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$launcherProject = Join-Path $repositoryRoot 'launcher\OldMuLauncher\OldMuLauncher.csproj'
if (-not (Test-Path (Join-Path $ClientPath 'Main.exe'))) {
    throw "No Main.exe in '$ClientPath'. Pass the client folder with -ClientPath."
}

$work = Join-Path ([System.IO.Path]::GetTempPath()) ('oldmu-package-' + [System.Guid]::NewGuid().ToString('N'))
$package = Join-Path $work 'OldMu'
New-Item -ItemType Directory -Force $package | Out-Null

try {
    Write-Host 'Publishing the launcher...'
    & dotnet publish $launcherProject -c Release -r win-x64 --self-contained true -nologo -v q `
        -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $package
    if ($LASTEXITCODE -ne 0) {
        throw "The launcher could not be published (exit code $LASTEXITCODE)."
    }

    $settings = [ordered]@{
        Servers         = @([ordered]@{ Name = $ServerName; Host = $ServerHost; Port = $Port })
        SelectedServer  = 0
        ClientPath      = 'Client\Main.exe'
        WebsiteUrl      = $WebsiteUrl
        DiscordUrl      = $DiscordUrl
        CloseAfterStart = $true
    }
    $json = ConvertTo-Json $settings -Depth 4
    [System.IO.File]::WriteAllText((Join-Path $package 'launcher.json'), $json, (New-Object System.Text.UTF8Encoding $false))

    Write-Host "Copying the client from $ClientPath..."
    $clientTarget = Join-Path $package 'Client'
    & robocopy $ClientPath $clientTarget /E /NFL /NDL /NJH /NJS /NP `
        /XD 'backup-*' `
        /XF '*.bak*' '*.old-build' '*.pdb' '*.map' '*.lib' '*.log' 'config.ini' | Out-Null

    # Robocopy returns 0 to 7 for success.
    if ($LASTEXITCODE -ge 8) {
        throw "The client could not be copied (robocopy exit code $LASTEXITCODE)."
    }

    # Otherwise the script would end with the robocopy code 1 (files copied), which callers take as a failure.
    $global:LASTEXITCODE = 0

    $readme = @"
Old MU
======

1. Unpack this folder anywhere, e.g. to C:\Games\OldMu.
2. Start OldMuLauncher.exe and press Start.

Windows 10 or 11 (64 bit) is required. Create your account on the website first.
Settings and logs of the game are kept in %APPDATA%\MuMain.
"@
    [System.IO.File]::WriteAllText((Join-Path $package 'README.txt'), $readme.Replace("`n", "`r`n"), (New-Object System.Text.UTF8Encoding $false))

    Write-Host "Creating $OutputPath..."
    $outputDirectory = Split-Path -Parent $OutputPath
    if ($outputDirectory) {
        New-Item -ItemType Directory -Force $outputDirectory | Out-Null
    }

    if (Test-Path $OutputPath) {
        Remove-Item $OutputPath -Force
    }

    # The entries are added one by one instead of with ZipFile.CreateFromDirectory, which writes backslashes into
    # the entry names under Windows PowerShell. Not every unpacker turns them into folders.
    Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
    $root = (Get-Item $work).FullName.TrimEnd('\') + '\'
    $stream = [System.IO.File]::Open($OutputPath, [System.IO.FileMode]::CreateNew)
    try {
        $archive = New-Object System.IO.Compression.ZipArchive($stream, [System.IO.Compression.ZipArchiveMode]::Create)
        try {
            foreach ($file in Get-ChildItem $package -Recurse -File) {
                $entryName = $file.FullName.Substring($root.Length).Replace('\', '/')
                [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $entryName, [System.IO.Compression.CompressionLevel]::Optimal)
            }
        }
        finally {
            $archive.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }

    $zip = Get-Item $OutputPath
    $hash = (Get-FileHash $OutputPath -Algorithm SHA256).Hash
    Write-Host ('Done: {0} ({1:N0} MB), server {2}:{3}' -f $zip.FullName, ($zip.Length / 1MB), $ServerHost, $Port)
    Write-Host "SHA256: $hash"
}
finally {
    Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
}
