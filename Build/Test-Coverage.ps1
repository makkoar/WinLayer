<#
.SYNOPSIS
Запускает тесты с измерением покрытия всех продуктовых C#-сборок.
.PARAMETER Configuration
Конфигурация сборки Debug или Release.
.PARAMETER RequireFullCoverage
Требует 100% покрытия строк, ветвей и методов в каждой сборке.
#>
param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Debug',
    [switch]$RequireFullCoverage
)

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$testProject = Join-Path $projectRoot 'Tests/WinLayer.Tests/WinLayer.Tests.csproj'
$coverageDirectory = Join-Path $projectRoot "artifacts/coverage/$Configuration"
$runDirectory = Join-Path $coverageDirectory ([Guid]::NewGuid().ToString('N'))
$reportPath = Join-Path $coverageDirectory 'coverage.json'
if (Test-Path -LiteralPath $reportPath) {
    Remove-Item -LiteralPath $reportPath
}

$testArguments = @(
    'test', '--project', $testProject, '--configuration', $Configuration,
    '--coverlet', '--results-directory', $runDirectory
)
if ($RequireFullCoverage) {
    $testArguments += @(
        '--coverlet-threshold', '100', '--coverlet-threshold-type', 'line,branch,method',
        '--coverlet-threshold-stat', 'Minimum'
    )
}

Push-Location -LiteralPath $projectRoot
try {
    & dotnet @testArguments
    $testExitCode = $LASTEXITCODE
    $reports = @(Get-ChildItem -LiteralPath $runDirectory -Filter 'coverage.*.json' -File -ErrorAction SilentlyContinue)
    if ($reports.Count -ne 1) {
        throw 'Ожидался один новый отчёт покрытия текущего запуска.'
    }
    & (Join-Path $PSScriptRoot 'Verify-CoverageScope.ps1') -ReportPath $reports[0].FullName
    Copy-Item -LiteralPath $reports[0].FullName -Destination $reportPath
    if ($testExitCode -ne 0) {
        throw 'Тесты или проверка покрытия завершились ошибкой.'
    }
}
finally {
    Pop-Location
}
