param([string]$ProjectRoot = (Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference = 'Stop'
$ProjectRoot = (Resolve-Path -LiteralPath $ProjectRoot).Path
$shaderRows = @()
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'Assets/Shader') -Filter '*.shader') {
    $source = [IO.File]::ReadAllText($file.FullName)
    $status = 'unclassified'
    if ($source.Contains('DummyShaderTextExporter')) { $status = 'export-placeholder' }
    elseif ($source -match 'Recovery (fallback|implementation)') { $status = 'approximate-reconstruction' }
    else { $status = 'no-recovery-marker-requires-provenance-check' }
    $shaderRows += [ordered]@{ path = "Assets/Shader/$($file.Name)"; status = $status }
}
$vfxRows = @()
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'Assets') -Recurse -File | Where-Object { $_.Extension -in '.unity','.prefab' }) {
    $source = [IO.File]::ReadAllText($file.FullName)
    foreach ($match in [regex]::Matches($source, '(?ms)^--- !u!2083052967 &([^\r\n]+)\r?\nVisualEffect:\r?\n(.*?)(?=^--- !u!|\z)')) {
        $body = $match.Groups[2].Value
        $vfxRows += [ordered]@{
            path = $file.FullName.Substring($ProjectRoot.Length + 1).Replace('\','/')
            fileId = $match.Groups[1].Value
            enabled = [regex]::Match($body, 'm_Enabled: (\d+)').Groups[1].Value -eq '1'
            assetGuid = [regex]::Match($body, 'm_Asset:.*guid: ([a-f0-9]+)').Groups[1].Value
        }
    }
}
$result = [ordered]@{
    scope = 'Static source and serialization audit; no editor compilation or visual equivalence claim.'
    placeholders = @($shaderRows | Where-Object { $_.status -eq 'export-placeholder' }).Count
    approximateShaders = @($shaderRows | Where-Object { $_.status -eq 'approximate-reconstruction' }).Count
    disabledVisualEffects = @($vfxRows | Where-Object { -not $_.enabled }).Count
    shaders = $shaderRows
    visualEffects = $vfxRows
    originalEquivalent = $false
}
$output = Join-Path $ProjectRoot 'CompilationChecks/visual-completeness-audit.json'
$result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $output -Encoding utf8
Write-Output "Placeholders: $($result.placeholders); approximate shaders: $($result.approximateShaders); disabled VisualEffects: $($result.disabledVisualEffects)"
Write-Output $output
