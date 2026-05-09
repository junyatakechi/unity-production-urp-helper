# PlanarReflection 実装記録

## 概要

URP環境でキャラクターの像をステージ床に鏡面反射させるシステムの実装記録。
プラナーリフレクションの理論を用い、仮想カメラによるリアルタイムレンダリングと専用シェーダーで構成する。

---

## 環境

| 項目 | 内容 |
|------|------|
| Unity | 2022.3 |
| レンダリングパイプライン | URP |
| Cinemachine | 2.x |
| namespace | JayT.UnityProductionUrpHelper |

---

## 構成ファイル

| ファイル | 役割 |
|------|------|
| `PlanarReflectionRenderer.cs` | 仮想カメラの生成・更新・レンダリング |
| `PlanarReflectionFloor.shader` | 床の鏡面反射マテリアル用シェーダー |
| RenderTexture（手動作成） | 仮想カメラの出力先 |

---

## PlanarReflectionRenderer.cs

### 理論

```
MainCamera (Y=1)
      ↓
─────────── 床面 (floorY)
      ↓
ReflectionCamera Y = 2 * floorY - MainCamera Y
```

仮想カメラを床面に対してY軸対称な位置に配置し、反射した向きを向かせることで鏡面反射像を生成する。

### 設計方針

- RenderTextureはProjectウィンドウで手動作成してアタッチする
- 仮想カメラはAwakeではなくStartで生成する（Cinemachine初期化タイミング対策）
- `[DefaultExecutionOrder(1000)]`でCinemachineBrainのLateUpdate後に確実に実行する
- cullingMaskはLayerMaskで制御し、映したいオブジェクトのみを映す

### パラメーター

| パラメーター | 型 | 説明 |
|------|------|------|
| `floorObject` | Transform | 床オブジェクト。Y座標を自動取得 |
| `renderTexture` | RenderTexture | 出力先RenderTexture |
| `reflectionLayers` | LayerMask | 反射に映すLayerの指定 |
| `rotationXOffset` | float | 仮想カメラのX回転オフセット（演出調整用） |
| `fovMultiplier` | float | FOVの倍率（演出調整用） |
| `usePostProcessing` | bool | 仮想カメラのPost Processing有効化 |

### 仮想カメラの位置計算

```csharp
reflectPos.y = 2f * floorY - mainPos.y;
```

### 仮想カメラの回転計算

```csharp
Vector3 reflectedForward = Vector3.Reflect(_mainCamera.transform.forward, Vector3.up);
Quaternion reflectedRotation = Quaternion.LookRotation(reflectedForward, Vector3.up);
_reflectionCamera.transform.rotation = reflectedRotation * Quaternion.Euler(rotationXOffset, 0f, 0f);
```

`reflectedUp`を`Vector3.up`に固定することで左右反転を防ぐ。

### MainCameraとの同期パラメーター

```csharp
_reflectionCamera.fieldOfView  = _mainCamera.fieldOfView * fovMultiplier;
_reflectionCamera.nearClipPlane = _mainCamera.nearClipPlane;
_reflectionCamera.farClipPlane  = _mainCamera.farClipPlane;
_reflectionCamera.aspect        = _mainCamera.aspect;
```

### Cinemachineとの同期

CinemachineBrainはLateUpdateでカメラを更新する。
`[DefaultExecutionOrder(1000)]`を付与することで、CinemachineBrainのLateUpdate完了後に仮想カメラを更新する。

### 映像の反転について

反転処理はシェーダー側（`screenUV.y = 1.0 - screenUV.y`）で行う。
`PlanarReflectionRenderer`側では反転処理を行わない。

---

## 演出調整パラメーターについて

プラナーリフレクションは物理的にカメラが水平を向いている場合、床への入射角がなく反射像が成立しにくい。
映像作品としての表現のため、以下の調整パラメーターを設けている。

| パラメーター | 効果 |
|------|------|
| `rotationXOffset` | 仮想カメラのX回転をオフセットし、キャラクターを画角に収めやすくする |
| `fovMultiplier` | FOVを広げ、キャラクターを画角に収めやすくする |

---

## PlanarReflectionFloor.shader

### 処理フロー

```
床テクスチャ サンプリング
      ↓
ノーマルマップ適用（TBN変換）
      ↓
スクリーンスペースUVで反射テクスチャをサンプリング（ブラー付き）
      ↓
反射色にHDR輝度乗算（_ReflectionIntensity）
      ↓
フレネルで反射強度を計算
      ↓
lerp(床テクスチャ, 反射テクスチャ, フレネル)
      ↓
_BaseColorで最終明るさ調整
      ↓
フォグ適用
```

### プロパティ

| プロパティ | 型 | 説明 |
|------|------|------|
| `_MainTex` | Texture2D | 床テクスチャ |
| `_BaseColor` | Color | 最終出力の明るさ・色調整 |
| `_BumpMap` | Texture2D | ノーマルマップ |
| `_BumpScale` | Float | ノーマル強度 |
| `_ReflectionTex` | Texture2D | RenderTextureをアサイン |
| `_FresnelPower` | Float | フレネルの強さ。大きいほど真上から見たとき反射が弱くなる |
| `_ReflectionStrength` | Float | 反射の最大強度 |
| `_ReflectionIntensity` | Float | 反射色のHDR輝度倍率。Bloomを発火させるために使用 |
| `_BlurSize` | Float | ブラーの広がり |
| `_BlurSamples` | Int | ブラーのサンプリング数（4〜8） |

### フレネル計算

```hlsl
float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);
fresnel *= _ReflectionStrength;
```

### ブラー実装

シェーダー内でUVをオフセットしたボックスブラー近似。
`samples * samples`回サンプリングして平均を取る。

---

## RenderTexture設定

| 項目 | 設定値 |
|------|------|
| Dimension | 2D |
| Size | 1920 x 1080 |
| Color Format | R16G16B16A16_SFloat（HDR対応） |
| Depth Stencil Format | D32_SFLOAT_S8_UINT |

---

## 既知の制限

| 項目 | 内容 |
|------|------|
| 斜め床への非対応 | Y軸のみの対称計算のため、傾いた床には対応しない |
| オブリーク投影なし | 床の下のオブジェクトが映り込む可能性がある |
| ブラーの品質 | シェーダー内近似のため高品質ブラーには別途RenderPassが必要 |
| SSRとの併用なし | 近距離での反射像は演出パラメーターで調整が必要 |