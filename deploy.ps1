$branch = git branch --show-current
$targetDirectory = "$env:APPDATA\SpaceEngineers\Mods\AssembleProjection"

if ($branch -ne "master") {
    $targetDirectory = $targetDirectory + "Experimental"
}

Remove-Item -LiteralPath $targetDirectory -Force -Recurse
mkdir $targetDirectory | Out-Null
Copy-Item -Path "./Data","./Textures","./metadata.mod","./thumb.jpg" -Recurse -Destination $targetDirectory
Write-Host "Deployed to $targetDirectory"
