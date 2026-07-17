[CmdletBinding()]
param(
    [string]$SettingsPath = (Join-Path $PSScriptRoot 'deploy.settings.ps1'),
    [switch]$SkipPull,
    [switch]$SkipBuild,
    [switch]$SkipTests,
    [switch]$SkipSmokeTest,
    [string]$Configuration,
    [int]$KeepReleaseCount = 5
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-DeployLog {
    param([string]$Message, [ValidateSet('INFO', 'WARN', 'ERROR')][string]$Level = 'INFO')

    $line = '{0} [{1}] {2}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $Level, $Message
    Write-Host $line
    Add-Content -LiteralPath $script:DeployLogPath -Value $line
}

function Ensure-Directory {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Assert-SafePath {
    param([string]$Path, [string]$Name)

    if ([string]::IsNullOrWhiteSpace($Path)) { throw "$Name is empty." }
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $forbidden = @(
        [System.IO.Path]::GetPathRoot($fullPath),
        'C:\Windows',
        'C:\Program Files',
        'C:\Program Files (x86)'
    )
    if ($fullPath.Length -lt 4 -or $forbidden -contains $fullPath) {
        throw "$Name is not a safe path: $fullPath"
    }
}

function Join-ProcessArguments {
    param([string[]]$Arguments)

    return [string]::Join(' ', ($Arguments | ForEach-Object {
        if ($_ -match '[\s"]') {
            '"' + ($_ -replace '(\\*)"', '$1$1\"' -replace '(\\+)$', '$1$1') + '"'
        }
        else {
            $_
        }
    }))
}

function Invoke-CommandLogged {
    param(
        [string]$FilePath,
        [string[]]$Arguments,
        [string]$WorkingDirectory,
        [int[]]$AllowedExitCodes = @(0)
    )

    Write-DeployLog ("EXEC  {0} {1}" -f $FilePath, ($Arguments -join ' '))

    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $FilePath
    $startInfo.Arguments = Join-ProcessArguments -Arguments $Arguments
    $startInfo.WorkingDirectory = $WorkingDirectory
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.CreateNoWindow = $true

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo
    [void]$process.Start()
    $standardOutput = $process.StandardOutput.ReadToEnd()
    $standardError = $process.StandardError.ReadToEnd()
    $process.WaitForExit()

    foreach ($stream in @($standardOutput, $standardError)) {
        foreach ($line in ($stream -split "`r?`n")) {
            if (-not [string]::IsNullOrWhiteSpace($line)) {
                Add-Content -LiteralPath $script:DeployLogPath -Value $line
            }
        }
    }
    if ($AllowedExitCodes -notcontains $process.ExitCode) {
        throw "Command failed with exit code $($process.ExitCode): $FilePath $($Arguments -join ' ')"
    }
}

function Invoke-Robocopy {
    param([string[]]$Arguments)
    Invoke-CommandLogged -FilePath 'robocopy' -Arguments $Arguments -WorkingDirectory $settings.RepoRoot -AllowedExitCodes @(0, 1, 2, 3, 4, 5, 6, 7)
}

function Import-IisModule { Import-Module WebAdministration -ErrorAction Stop }

function Wait-AppPoolState {
    param([string]$AppPoolName, [string]$ExpectedState)

    $deadline = (Get-Date).AddSeconds(30)
    do {
        $state = (Get-WebAppPoolState -Name $AppPoolName).Value
        if ($state -eq $ExpectedState) { return }
        Start-Sleep -Milliseconds 500
    } while ((Get-Date) -lt $deadline)
    throw "App pool '$AppPoolName' did not reach '$ExpectedState'."
}

function Set-IisAuthentication {
    param([string]$SiteName, [string]$Filter, [bool]$Enabled)

    $path = "IIS:\Sites\$SiteName"
    $current = Get-WebConfigurationProperty -PSPath $path -Filter $Filter -Name enabled
    if ([bool]$current.Value -ne $Enabled) {
        Set-WebConfigurationProperty -PSPath $path -Filter $Filter -Name enabled -Value $Enabled
        Write-DeployLog "Updated IIS authentication setting. Filter=$Filter Enabled=$Enabled"
    }
}

function Ensure-IisConfiguration {
    Import-IisModule
    $site = Get-Website -Name $settings.SiteName -ErrorAction Stop
    $sitePath = [System.IO.Path]::GetFullPath($site.PhysicalPath)
    $deployPath = [System.IO.Path]::GetFullPath($settings.DeployRoot)
    if ($sitePath -ne $deployPath) {
        throw "IIS site path differs from DeployRoot. Site=$sitePath DeployRoot=$deployPath"
    }

    if ($settings.EnsureWindowsAuthentication) {
        Set-IisAuthentication -SiteName $settings.SiteName -Filter 'system.webServer/security/authentication/windowsAuthentication' -Enabled $true
    }
    if ($settings.EnsureAnonymousAuthentication) {
        Set-IisAuthentication -SiteName $settings.SiteName -Filter 'system.webServer/security/authentication/anonymousAuthentication' -Enabled $true
    }
}

function Sync-Repository {
    if (-not (Test-Path -LiteralPath $settings.RepoRoot)) {
        if ([string]::IsNullOrWhiteSpace($settings.RepositoryUrl)) { throw 'RepositoryUrl is required for the initial clone.' }
        Ensure-Directory -Path ([System.IO.Path]::GetDirectoryName($settings.RepoRoot))
        Invoke-CommandLogged -FilePath 'git' -Arguments @('clone', '--branch', $settings.BranchName, $settings.RepositoryUrl, $settings.RepoRoot) -WorkingDirectory (Get-Location).Path
        return
    }

    Invoke-CommandLogged -FilePath 'git' -Arguments @('-C', $settings.RepoRoot, 'checkout', $settings.BranchName) -WorkingDirectory $settings.RepoRoot
    Invoke-CommandLogged -FilePath 'git' -Arguments @('-C', $settings.RepoRoot, 'pull', '--ff-only', 'origin', $settings.BranchName) -WorkingDirectory $settings.RepoRoot
}

function Build-Test-And-Publish {
    if (Test-Path -LiteralPath $settings.StagingRoot) {
        Remove-Item -LiteralPath $settings.StagingRoot -Recurse -Force
    }
    Ensure-Directory -Path $settings.StagingRoot

    Invoke-CommandLogged -FilePath 'dotnet' -Arguments @('restore', $settings.SolutionPath) -WorkingDirectory $settings.RepoRoot
    Invoke-CommandLogged -FilePath 'dotnet' -Arguments @('build', $settings.SolutionPath, '-c', $settings.Configuration, '-p:UseAppHost=false') -WorkingDirectory $settings.RepoRoot

    if ($SkipTests) {
        Write-DeployLog 'Tests were skipped.' 'WARN'
    }
    else {
        foreach ($testProject in $settings.TestProjects) {
            Invoke-CommandLogged -FilePath 'dotnet' -Arguments @('test', $testProject, '-c', $settings.Configuration, '--no-build', '--verbosity', 'minimal') -WorkingDirectory $settings.RepoRoot
        }
    }

    Invoke-CommandLogged -FilePath 'dotnet' -Arguments @('publish', $settings.PublishProjectPath, '-c', $settings.Configuration, '-o', $settings.StagingRoot, '-p:UseAppHost=false', '--no-build') -WorkingDirectory $settings.RepoRoot
}

function Save-ReleaseSnapshot {
    Ensure-Directory -Path $settings.ReleaseRoot
    $releasePath = Join-Path $settings.ReleaseRoot (Get-Date -Format 'yyyyMMdd_HHmmss')
    Ensure-Directory -Path $releasePath
    Invoke-Robocopy -Arguments @($settings.StagingRoot, $releasePath, '/MIR', '/R:1', '/W:1', '/NFL', '/NDL', '/NP')
    [ordered]@{ createdAt = (Get-Date).ToString('s'); commitHash = (& git -C $settings.RepoRoot rev-parse HEAD).Trim(); branch = $settings.BranchName } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $releasePath 'release.json') -Encoding UTF8
    return $releasePath
}

function Deploy-Staging {
    Assert-SafePath -Path $settings.DeployRoot -Name 'DeployRoot'
    Ensure-Directory -Path $settings.DeployRoot
    $arguments = [System.Collections.Generic.List[string]]::new(@($settings.StagingRoot, $settings.DeployRoot, '/MIR', '/R:1', '/W:1', '/NFL', '/NDL', '/NP'))
    foreach ($directory in $settings.PreserveDirectories) {
        $arguments.Add('/XD'); $arguments.Add((Join-Path $settings.DeployRoot $directory))
    }
    foreach ($file in $settings.PreserveFiles) {
        $arguments.Add('/XF'); $arguments.Add($file)
    }
    Invoke-Robocopy -Arguments $arguments.ToArray()
}

function Invoke-SmokeTest {
    if ([string]::IsNullOrWhiteSpace($settings.HealthCheckUrl)) { return }
    try { $statusCode = [int](Invoke-WebRequest -Uri $settings.HealthCheckUrl -UseBasicParsing -TimeoutSec 30).StatusCode }
    catch [System.Net.WebException] { $statusCode = [int]$_.Exception.Response.StatusCode }
    if (($statusCode -ge 200 -and $statusCode -lt 400) -or $statusCode -in @(401, 403)) {
        Write-DeployLog "Smoke test passed: $($settings.HealthCheckUrl) ($statusCode)"
        return
    }
    throw "Smoke test failed. StatusCode=$statusCode"
}

function Remove-OldReleases {
    Get-ChildItem -LiteralPath $settings.ReleaseRoot -Directory |
        Sort-Object Name -Descending | Select-Object -Skip $KeepReleaseCount |
        ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force; Write-DeployLog "Removed old release: $($_.FullName)" }
}

if (-not (Test-Path -LiteralPath $SettingsPath)) { throw "Settings file was not found: $SettingsPath" }
. $SettingsPath
$settings = $DeploySettings
if ($null -eq $settings) { throw 'The settings file must define $DeploySettings.' }
if ($Configuration) { $settings.Configuration = $Configuration }
foreach ($required in @('RepoRoot', 'StagingRoot', 'ReleaseRoot', 'DeployRoot', 'LogRoot', 'SolutionPath', 'PublishProjectPath', 'Configuration', 'SiteName', 'AppPoolName')) {
    if ([string]::IsNullOrWhiteSpace($settings[$required])) { throw "Missing required setting: $required" }
}

Assert-SafePath -Path $settings.DeployRoot -Name 'DeployRoot'
Assert-SafePath -Path $settings.ReleaseRoot -Name 'ReleaseRoot'
Ensure-Directory -Path $settings.LogRoot
$script:DeployLogPath = Join-Path $settings.LogRoot ("deploy_{0}.log" -f (Get-Date -Format 'yyyyMMdd_HHmmss'))

try {
    Write-DeployLog 'Deployment started.'
    if (-not $SkipPull) { Sync-Repository }
    if (-not $SkipBuild) { Build-Test-And-Publish }
    $releasePath = Save-ReleaseSnapshot
    Ensure-IisConfiguration
    Import-IisModule
    Stop-WebAppPool -Name $settings.AppPoolName
    Wait-AppPoolState -AppPoolName $settings.AppPoolName -ExpectedState 'Stopped'
    try { Deploy-Staging }
    finally { Start-WebAppPool -Name $settings.AppPoolName; Wait-AppPoolState -AppPoolName $settings.AppPoolName -ExpectedState 'Started' }
    if (-not $SkipSmokeTest) { Invoke-SmokeTest }
    Remove-OldReleases
    Write-DeployLog "Deployment completed. Release=$releasePath"
}
catch {
    Write-DeployLog $_.Exception.ToString() 'ERROR'
    throw
}
