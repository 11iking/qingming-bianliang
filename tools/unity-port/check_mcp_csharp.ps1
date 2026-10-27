param([string]$EditorRoot = 'C:\Users\lty06\Downloads\Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$editorData = Join-Path $EditorRoot 'Data'
$dotnetExe = Join-Path $editorData 'NetCoreRuntime\dotnet.exe'
$compiler = Join-Path $editorData 'DotNetSdkRoslyn\csc.dll'
$packageRoot = Join-Path $projectRoot 'tools\unity-mcp\MCPForUnity'
$references = @(
    Get-ChildItem -LiteralPath (Join-Path $editorData 'MonoBleedingEdge\lib\mono\4.5') -Filter '*.dll' | Where-Object { $_.Name -match '^(System|mscorlib|Microsoft.CSharp|netstandard)' -and $_.Name -notmatch '^(System.Collections.Immutable|System.Reflection.Metadata|System.Runtime.CompilerServices.Unsafe|System.Net.Http.Formatting)' }
    Get-ChildItem -LiteralPath (Join-Path $editorData 'MonoBleedingEdge\lib\mono\4.5\Facades') -Filter '*.dll'
    Get-ChildItem -LiteralPath (Join-Path $editorData 'Managed\UnityEngine') -Filter '*.dll'
    Get-Item -LiteralPath (Join-Path $editorData 'Managed\Newtonsoft.Json.dll')
)
$baseArgs = @('-nologo','-target:library','-langversion:9','-nostdlib+',
    '-define:UNITY_EDITOR,UNITY_EDITOR_WIN,UNITY_2022_1_OR_NEWER,UNITY_2022_2_OR_NEWER,UNITY_2022_3_OR_NEWER')
$baseArgs += $references | ForEach-Object { '-r:"' + $_.FullName + '"' }
function Compile-Check([string]$name, [string[]]$arguments) {
    $responsePath = Join-Path $PSScriptRoot ($name + '.rsp')
    $allArgs = $baseArgs + @('-out:"' + (Join-Path $PSScriptRoot ($name + '.dll')) + '"') + $arguments
    [IO.File]::WriteAllLines($responsePath, $allArgs, [Text.UTF8Encoding]::new($false))
    $output = & $dotnetExe $compiler "@$responsePath" 2>&1
    $result = $LASTEXITCODE
    $output | Out-File -LiteralPath (Join-Path $PSScriptRoot ($name + '.log')) -Encoding utf8
    $output | Write-Output
    if ($result -ne 0) { throw "$name failed with exit code $result" }
    Write-Output "PASS: $name compiled with installed editor reference assemblies"
}
$runtimeSources = Get-ChildItem -LiteralPath (Join-Path $packageRoot 'Runtime') -Recurse -Filter '*.cs' | ForEach-Object { '"' + $_.FullName + '"' }
Compile-Check 'MCPForUnity.Runtime' $runtimeSources
$editorArgs = @('-r:"' + (Join-Path $PSScriptRoot 'MCPForUnity.Runtime.dll') + '"')
$editorArgs += Get-ChildItem -LiteralPath (Join-Path $projectRoot 'UnityProject\Assets\Plugins\Roslyn') -Filter '*.dll' | ForEach-Object { '-r:"' + $_.FullName + '"' }
$templateAssemblies = Join-Path $editorData 'Resources\PackageManager\ProjectTemplates\libcache\cn.tuanjie.template.3d-8.1.4\ScriptAssemblies'
$editorArgs += @('UnityEngine.TestRunner.dll', 'UnityEditor.TestRunner.dll') | ForEach-Object { '-r:"' + (Join-Path $templateAssemblies $_) + '"' }
$editorArgs += Get-ChildItem -LiteralPath (Join-Path $packageRoot 'Editor') -Recurse -Filter '*.cs' | ForEach-Object { '"' + $_.FullName + '"' }
Compile-Check 'MCPForUnity.Editor' $editorArgs
$startupArgs = @(
    '-r:"' + (Join-Path $PSScriptRoot 'MCPForUnity.Editor.dll') + '"'
    '-r:"' + (Join-Path $PSScriptRoot 'Qingming.Importer.Check.dll') + '"'
    '"' + (Join-Path $projectRoot 'UnityProject\Assets\Editor\QingmingMcpStartup.cs') + '"'
)
Compile-Check 'Qingming.McpStartup.Check' $startupArgs
