param(
    [ValidateSet('EditMode', 'PlayMode')]
    [string] $TestPlatform = 'EditMode',

    [string] $TestFilter = '',

    [string] $AssemblyNames = '',

    [string] $UnityPath = '',

    [string] $OutputDir = 'Temp/TestResults',

    [int] $TimeoutSeconds = 900,

    [switch] $NoSynchronous
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RepoRoot {
    if ($PSScriptRoot) {
        return $PSScriptRoot
    }

    return (Get-Location).Path
}

function Get-UnityVersion([string] $RepoRoot) {
    $projectVersionPath = Join-Path $RepoRoot 'ProjectSettings/ProjectVersion.txt'
    if (-not (Test-Path $projectVersionPath)) {
        throw "Cannot find Unity project version file: $projectVersionPath"
    }

    $line = Get-Content $projectVersionPath | Where-Object { $_ -match '^m_EditorVersion:\s*(.+)$' } | Select-Object -First 1
    if (-not $line) {
        throw "Cannot read m_EditorVersion from: $projectVersionPath"
    }

    return ($line -replace '^m_EditorVersion:\s*', '').Trim()
}

function Resolve-UnityPath([string] $RequestedPath, [string] $UnityVersion) {
    if ($RequestedPath) {
        if (-not (Test-Path $RequestedPath)) {
            throw "UnityPath does not exist: $RequestedPath"
        }

        return (Resolve-Path $RequestedPath).Path
    }

    if ($Env:UNITY_EXE -and (Test-Path $Env:UNITY_EXE)) {
        return (Resolve-Path $Env:UNITY_EXE).Path
    }

    $candidates = @(
        "C:/Program Files/Unity/Hub/Editor/$UnityVersion/Editor/Unity.exe",
        "C:/Program Files/Unity/Editor/Unity.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return (Resolve-Path $candidate).Path
        }
    }

    throw "Cannot find Unity.exe for version $UnityVersion. Pass -UnityPath or set UNITY_EXE."
}

function Wait-ForUnityTestResults([string] $ResultsPath, [string] $LogPath, [int] $TimeoutSeconds) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $hasResults = Test-Path $ResultsPath
        $logCompleted = $false
        if (Test-Path $LogPath) {
            $logCompleted = Select-String -Path $LogPath -Pattern 'Test run completed\. Exiting with code' -Quiet -ErrorAction SilentlyContinue
        }

        if ($hasResults -and $logCompleted) {
            return $true
        }

        Start-Sleep -Milliseconds 500
    }

    return $false
}

function Get-UnityExitCodeFromLog([string] $LogPath, [int] $FallbackExitCode) {
    if (Test-Path $LogPath) {
        $match = Select-String -Path $LogPath -Pattern 'Test run completed\. Exiting with code (\d+)' | Select-Object -Last 1
        if ($match -and $match.Line -match 'Test run completed\. Exiting with code (\d+)') {
            return [int] $Matches[1]
        }
    }

    return $FallbackExitCode
}

function Convert-TestResultXml([string] $XmlPath) {
    [xml] $xml = Get-Content $XmlPath
    $run = $xml.'test-run'
    $failedCases = @()

    $xml.SelectNodes('//test-case[@result and not(starts-with(@result, ''Passed''))]') | ForEach-Object {
        $failedCases += [ordered] @{
            name = $_.name
            fullname = $_.fullname
            result = $_.result
            message = if ($_.failure -and $_.failure.message) { $_.failure.message.'#cdata-section' } else { $null }
        }
    }

    return [ordered] @{
        result = $run.result
        total = [int] $run.total
        passed = [int] $run.passed
        failed = [int] $run.failed
        inconclusive = [int] $run.inconclusive
        skipped = [int] $run.skipped
        duration = [double] $run.duration
        failedCases = $failedCases
    }
}

$repoRoot = Get-RepoRoot
$unityVersion = Get-UnityVersion $repoRoot
$unityExe = Resolve-UnityPath $UnityPath $unityVersion
$outputPath = Join-Path $repoRoot $OutputDir
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$resultsPath = Join-Path $outputPath "unity-$TestPlatform-$timestamp.xml"
$logPath = Join-Path $outputPath "unity-$TestPlatform-$timestamp.log"
$jsonPath = Join-Path $outputPath "unity-$TestPlatform-$timestamp.summary.json"

$args = @(
    '-batchmode',
    '-projectPath', $repoRoot,
    '-runTests',
    '-testPlatform', $TestPlatform,
    '-testResults', $resultsPath,
    '-logFile', $logPath
)

if ($AssemblyNames) {
    $args += @('-assemblyNames', $AssemblyNames)
}

if ($TestFilter) {
    $args += @('-testFilter', $TestFilter)
}

if (-not $NoSynchronous -and $TestPlatform -eq 'EditMode') {
    $args += '-runSynchronously'
}

$startedAt = Get-Date
& $unityExe @args
$rawUnityExitCode = if (Get-Variable -Name LASTEXITCODE -Scope Global -ErrorAction SilentlyContinue) { $LASTEXITCODE } else { 0 }
$completed = Wait-ForUnityTestResults $resultsPath $logPath $TimeoutSeconds
$finishedAt = Get-Date
$unityExitCode = Get-UnityExitCodeFromLog $logPath $rawUnityExitCode

if (-not $completed -or -not (Test-Path $resultsPath)) {
    $summary = [ordered] @{
        ok = $false
        exitCode = $unityExitCode
        rawExitCode = $rawUnityExitCode
        error = if (Test-Path $resultsPath) { 'Unity produced results, but completion was not observed before timeout.' } else { 'Unity did not produce a test results XML file before timeout.' }
        unityVersion = $unityVersion
        unityPath = $unityExe
        testPlatform = $TestPlatform
        testFilter = $TestFilter
        assemblyNames = $AssemblyNames
        startedAt = $startedAt.ToString('o')
        finishedAt = $finishedAt.ToString('o')
        durationSeconds = [Math]::Round(($finishedAt - $startedAt).TotalSeconds, 3)
        timeoutSeconds = $TimeoutSeconds
        resultsPath = $resultsPath
        logPath = $logPath
    }

    $summary | ConvertTo-Json -Depth 8 | Set-Content -Encoding UTF8 $jsonPath
    $summary | ConvertTo-Json -Depth 8
    exit 1
}

$resultSummary = Convert-TestResultXml $resultsPath
$ok = $unityExitCode -eq 0 -and $resultSummary.failed -eq 0 -and $resultSummary.result -like 'Passed*'
$summary = [ordered] @{
    ok = $ok
    exitCode = $unityExitCode
    rawExitCode = $rawUnityExitCode
    unityVersion = $unityVersion
    unityPath = $unityExe
    testPlatform = $TestPlatform
    testFilter = $TestFilter
    assemblyNames = $AssemblyNames
    startedAt = $startedAt.ToString('o')
    finishedAt = $finishedAt.ToString('o')
    durationSeconds = [Math]::Round(($finishedAt - $startedAt).TotalSeconds, 3)
    timeoutSeconds = $TimeoutSeconds
    resultsPath = $resultsPath
    logPath = $logPath
    summaryPath = $jsonPath
    result = $resultSummary
}

$summary | ConvertTo-Json -Depth 10 | Set-Content -Encoding UTF8 $jsonPath
$summary | ConvertTo-Json -Depth 10

if ($ok) {
    exit 0
}

exit $(if ($unityExitCode -ne 0) { $unityExitCode } else { 1 })
