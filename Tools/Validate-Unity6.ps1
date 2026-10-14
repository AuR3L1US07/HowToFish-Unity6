param([Parameter(Mandatory=$true)][string]$UnityEditorPath)
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path -Parent $PSScriptRoot
$editorFile = Get-Item -LiteralPath $UnityEditorPath
if ($editorFile.Name -ne 'Unity.exe') { throw 'Provide the full path to Unity 6000.4.4f1 Editor/Unity.exe.' }
$editorReportedVersion = $editorFile.VersionInfo.ProductVersion
if ($editorReportedVersion -notmatch '^6000\.4\.4') { throw "This project targets 6000.4.4f1. Detected editor product version: $editorReportedVersion" }
$logDirectory = Join-Path $projectPath 'CompilationChecks'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$importLog = Join-Path $logDirectory 'unity-editor-import.log'
$editorArguments = @('-batchmode','-nographics','-quit','-projectPath',('"' + $projectPath + '"'),'-logFile',('"' + $importLog + '"'))
$editorProcess = Start-Process -FilePath $editorFile.FullName -ArgumentList $editorArguments -WindowStyle Hidden -Wait -PassThru
$compileErrors = if (Test-Path -LiteralPath $importLog) {@(Select-String -LiteralPath $importLog -Pattern 'error CS\d+|Scripts have compiler errors|compilation failed|Error while compiling')} else {@()}
$licensingIssues = if (Test-Path -LiteralPath $importLog) {@(Select-String -LiteralPath $importLog -Pattern 'No valid Unity Editor license|No valid license|License is not active')} else {@()}
$status = if ($editorProcess.ExitCode -eq 0 -and $compileErrors.Count -eq 0 -and $licensingIssues.Count -eq 0 -and (Test-Path -LiteralPath $importLog)) {'editor-import-command-succeeded-review-log'} else {'failed-review-log'}
$result = [pscustomobject]@{editor=$editorFile.FullName;reportedVersion=$editorReportedVersion;exitCode=$editorProcess.ExitCode;compilerErrorLines=$compileErrors.Count;licensingErrorLines=$licensingIssues.Count;status=$status;log=$importLog;playerBuildTested=$false;gameplayTested=$false}
$result | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $logDirectory 'unity-editor-validation.json') -Encoding utf8
$result | ConvertTo-Json
if ($status -eq 'failed-review-log') { exit 1 }
