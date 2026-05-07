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
        static readonly int _InvVPMatrix_ID    = Shader.PropertyToID("_InvVPMatrix");
        static readonly int _CameraDir_ID      = Shader.PropertyToID("_CameraDirection");

        const string KERNEL_CLEAR   = "Clear";
        const string KERNEL_HASH    = "RenderHashRT";
        const string KERNEL_RESOLVE = "ResolveColorRT";
        const string KERNEL_FILL    = "FillHoles";

        const int THREAD_X = 8;
        const int THREAD_Y = 8;

        readonly PlanarReflectionFeature.Settings _settings;
        ComputeShader _cs;

        int _kernelClear;
        int _kernelHash;
        int _kernelResolve;
        int _kernelFill;

        RenderTexture _colorRT;
        RenderTexture _hashRT;
        int _currentWidth;
        int _currentHeight;

        public PlanarReflectionPass(PlanarReflectionFeature.Settings settings)
        {
            _settings = settings;
            _cs = Resources.Load<ComputeShader>("PlanarReflectionCS");
            if (_cs == null)
                Debug.LogError("[PlanarReflectionPass] ComputeShader 'PlanarReflectionCS' not found in Resources.");

            ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Color);
        }

        int GetRTHeight() =>
            Mathf.CeilToInt(_settings.RTHeight / (float)THREAD_Y) * THREAD_Y;

        int GetRTWidth()
        {
            float aspect = (float)Screen.width / Screen.height;
            return Mathf.CeilToInt(GetRTHeight() * aspect / (float)THREAD_X) * THREAD_X;
        }

        void EnsureRTs(int w, int h)
        {
            if (_colorRT != null && _currentWidth == w && _currentHeight == h)
                return;

            CleanupRTs();

            _colorRT = new RenderTexture(w, h, 0,
                _settings.UseHDR ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32);
            _colorRT.enableRandomWrite = true;
            _colorRT.filterMode = FilterMode.Bilinear;
            _colorRT.name = "_PlanarReflection_ColorRT";
            _colorRT.Create();

            _hashRT = new RenderTexture(w, h, 0, RenderTextureFormat.RInt);
            _hashRT.enableRandomWrite = true;
            _hashRT.name = "_PlanarReflection_HashRT";
            _hashRT.Create();

            _currentWidth  = w;
            _currentHeight = h;

            Shader.SetGlobalTexture(_ColorRT_ID, _colorRT);
        }

        public void CleanupRTs()
        {
            if (_colorRT != null) { _colorRT.Release(); _colorRT = null; }
            if (_hashRT  != null) { _hashRT.Release();  _hashRT  = null; }
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (_cs == null) return;

            _kernelClear   = _cs.FindKernel(KERNEL_CLEAR);
            _kernelHash    = _cs.FindKernel(KERNEL_HASH);
            _kernelResolve = _cs.FindKernel(KERNEL_RESOLVE);
            _kernelFill    = _cs.FindKernel(KERNEL_FILL);

            EnsureRTs(GetRTWidth(), GetRTHeight());
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_cs == null || _colorRT == null) return;

            int w = _currentWidth;
            int h = _currentHeight;
            int groupX = w / THREAD_X;
            int groupY = h / THREAD_Y;

            Camera cam = renderingData.cameraData.camera;
            Matrix4x4 vp    = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true) * cam.worldToCameraMatrix;
            Matrix4x4 invVP = vp.inverse;

            var cmd = CommandBufferPool.Get("PlanarReflection");

            cmd.SetComputeVectorParam(_cs,  _RTSize_ID,         new Vector2(w, h));
            cmd.SetComputeFloatParam(_cs,   _PlaneHeightWS_ID,  _settings.PlaneHeightWS);
            cmd.SetComputeFloatParam(_cs,   _FadeVertical_ID,   _settings.FadeOutVertical);
            cmd.SetComputeFloatParam(_cs,   _FadeHorizontal_ID, _settings.FadeOutHorizontal);
            cmd.SetComputeVectorParam(_cs,  _TintColor_ID,      _settings.TintColor);
            cmd.SetComputeVectorParam(_cs,  _CameraDir_ID,      cam.transform.forward);
            cmd.SetComputeMatrixParam(_cs,  _VPMatrix_ID,       vp);
            cmd.SetComputeMatrixParam(_cs,  _InvVPMatrix_ID,    invVP);

            var depthTex  = Shader.GetGlobalTexture("_CameraDepthTexture");
            var opaqueTex = Shader.GetGlobalTexture("_CameraOpaqueTexture");
            if (depthTex == null || opaqueTex == null) return;

            // Step1: Clear
            cmd.SetComputeTextureParam(_cs, _kernelClear, "ColorRT", _colorRT);
            cmd.SetComputeTextureParam(_cs, _kernelClear, "HashRT",  _hashRT);
            cmd.DispatchCompute(_cs, _kernelClear, groupX, groupY, 1);

            // Step2: 深度→反射ハッシュ
            cmd.SetComputeTextureParam(_cs, _kernelHash, "ColorRT",              _colorRT);
            cmd.SetComputeTextureParam(_cs, _kernelHash, "HashRT",               _hashRT);
            cmd.SetComputeTextureParam(_cs, _kernelHash, "_CameraDepthTexture",  depthTex);
            cmd.DispatchCompute(_cs, _kernelHash, groupX, groupY, 1);

            // Step3: ハッシュ→カラー
            cmd.SetComputeTextureParam(_cs, _kernelResolve, "ColorRT",              _colorRT);
            cmd.SetComputeTextureParam(_cs, _kernelResolve, "HashRT",               _hashRT);
            cmd.SetComputeTextureParam(_cs, _kernelResolve, "_CameraOpaqueTexture", opaqueTex);
            cmd.DispatchCompute(_cs, _kernelResolve, groupX, groupY, 1);

            // Step4: 穴埋め
            cmd.SetComputeTextureParam(_cs, _kernelFill, "ColorRT", _colorRT);
            cmd.SetComputeTextureParam(_cs, _kernelFill, "HashRT",  _hashRT);
            cmd.DispatchCompute(_cs, _kernelFill,
                Mathf.CeilToInt(groupX / 2f),
                Mathf.CeilToInt(groupY / 2f), 1);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            // 次フレームに向けてグローバルテクスチャを更新（CPU側即時反映）
            Shader.SetGlobalTexture(_ColorRT_ID, _colorRT);
        }

        public override void FrameCleanup(CommandBuffer cmd) { }
    }
}
