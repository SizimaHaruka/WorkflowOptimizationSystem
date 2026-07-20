# E2E テスト

Playwright で、次を確認します。

- Windows自動ログイン、ログアウト後の自動再ログイン抑止、ログイン画面のCSS取得
- F01保存、G0承認、基準版の表示、再編集後の下書き復帰、基準版の不変性

初回のみ、Chromiumをインストールします。

```powershell
dotnet build tests\dxpmt.E2E.Tests\dxpmt.E2E.Tests.csproj
powershell -ExecutionPolicy Bypass -File tests\dxpmt.E2E.Tests\bin\Debug\net10.0\playwright.ps1 install chromium
```

専用のテスト環境のIISサイトと、その環境のDB接続文字列を指定して実行します。基準版テストは終了時（成否を問わず）に、生成した案件と関連レコードを削除します。本番環境を指定してはいけません。

```powershell
$env:DXPMT_E2E_BASE_URL = "http://server-am:8082"
$env:DXPMT_E2E_CONNECTION_STRING = "Server=SERVER;Database=dxpmt_test;User Id=...;Password=...;TrustServerCertificate=True"
dotnet test tests\dxpmt.E2E.Tests\dxpmt.E2E.Tests.csproj
```

`DXPMT_E2E_BASE_URL` 未指定時はテストをスキップします。`DXPMT_E2E_CONNECTION_STRING` 未指定時は、データを作成する基準版テストをエラーとして停止します。失敗時の画面・HTMLは `e2e-artifacts` に保存されます。
