# UnityRecorderBatchRunner — アーキテクチャ概要

Unity Recorderを使って複数の映像をバッチ処理で書き出すシステム。
JSONで定義されたレンダリングキューを1件ずつPlayModeで実行し、自動的に次のキューへ進む。

---

## ファイル構成

| ファイル | 役割 |
|---|---|
| `RenderQueueConfig.cs` | JSONデータモデルの定義 |
| `RenderQueueRunner.cs` | EditorWindow。キューの管理・起動・停止 |
| `BatchRecorderController.cs` | PlayMode内でRecorderを制御するMonoBehaviour |
| `PlayModeExitWatcher.cs` | PlayMode終了を検知して次のキューへ進める |
| `RenderQueueConfig.json` | レンダリングキューの設定ファイル（サンプル） |

---

## システム全体のフロー

```
[ユーザーが "Start Batch Render" を押す]
          │
          ▼
RenderQueueRunner.RunNext()          ← システム全体のエントリーポイント
  ├─ PlayerPrefsにIndex=0とConfigPathを保存
  ├─ 3つのシーンをロード (background / main / timeline)
  ├─ GameObjectを生成し BatchRecorderController をAddComponent
  ├─ controller.Setup(item, settings) でデータを渡す
  └─ EditorApplication.EnterPlaymode()
          │
          ▼ [PlayMode開始]
          │
BatchRecorderController.Start()     ← PlayMode内のエントリーポイント
  ├─ backgroundシーンをActive Sceneに設定（環境ライト有効化）
  └─ StartRecording() を呼ぶ
          │
          ▼
BatchRecorderController.Update()    ← 毎フレーム監視
  └─ IsRecording() が false になったら
       ├─ PlayerPrefsのIndexを +1
       └─ EditorApplication.ExitPlaymode()
          │
          ▼ [PlayMode終了]
          │
PlayModeExitWatcher.OnPlayModeStateChanged()
  └─ EnteredEditMode を検知 → RenderQueueRunner.RunNext()
          │
          ▼
  次のキューがあれば繰り返し、なければ完了
```

---

## BatchRecorderController の詳細

### 概要

`MonoBehaviour`を継承したクラス。PlayMode中に録画を担当する。

`RenderQueueRunner`が毎回 `new GameObject` → `AddComponent<BatchRecorderController>()` で生成・設定し、1回の録画が終わると破棄される**使い捨て設計**。

### なぜ MonoBehaviour か

- `Start()` / `Update()` などのUnityライフサイクルを使うため
- PlayMode中のCoroutineが使えるため（シーンのActive設定を1フレーム待つ必要がある）
- `[ExecuteAlways]`属性でEditMode中も動作可能

### なぜ EditorScriptか

Unity Recorderの `RecorderController` はEditorアセンブリに属するため、
`MonoBehaviour`であっても `Editor/` フォルダに配置する必要がある。

### データの受け渡し

GameObjectを生成したあと `Setup()` でデータを注入する。
PlayMode再起動をまたぐため、キューのインデックスは `PlayerPrefs` で永続化している。

```csharp
// RenderQueueRunner.cs 内
var controller = controllerGO.AddComponent<BatchRecorderController>();
controller.Setup(item, config.settings);  // ← PlayMode前にデータを渡す
```

### シーン構成

1本の映像は3つのシーンを組み合わせてレンダリングする。

| シーン | 役割 | OpenSceneMode |
|---|---|---|
| background（例: Skystage） | 環境ライト・スカイボックス。**Active Scene**に設定される | Single |
| main（例: Main） | メインコンテンツ | Additive |
| timeline（例: 022） | アニメーション・タイムライン。ファイル名にも使われる | Additive |

backgroundをActive Sceneにすることで、Unityの環境ライト設定が正しく機能する。

### Stop処理との連携

外部から `RenderQueueRunner.StopBatch()` が呼ばれると `JayT_ConfigPath` キーが削除される。
`Update()` 内でこのキーの有無をチェックし、存在しない場合はIndexを進めずにPlayModeを終了する。
これにより `PlayModeExitWatcher` が次のキューを起動しない。

```
StopBatch() 呼び出し
  ├─ PlayerPrefs の JayT_ConfigPath を削除
  └─ ExitPlaymode()
        │
        ▼
BatchRecorderController.Update()
  └─ HasKey("JayT_ConfigPath") == false → Indexを進めない → ExitPlaymode()
        │
        ▼
PlayModeExitWatcher
  └─ HasKey("JayT_ConfigPath") == false → RunNext() を呼ばない → 停止
```

---

## PlayerPrefs キー一覧

| キー | 型 | 用途 |
|---|---|---|
| `JayT_RenderIndex` | int | 現在処理中のキューインデックス |
| `JayT_ConfigPath` | string | JSONファイルのパス。このキーの有無でバッチ実行中かを判定する |

---

## RenderQueueConfig.json の構造

```json
{
  "settings": {
    "targetFPS": 60,
    "capFPS": false,
    "resolution": "4K"
  },
  "renderingList": [
    {
      "renderingId": "022",
      "scene": {
        "background": "Skystage",
        "main": "Main",
        "timeline": "022"
      },
      "frameInterval": {
        "start": 600,
        "end": 1800
      }
    }
  ]
}
```

### resolution に指定できる値

| 値 | 解像度 |
|---|---|
| `"4K"` | 3840 × 2160 |
| `"2K"` | 2560 × 1440 |
| `"1080p"` | 1920 × 1080 |
| `"720p"` | 1280 × 720 |
