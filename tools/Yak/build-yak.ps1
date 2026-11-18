# Builds the Rhino Package Manager (Yak) package for Hezarbaaf 1.0.
# Run on Windows with Rhino 8 installed:   powershell -ExecutionPolicy Bypass -File tools\Yak\build-yak.ps1
$ErrorActionPreference = "Stop"
$root = Resolve-Path "$PSScriptRoot\..\.."
$out  = Join-Path $root "dist\yak"
$yak  = "C:\Program Files\Rhino 8\System\Yak.exe"
if (!(Test-Path $yak)) { throw "Yak.exe not found at $yak (install Rhino 8)" }

dotnet build "$root\src\MasonryGH" -c Release            # 1.0 build: labs (karbandi, muqarnas) left out
Remove-Item $out -Recurse -Force -ErrorAction SilentlyContinue
New-Item $out -ItemType Directory | Out-Null

# Rhino 8 loads the net7.0 build; net48 for Rhino 7 / Rhino 8 in .NET Framework mode
foreach ($tf in "net7.0", "net48") {
  $dst = Join-Path $out $tf; New-Item $dst -ItemType Directory | Out-Null
  Copy-Item "$root\src\MasonryGH\bin\Release\$tf\Hezarbaaf.gha" $dst
  Copy-Item "$root\src\MasonryGH\bin\Release\$tf\Masonry.Core.dll" $dst
}
Copy-Item "$PSScriptRoot\manifest.yml" $out
Copy-Item "$root\src\MasonryGH\Icons\Hezarbaaf.png" "$out\icon.png"
Copy-Item "$root\LICENSE" $out
Copy-Item "$root\examples\*.ghx" $out

Push-Location $out
& $yak build --platform win
& $yak build --platform mac
Pop-Location
Write-Host "Packages in $out. Publish with:  `"$yak`" login  then  `"$yak`" push <file>.yak"
