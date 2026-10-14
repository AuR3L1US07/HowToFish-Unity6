$ErrorActionPreference = 'Stop'
$projectRoot = (Split-Path -Parent $PSScriptRoot)
$managedRoot = (Join-Path $projectRoot 'ReferenceOnly\original-artifacts\How to Fish_Data\Managed')
$checkRoot = Join-Path $projectRoot 'CompilationChecks'
New-Item -ItemType Directory -Path $checkRoot -Force | Out-Null
$projectPlugins = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets\Plugins') -Filter '*.dll')
$engineAndFramework = @(Get-ChildItem -LiteralPath $managedRoot -Filter '*.dll' | Where-Object { $_.Name -notin @('Assembly-CSharp.dll', 'Assembly-CSharp-firstpass.dll', 'Unity.MonoScript.dll') -and $_.Name -notin $projectPlugins.Name })
$compileReferences = @($projectPlugins) + @($engineAndFramework)
$compilerArgs = [Collections.Generic.List[string]]::new()
$compilerArgs.Add('/nostdlib+')
$compilerArgs.Add('/target:library')
$compilerArgs.Add('/unsafe+')
$compilerArgs.Add('/langversion:9.0')
$compilerArgs.Add('/define:UNITY_EDITOR;UNITY_EDITOR_WIN;UNITY_STANDALONE;UNITY_STANDALONE_WIN;UNITY_6000_4;UNITY_6000_0_OR_NEWER')
$firstPassArgs = [Collections.Generic.List[string]]::new()
foreach ($argument in $compilerArgs) { $firstPassArgs.Add($argument) }
$firstPassArgs.Add('/out:"' + (Join-Path $checkRoot 'Assembly-CSharp-firstpass.dll') + '"')
foreach ($referenceFile in $compileReferences) {
    $firstPassArgs.Add('/reference:"' + $referenceFile.FullName + '"')
}
foreach ($sourceFile in Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets\Plugins\Assembly-CSharp-firstpass') -Recurse -Filter '*.cs') {
    $firstPassArgs.Add('"' + $sourceFile.FullName + '"')
}
$firstPassResponse = Join-Path $checkRoot 'firstpass.rsp'
[IO.File]::WriteAllLines($firstPassResponse, $firstPassArgs, [Text.UTF8Encoding]::new($false))
& 'C:\Program Files\dotnet\dotnet.exe' (Join-Path $PSScriptRoot 'Compiler\csc.dll') /noconfig ('@' + $firstPassResponse) *> (Join-Path $checkRoot 'firstpass-compile.log')
if ($LASTEXITCODE -ne 0) { Get-Content -LiteralPath (Join-Path $checkRoot 'firstpass-compile.log') -TotalCount 30; exit $LASTEXITCODE }
$compilerArgs.Add('/out:"' + (Join-Path $checkRoot 'Assembly-CSharp.dll') + '"')
$compilerArgs.Add('/reference:"' + (Join-Path $checkRoot 'Assembly-CSharp-firstpass.dll') + '"')
foreach ($referenceFile in $compileReferences) {
    $compilerArgs.Add('/reference:"' + $referenceFile.FullName + '"')
}
foreach ($sourceFile in Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets\Scripts') -Recurse -Filter '*.cs') {
    $compilerArgs.Add('"' + $sourceFile.FullName + '"')
}
$responseFile = Join-Path $checkRoot 'sources.rsp'
[IO.File]::WriteAllLines($responseFile, $compilerArgs, [Text.UTF8Encoding]::new($false))
& 'C:\Program Files\dotnet\dotnet.exe' (Join-Path $PSScriptRoot 'Compiler\csc.dll') /noconfig ('@' + $responseFile) *> (Join-Path $checkRoot 'compile.log')
$compileExitCode = $LASTEXITCODE
Get-Content -LiteralPath (Join-Path $checkRoot 'compile.log') | Select-String -Pattern 'error CS' | Select-Object -First 30
Write-Output "Compiler exit code: $compileExitCode"
exit $compileExitCode
