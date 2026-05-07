1. ファイル配置
- PlanarReflectionCS.compute    ← Resources直下必須

1. URP Asset の設定
- Opaque Texture: ON
- Depth Texture:  ON

1. Universal Renderer の設定
- Renderer Features → Add → PlanarReflectionFeature

2. 床オブジェクトのマテリアル
- Shader: JayT/PlanarReflectionFloor を割り当て


# Memo
Main CameraのRenderer設定にRenderer Dataが反映されなかった原因は、Project Settings > Graphics では正しいURP Assetを設定していたものの、Project Settings > Quality 側に別のRender Pipeline Assetが設定されており、Quality設定側が優先されていたため。Quality側のRender Pipeline Assetを修正することで解決した。