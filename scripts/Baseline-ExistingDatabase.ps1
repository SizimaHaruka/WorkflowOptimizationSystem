[CmdletBinding()]
param(
    [string]$ConfigurationPath = (Join-Path $PSScriptRoot '..\dxpmt\appsettings.Production.Local.json'),
    [switch]$Apply
)

$ErrorActionPreference = 'Stop'
$migrationId = '20260716000137_InitialCreate'
$productVersion = '10.0.8'
$requiredTables = @(
    'Cases', 'ActionItems', 'CaseForms', 'CaseStatusHistories', 'DecisionRecords',
    'GateReviews', 'Problems', 'TraceLinks', 'WorkItems'
)

if (-not (Test-Path -LiteralPath $ConfigurationPath)) {
    throw "接続設定ファイルが見つかりません: $ConfigurationPath"
}

$settings = Get-Content -LiteralPath $ConfigurationPath -Raw | ConvertFrom-Json
$connection = [System.Data.SqlClient.SqlConnection]::new($settings.ConnectionStrings.DefaultConnection)

try {
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = @'
SELECT name FROM sys.tables WHERE name IN (
    N'Cases', N'ActionItems', N'CaseForms', N'CaseStatusHistories', N'DecisionRecords',
    N'GateReviews', N'Problems', N'TraceLinks', N'WorkItems'
);
'@
    $reader = $command.ExecuteReader()
    $existingTables = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    while ($reader.Read()) { [void]$existingTables.Add($reader.GetString(0)) }
    $reader.Close()

    $missing = $requiredTables | Where-Object { -not $existingTables.Contains($_) }
    if ($missing) { throw "初期スキーマが不足しています: $($missing -join ', ')" }

    $command.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE name = N'__EFMigrationsHistory';"
    if ([int]$command.ExecuteScalar() -ne 0) {
        throw 'EFのマイグレーション履歴が既に存在します。基準線登録は不要です。'
    }

    if (-not $Apply) {
        Write-Host "検証済みです。基準線を登録するには -Apply を指定してください: $migrationId"
        return
    }

    $command.CommandText = @"
CREATE TABLE [__EFMigrationsHistory] (
    [MigrationId] nvarchar(150) NOT NULL,
    [ProductVersion] nvarchar(32) NOT NULL,
    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
);
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'$migrationId', N'$productVersion');
"@
    [void]$command.ExecuteNonQuery()
    Write-Host "基準線を登録しました: $migrationId"
}
finally {
    $connection.Dispose()
}
