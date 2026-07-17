# IIS デプロイ

管理者としてPowerShellを起動し、`deploy.settings.example.ps1` を `deploy.settings.ps1` として複製して、リポジトリURL・ブランチ名・IISサイト名・アプリプール名・配置先を実環境に合わせます。既定では、指定ブランチをGitリポジトリから同期してビルドします。

```powershell
Set-Location C:\deploy\dxpmt-repo\scripts
.\deploy.ps1
```

リポジトリ同期を省略して現在の作業ツリーを使う場合は `-SkipPull` を指定します。ビルド済みステージングを配置するだけの場合は `-SkipPull -SkipBuild` を指定します。

```powershell
.\deploy.ps1 -SkipPull
```

```powershell
.\deploy.ps1 -SkipPull
```

スクリプトは、リリース退避後にIISアプリプールを停止し、`appsettings.Production.Local.json` と指定した永続ディレクトリを保持したまま公開物を同期します。例外時もアプリプールを起動し直し、完了後はHTTP疎通を確認します。
