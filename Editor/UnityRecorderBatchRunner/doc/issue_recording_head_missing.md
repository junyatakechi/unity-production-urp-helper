# Issue: 録画の冒頭が欠ける・早送りになる

## 症状

- 書き出し動画の冒頭約50フレームが欠落する
- 冒頭部分の映像が早送りになる

## 原因

`PlayModeStateChange.EnteredPlayMode` イベントは、PlayMode開始後にシーンの初期化（Awake/Start/Update）が数十フレーム走ってから発火する。

`PlayableDirector.playOnAwake = true`（デフォルト）のままだと、`EnteredPlayMode`発火までの間にTimelineが`initialTime`から自動再生で進んでしまい、録画開始時点でズレが生じる。

```
PlayMode開始
  └─ Awake() → Update() → ... (≈50フレーム分動く)
      └─ EnteredPlayMode 発火 ← ここで録画開始 → 冒頭50フレーム欠落
```

## 解決策

PlayMode入場前（`RunNext()`でシーンを開いた直後）に`playOnAwake = false`を設定し、directorを静止させる。
`EnteredPlayMode`でRecorder開始後に明示的に`director.Play()`を呼ぶ。

```csharp
// UnityRecorderBatchRunner.RunNext() — EnterPlaymode()の前
director.initialTime = item.frameInterval.start / fps;
director.playOnAwake = false; // EnteredPlayMode発火まで静止させる

// BatchRecordingSession.OnPlayModeStateChanged(EnteredPlayMode)
StartRecording(item, config.settings);
foreach (var director in Object.FindObjectsByType<PlayableDirector>(...))
    director.Play();
```

PlayMode終了時にUnityがシーンの状態を自動的に元に戻すため、`playOnAwake`の変更はシーンアセットに永続しない。

## 関連ファイル

- `BatchRecordingSession.cs`
- `UnityRecorderBatchRunner.cs`
