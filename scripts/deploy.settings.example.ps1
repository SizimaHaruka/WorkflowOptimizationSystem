$DeploySettings = @{
    # 実ファイル deploy.settings.ps1 は Git 管理外です。
    # 閉域環境では、スクリプトを配置したローカルリポジトリを直接ビルドします。
    # Git同期が必要なときだけ、実行時に -SyncRepository を指定します。
    RepositoryUrl = ''
    BranchName = 'develop' # -SyncRepository 使用時だけ参照されます。

    RepoRoot = (Split-Path $PSScriptRoot -Parent)
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
