param([Parameter(Mandatory=$true)][string]$ProjectRoot)
$ErrorActionPreference = 'Stop'
$projectPath = (Resolve-Path -LiteralPath $ProjectRoot).Path
$assetsPath = Join-Path $projectPath 'Assets'
$sharedFiles = Get-ChildItem -LiteralPath (Join-Path $assetsPath 'Localization') -Recurse -Filter '*.asset'
$allMetas = Get-ChildItem -LiteralPath $assetsPath -Recurse -Filter '*.meta'
$guidOwners = @{}
foreach ($meta in $allMetas) {
    $match = [regex]::Match([IO.File]::ReadAllText($meta.FullName), '(?m)^guid: ([a-f0-9]{32})\s*$')
    if ($match.Success) { $guidOwners[$match.Groups[1].Value] = $meta.FullName }
}
$mapping = @{}
$metaEdits = @{}
foreach ($file in $sharedFiles) {
    $match = [regex]::Match([IO.File]::ReadAllText($file.FullName), '(?m)^  m_TableCollectionNameGuidString: ([a-f0-9]{32})\s*$')
    if (!$match.Success) { continue }
    $metaPath = $file.FullName + '.meta'
    $metaText = [IO.File]::ReadAllText($metaPath)
    $oldGuid = [regex]::Match($metaText, '(?m)^guid: ([a-f0-9]{32})\s*$').Groups[1].Value
    $newGuid = $match.Groups[1].Value
    if ($oldGuid -eq $newGuid) { continue }
    if ($guidOwners.ContainsKey($newGuid) -and $guidOwners[$newGuid] -ne $metaPath) { throw "GUID collision: $newGuid" }
    $mapping[$oldGuid] = $newGuid
    $metaEdits[$metaPath] = $metaText.Replace('guid: ' + $oldGuid, 'guid: ' + $newGuid)
}
$edits = @{}
foreach ($file in Get-ChildItem -LiteralPath $assetsPath -Recurse -File | Where-Object { $_.Extension -in @('.asset','.prefab','.unity','.mat','.meta','.controller','.overrideController') }) {
    $content = [IO.File]::ReadAllText($file.FullName)
    $updated = $content
    foreach ($oldGuid in $mapping.Keys) { $updated = $updated.Replace('guid: ' + $oldGuid, 'guid: ' + $mapping[$oldGuid]) }
    if ($updated -ne $content) { $edits[$file.FullName] = $updated }
}
foreach ($entry in $metaEdits.GetEnumerator()) { $edits[$entry.Key] = $entry.Value }
$backupPath = Join-Path $projectPath 'ReferenceOnly\BeforeLocalizationGuidRepair'
if ($mapping.Count -gt 0 -and (Test-Path -LiteralPath $backupPath)) { throw 'Repair backup already exists; review before repeating.' }
foreach ($entry in $edits.GetEnumerator()) {
    $relativePath = $entry.Key.Substring($projectPath.Length).TrimStart('\','/')
    $destination = Join-Path $backupPath $relativePath
    New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
    Copy-Item -LiteralPath $entry.Key -Destination $destination
    [IO.File]::WriteAllText($entry.Key,$entry.Value,[Text.UTF8Encoding]::new($false))
}
$checkPath = Join-Path $projectPath 'CompilationChecks'
New-Item -ItemType Directory -Path $checkPath -Force | Out-Null
@{ preservedTableIdentities=$mapping; changedFiles=@($edits.Keys); rationale='Align exported meta identities with original serialized TableCollectionNameGuid; preserve all table lookup identities.' } |
    ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $checkPath 'localization-guid-repair.json') -Encoding utf8
Write-Output "Aligned $($mapping.Count) table identities; updated $($edits.Count) files."
