[CmdletBinding()]
param(
	[ValidateSet("Game", "Benchmark", "FullBenchmark")]
	[string]$Target = "Game",

	[ValidatePattern("^[1-9][0-9]*x[1-9][0-9]*$")]
	[string]$Resolution = "1280x720",

	[string]$GodotPath,

	[switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Resolve-GodotExecutable {
	param([string]$ConfiguredPath)

	if (-not [string]::IsNullOrWhiteSpace($ConfiguredPath)) {
		if (Test-Path -LiteralPath $ConfiguredPath -PathType Leaf) {
			return (Resolve-Path -LiteralPath $ConfiguredPath).Path
		}

		$configuredCommand = Get-Command $ConfiguredPath -ErrorAction SilentlyContinue |
			Select-Object -First 1
		if ($null -ne $configuredCommand) {
			return $configuredCommand.Source
		}

		throw "Godot executable was not found at or on PATH as '$ConfiguredPath'."
	}

	foreach ($commandName in @("godot", "godot4", "godot-mono")) {
		$command = Get-Command $commandName -ErrorAction SilentlyContinue |
			Select-Object -First 1
		if ($null -ne $command) {
			return $command.Source
		}
	}

	throw ("Godot was not found. Pass -GodotPath, set ASHWOOD_GODOT_PATH, " +
		"or add a Godot executable to PATH.")
}

$projectRoot = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $projectRoot "project.godot"
if (-not (Test-Path -LiteralPath $projectFile -PathType Leaf)) {
	throw "project.godot was not found at '$projectFile'."
}

$configuredGodot = if (-not [string]::IsNullOrWhiteSpace($GodotPath)) {
	$GodotPath
} else {
	$env:ASHWOOD_GODOT_PATH
}
$godotExecutable = Resolve-GodotExecutable -ConfiguredPath $configuredGodot
$godotArguments = @(
	"--path",
	$projectRoot,
	"--resolution",
	$Resolution
)
if ($Target -eq "Benchmark") {
	$godotArguments += @(
		"--scene",
		"res://tests/rendered_performance_benchmark.tscn"
	)
} elseif ($Target -eq "FullBenchmark") {
	$godotArguments += @(
		"--scene",
		"res://tests/full_game_performance_benchmark.tscn"
	)
}

Write-Host "Ashwood County direct launch"
Write-Host "  Target:     $Target"
Write-Host "  Godot:      $godotExecutable"
Write-Host "  Project:    $projectRoot"
Write-Host "  Resolution: $Resolution"

if ($DryRun) {
	Write-Host "Dry run complete; Godot was not started."
	exit 0
}

& $godotExecutable @godotArguments
if ($LASTEXITCODE -ne 0) {
	throw "Godot exited with code $LASTEXITCODE."
}
