$DeploySettings = @{
    # 実ファイル deploy.settings.ps1 は Git 管理外です。
    # デプロイ前に指定ブランチを同期します。同期を省略する場合は -SkipPull を指定します。
    RepositoryUrl = '<REPOSITORY_URL>'
    BranchName = 'develop'

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
    PreserveFiles = @(
        'appsettings.Production.Local.json',
        'appsettings.Development.Local.json'
    )

    # アプリプールに変更権限を付与する実行時ディレクトリです。
    RuntimeDirectories = @('logs', 'keys')
    HealthCheckHostHeader = ''
}
