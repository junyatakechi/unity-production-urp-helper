# ISSUE: ダッチアングル時の反射像ズレ

**発生日**: 2026-05-09
**対象ファイル**: `PlanarReflectionRenderer.cs`
**ステータス**: 修正済み

---

## 症状

カメラの Z 軸回転（ダッチアングル）を使用したカットで、床の反射像の位置がズレる。
水平カメラ（Z 回転なし）では正常に動作していた。

---

## 原因

`UpdateReflectionCamera()` が `Quaternion.LookRotation` で反射カメラの向きを設定していたことが根本原因。

床面反射の正しい回転行列は以下の積になる：

```
R_reflect = M_reflect × R_main
```

ここで `M_reflect = diag(1, -1, 1)`（Y 軸反転）。この行列は **det = -1 の不適切な回転行列**（improper rotation）であり、`Quaternion` では表現できない。

`Quaternion.LookRotation` は常に det = +1 の正則な回転しか生成しないため、どの up ベクトルを渡しても正確な行列を再現することは不可能。

### ダッチアングルで顕在化する理由

水平カメラ（Z = 0）では Z 回転がないため、クロス項（X 成分が Y NDC に混入する項）がゼロになり誤差が見えにくかった。Z 回転が加わるとクロス項が発生し、サンプリング UV が系統的にずれる。

具体的には、`LookRotation` 近似では `V_reflect` の回転部分が：

```
R_main^T                              ← 近似（符号誤り）
[[cos θ,  sin θ, 0],
 [-sin θ, cos θ, 0],
 [0,      0,     1]]
```

正しくは：

```
R_main^T × diag(1,-1,1)              ← 正解（det = -1）
[[cos θ, -sin θ, 0],
 [-sin θ,-cos θ, 0],
 [0,      0,     1]]
```

sin θ の符号が column 1 で反転しており、ダッチアングルが大きいほど反射位置のズレが拡大する。

---

## 修正

`worldToCameraMatrix` と `projectionMatrix` を直接設定する方式に変更した。

```csharp
// 床面 y = floorY の反射行列  (x,y,z) -> (x, 2f-y, z)
var reflectionMatrix = Matrix4x4.identity;
reflectionMatrix.m11 = -1f;
reflectionMatrix.m13 = 2f * floorY;

// ビュー行列を直接合成: V_reflect = V_main * M_reflect
_reflectionCamera.worldToCameraMatrix =
    _mainCamera.worldToCameraMatrix * reflectionMatrix;

// プロジェクションの Y を反転（シェーダーの 1 - screenUV.y と対応）
var proj = _mainCamera.projectionMatrix;
proj.m11 = -proj.m11;
_reflectionCamera.projectionMatrix = proj;

// Unity のカリング計算用に位置も更新
_reflectionCamera.transform.position =
    reflectionMatrix.MultiplyPoint(_mainCamera.transform.position);
```

### なぜ GL.invertCulling が不要か

| 変換 | Y の符号変化 |
|------|------------|
| `V_main * M_reflect`（反射行列） | det に -1 を乗算 |
| `proj.m11 = -proj.m11`（Y 反転） | det に -1 を乗算 |
| 合計 | (-1) × (-1) = +1 → 元と同じ巻き順 |

2 つの Y 反転が打ち消し合うため、フェースカリングの逆転は起きない。

### シェーダー側との対応

`PlanarReflectionFloor.shader` は下記で Y を反転してサンプリングする：

```hlsl
float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
screenUV.y = 1.0 - screenUV.y;
```

この `1 - v` 補正が `proj.m11` の反転と 1 対 1 で対応している。

---

## 検証

| カメラ状態 | 修正前 | 修正後 |
|-----------|--------|--------|
| 水平（Z = 0°） | 正常 | 正常 |
| ダッチアングル（Z = 11°） | 反射像がズレる | 正常 |

Inspector 確認：反射カメラの `Rotation` が `(0, 0, 0)` と表示されるが、これは `worldToCameraMatrix` を直接設定した場合 `transform` が自動更新されない Unity の仕様によるもので、実際の描画は正しい行列を使用している。
