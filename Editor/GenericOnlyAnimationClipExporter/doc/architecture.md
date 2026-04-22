# GenericOnlyAnimationClipExporter 実装メモ

## 背景

Unityの映像制作においてTimelineを使用している。
Timeline上に並べた複数の.animクリップを、別のTimelineで再利用したい。

Timelineの標準機能では以下ができない：
- 指定範囲のみを.animとして切り出す
- 複数クリップをマージして1つの.animとして出力する

そのためカスタムEditorスクリプトで対応する。

## 要件

- メニュー `Tools/JayT/ProductionUrpHelper/Export Range as AnimationClip` からいつでもウィンドウを開けること
- ウィンドウ内で開始・終了フレームをフレーム単位で入力できること
- Timeline上の AnimationTrack を選択した状態で Export ボタンが有効になること（クリップの有無は問わない）
- 指定フレーム範囲のアニメーションを1つの.animファイルとして出力すること
- 出力先は `Assets/ExportedClips/` に固定（フォルダがなければ自動作成）
- ファイル名は `トラック名_開始f_終了f.anim` の形式
- 同名ファイルが存在する場合は自動で連番を付与すること

## 実装方針

### クラス構成

| クラス名 | 役割 |
|---|---|
| `AnimationClipExporterSelectionCache` | Timeline の選択状態をキャッシュする `[InitializeOnLoad]` クラス |
| `GenericOnlyAnimationClipExporter` | メニュー登録・出力処理本体 |
| `ExportRangeDialog` | フレーム範囲入力ウィンドウ（通常の EditorWindow。タブ統合・リサイズ可） |

### 処理フロー

1. `[MenuItem]` でメニューを登録。validate なし（常に開ける）
2. ウィンドウを開く（`GetWindow` で既存ウィンドウを再利用）
3. ウィンドウは `AnimationClipExporterSelectionCache` から現在の状態を読み取り表示
   - Timeline を開いているか
   - AnimationTrack を選択中か・選択中のトラック名
4. Export ボタンは「Timeline が開いている」かつ「AnimationTrack を選択中」のときのみ有効
5. Export 実行：指定範囲とトラック上の各クリップの重複区間を計算し曲線をコピー
6. `AssetDatabase.CreateAsset` で.animとして保存

### 選択キャッシュの仕組み

`EditorApplication.update` で `Selection.objects` をポーリングし、`AnimationTrack` のみを抽出してキャッシュする。
これにより、メニューを開いた際に Timeline ウィンドウがフォーカスを失っても選択状態が保持される。

フレームレートは `TimelineEditor.inspectedAsset.editorSettings.frameRate` から取得する。

選択が変化したタイミングで開いている `ExportRangeDialog` を `Repaint()` して表示を即時更新する。

### フレーム範囲とソースclipの時間変換

```
rangeStart/End    : Timeline上の指定範囲（秒）
clipTimelineStart : Timeline上のクリップ開始時間（秒）
clipIn            : ソースclip内の読み取り開始時間（Trim In）

sourceStart = clipIn + (rangeStart - clipTimelineStart)
sourceEnd   = clipIn + (rangeEnd   - clipTimelineStart)
timeOffset  = rangeStart - startTime  // 出力clipの先頭を0に正規化
```

### 対応曲線の種類

| 種類 | 対応 |
|---|---|
| Float曲線（Transform・プロパティ） | ✅ |
| ObjectReference曲線（Sprite等） | ✅ |
| Humanoid Muscle曲線 | ❌ |

### Humanoidの制限

`AnimationUtility.GetCurveBindings` はHumanoidのMuscle曲線を返さない。
これはUnity公式APIの制約であり回避不可。
クラス名 `GenericOnly〜` にその制限を明示している。

### 名前空間

```csharp
namespace JayT.UnityProductionUrpHelper
```
