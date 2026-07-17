# 認証 E2E テスト

Playwright で、Windows自動ログイン・ログアウト後の自動再ログイン抑止・ログイン画面のCSS取得を確認します。

初回のみ、Chromiumをインストールします。

```powershell
dotnet build tests\dxpmt.E2E.Tests\dxpmt.E2E.Tests.csproj
powershell -ExecutionPolicy Bypass -File tests\dxpmt.E2E.Tests\bin\Debug\net10.0\playwright.ps1 install chromium
```

対象のIISサイトを指定して実行します。

```powershell
$env:DXPMT_E2E_BASE_URL = "http://server-am:8082"
dotnet test tests\dxpmt.E2E.Tests\dxpmt.E2E.Tests.csproj
```

`DXPMT_E2E_BASE_URL` 未指定時はテストをスキップします。失敗時の画面・HTMLは `e2e-artifacts` に保存されます。
