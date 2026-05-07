using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace JayT.UnityProductionUrpHelper
{
    public class PlanarReflectionPass : ScriptableRenderPass
    {
        // ---- Shader Property IDs ----
        static readonly int _ColorRT_ID        = Shader.PropertyToID("_PlanarReflection_ColorRT");
        static readonly int _HashRT_ID         = Shader.PropertyToID("_PlanarReflection_HashRT");
        static readonly int _RTSize_ID         = Shader.PropertyToID("_RTSize");
        static readonly int _PlaneHeightWS_ID  = Shader.PropertyToID("_PlaneHeightWS");
        static readonly int _FadeVertical_ID   = Shader.PropertyToID("_FadeOutVertical");
        static readonly int _FadeHorizontal_ID = Shader.PropertyToID("_FadeOutHorizontal");
        static readonly int _TintColor_ID      = Shader.PropertyToID("_TintColor");
        static readonly int _VPMatrix_ID       = Shader.PropertyToID("_VPMatrix");
        static readonly int _CameraDir_ID      = Shader.PropertyToID("_CameraDirection");

        // Compute Shader Kernel 名
        const string KERNEL_CLEAR   = "Clear";
        const string KERNEL_HASH    = "RenderHashRT";
        const string KERNEL_RESOLVE = "ResolveColorRT";
        const string KERNEL_FILL    = "FillHoles";

        const int THREAD_X = 8;
        const int THREAD_Y = 8;

        // ---- Fields ----
        readonly PlanarReflectionFeature.Settings _settings;
        ComputeShader _cs;

        int _kernelClear;
        int _kernelHash;
        int _kernelResolve;
        int _kernelFill;

        RenderTargetIdentifier _colorRTI;
        RenderTargetIdentifier _hashRTI;

        // ---- Constructor ----
        public PlanarReflectionPass(PlanarReflectionFeature.Settings settings)
        {
            _settings = settings;
            _cs = Resources.Load<ComputeShader>("PlanarReflectionCS");
        }

        // ---- RT サイズ計算 ----
        int GetRTHeight() =>
            Mathf.CeilToInt(_settings.RTHeight / (float)THREAD_Y) * THREAD_Y;

        int GetRTWidth()
        {
            float aspect = (float)Screen.width / Screen.height;
            return Mathf.CeilToInt(GetRTHeight() * aspect / (float)THREAD_X) * THREAD_X;
        }

        // ---- Configure: 一時 RT を確保 ----
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (_cs == null)
            {
                Debug.LogError("[PlanarReflectionPass] ComputeShader 'PlanarReflectionCS' not found in Resources.");
                return;
            }

            _kernelClear   = _cs.FindKernel(KERNEL_CLEAR);
            _kernelHash    = _cs.FindKernel(KERNEL_HASH);
            _kernelResolve = _cs.FindKernel(KERNEL_RESOLVE);
            _kernelFill    = _cs.FindKernel(KERNEL_FILL);

            int w = GetRTWidth();
            int h = GetRTHeight();

            // Color RT: ARGBHalf (HDR) or ARGB32
            var colorDesc = new RenderTextureDescriptor(w, h, _settings.UseHDR ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32, 0, 0);
            colorDesc.sRGB = false;
            colorDesc.enableRandomWrite = true;
            cmd.GetTemporaryRT(_ColorRT_ID, colorDesc);
            _colorRTI = new RenderTargetIdentifier(_ColorRT_ID);

            // Hash RT: RInt (PC専用, InterlockedMin に使う)
            var hashDesc = new RenderTextureDescriptor(w, h, RenderTextureFormat.RInt, 0, 0);
            hashDesc.sRGB = false;
            hashDesc.enableRandomWrite = true;
            cmd.GetTemporaryRT(_HashRT_ID, hashDesc);
            _hashRTI = new RenderTargetIdentifier(_HashRT_ID);
        }

        // ---- Execute: Compute Dispatch ----
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_cs == null) return;

            var cmd = CommandBufferPool.Get("PlanarReflection");

            int w = GetRTWidth();
            int h = GetRTHeight();
            int groupX = w / THREAD_X;
            int groupY = h / THREAD_Y;

            Camera cam = renderingData.cameraData.camera;
            Matrix4x4 vp = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true) * cam.worldToCameraMatrix;

            // ---- 共通パラメータ ----
            cmd.SetComputeVectorParam(_cs,  _RTSize_ID,         new Vector2(w, h));
            cmd.SetComputeFloatParam(_cs,   _PlaneHeightWS_ID,  _settings.PlaneHeightWS);
            cmd.SetComputeFloatParam(_cs,   _FadeVertical_ID,   _settings.FadeOutVertical);
            cmd.SetComputeFloatParam(_cs,   _FadeHorizontal_ID, _settings.FadeOutHorizontal);
            cmd.SetComputeVectorParam(_cs,  _TintColor_ID,      _settings.TintColor);
            cmd.SetComputeVectorParam(_cs,  _CameraDir_ID,      cam.transform.forward);
            cmd.SetComputeMatrixParam(_cs,  _VPMatrix_ID,       vp);

            // ---- Step1: Clear ----
            cmd.SetComputeTextureParam(_cs, _kernelClear, "ColorRT", _colorRTI);
            cmd.SetComputeTextureParam(_cs, _kernelClear, "HashRT",  _hashRTI);
            cmd.DispatchCompute(_cs, _kernelClear, groupX, groupY, 1);

            // ---- Step2: 深度から反射先スクリーン位置をハッシュに書き込む ----
            cmd.SetComputeTextureParam(_cs, _kernelHash, "HashRT",             _hashRTI);
            cmd.SetComputeTextureParam(_cs, _kernelHash, "_CameraDepthTexture", new RenderTargetIdentifier("_CameraDepthTexture"));
            cmd.DispatchCompute(_cs, _kernelHash, groupX, groupY, 1);

            // ---- Step3: ハッシュを解決して ColorRT に色を書き込む ----
            cmd.SetComputeTextureParam(_cs, _kernelResolve, "ColorRT",              _colorRTI);
            cmd.SetComputeTextureParam(_cs, _kernelResolve, "HashRT",               _hashRTI);
            cmd.SetComputeTextureParam(_cs, _kernelResolve, "_CameraOpaqueTexture", new RenderTargetIdentifier("_CameraOpaqueTexture"));
            cmd.DispatchCompute(_cs, _kernelResolve, groupX, groupY, 1);

            // ---- Step4: 穴埋め ----
            cmd.SetComputeTextureParam(_cs, _kernelFill, "ColorRT", _colorRTI);
            cmd.SetComputeTextureParam(_cs, _kernelFill, "HashRT",  _hashRTI);
            cmd.DispatchCompute(_cs, _kernelFill,
                Mathf.CeilToInt(groupX / 2f),
                Mathf.CeilToInt(groupY / 2f), 1);

            // ---- グローバルテクスチャとしてシェーダーに渡す ----
            cmd.SetGlobalTexture(_ColorRT_ID, _colorRTI);
            cmd.EnableShaderKeyword("_PLANAR_REFLECTION_ON");

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // ---- Cleanup ----
        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_ColorRT_ID);
            cmd.ReleaseTemporaryRT(_HashRT_ID);
            cmd.DisableShaderKeyword("_PLANAR_REFLECTION_ON");
        }
    }
}
