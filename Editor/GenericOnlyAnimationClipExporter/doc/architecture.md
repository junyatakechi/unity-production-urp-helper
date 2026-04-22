# GenericOnlyAnimationClipExporter 実装メモ

## 背景

Unityの映像制作においてTimelineを使用している。
Timeline上に並べた複数の.animクリップを、別のTimelineで再利用したい。

Timelineの標準機能では以下ができない：
- 指定範囲のみを.animとして切り出す
- 複数クリップをマージして1つの.animとして出力する

そのためカスタムEditorスクリプトで対応する。

## 要件

- メニュー `Tools/JayT/ProductionUrpHelper/Export Range as AnimationClip` から実行できること
- 実行時にダイアログで開始・終了フレームをフレーム単位で入力できること
- 指定フレーム範囲のアニメーションを1つの.animファイルとして出力すること
- 出力先は `Assets/ExportedClips/` に固定（フォルダがなければ自動作成）
- ファイル名は `トラック名_開始f_終了f.anim` の形式
- 同名ファイルが存在する場合は自動で連番を付与すること
- 主にEditorでの操作で完結すること（スクリプト記述は最小限）

## 実装方針

### クラス構成

| クラス名 | 役割 |
|---|---|
| `GenericOnlyAnimationClipExporter` | `[MenuItem]` からの実行・出力処理本体 |
| `ExportRangeDialog` | フレーム範囲入力ウィンドウ（通常の EditorWindow。タブ統合・リサイズ可） |

### 処理フロー

1. `[MenuItem("Tools/JayT/ProductionUrpHelper/Export Range as AnimationClip")]` でメニュー登録
   - `PlayableDirector` が存在しない場合はグレーアウト（validate メソッドで制御）
2. 選択クリップの親トラック（AnimationTrack）を取得
3. `ExportRangeDialog` を開きフレーム範囲を入力
4. 指定範囲とトラック上の各クリップの重複区間を計算
5. `AnimationUtility.GetEditorCurve` で曲線を取得し時間オフセット付きでコピー
6. `AssetDatabase.CreateAsset` で.animとして保存

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