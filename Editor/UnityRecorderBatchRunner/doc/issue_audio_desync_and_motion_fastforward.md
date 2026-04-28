# Issue: 音声・映像のズレ、モーションの部分的な早送り
update: 2026-04-28

## 症状

- 書き出し動画の音声と映像が大きくズレる
- モーションが部分的に早送りになる（重いフレームがある箇所に集中する）

---

## 原因1: 音声・映像ズレ — CapFrameRate が無効のとき音声がDSP実時間で動く

`FrameRatePlayback.Constant` は `Time.captureFramerate` を設定し、ゲーム時間を `1/FPS` 固定で進める。  
しかし**音声はDSPクロック（実時間）**で動くため、`CapFrameRate = false` の場合に乖離が発生する。

```
CapFrameRate = false の場合（GPUが速いとき）:

  実時間 1秒で:
    映像 → 300フレーム描画 = 10秒分のゲーム時間を消費
    音声 → 1秒分のDSP出力

  結果: 10秒の映像に1秒の音声 → 深刻なズレ
```

### 解決策

`includeAudio = true` のときは `CapFrameRate` を必ず `true` にする。

```csharp
// BatchRecordingSession.StartRecording()
controllerSettings.CapFrameRate = settings.includeAudio || settings.capFPS;
```

---

## 原因2: モーション早送り — 全PlayableDirectorにPlay()を呼んでいた

`Object.FindObjectsByType<PlayableDirector>()` は全シーンの全ディレクターを返す。  
ControlTrack で制御されているサブタイムラインのディレクターにも `initialTime` を設定し `Play()` を呼ぶと、**親ControlTrackと競合**してサブタイムラインが独立再生され、モーションがずれる。

```
呼び出し前の想定:
  MasterDirector.Play()  ← 正しい
  SubDirector (ControlTrack が制御)  ← 触らない

実際の挙動（修正前）:
  MasterDirector.Play()
  SubDirector.Play(initialTime=マスターの開始時刻)  ← 競合 → 早送り
```

### 解決策

`initialTime` の設定と `Play()` の呼び出しを **timelineシーン内のディレクターのみ**に限定する。

```csharp
// UnityRecorderBatchRunner.RunNext()
string timelineSceneName = item.scene.timeline;
foreach (var director in Object.FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None))
{
    if (director.gameObject.scene.name != timelineSceneName) continue;
    director.initialTime = item.frameInterval.start / fps;
    director.playOnAwake = false;
}

// BatchRecordingSession.OnPlayModeStateChanged()
foreach (var director in Object.FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None))
{
    if (director.gameObject.scene.name != timelineSceneName) continue;
    director.Play();
}
```

---

## 原因3: モーション早送り — DSPClockモードが重いフレームで乖離する

`PlayableDirector.timeUpdateMode = DSPClock` はリアルタイム再生での音声同期用モード。  
`FrameRatePlayback.Constant` でゲーム時間を制御する場合、描画の遅いフレームでDSP時間がゲーム時間を追い抜き、アニメーションが先に進んで早送りに見える。

```
重いフレーム（例: 50ms/frame, 目標33ms/frame）の場合:

  ゲーム時間: +33ms（Recorder固定）
  DSP時間:   +50ms（実時間で進む）

  DSPClockモードのDirectorはDSP時間に追従
  → 録画フレームに対してアニメーションが17ms先に進む → 早送り
```

### 解決策

録画開始時に `timeUpdateMode` を強制的に `GameTime` に設定する。  
PlayMode終了でシーンが元に戻るため、永続的な変更にはならない。

```csharp
// BatchRecordingSession.OnPlayModeStateChanged()
director.timeUpdateMode = DirectorUpdateMode.GameTime;
director.Play();
```

---

## 付記: DSPサンプルレートの確認

音声録音時に非標準サンプルレート（44100/48000 Hz以外）が設定されている場合、出力音声の品質に影響する可能性がある。  
サンプルレートの変更は `Edit > Project Settings > Audio > System Sample Rate` で行う。  
（実行時の `AudioSettings.Reset()` は音声エンジン全体をリセットするため避ける）

---

## 関連ファイル

- `BatchRecordingSession.cs`
- `UnityRecorderBatchRunner.cs`
