using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace JayT.UnityProductionUrpHelper
{
    public class PlanarReflectionFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            [Header("Reflection Plane")]
            public float PlaneHeightWS = 0.01f;

            [Header("Render Texture")]
            [Range(128, 2048)]
            public int RTHeight = 512;
            public bool UseHDR = true;

            [Header("Fade")]
            [Range(0.01f, 1f)]
            public float FadeOutVertical = 0.25f;
            [Range(0.01f, 1f)]
            public float FadeOutHorizontal = 0.35f;

            [Header("Tint")]
            [ColorUsage(true, true)]
            public Color TintColor = Color.white;
        }

        public Settings PassSettings = new Settings();

        PlanarReflectionPass _pass;

        public override void Create()
        {
            _pass = new PlanarReflectionPass(PassSettings);
            // OpaqueTexture コピー後・透明パス前に実行
            _pass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // SceneView では反射を省略
            if (renderingData.cameraData.cameraType == CameraType.Preview)
                return;

            renderer.EnqueuePass(_pass);
        }
    }
}
