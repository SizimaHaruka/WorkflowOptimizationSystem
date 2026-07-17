# IIS デプロイ

管理者としてPowerShellを起動し、`deploy.settings.example.ps1` を `deploy.settings.ps1` として複製して、リポジトリURL・IISサイト名・アプリプール名・配置先を実環境に合わせます。

```powershell
Set-Location C:\deploy\dxpmt-repo\scripts
.\deploy.ps1
```

初回以外でリポジトリ同期を行わない場合は `-SkipPull`、ビルド済みステージングを配置するだけの場合は `-SkipPull -SkipBuild` を指定します。

```powershell
.\deploy.ps1 -SkipPull
```

スクリプトは、リリース退避後にIISアプリプールを停止し、`appsettings.Production.Local.json` と指定した永続ディレクトリを保持したまま公開物を同期します。完了後はアプリプールを起動し、HTTP疎通を確認します。
