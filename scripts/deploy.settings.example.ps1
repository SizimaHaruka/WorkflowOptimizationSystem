$DeploySettings = @{
    # 実ファイル deploy.settings.ps1 は Git 管理外です。
    RepositoryUrl = '<REPOSITORY_URL>'
    BranchName = 'main'

    RepoRoot = 'C:\deploy\dxpmt-repo'
    StagingRoot = 'C:\deploy\dxpmt-staging'
    ReleaseRoot = 'C:\deploy\dxpmt-releases'
    DeployRoot = 'C:\inetpub\dxpmt'
    LogRoot = 'C:\deploy\logs'

    SolutionPath = 'dxpmt.slnx'
    PublishProjectPath = 'dxpmt\dxpmt.csproj'
    Configuration = 'Release'
    TestProjects = @('tests\dxpmt.E2E.Tests\dxpmt.E2E.Tests.csproj')

    SiteName = 'dxpmt'
    AppPoolName = 'dxpmt'
    EnsureWindowsAuthentication = $true
    EnsureAnonymousAuthentication = $true
    HealthCheckUrl = 'http://server-am:8082/'

    PreserveDirectories = @('logs', 'keys')
    PreserveFiles = @('appsettings.Production.Local.json')
}
