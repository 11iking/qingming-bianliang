param([string]$EditorRoot = 'C:\Users\lty06\Downloads\Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$editorData = Join-Path $EditorRoot 'Data'
$dotnetExe = Join-Path $editorData 'NetCoreRuntime\dotnet.exe'
$compiler = Join-Path $editorData 'DotNetSdkRoslyn\csc.dll'
$references = @(
    Get-ChildItem -LiteralPath (Join-Path $editorData 'NetStandard\ref\2.1.0') -Filter '*.dll'
    Get-ChildItem -LiteralPath (Join-Path $editorData 'Managed\UnityEngine') -Filter '*.dll'
    Get-Item -LiteralPath (Join-Path $editorData 'Managed\Newtonsoft.Json.dll')
)
$sources = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'UnityProject\Assets\Scripts') -Filter '*.cs'
$response = @(
    '-nologo'
    '-target:library'
    '-langversion:9'
    '-nostdlib+'
    '-out:"' + (Join-Path $PSScriptRoot 'Qingming.Runtime.Check.dll') + '"'
)
$response += $references | ForEach-Object { '-r:"' + $_.FullName + '"' }
$response += $sources | ForEach-Object { '"' + $_.FullName + '"' }
$responsePath = Join-Path $PSScriptRoot 'RuntimeCheck.rsp'
[IO.File]::WriteAllLines($responsePath, $response, [Text.UTF8Encoding]::new($false))
$compilerOutput = & $dotnetExe $compiler "@$responsePath" 2>&1
$resultCode = $LASTEXITCODE
$compilerOutput | Out-File -LiteralPath (Join-Path $PSScriptRoot 'CSharpCompileCheck.txt') -Encoding utf8
$compilerOutput | Write-Output
if ($resultCode -ne 0) { throw "Runtime source compilation failed with exit code $resultCode" }
Write-Output 'PASS: runtime C# compiled against installed editor assemblies; editor import, shaders and execution are not covered.'
$importResponse = @(
    '-nologo'
    '-target:library'
    '-langversion:9'
    '-nostdlib+'
    '-out:"' + (Join-Path $PSScriptRoot 'Qingming.Importer.Check.dll') + '"'
    '-r:"' + (Join-Path $PSScriptRoot 'Qingming.Runtime.Check.dll') + '"'
)
$importResponse += $references | ForEach-Object { '-r:"' + $_.FullName + '"' }
$importResponse += '"' + (Join-Path $projectRoot 'UnityProject\Assets\Editor\ImportGodotWorld.cs') + '"'
$importResponsePath = Join-Path $PSScriptRoot 'ImporterCheck.rsp'
[IO.File]::WriteAllLines($importResponsePath, $importResponse, [Text.UTF8Encoding]::new($false))
$importOutput = & $dotnetExe $compiler "@$importResponsePath" 2>&1
$importResult = $LASTEXITCODE
$importOutput | Out-File -LiteralPath (Join-Path $PSScriptRoot 'ImporterCompileCheck.txt') -Encoding utf8
$importOutput | Write-Output
if ($importResult -ne 0) { throw "Importer source compilation failed with exit code $importResult" }
Write-Output 'PASS: importer C# compiled against installed editor assemblies; MCP plugin and actual scene import are not covered.'
