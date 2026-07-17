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

function Get-SettingValue {
    param(
        [hashtable]$Settings,
        [string]$Name,
        $DefaultValue = $null
    )

    if ($Settings.ContainsKey($Name)) {
        return $Settings[$Name]
    }

    return $DefaultValue
}

function Write-Log {
    param(
        [string]$Message,
        [ValidateSet('INFO', 'WARN', 'ERROR')]
        [string]$Level = 'INFO'
    )

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

function Assert-SafeDeployPath {
    param([string]$Path, [string]$Name = 'Deploy path')

    if ([string]::IsNullOrWhiteSpace($Path)) {
        throw "$Name is empty."
    }

    $resolved = [System.IO.Path]::GetFullPath($Path)
    $forbidden = @(
        [System.IO.Path]::GetPathRoot($resolved),
        'C:\Windows',
        'C:\Program Files',
        'C:\Program Files (x86)'
    )

    if ($resolved.Length -lt 4 -or $forbidden -contains $resolved) {
        throw "$Name is not safe: $resolved"
    }
}

function Invoke-LoggedStep {
    param([string]$Name, [scriptblock]$Action)

    Write-Log "START $Name"
    & $Action
    Write-Log "END   $Name"
}

function Invoke-ExternalCommand {
    param(
        [string]$FilePath,
        [string[]]$Arguments,
        [string]$WorkingDirectory,
        [int[]]$AllowedExitCodes = @(0)
    )

    Write-Log ("EXEC  {0} {1}" -f $FilePath, ($Arguments -join ' '))

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
        if (-not [string]::IsNullOrWhiteSpace($stream)) {
            foreach ($line in ($stream -split "`r?`n")) {
                if (-not [string]::IsNullOrWhiteSpace($line)) {
                    Add-Content -LiteralPath $script:DeployLogPath -Value $line
                }
            }
        }
    }

    if ($AllowedExitCodes -notcontains $process.ExitCode) {
        throw "Command failed with exit code $($process.ExitCode): $FilePath $($Arguments -join ' ')"
    }
}

function Sync-Repository {
    param([hashtable]$Settings)

    Ensure-Directory -Path ([System.IO.Path]::GetDirectoryName($Settings.RepoRoot))
    if (-not (Test-Path -LiteralPath $Settings.RepoRoot)) {
        if ([string]::IsNullOrWhiteSpace($Settings.RepositoryUrl)) {
            throw 'RepositoryUrl is required for the initial clone.'
        }

        Invoke-ExternalCommand -FilePath 'git' -Arguments @('clone', '--branch', $Settings.BranchName, $Settings.RepositoryUrl, $Settings.RepoRoot) -WorkingDirectory (Get-Location).Path
        return
    }

    Invoke-ExternalCommand -FilePath 'git' -Arguments @('-C', $Settings.RepoRoot, 'checkout', $Settings.BranchName) -WorkingDirectory $Settings.RepoRoot
    Invoke-ExternalCommand -FilePath 'git' -Arguments @('-C', $Settings.RepoRoot, 'pull', '--ff-only', 'origin', $Settings.BranchName) -WorkingDirectory $Settings.RepoRoot
}

function Get-CommitHash {
    param([hashtable]$Settings)

    $hash = & git -C $Settings.RepoRoot rev-parse HEAD 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to resolve the git commit hash.'
    }

    return $hash.Trim()
}

function Invoke-DeployTests {
    param([hashtable]$Settings, [string]$ConfigurationName)

    $testProjects = @(Get-SettingValue -Settings $Settings -Name 'TestProjects' -DefaultValue @())
    if ($testProjects.Count -eq 0) {
        Invoke-ExternalCommand -FilePath 'dotnet' -Arguments @('test', $Settings.SolutionPath, '-c', $ConfigurationName, '--no-build', '--verbosity', 'minimal') -WorkingDirectory $Settings.RepoRoot
        return
    }

    foreach ($testProject in $testProjects) {
        if (-not [string]::IsNullOrWhiteSpace($testProject)) {
            Invoke-ExternalCommand -FilePath 'dotnet' -Arguments @('test', $testProject, '-c', $ConfigurationName, '--no-build', '--verbosity', 'minimal') -WorkingDirectory $Settings.RepoRoot
        }
    }
}

function Build-Test-And-Publish {
    param([hashtable]$Settings, [string]$ConfigurationName, [bool]$SkipTestExecution)

    Assert-SafeDeployPath -Path $Settings.StagingRoot -Name 'StagingRoot'
    if (Test-Path -LiteralPath $Settings.StagingRoot) {
        Remove-Item -LiteralPath $Settings.StagingRoot -Recurse -Force
    }
    Ensure-Directory -Path $Settings.StagingRoot

    Invoke-ExternalCommand -FilePath 'dotnet' -Arguments @('restore', $Settings.SolutionPath) -WorkingDirectory $Settings.RepoRoot
    Invoke-ExternalCommand -FilePath 'dotnet' -Arguments @('build', $Settings.SolutionPath, '-c', $ConfigurationName, '-p:UseAppHost=false') -WorkingDirectory $Settings.RepoRoot

    if ($SkipTestExecution) {
        Write-Log 'Tests skipped by -SkipTests.' 'WARN'
    }
    else {
        Invoke-DeployTests -Settings $Settings -ConfigurationName $ConfigurationName
    }

    Invoke-ExternalCommand -FilePath 'dotnet' -Arguments @('publish', $Settings.PublishProjectPath, '-c', $ConfigurationName, '-o', $Settings.StagingRoot, '-p:UseAppHost=false', '--no-build') -WorkingDirectory $Settings.RepoRoot
}

function Save-ReleaseSnapshot {
    param([hashtable]$Settings, [string]$CommitHash)

    Assert-SafeDeployPath -Path $Settings.ReleaseRoot -Name 'ReleaseRoot'
    Ensure-Directory -Path $Settings.ReleaseRoot
    $releasePath = Join-Path $Settings.ReleaseRoot (Get-Date -Format 'yyyyMMdd_HHmmss')
    Ensure-Directory -Path $releasePath

    Invoke-ExternalCommand -FilePath 'robocopy' -Arguments @($Settings.StagingRoot, $releasePath, '/MIR', '/R:1', '/W:1', '/NFL', '/NDL', '/NP') -WorkingDirectory $Settings.RepoRoot -AllowedExitCodes @(0, 1, 2, 3, 4, 5, 6, 7)
    [ordered]@{
        createdAt = (Get-Date).ToString('s')
        commitHash = $CommitHash
        sourceBranch = $Settings.BranchName
        stagingRoot = $Settings.StagingRoot
        deployRoot = $Settings.DeployRoot
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $releasePath 'release.json') -Encoding UTF8

    return $releasePath
}

function Import-WebAdministrationModule { Import-Module WebAdministration -ErrorAction Stop }

function Get-AppPoolPrincipalName {
    param([string]$AppPoolName)
    return "IIS AppPool\$AppPoolName"
}

function Set-IisAuthenticationSetting {
    param([string]$SiteName, [string]$Filter, [bool]$Enabled)

    $current = Get-WebConfigurationProperty -PSPath "IIS:\Sites\$SiteName" -Filter $Filter -Name enabled
    if ([bool]$current.Value -ne $Enabled) {
        Set-WebConfigurationProperty -PSPath "IIS:\Sites\$SiteName" -Filter $Filter -Name enabled -Value $Enabled
        Write-Log "Updated IIS authentication. Filter=$Filter Enabled=$Enabled"
    }
}

function Ensure-SiteConfiguration {
    param([hashtable]$Settings)

    $siteName = Get-SettingValue -Settings $Settings -Name 'SiteName' -DefaultValue ''
    if ([string]::IsNullOrWhiteSpace($siteName)) { return }

    Import-WebAdministrationModule
    $site = Get-Website -Name $siteName -ErrorAction Stop
    $sitePath = [System.IO.Path]::GetFullPath($site.PhysicalPath)
    $deployPath = [System.IO.Path]::GetFullPath($Settings.DeployRoot)
    if ($sitePath -ne $deployPath) {
        throw "IIS site '$siteName' physical path does not match DeployRoot. SitePath=$sitePath DeployRoot=$deployPath"
    }

    if (Get-SettingValue -Settings $Settings -Name 'EnsureWindowsAuthentication' -DefaultValue $false) {
        Set-IisAuthenticationSetting -SiteName $siteName -Filter 'system.webServer/security/authentication/windowsAuthentication' -Enabled $true
    }
    if (Get-SettingValue -Settings $Settings -Name 'EnsureAnonymousAuthentication' -DefaultValue $false) {
        Set-IisAuthenticationSetting -SiteName $siteName -Filter 'system.webServer/security/authentication/anonymousAuthentication' -Enabled $true
    }
}

function Ensure-AppPoolModifyAccess {
    param([string]$Path, [string]$AppPoolName)

    Invoke-ExternalCommand -FilePath 'icacls' -Arguments @($Path, '/grant', "$(Get-AppPoolPrincipalName -AppPoolName $AppPoolName):(OI)(CI)M") -WorkingDirectory (Get-Location).Path
}

function Ensure-DeployRuntimePrerequisites {
    param([hashtable]$Settings)

    Ensure-SiteConfiguration -Settings $Settings
    $paths = @($Settings.DeployRoot)
    $paths += Get-SettingValue -Settings $Settings -Name 'PreserveDirectories' -DefaultValue @() | ForEach-Object { Join-Path $Settings.DeployRoot $_ }
    $paths += Get-SettingValue -Settings $Settings -Name 'RuntimeDirectories' -DefaultValue @() | ForEach-Object { Join-Path $Settings.DeployRoot $_ }

    foreach ($path in ($paths | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Unique)) {
        Ensure-Directory -Path $path
        Ensure-AppPoolModifyAccess -Path $path -AppPoolName $Settings.AppPoolName
    }
}

function Wait-AppPoolState {
    param([string]$AppPoolName, [string]$ExpectedState, [int]$TimeoutSeconds = 30)

    Import-WebAdministrationModule
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        $state = (Get-WebAppPoolState -Name $AppPoolName).Value
        if ($state -eq $ExpectedState) { return }
        Start-Sleep -Milliseconds 500
    } while ((Get-Date) -lt $deadline)

    throw "App pool '$AppPoolName' did not reach '$ExpectedState'. CurrentState='$state'"
}

function Stop-AppPoolSafe {
    param([string]$AppPoolName)

    Import-WebAdministrationModule
    if ((Get-WebAppPoolState -Name $AppPoolName).Value -ne 'Stopped') {
        Stop-WebAppPool -Name $AppPoolName
        Wait-AppPoolState -AppPoolName $AppPoolName -ExpectedState 'Stopped'
    }
}

function Start-AppPoolSafe {
    param([string]$AppPoolName)

    Import-WebAdministrationModule
    if ((Get-WebAppPoolState -Name $AppPoolName).Value -eq 'Started') { return }

    $lastError = $null
    foreach ($attempt in 1..5) {
        try {
            Start-WebAppPool -Name $AppPoolName
            Wait-AppPoolState -AppPoolName $AppPoolName -ExpectedState 'Started'
            return
        }
        catch {
            $lastError = $_
            Start-Sleep -Seconds ([Math]::Min($attempt, 3))
        }
    }

    throw $lastError
}

function Start-WebsiteSafe {
    param([string]$SiteName)

    if ([string]::IsNullOrWhiteSpace($SiteName)) { return }
    Import-WebAdministrationModule
    if ((Get-WebsiteState -Name $SiteName).Value -ne 'Started') {
        Start-Website -Name $SiteName
    }
}

function Deploy-StagingToTarget {
    param([hashtable]$Settings)

    Assert-SafeDeployPath -Path $Settings.DeployRoot -Name 'DeployRoot'
    Ensure-Directory -Path $Settings.DeployRoot
    $arguments = @($Settings.StagingRoot, $Settings.DeployRoot, '/MIR', '/R:1', '/W:1', '/NFL', '/NDL', '/NP')

    foreach ($directory in (Get-SettingValue -Settings $Settings -Name 'PreserveDirectories' -DefaultValue @())) {
        $arguments += '/XD'
        $arguments += (Join-Path $Settings.DeployRoot $directory)
    }
    foreach ($file in (Get-SettingValue -Settings $Settings -Name 'PreserveFiles' -DefaultValue @())) {
        $arguments += '/XF'
        $arguments += $file
    }

    Invoke-ExternalCommand -FilePath 'robocopy' -Arguments $arguments -WorkingDirectory $Settings.RepoRoot -AllowedExitCodes @(0, 1, 2, 3, 4, 5, 6, 7)
}

function Invoke-SmokeTest {
    param([string]$Url, [string]$HostHeader)

    if ([string]::IsNullOrWhiteSpace($Url)) {
        Write-Log 'Smoke test skipped because HealthCheckUrl is empty.' 'WARN'
        return
    }

    $parameters = @{ Uri = $Url; Method = 'Get'; UseBasicParsing = $true; TimeoutSec = 30 }
    if (-not [string]::IsNullOrWhiteSpace($HostHeader)) {
        $parameters.Headers = @{ Host = $HostHeader }
    }

    try { $statusCode = [int](Invoke-WebRequest @parameters).StatusCode }
    catch [System.Net.WebException] {
        if ($null -eq $_.Exception.Response) { throw }
        $statusCode = [int]$_.Exception.Response.StatusCode
    }

    if (($statusCode -ge 200 -and $statusCode -lt 400) -or $statusCode -in @(401, 403)) {
        Write-Log "Smoke test passed: $Url ($statusCode)"
        return
    }
    throw "Smoke test failed. StatusCode=$statusCode Url=$Url"
}

function Remove-OldReleases {
    param([string]$ReleaseRoot, [int]$KeepCount)

    if (-not (Test-Path -LiteralPath $ReleaseRoot)) { return }
    Get-ChildItem -LiteralPath $ReleaseRoot -Directory | Sort-Object Name -Descending | Select-Object -Skip $KeepCount | ForEach-Object {
        Remove-Item -LiteralPath $_.FullName -Recurse -Force
        Write-Log "Removed old release: $($_.FullName)"
    }
}

if (-not (Test-Path -LiteralPath $SettingsPath)) { throw "Settings file was not found: $SettingsPath" }
. $SettingsPath
$settings = $DeploySettings
if ($null -eq $settings) { throw 'The settings file must define $DeploySettings.' }
if ($Configuration) { $settings.Configuration = $Configuration }

foreach ($name in @('RepoRoot', 'StagingRoot', 'ReleaseRoot', 'DeployRoot', 'LogRoot', 'SolutionPath', 'PublishProjectPath', 'Configuration', 'AppPoolName')) {
    if ([string]::IsNullOrWhiteSpace((Get-SettingValue -Settings $settings -Name $name))) {
        throw "Missing required setting: $name"
    }
}

Assert-SafeDeployPath -Path $settings.StagingRoot -Name 'StagingRoot'
Assert-SafeDeployPath -Path $settings.ReleaseRoot -Name 'ReleaseRoot'
Assert-SafeDeployPath -Path $settings.DeployRoot -Name 'DeployRoot'
Ensure-Directory -Path $settings.LogRoot
$script:DeployLogPath = Join-Path $settings.LogRoot ("deploy_{0}.log" -f (Get-Date -Format 'yyyyMMdd_HHmmss'))

try {
    Write-Log 'Deployment started.'
    if (-not $SkipPull) {
        Invoke-LoggedStep -Name 'Sync repository' -Action { Sync-Repository -Settings $settings }
    }
    else {
        Write-Log 'Repository synchronization skipped by -SkipPull.' 'WARN'
    }

    $commitHash = Get-CommitHash -Settings $settings
    Write-Log "Commit hash: $commitHash"
    if (-not $SkipBuild) { Invoke-LoggedStep -Name 'Build, test, and publish' -Action { Build-Test-And-Publish -Settings $settings -ConfigurationName $settings.Configuration -SkipTestExecution $SkipTests.IsPresent } }

    $releasePath = $null
    Invoke-LoggedStep -Name 'Create release snapshot' -Action { $script:releasePath = Save-ReleaseSnapshot -Settings $settings -CommitHash $commitHash }
    Invoke-LoggedStep -Name 'Stop IIS app pool' -Action { Stop-AppPoolSafe -AppPoolName $settings.AppPoolName }
    try {
        Invoke-LoggedStep -Name 'Deploy publish output' -Action { Deploy-StagingToTarget -Settings $settings }
        Invoke-LoggedStep -Name 'Ensure runtime prerequisites' -Action { Ensure-DeployRuntimePrerequisites -Settings $settings }
    }
    finally {
        Invoke-LoggedStep -Name 'Start IIS app pool' -Action { Start-AppPoolSafe -AppPoolName $settings.AppPoolName }
        Invoke-LoggedStep -Name 'Start IIS website' -Action { Start-WebsiteSafe -SiteName (Get-SettingValue -Settings $settings -Name 'SiteName' -DefaultValue '') }
    }

    if (-not $SkipSmokeTest) {
        Invoke-LoggedStep -Name 'Smoke test' -Action { Invoke-SmokeTest -Url (Get-SettingValue -Settings $settings -Name 'HealthCheckUrl' -DefaultValue '') -HostHeader (Get-SettingValue -Settings $settings -Name 'HealthCheckHostHeader' -DefaultValue '') }
    }
    Invoke-LoggedStep -Name 'Cleanup old releases' -Action { Remove-OldReleases -ReleaseRoot $settings.ReleaseRoot -KeepCount $KeepReleaseCount }
    Write-Log "Deployment completed successfully. ReleasePath=$releasePath"
}
catch {
    Write-Log $_.Exception.ToString() 'ERROR'
    exit 1
}
