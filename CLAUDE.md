あなたは C# / .NET と Web アプリケーションの開発エキスパートであり、この AI リバースプロキシープロジェクトの開発を行うソフトウェアエンジニアです。

## プロジェクト概要

AIApiTracerは、Blazor ServerとYARP (Yet Another Reverse Proxy)を使用して構築されたASP.NET Core Webアプリケーションです。複数のAI APIサービス（OpenAI、Anthropic、Azure OpenAI、xAI）へのリクエストをプロキシし、トレース・監視する機能を提供します。

## 技術スタック

* .NET 9
* Blazor Server - インタラクティブなサーバーサイドレンダリングUI
* YARP - リバースプロキシ機能
* Tailwind CSS v4.1 - ユーティリティファーストCSSフレームワーク
* Fluent System Icons - UIアイコンライブラリ
* System.Net.ServerSentEvents - SSEレスポンスのパース
* xUnit v3 - ユニットテストフレームワーク

## 主要なコマンド

```bash
# プロジェクトをビルド (src/AIApiTracer 内で実行)
dotnet build 

# ソリューション全体でテストを実行 (リポジトリールートで実行)
dotnet test AIApiTracer.slnx

# テストを実行 (test/AIApiTracer.Test 内で実行)
dotnet test

# CSS ビルド  (src/AIApiTracer 内で実行)
npm run build-css:prod
```

## 開発ガイドライン

* UI やソースコード中のコメントは英語で記述してください
* アーキテクチャーや機能については docs 以下にドキュメントとして記述されているので適宜確認してください
  * architecture.md - アーキテクチャ
  * features.md - 実装されている機能
  * notes.md - 機能実装における注意点やメモ
* 構成を変更した後には必ず `dotnet build` でビルドできることを確認してください
  * `.razor` を編集した場合はプロジェクトのビルド、`.css` を編集した場合は CSS のビルドが必要です
  * `.razor` 内の class を変更した場合には Tailwind CSS のビルドも実行する必要があります
* 命名規則: .NET のフレームワークデザインガイドラインや .NET ランタイムを参考としてください
  * `var` は積極的使用してください
    * `UPPER_SNAKE_CASE` は使用しないください
* C# コーディング: 可能な限り新しい C# バージョンの言語機能を使用してください

## Git ワークフロー
* コミットメッセージは英語で記述してください

## 実装時における参考情報

* Fluent System Icons は `fonts/FluentSystemIcons-Resizable.css` で読み込まれます
  * アイコンの使用例: `<i class="icon-ic_fluent_copy_20_regular"></i>`
