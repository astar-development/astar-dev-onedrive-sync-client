param(
    [ValidateSet("major", "minor", "patch")]
    [string]$Part = "patch"
)

$file = "$PSScriptRoot/src/Directory.Build.props"
$xml = [xml](Get-Content $file)
$version = $xml.Project.PropertyGroup.Version
$parts = $version -split "\."

switch ($Part) {
    "major" { $parts[0] = [int]$parts[0] + 1; $parts[1] = 0; $parts[2] = 0 }
    "minor" { $parts[1] = [int]$parts[1] + 1; $parts[2] = 0 }
    "patch" { $parts[2] = [int]$parts[2] + 1 }
}

$newVersion = $parts -join "."
$xml.Project.PropertyGroup.Version = $newVersion
$xml.Save($file)

Write-Host "Version bumped: $version → $newVersion"
