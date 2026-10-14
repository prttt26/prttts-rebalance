$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$modId = 'Rebalance_by_sts1_revival'
$workspace = Join-Path $projectRoot 'publish/prttts-rebalance'
$content = Join-Path $workspace 'content'
$output = Join-Path $projectRoot '.godot/mono/temp/bin/Release'

# The packer targets .NET 9; allow an installed newer runtime to run it.
$previousRollForward = $env:DOTNET_ROLL_FORWARD
try {
    $env:DOTNET_ROLL_FORWARD = 'Major'
    & dotnet build (Join-Path $projectRoot "$modId.csproj") -c Release --no-restore -p:DeployToGame=false
    if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
} finally {
    $env:DOTNET_ROLL_FORWARD = $previousRollForward
}

New-Item -ItemType Directory -Path $content -Force | Out-Null
foreach ($extension in @('dll', 'pck')) {
    $source = Join-Path $output "$modId.$extension"
    if (!(Test-Path -LiteralPath $source)) { throw "Missing build artifact: $source" }
    Copy-Item -LiteralPath $source -Destination $content -Force
}
Copy-Item -LiteralPath (Join-Path $projectRoot "$modId.json") -Destination $content -Force
$config = Get-Content (Join-Path $PSScriptRoot 'workshop.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$config.description = (Get-Content (Join-Path $PSScriptRoot 'description.english.txt') -Raw -Encoding UTF8).Trim()
$utf8 = New-Object System.Text.UTF8Encoding($false)
[IO.File]::WriteAllText((Join-Path $workspace 'workshop.json'), ($config | ConvertTo-Json -Depth 10), $utf8)
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'image.png') -Destination $workspace -Force
if ((Get-Item (Join-Path $workspace 'image.png')).Length -ge 1MB) { throw 'Workshop image must be less than 1MB.' }

# A folder named after the internal ID can be copied into the game's mods directory.
# Keep it outside official content so the two layouts cannot be uploaded together.
$localMod = Join-Path $projectRoot "publish/in-game-uploader/$modId"
New-Item -ItemType Directory -Path $localMod -Force | Out-Null
foreach ($extension in @('dll', 'pck', 'json')) {
    Copy-Item -LiteralPath (Join-Path $content "$modId.$extension") -Destination $localMod -Force
}
Write-Host "Prepared local mod folder: $localMod"
Write-Host "Prepared Workshop workspace: $workspace"
Get-ChildItem -LiteralPath $content | Select-Object Name, Length
