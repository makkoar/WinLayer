<#
.SYNOPSIS
Проверяет наличие всех продуктовых C#-сборок в отчёте покрытия.
.PARAMETER ReportPath
Путь к JSON-отчёту Coverlet.
#>
param([Parameter(Mandatory = $true)][string]$ReportPath)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (!(Test-Path -LiteralPath $ReportPath -PathType Leaf)) {
    throw 'Отчёт покрытия отсутствует.'
}
$report = Get-Content -LiteralPath $ReportPath -Raw | ConvertFrom-Json
$modules = @($report.PSObject.Properties.Name | ForEach-Object { [IO.Path]::GetFileNameWithoutExtension($_) })
$projects = @(Get-ChildItem -LiteralPath $projectRoot -Directory | ForEach-Object {
    Get-ChildItem -LiteralPath $_.FullName -Filter '*.csproj' -File
})
if ($projects.Count -eq 0) {
    throw 'Не найдены продуктовые C#-проекты.'
}
foreach ($project in $projects) {
    [xml]$definition = Get-Content -LiteralPath $project.FullName -Raw
    $assembly = $definition.SelectSingleNode('/Project/PropertyGroup/AssemblyName')
    $name = if ($null -ne $assembly) { $assembly.InnerText } else { $project.BaseName }
    if ($name -notin $modules) {
        throw "Сборка $name отсутствует в отчёте покрытия. Подключите её к тестовому проекту."
    }
}
Write-Output "В отчёте присутствуют все продуктовые C#-сборки: $($projects.Count)."
